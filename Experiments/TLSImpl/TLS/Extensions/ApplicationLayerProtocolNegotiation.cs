using System.Text;
using HSB.TLS.Constants;

namespace HSB.TLS.Extensions;

public class ApplicationLayerProtocolNegotiation : IExtension
{
    public ExtensionName ExtensionName = ExtensionName.APPLICATION_LAYER_PROTOCOL_NEGOTIATION;
    public byte[] ExtensionData;
    public uint Length;

    ExtensionName IExtension.ExtensionName => ExtensionName;

    byte[] IExtension.ExtensionData => ExtensionData;

    uint IExtension.Length => Length;


    List<(ushort size, byte[] data)> Protocols = new();


    public ApplicationLayerProtocolNegotiation(uint length, byte[] extensionData)
    {
        Length = length;
        ExtensionData = extensionData;
        DataReader dr = new(extensionData);
        var protocolSize = dr.ReadUShort(); //in bytes

        while (dr.DataAvailable())
        {
            //the protocol are made of a length byte and a protocol name
            var protocolDataLength = dr.ReadSmallUint();
            var nextProtocol = dr.ReadBytes(protocolDataLength);
            Protocols.Add(new(protocolDataLength, nextProtocol));
        }

    }

    public override string ToString()
    {
        string extensionStr = "";
        foreach (var (size, data) in Protocols)
        {
            extensionStr += $"0x{BitConverter.ToString(data).Replace("-", " 0x")} -> {Encoding.ASCII.GetString(data)}\n";
        }

        return
        $"{ExtensionName.ToString().Replace("HSB.TLS.Extensions.", "")}" +
        $" ({Length} bytes) - RawData -> 0x{BitConverter.ToString(ExtensionData).Replace("-", " 0x")}\t" +
        $"{extensionStr}\n"
        ;

    }
}