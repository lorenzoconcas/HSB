namespace HSB.Http;

internal sealed class RequestBodyStream : Stream
{
    private readonly ITransportConnection transport;
    private readonly long contentLength;
    private ReadOnlyMemory<byte> bufferedPrefix;
    private long remainingBytes;
    private long position;
    private bool disposed;

    public RequestBodyStream(ITransportConnection transport, ReadOnlyMemory<byte> bufferedPrefix, long contentLength)
    {
        this.transport = transport;
        this.contentLength = Math.Max(0, contentLength);
        this.bufferedPrefix = bufferedPrefix.Length > contentLength
            ? bufferedPrefix[..(int)contentLength]
            : bufferedPrefix;
        remainingBytes = Math.Max(0, contentLength - this.bufferedPrefix.Length);
    }

    public override bool CanRead => !disposed;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => contentLength;
    public override long Position
    {
        get => position;
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

        if (buffer.Length == 0)
        {
            return 0;
        }

        var totalRead = 0;
        if (!bufferedPrefix.IsEmpty)
        {
            var copyLength = Math.Min(bufferedPrefix.Length, buffer.Length);
            bufferedPrefix[..copyLength].CopyTo(buffer);
            bufferedPrefix = bufferedPrefix[copyLength..];
            position += copyLength;
            totalRead += copyLength;

            if (copyLength == buffer.Length)
            {
                return totalRead;
            }

            buffer = buffer[copyLength..];
        }

        if (remainingBytes <= 0)
        {
            return totalRead;
        }

        var requestedRead = (int)Math.Min(buffer.Length, remainingBytes);
        var read = await transport.ReadAsync(buffer[..requestedRead], cancellationToken);
        if (read <= 0)
        {
            return totalRead;
        }

        remainingBytes -= read;
        position += read;
        return totalRead + read;
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
        bufferedPrefix = ReadOnlyMemory<byte>.Empty;
        base.Dispose(disposing);
    }
}
