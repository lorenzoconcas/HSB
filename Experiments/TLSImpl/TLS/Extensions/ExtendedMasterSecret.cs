using System;
using HSB.TLS.Constants;
namespace HSB.TLS.Extensions;
public class ExtendedMasterSecret : IExtension
{
    //Generic extension fields
    public ExtensionName ExtensionName = ExtensionName.SERVER_NAME;
    public byte[] ExtensionData;
    public uint Length;

    ExtensionName IExtension.ExtensionName => ExtensionName;

    byte[] IExtension.ExtensionData => ExtensionData;

    uint IExtension.Length => Length;


    //the presence of this extension is indication that is eanbled
    //in v1.3 it is always enabled
    public ExtendedMasterSecret(uint length, byte[] extensionData)
    {
        Length = length;
        ExtensionData = extensionData;
    }

    public override string ToString()
    {
        return ExtensionName.ToString().Replace("HSB.TLS.Extensions.", "");
    }
}


