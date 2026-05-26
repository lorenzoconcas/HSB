using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using HSB.Constants;
using HSB.Constants.WebSocket;

namespace HSB.Components.WebSockets;

internal sealed class WebSocket(
    Request req,
    Response res,
    Configuration c,
    WebSocketConnection connection)
{
    private const ushort CloseNormalClosure = 1000;
    private const ushort CloseProtocolError = 1002;
    private const ushort CloseMessageTooBig = 1009;
    private const ushort CloseInternalServerError = 1011;

    private readonly Socket socket = req.GetSocket();
    private readonly SemaphoreSlim writeLock = new(1, 1);
    private readonly WebSocketOptions options = c.WebSocketOptions;
    private int state = (int)WebSocketState.CLOSED;
    private int closeDispatched;
    private int errorDispatched;

    private static string DigestKey(string key)
    {
        return Convert.ToBase64String(
            SHA1.HashData(
                Encoding.UTF8.GetBytes(key + WebSocketsContants.WS_GUID)
            ));
    }

    internal async Task ProcessAsync(Func<Task> configureConnectionAsync)
    {
        if (!Accept())
        {
            return;
        }

        try
        {
            await configureConnectionAsync();
            await connection.DispatchOpenAsync();
            await MessageLoopAsync();
        }
        catch (Exception e)
        {
            await HandleFailureAsync(e, CloseInternalServerError);
        }
        finally
        {
            MarkClosed();
            CloseSocket();
            await DispatchCloseOnceAsync();
        }
    }

    internal Task SendFrameAsync(byte[] payload, Opcode opcode)
    {
        var frame = new Frame(opcode: opcode);
        frame.SetPayload(payload);
        return SendFrameAsync(frame);
    }

    internal Task CloseAsync()
    {
        return CloseAsync(CloseNormalClosure);
    }

    internal async Task FailAsync(Exception exception)
    {
        await HandleFailureAsync(exception, CloseInternalServerError);
    }

    private bool Accept()
    {
        if (!req.IsWebSocket())
        {
            c.Debug.WARNING("Not a websocket request, this code should never be reached");
            res.SendCode(HttpCodes.BAD_REQUEST);
            return false;
        }

        var headers = req.Headers;
        if (!headers.ContainsKey("Sec-WebSocket-Key") || !headers.ContainsKey("Sec-WebSocket-Version"))
        {
            c.Debug.WARNING("Missing Sec-WebSocket-Key or Sec-WebSocket-Version, malformed request");
            res.SendCode(HttpCodes.BAD_REQUEST);
            return false;
        }

        if (!headers["Sec-WebSocket-Version"].Equals("13", StringComparison.Ordinal))
        {
            c.Debug.WARNING("Unsupported websocket version");
            res.SendCode(HttpCodes.UPGRADE_REQUIRED);
            return false;
        }

        if (!TryValidateKey(headers["Sec-WebSocket-Key"]))
        {
            c.Debug.WARNING("Malformed Sec-WebSocket-Key");
            res.SendCode(HttpCodes.BAD_REQUEST);
            return false;
        }

        if (options.ValidateOriginWithCors &&
            headers.TryGetValue("Origin", out var origin) &&
            c.GlobalCors != null &&
            !c.GlobalCors.IsOriginAllowed(origin))
        {
            c.Debug.WARNING($"Rejected websocket origin '{origin}'");
            res.SendCode(HttpCodes.FORBIDDEN);
            return false;
        }

        res.SetReadTimeout(options.ReceivePollTimeoutMilliseconds);
        res.SetWriteTimeout(options.IdleTimeoutMilliseconds);

        SetState(WebSocketState.CONNECTING);

        var key = DigestKey(headers["Sec-WebSocket-Key"]);
        List<string> response =
        [
            "HTTP/1.1 101 Switching Protocols\r\n",
            "Upgrade: websocket\r\n",
            "Connection: Upgrade\r\n",
            "Sec-WebSocket-Accept: " + key + "\r\n",
        ];

        if (headers.TryGetValue("Sec-WebSocket-Protocol", out var protocolHeader))
        {
            var protocol = protocolHeader
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(protocol))
            {
                response.Add("Sec-WebSocket-Protocol: " + protocol + "\r\n");
            }
        }

        response.Add("\r\n");

        res.SendOrThrow(Encoding.UTF8.GetBytes(string.Join("", response)), false);

        SetState(WebSocketState.OPEN);
        connection.MarkOpen();
        return true;
    }

    private async Task MessageLoopAsync()
    {
        var buffer = new byte[options.ReceiveChunkSize];
        var pending = new byte[options.ReceiveChunkSize * 2];
        var pendingCount = 0;
        var fragmentedPayload = new List<byte>(options.ReceiveChunkSize * 2);
        Opcode? fragmentedOpcode = null;
        var lastReceiveAt = DateTime.UtcNow;

        while (GetState() == WebSocketState.OPEN)
        {
            int received;
            try
            {
                received = res.Read(buffer, 0, buffer.Length);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                if (DateTime.UtcNow - lastReceiveAt >= TimeSpan.FromMilliseconds(options.IdleTimeoutMilliseconds))
                {
                    await CloseAsync(CloseNormalClosure);
                    return;
                }

                continue;
            }
            catch (IOException ex) when (ex.InnerException is SocketException sockEx &&
                                         sockEx.SocketErrorCode == SocketError.TimedOut)
            {
                if (DateTime.UtcNow - lastReceiveAt >= TimeSpan.FromMilliseconds(options.IdleTimeoutMilliseconds))
                {
                    await CloseAsync(CloseNormalClosure);
                    return;
                }

                continue;
            }
            catch (Exception ex) when (IsExpectedDisconnect(ex))
            {
                await HandleFailureAsync(ex, CloseNormalClosure, dispatchError: false);
                return;
            }

            if (received <= 0)
            {
                await CloseAsync(CloseNormalClosure, sendCloseFrame: false);
                return;
            }

            lastReceiveAt = DateTime.UtcNow;
            pending = EnsurePendingCapacity(pending, pendingCount + received);
            Buffer.BlockCopy(buffer, 0, pending, pendingCount, received);
            pendingCount += received;

            while (pendingCount > 0)
            {
                if (!Frame.TryRead(pending.AsSpan(0, pendingCount), options.MaxFramePayloadBytes, out var frame, out var consumed, out var error))
                {
                    if (error != null)
                    {
                        await CloseAsync(
                            error.Contains("limit", StringComparison.OrdinalIgnoreCase)
                                ? CloseMessageTooBig
                                : CloseProtocolError);
                        return;
                    }

                    break;
                }

                pendingCount = CompactPendingBuffer(pending, pendingCount, consumed);

                if (frame == null)
                {
                    continue;
                }

                if (!frame.GetMask())
                {
                    await CloseAsync(CloseProtocolError);
                    return;
                }

                switch (frame.GetOpcode())
                {
                    case Opcode.CLOSE:
                        await CloseAsync(CloseNormalClosure);
                        return;
                    case Opcode.PING:
                        await SendControlFrameAsync(Opcode.PONG, frame.GetPayload());
                        break;
                    case Opcode.PONG:
                        break;
                    case Opcode.TEXT:
                    case Opcode.BINARY:
                    {
                        if (fragmentedOpcode != null)
                        {
                            await CloseAsync(CloseProtocolError);
                            return;
                        }

                        if (frame.GetFIN())
                        {
                            if (!await DispatchMessageAsync(frame.GetOpcode(), frame.GetPayload()))
                            {
                                return;
                            }

                            break;
                        }

                        fragmentedOpcode = frame.GetOpcode();
                        fragmentedPayload.Clear();
                        fragmentedPayload.AddRange(frame.GetPayload());
                        if (fragmentedPayload.Count > options.MaxMessagePayloadBytes)
                        {
                            await CloseAsync(CloseMessageTooBig);
                            return;
                        }

                        break;
                    }
                    case Opcode.CONTINUATION:
                    {
                        if (fragmentedOpcode == null)
                        {
                            await CloseAsync(CloseProtocolError);
                            return;
                        }

                        fragmentedPayload.AddRange(frame.GetPayload());
                        if (fragmentedPayload.Count > options.MaxMessagePayloadBytes)
                        {
                            await CloseAsync(CloseMessageTooBig);
                            return;
                        }

                        if (!frame.GetFIN())
                        {
                            break;
                        }

                        var completedOpcode = fragmentedOpcode.Value;
                        var payload = fragmentedPayload.ToArray();
                        fragmentedOpcode = null;
                        fragmentedPayload.Clear();

                        if (!await DispatchMessageAsync(completedOpcode, payload))
                        {
                            return;
                        }

                        break;
                    }
                    default:
                        await CloseAsync(CloseProtocolError);
                        return;
                }

                frame.Dispose();
            }
        }
    }

    private static byte[] EnsurePendingCapacity(byte[] pending, int requiredCapacity)
    {
        if (pending.Length >= requiredCapacity)
        {
            return pending;
        }

        var newCapacity = pending.Length;
        while (newCapacity < requiredCapacity)
        {
            newCapacity *= 2;
        }

        Array.Resize(ref pending, newCapacity);
        return pending;
    }

    private static int CompactPendingBuffer(byte[] pending, int pendingCount, int consumed)
    {
        if (consumed <= 0)
        {
            return pendingCount;
        }

        var remaining = pendingCount - consumed;
        if (remaining > 0)
        {
            Buffer.BlockCopy(pending, consumed, pending, 0, remaining);
        }

        return remaining;
    }

    private async Task<bool> DispatchMessageAsync(Opcode opcode, byte[] payload)
    {
        try
        {
            await connection.DispatchMessageAsync(new WebSocketMessage(payload, opcode == Opcode.TEXT));
            return true;
        }
        catch (Exception e)
        {
            await HandleFailureAsync(e, CloseInternalServerError);
            return false;
        }
    }

    private async Task SendFrameAsync(Frame frame)
    {
        await writeLock.WaitAsync();
        try
        {
            if (GetState() is WebSocketState.CLOSED or WebSocketState.CLOSING)
            {
                throw new InvalidOperationException("WebSocket is not connected");
            }

            res.SendOrThrow(frame.Build(), false);
        }
        catch (Exception ex) when (IsExpectedDisconnect(ex))
        {
            await HandleFailureAsync(ex, CloseNormalClosure, dispatchError: false);
            throw;
        }
        finally
        {
            writeLock.Release();
            frame.Dispose();
        }
    }

    private Task SendControlFrameAsync(Opcode opcode, byte[] payload)
    {
        var frame = new Frame(opcode: opcode);
        frame.SetPayload(payload);
        return SendFrameAsync(frame);
    }

    private async Task CloseAsync(ushort closeCode, bool sendCloseFrame = true)
    {
        var previousState = TransitionToClosing();
        if (previousState == WebSocketState.CLOSED)
        {
            return;
        }

        connection.MarkClosed();

        if (sendCloseFrame && previousState == WebSocketState.OPEN)
        {
            var frame = new Frame(opcode: Opcode.CLOSE);
            frame.SetPayload(BuildClosePayload(closeCode));

            try
            {
                await writeLock.WaitAsync();
                try
                {
                    if (previousState == WebSocketState.OPEN)
                    {
                        res.SendOrThrow(frame.Build(), false);
                    }
                }
                finally
                {
                    writeLock.Release();
                }
            }
            catch (Exception ex) when (IsExpectedDisconnect(ex))
            {
                if (!options.SuppressExpectedDisconnectErrors)
                {
                    c.Debug.DEBUG($"WebSocket close write failed: {ex.Message}");
                }
            }
            finally
            {
                frame.Dispose();
            }
        }

        MarkClosed();
    }

    private static byte[] BuildClosePayload(ushort closeCode)
    {
        var payload = new byte[2];
        payload[0] = (byte)(closeCode >> 8);
        payload[1] = (byte)(closeCode & 0xFF);
        return payload;
    }

    private async Task HandleFailureAsync(Exception exception, ushort closeCode, bool dispatchError = true)
    {
        if (dispatchError &&
            !(options.SuppressExpectedDisconnectErrors && IsExpectedDisconnect(exception)) &&
            Interlocked.Exchange(ref errorDispatched, 1) == 0)
        {
            await DispatchErrorAsync(exception);
        }

        await CloseAsync(closeCode, sendCloseFrame: closeCode != CloseNormalClosure || !IsExpectedDisconnect(exception));
    }

    private void CloseSocket()
    {
        try
        {
            socket.Shutdown(SocketShutdown.Both);
        }
        catch
        {
            // ignored
        }

        try
        {
            socket.Close();
        }
        catch
        {
            // ignored
        }
    }

    private async Task DispatchErrorAsync(Exception exception)
    {
        try
        {
            await connection.DispatchErrorAsync(exception);
        }
        catch (Exception e)
        {
            c.Debug.ERROR($"WebSocket error handler failed: {e}");
        }
    }

    private async Task DispatchCloseOnceAsync()
    {
        if (Interlocked.Exchange(ref closeDispatched, 1) == 1)
        {
            return;
        }

        try
        {
            await connection.DispatchCloseAsync();
        }
        catch (Exception e)
        {
            await DispatchErrorAsync(e);
        }
    }

    private WebSocketState GetState()
    {
        return (WebSocketState)Volatile.Read(ref state);
    }

    private void SetState(WebSocketState newState)
    {
        Volatile.Write(ref state, (int)newState);
    }

    private WebSocketState TransitionToClosing()
    {
        while (true)
        {
            var current = GetState();
            if (current is WebSocketState.CLOSED or WebSocketState.CLOSING)
            {
                return current;
            }

            if (Interlocked.CompareExchange(ref state, (int)WebSocketState.CLOSING, (int)current) == (int)current)
            {
                return current;
            }
        }
    }

    private void MarkClosed()
    {
        SetState(WebSocketState.CLOSED);
        connection.MarkClosed();
    }

    private static bool TryValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        try
        {
            return Convert.FromBase64String(key).Length == 16;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsExpectedDisconnect(Exception exception)
    {
        return exception switch
        {
            ObjectDisposedException => true,
            IOException { InnerException: SocketException inner } => IsExpectedSocketError(inner.SocketErrorCode),
            SocketException socketException => IsExpectedSocketError(socketException.SocketErrorCode),
            _ => false
        };
    }

    private static bool IsExpectedSocketError(SocketError socketError)
    {
        return socketError is
            SocketError.ConnectionAborted or
            SocketError.ConnectionReset or
            SocketError.Shutdown or
            SocketError.NotConnected or
            SocketError.TimedOut or
            SocketError.OperationAborted;
    }
}
