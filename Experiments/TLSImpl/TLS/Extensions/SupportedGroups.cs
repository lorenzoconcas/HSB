using System;
using System.Text;
using HSB.TLS.Constants;
namespace HSB.TLS.Extensions;

public class SupportedGroups : IExtension
{

    //Generic extension fields

    public ExtensionName ExtensionName = ExtensionName.SUPPORTED_GROUPS;
    public byte[] ExtensionData;
    public uint Length;

    ExtensionName IExtension.ExtensionName => ExtensionName;

    byte[] IExtension.ExtensionData => ExtensionData;

    uint IExtension.Length => Length;

    List<byte[]> _supportedGroups;
    public SupportedGroups(uint length, byte[] extensionData)
    {
        Length = length;
        ExtensionData = extensionData;

        if (extensionData.Length == 0)
        {
            _supportedGroups = new List<byte[]>();
            return;
        }

        DataReader dr = new(extensionData);
        _supportedGroups = new List<byte[]>();
        //todo implement reading of supported groups
        /*  dr.SetEndPosition(length);
          while (dr.DataAvailable())
          {
              byte[] supportedGroup = dr.ReadBytes(2);
              _supportedGroups.Add(supportedGroup);
          }*/
    }

    public override string ToString()
    {
        return $"{ExtensionName.ToString().Replace("HSB.TLS.Extensions.", "")} ({Length} bytes) - RawData -> 0x{BitConverter.ToString(ExtensionData).Replace("-", " 0x")}\t{Encoding.ASCII.GetString(ExtensionData)}";
    }

}


