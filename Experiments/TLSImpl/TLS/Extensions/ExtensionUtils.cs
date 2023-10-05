using HSB.TLS.Constants;

namespace HSB.TLS.Extensions;

public static class ExtensionUtils
{
    public static IExtension ReadExtension(DataReader dr)
    {
        byte[] extName = dr.ReadBytes(2);

        ExtensionName extensionName = ExtensionsSuites.GetExtensionName(extName);
        ushort length = dr.ReadUShort();
        uint totalBytes = (uint)(length + 2);
        byte[] extensionData = dr.ReadBytes(length);
        try
        {
           return Instantiate(extensionName, totalBytes, extensionData);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return new GenericExtension(extensionName, length, extensionData);
        }
    }

    private static IExtension Instantiate(ExtensionName extensionName, uint length, byte[] extensionData)
    {
        return extensionName switch
        {
            ExtensionName.SERVER_NAME => new ServerName(length, extensionData),
            ExtensionName.EC_POINT_FORMATS => new ECPointFormats(length, extensionData),
            ExtensionName.SUPPORTED_GROUPS => new SupportedGroups(length, extensionData),
            ExtensionName.KEY_SHARE => new KeyShare(length, extensionData),
            ExtensionName.APPLICATION_LAYER_PROTOCOL_NEGOTIATION => new ApplicationLayerProtocolNegotiation(length, extensionData),
            ExtensionName.SIGNATURE_ALGORITHMS => new SignatureAlgorithms(length, extensionData),
            _ => new GenericExtension(extensionName, length, extensionData)
        };
    }

}
