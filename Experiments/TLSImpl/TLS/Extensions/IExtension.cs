using System.Text;
using HSB;
using HSB.TLS.Constants;

namespace HSB.TLS.Extensions;

public interface IExtension
{
    public ExtensionName ExtensionName { get; }
    public byte[] ExtensionData { get; }
    public uint Length { get; } //rappresents the length of the extension (including type and payload size)

    public string ToString()
    {
        return $"{ExtensionName.ToString().Replace("HSB.TLS.Extensions.", "")} ({Length} bytes) - RawData -> 0x{BitConverter.ToString(ExtensionData).Replace("-", " 0x")}\t{Encoding.ASCII.GetString(ExtensionData)}";
    }

    //two methods todo -> GetExtensionBytes and GetBytes used to reconstruct the extension

}
