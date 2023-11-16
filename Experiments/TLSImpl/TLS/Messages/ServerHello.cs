using System.Security.Cryptography;
using HSB.TLS.Constants;
namespace HSB.TLS.Messages;

public class ServerHello(ClientHello clientHello)
{

    private readonly ClientHello clientHello = clientHello;

    private byte[] _random;
    private readonly CipherSuite.Ciphers _cipher;
    private byte[] _publicKey;

    public void BuildResponse(){
        //build the private/public key-pair
        //generate 32bytes random data
        _random = new byte[32];
        RandomNumberGenerator.Fill(_random);
        int random = BitConverter.ToInt32(_random);
        int clientRandom = BitConverter.ToInt32(clientHello.clientRandom);

      
               

    }
}