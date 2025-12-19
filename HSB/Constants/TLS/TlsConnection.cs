using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using HSB.Exceptions;

namespace HSB.Constants.TLS;

public class TlsConnection
{
    private readonly X509Certificate2 _serverCertificate;
    private readonly SslProtocols _sslProtocols;
    private readonly bool _checkCertificateRevocation;
    private readonly bool _clientCertificateRequired;

    public TlsConnection(
        X509Certificate2 serverCertificate, 
        SslProtocols sslProtocols, 
        bool checkCertificateRevocation = true, 
        bool clientCertificateRequired = false)
    {
        _serverCertificate = serverCertificate;
        _sslProtocols = sslProtocols;
        _checkCertificateRevocation = checkCertificateRevocation;
        _clientCertificateRequired = clientCertificateRequired;
    }

    /// <summary>
    /// Establishes an SSL connection on the given socket.
    /// </summary>
    /// <param name="socket">The client socket.</param>
    /// <param name="leaveInnerStreamOpen">Whether to leave the inner stream open.</param>
    /// <returns>Authenticaded SslStream or null if authentication fails.</returns>
    public SslStream? EstablishSsl(Socket socket, bool leaveInnerStreamOpen = true)
    {
        var netStream = new NetworkStream(socket, false); // We handle closing manually if needed, or via SslStream
        var sslStream = new SslStream(netStream, leaveInnerStreamOpen);

        try
        {
            sslStream.AuthenticateAsServer(
                _serverCertificate,
                _clientCertificateRequired,
                _sslProtocols,
                _checkCertificateRevocation
            );
            return sslStream;
        }
        catch (Exception)
        {
            sslStream.Dispose();
            // netStream.Dispose(); // NetworkStream owns the socket if ownsSocket is true, but we passed false. 
            // However, we want to return null to indicate failure so the caller can decide (e.g. redirect or close).
            return null;
        }
    }
}
