using System.Net;
using System.Net.Sockets;

namespace HSB.Http;

internal interface ITransportConnection : IDisposable
{
    Socket Socket { get; }
    EndPoint? RemoteEndPoint { get; }
    ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);
    ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);
    void SetTimeouts(int receiveTimeoutMilliseconds, int sendTimeoutMilliseconds);
    void Close();
}
