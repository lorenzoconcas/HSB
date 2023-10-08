using HSB.TLS.Constants;
using HSB.TLS.Extensions;
namespace HSB.TLS.Messages;

public class ClientHello
{
    public readonly ProtocolVersion protocolVersion = new(ProtocolVersion.TLSVersionEnum.TLS_1_3);
    public readonly byte[] clientRandom = new byte[32];
    public readonly byte[] sessionID = new byte[32];
    public readonly List<CipherSuite.Ciphers> cipherSuites = new();
    public readonly List<IExtension> extensions = new();

    public ClientHello(byte[] clientRandom, byte[] sessionID, List<CipherSuite.Ciphers> cipherSuites, List<IExtension> extensions, ProtocolVersion clientProtocolVersion)
    {
        this.clientRandom = clientRandom;
        this.sessionID = sessionID;
        this.cipherSuites = cipherSuites;
        this.extensions = extensions;
    }


}