using HSB.TLS.Constants;
namespace HSB.TLS.Messages;

public class ServerHello{

    private ClientHello clientHello;

    private byte[] _random;
    private CipherSuite.Ciphers _cipher;
    private byte[] _publicKey;
  
    public ServerHello(ClientHello clientHello){
        this.clientHello = clientHello;

    }

    public void BuildResponse(){

    }
}