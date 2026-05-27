using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using HSB.Constants.TLS.Manual;

namespace HSB.Http;

internal static class TransportConnectionFactory
{
    public static ITransportConnection Create(Socket socket, SslStream? sslStream, Tls12Handler? tlsHandler)
    {
        if (tlsHandler != null)
        {
            return new ManualTlsTransportConnection(socket, tlsHandler);
        }

        if (sslStream != null)
        {
            return new SslTransportConnection(socket, sslStream);
        }

        return new SocketTransportConnection(socket);
    }
}

internal sealed class SocketTransportConnection : ITransportConnection
{
    private readonly Socket socket;
    private int closed;

    public SocketTransportConnection(Socket socket)
    {
        this.socket = socket;
    }

    public Socket Socket => socket;
    public EndPoint? RemoteEndPoint => socket.RemoteEndPoint;

    public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);
    }

    public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return WriteAllAsync(buffer, cancellationToken);
    }

    public void SetTimeouts(int receiveTimeoutMilliseconds, int sendTimeoutMilliseconds)
    {
        socket.ReceiveTimeout = receiveTimeoutMilliseconds;
        socket.SendTimeout = sendTimeoutMilliseconds;
    }

    public void Close()
    {
        if (Interlocked.Exchange(ref closed, 1) == 1)
        {
            return;
        }

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

    public void Dispose()
    {
        Close();
    }

    private async ValueTask WriteAllAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        var totalSent = 0;
        while (totalSent < buffer.Length)
        {
            totalSent += await socket.SendAsync(buffer[totalSent..], SocketFlags.None, cancellationToken);
        }
    }
}

internal sealed class SslTransportConnection : ITransportConnection
{
    private readonly Socket socket;
    private readonly SslStream sslStream;
    private int closed;

    public SslTransportConnection(Socket socket, SslStream sslStream)
    {
        this.socket = socket;
        this.sslStream = sslStream;
    }

    public Socket Socket => socket;
    public EndPoint? RemoteEndPoint => socket.RemoteEndPoint;

    public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return sslStream.ReadAsync(buffer, cancellationToken);
    }

    public async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await sslStream.WriteAsync(buffer, cancellationToken);
        await sslStream.FlushAsync(cancellationToken);
    }

    public void SetTimeouts(int receiveTimeoutMilliseconds, int sendTimeoutMilliseconds)
    {
        socket.ReceiveTimeout = receiveTimeoutMilliseconds;
        socket.SendTimeout = sendTimeoutMilliseconds;
        sslStream.ReadTimeout = receiveTimeoutMilliseconds;
        sslStream.WriteTimeout = sendTimeoutMilliseconds;
    }

    public void Close()
    {
        if (Interlocked.Exchange(ref closed, 1) == 1)
        {
            return;
        }

        try
        {
            sslStream.Dispose();
        }
        catch
        {
            // ignored
        }

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

    public void Dispose()
    {
        Close();
    }
}

internal sealed class ManualTlsTransportConnection : ITransportConnection
{
    private readonly Socket socket;
    private readonly Tls12Handler tlsHandler;
    private int closed;

    public ManualTlsTransportConnection(Socket socket, Tls12Handler tlsHandler)
    {
        this.socket = socket;
        this.tlsHandler = tlsHandler;
    }

    public Socket Socket => socket;
    public EndPoint? RemoteEndPoint => socket.RemoteEndPoint;

    public async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tempBuffer = new byte[buffer.Length];
        var read = await Task.Run(() => tlsHandler.Read(tempBuffer, 0, tempBuffer.Length), cancellationToken);
        if (read > 0)
        {
            tempBuffer.AsMemory(0, read).CopyTo(buffer);
        }

        return read;
    }

    public async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = buffer.ToArray();
        await Task.Run(() => tlsHandler.Write(bytes, 0, bytes.Length), cancellationToken);
    }

    public void SetTimeouts(int receiveTimeoutMilliseconds, int sendTimeoutMilliseconds)
    {
        socket.ReceiveTimeout = receiveTimeoutMilliseconds;
        socket.SendTimeout = sendTimeoutMilliseconds;
    }

    public void Close()
    {
        if (Interlocked.Exchange(ref closed, 1) == 1)
        {
            return;
        }

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

    public void Dispose()
    {
        Close();
    }
}
