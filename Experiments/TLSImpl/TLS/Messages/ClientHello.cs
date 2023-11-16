using System.Text.Json;
using HSB.TLS.Constants;
using HSB.TLS.Extensions;
namespace HSB.TLS.Messages;

public class ClientHello
{

    private static readonly JsonSerializerOptions jo = new() { WriteIndented = true, IncludeFields = true };


    public readonly ProtocolVersion protocolVersion = new(TLSVersion.TLS_1_3);
    public readonly byte[] clientRandom = [];
    public readonly byte[] legacy_session_id_echo = [];
    public readonly byte[] sessionID = [];

    public readonly List<CipherSuite.Ciphers> cipherSuites = [];
    public readonly List<IExtension> extensions = [];

    public ClientHello(byte[] clientRandom, byte[] sessionID, List<CipherSuite.Ciphers> cipherSuites, List<IExtension> extensions)
    {
        this.clientRandom = clientRandom;
        this.sessionID = sessionID;
        this.cipherSuites = cipherSuites;
        this.extensions = extensions;
    }

    public ClientHello(byte[] data)
    {
        //print data length
        Console.WriteLine("ClientHello: " + data.Length);
        //2 + 32 + 1 + 1 + 2 + 2
        if (data.Length < 40)
            throw new Exception("ClientHello: data.Length < 40");

        DataReader reader = new(data);

        protocolVersion = new(reader.ReadBytes(2));
        clientRandom = reader.ReadBytes(32);
        //legacy session id can be 0 or 32 bytes, in tls 1.3 it is 0
        legacy_session_id_echo = [];
        //parse cipher suites
        ushort cipherSuitesLength = Utils.BytesToUShort(reader.ReadBytes(2));
        if (cipherSuitesLength % 2 != 0)
            throw new Exception("ClientHello: cipherSuitesLength % 2 != 0");

        for (int i = 0; i < cipherSuitesLength / 2; i++)
        {
            cipherSuites.Add((CipherSuite.Ciphers)Utils.BytesToUShort(reader.ReadBytes(2)));
        }
        //parse extensions
        ushort extensionsLength = Utils.BytesToUShort(reader.ReadBytes(2));
        if (extensionsLength == 0)
            return;

        while (reader.DataAvailable())
        {
            extensions.Add(ExtensionUtils.ReadExtension(reader));
        }

    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, jo);
    }


}