using System;
using System.Text;
using HSB;
using HSB.TLS.Constants;
namespace HSB.TLS.Extensions;

public class ECPointFormats : IExtension
{
    //Generic extension fields
    public ExtensionName ExtensionName = ExtensionName.EC_POINT_FORMATS;
    public byte[] ExtensionData;
    public uint Length;
    ExtensionName IExtension.ExtensionName => ExtensionName;
    byte[] IExtension.ExtensionData => ExtensionData;
    uint IExtension.Length => Length;

    private byte[] _length;
    private byte _formatTypesContained;
    private byte[] _formatTypes;

    //to do: implement reading of format types

    public List<byte> FormatTypes => new List<byte>(_formatTypes);

    public ECPointFormats(uint length, byte[] extensionData)
    {
        Length = length;
        ExtensionData = extensionData;

        if (extensionData.Length == 0)
        {
            _length = Array.Empty<byte>();
            _formatTypesContained = 0;
            _formatTypes = Array.Empty<byte>();
            return;
        }

        DataReader dr = new(extensionData);
        _length = dr.ReadBytes(1);
        _formatTypesContained = dr.ReadByte();
        _formatTypes = dr.ReadBytes(_formatTypesContained);
    }


    public override string ToString()
    {
        return $"{ExtensionName.ToString().Replace("HSB.TLS.Extensions.", "")} ({Length} bytes) - RawData -> 0x{BitConverter.ToString(ExtensionData).Replace("-", " 0x")}\t{Encoding.ASCII.GetString(ExtensionData)}";
    }
}