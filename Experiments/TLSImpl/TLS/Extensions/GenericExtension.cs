//this class is the implementation of the Extension interface without any other fields, 
//is used to hold the unimplemented extensions

using System.Text;
using HSB.TLS.Constants;
using HSB.TLS.Extensions;

namespace HSB.TLS.Extensions;

public class GenericExtension : IExtension
{
    public ExtensionName ExtensionName;
    public byte[] ExtensionData;
    public uint Length;

    ExtensionName IExtension.ExtensionName => ExtensionName;

    byte[] IExtension.ExtensionData => ExtensionData;

    uint IExtension.Length => Length;

    public GenericExtension(ExtensionName extensionName, uint length, byte[] extensionData)
    {
        ExtensionName = extensionName;
        Length = length;
        ExtensionData = extensionData;
    }
    public override string ToString()
    {
        return $"{ExtensionName.ToString().Replace("HSB.TLS.Extensions.", "")} ({Length} bytes) - RawData -> 0x{BitConverter.ToString(ExtensionData).Replace("-", " 0x")}\n\t{Encoding.ASCII.GetString(ExtensionData)}";
    }
  
}