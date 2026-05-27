using System.Buffers;

namespace HSB.Http;

internal sealed class PooledByteBuffer : IDisposable
{
    private readonly ArrayPool<byte> pool;
    private byte[] buffer;
    private bool disposed;

    public PooledByteBuffer(int initialCapacity, ArrayPool<byte>? pool = null)
    {
        this.pool = pool ?? ArrayPool<byte>.Shared;
        buffer = this.pool.Rent(Math.Max(256, initialCapacity));
    }

    public int Length { get; private set; }

    public ReadOnlySpan<byte> WrittenSpan => buffer.AsSpan(0, Length);
    public ReadOnlyMemory<byte> WrittenMemory => buffer.AsMemory(0, Length);

    public void Append(ReadOnlySpan<byte> bytes)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        EnsureCapacity(Length + bytes.Length);
        bytes.CopyTo(buffer.AsSpan(Length));
        Length += bytes.Length;
    }

    public int IndexOf(ReadOnlySpan<byte> value)
    {
        return WrittenSpan.IndexOf(value);
    }

    public byte[] CopyRangeToArray(int start, int length)
    {
        var result = new byte[length];
        buffer.AsSpan(start, length).CopyTo(result);
        return result;
    }

    public byte[] ToArray()
    {
        return CopyRangeToArray(0, Length);
    }

    public void Clear()
    {
        Length = 0;
    }

    private void EnsureCapacity(int requiredCapacity)
    {
        if (requiredCapacity <= buffer.Length)
        {
            return;
        }

        var newBuffer = pool.Rent(Math.Max(requiredCapacity, buffer.Length * 2));
        buffer.AsSpan(0, Length).CopyTo(newBuffer);
        pool.Return(buffer);
        buffer = newBuffer;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        pool.Return(buffer);
        buffer = Array.Empty<byte>();
        Length = 0;
    }
}
