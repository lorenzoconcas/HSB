using System.Globalization;

namespace HSB.Http;

internal sealed class ChunkedRequestBodyStream : Stream
{
    private static readonly byte[] CrLf = "\r\n"u8.ToArray();

    private readonly ITransportConnection transport;
    private readonly long maxBodySizeBytes;
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
        pendingBuffer = bufferedPrefix.ToArray();
        pendingOffset = 0;
        pendingCount = pendingBuffer.Length;
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
        var lineBuffer = new List<byte>(128);

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
                        lineBuffer.AddRange(available[..crlfIndex].ToArray());
                    }

                    AdvancePending(crlfIndex + CrLf.Length);
                    return System.Text.Encoding.ASCII.GetString(lineBuffer.ToArray());
                }

                lineBuffer.AddRange(available.ToArray());
                AdvancePending(pendingCount);
            }

            var tempBuffer = new byte[8 * 1024];
            var read = await transport.ReadAsync(tempBuffer, cancellationToken);
            if (read <= 0)
            {
                throw HttpRequestRejectedException.BadRequest("Unexpected end of chunked body");
            }

            AppendPending(tempBuffer.AsSpan(0, read));
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
        if (pendingCount == 0)
        {
            pendingBuffer = bytes.ToArray();
            pendingOffset = 0;
            pendingCount = pendingBuffer.Length;
            return;
        }

        var combined = new byte[pendingCount + bytes.Length];
        pendingBuffer.AsSpan(pendingOffset, pendingCount).CopyTo(combined);
        bytes.CopyTo(combined.AsSpan(pendingCount));
        pendingBuffer = combined;
        pendingOffset = 0;
        pendingCount = combined.Length;
    }

    private void AdvancePending(int count)
    {
        pendingOffset += count;
        pendingCount -= count;

        if (pendingCount == 0)
        {
            pendingBuffer = [];
            pendingOffset = 0;
        }
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
        pendingBuffer = [];
        pendingOffset = 0;
        pendingCount = 0;
        currentChunkRemaining = 0;
        base.Dispose(disposing);
    }
}
