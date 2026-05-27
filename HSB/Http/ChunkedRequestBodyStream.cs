using System.Buffers;
using System.Globalization;
using System.Text;

namespace HSB.Http;

internal sealed class ChunkedRequestBodyStream : Stream
{
    private static readonly byte[] CrLf = "\r\n"u8.ToArray();

    private readonly ITransportConnection transport;
    private readonly long maxBodySizeBytes;
    private readonly ArrayPool<byte> pool = ArrayPool<byte>.Shared;
    private byte[] transportReadBuffer;
    private byte[] pendingBuffer;
    private int pendingOffset;
    private int pendingCount;
    private long currentChunkRemaining;
    private long totalBytesRead;
    private bool completed;
    private bool disposed;

    public ChunkedRequestBodyStream(ITransportConnection transport, ReadOnlyMemory<byte> bufferedPrefix, long maxBodySizeBytes)
    {
        this.transport = transport;
        this.maxBodySizeBytes = maxBodySizeBytes;
        pendingBuffer = pool.Rent(Math.Max(256, bufferedPrefix.Length));
        bufferedPrefix.Span.CopyTo(pendingBuffer);
        pendingOffset = 0;
        pendingCount = bufferedPrefix.Length;
        transportReadBuffer = pool.Rent(8 * 1024);
    }

    public override bool CanRead => !disposed;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => totalBytesRead;
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return ReadAsync(buffer.AsMemory(offset, count)).GetAwaiter().GetResult();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (buffer.Length == 0 || completed)
        {
            return 0;
        }

        if (currentChunkRemaining == 0)
        {
            if (!await MoveToNextChunkAsync(cancellationToken))
            {
                return 0;
            }
        }

        var requestedLength = (int)Math.Min(buffer.Length, currentChunkRemaining);
        var read = await ReadExactUpToAsync(buffer[..requestedLength], requestedLength, cancellationToken);
        if (read <= 0)
        {
            throw HttpRequestRejectedException.BadRequest("Unexpected end of chunked body");
        }

        currentChunkRemaining -= read;
        totalBytesRead += read;

        if (totalBytesRead > maxBodySizeBytes)
        {
            throw HttpRequestRejectedException.FromStatus(HSB.Constants.HttpCodes.PAYLOAD_TOO_LARGE, "Request body too large");
        }

        if (currentChunkRemaining == 0)
        {
            await ConsumeExpectedCrlfAsync(cancellationToken);
        }

        return read;
    }

    private async Task<bool> MoveToNextChunkAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var line = await ReadLineAsync(cancellationToken);
            if (line.Length == 0)
            {
                continue;
            }

            var separatorIndex = line.IndexOf(';');
            var sizeText = separatorIndex >= 0 ? line[..separatorIndex] : line;

            if (!long.TryParse(sizeText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var chunkSize) || chunkSize < 0)
            {
                throw HttpRequestRejectedException.BadRequest("Invalid chunked body");
            }

            if (chunkSize == 0)
            {
                await ConsumeTrailersAsync(cancellationToken);
                completed = true;
                return false;
            }

            currentChunkRemaining = chunkSize;
            return true;
        }
    }

    private async Task<string> ReadLineAsync(CancellationToken cancellationToken)
    {
        using var lineBuffer = new PooledByteBuffer(128, pool);

        while (true)
        {
            if (pendingCount > 0)
            {
                var available = pendingBuffer.AsSpan(pendingOffset, pendingCount);
                var crlfIndex = available.IndexOf(CrLf);
                if (crlfIndex >= 0)
                {
                    if (crlfIndex > 0)
                    {
                        lineBuffer.Append(available[..crlfIndex]);
                    }

                    AdvancePending(crlfIndex + CrLf.Length);
                    return Encoding.ASCII.GetString(lineBuffer.WrittenSpan);
                }

                lineBuffer.Append(available);
                AdvancePending(pendingCount);
            }

            var read = await transport.ReadAsync(transportReadBuffer.AsMemory(0, 8 * 1024), cancellationToken);
            if (read <= 0)
            {
                throw HttpRequestRejectedException.BadRequest("Unexpected end of chunked body");
            }

            AppendPending(transportReadBuffer.AsSpan(0, read));
        }
    }

    private async Task ConsumeTrailersAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var trailerLine = await ReadLineAsync(cancellationToken);
            if (trailerLine.Length == 0)
            {
                return;
            }
        }
    }

    private async Task ConsumeExpectedCrlfAsync(CancellationToken cancellationToken)
    {
        var trailer = new byte[2];
        var read = await ReadExactUpToAsync(trailer.AsMemory(), 2, cancellationToken);
        if (read != 2 || trailer[0] != '\r' || trailer[1] != '\n')
        {
            throw HttpRequestRejectedException.BadRequest("Invalid chunked body terminator");
        }
    }

    private async Task<int> ReadExactUpToAsync(Memory<byte> destination, int requestedLength, CancellationToken cancellationToken)
    {
        var totalRead = 0;

        if (pendingCount > 0)
        {
            var toCopy = Math.Min(requestedLength, pendingCount);
            pendingBuffer.AsMemory(pendingOffset, toCopy).CopyTo(destination);
            AdvancePending(toCopy);
            totalRead += toCopy;

            if (totalRead == requestedLength)
            {
                return totalRead;
            }
        }

        while (totalRead < requestedLength)
        {
            var read = await transport.ReadAsync(destination[totalRead..requestedLength], cancellationToken);
            if (read <= 0)
            {
                return totalRead;
            }

            totalRead += read;
        }

        return totalRead;
    }
    private void AppendPending(ReadOnlySpan<byte> bytes)
    {
        if (pendingCount == 0 && pendingOffset == 0 && pendingBuffer.Length >= bytes.Length)
        {
            bytes.CopyTo(pendingBuffer);
            pendingCount = bytes.Length;
            return;
        }

        EnsurePendingCapacity(pendingCount + bytes.Length);
        if (pendingCount > 0 && pendingOffset > 0)
        {
            pendingBuffer.AsSpan(pendingOffset, pendingCount).CopyTo(pendingBuffer);
        }

        bytes.CopyTo(pendingBuffer.AsSpan(pendingCount));
        pendingOffset = 0;
        pendingCount += bytes.Length;
    }

    private void AdvancePending(int count)
    {
        pendingOffset += count;
        pendingCount -= count;

        if (pendingCount == 0)
        {
            pendingOffset = 0;
        }
    }

    private void EnsurePendingCapacity(int requiredCapacity)
    {
        if (pendingBuffer.Length >= requiredCapacity)
        {
            return;
        }

        var newBuffer = pool.Rent(Math.Max(requiredCapacity, pendingBuffer.Length * 2));
        if (pendingCount > 0)
        {
            pendingBuffer.AsSpan(pendingOffset, pendingCount).CopyTo(newBuffer);
        }

        pool.Return(pendingBuffer);
        pendingBuffer = newBuffer;
        pendingOffset = 0;
    }

    public override int ReadByte()
    {
        var buffer = new byte[1];
        var read = Read(buffer, 0, 1);
        return read == 0 ? -1 : buffer[0];
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
        disposed = true;
        pool.Return(pendingBuffer);
        pool.Return(transportReadBuffer);
        pendingBuffer = Array.Empty<byte>();
        transportReadBuffer = Array.Empty<byte>();
        pendingOffset = 0;
        pendingCount = 0;
        currentChunkRemaining = 0;
        base.Dispose(disposing);
    }
}
