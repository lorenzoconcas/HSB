using static HSB.TLS.Constants.SupportedGroupsTypes;
using HSB.TLS.Constants;

namespace HSB.TLS.Extensions;
public class KeyShare : IExtension
{
    //Generic extension fields

    public ExtensionName ExtensionName = ExtensionName.KEY_SHARE;
    public byte[] ExtensionData;
    public uint Length;

    ExtensionName IExtension.ExtensionName => ExtensionName;

    byte[] IExtension.ExtensionData => ExtensionData;

    uint IExtension.Length => Length;


    private byte[] _keyShareDataLength;
    private byte[] _keyGroup;
    private byte[] _publicKeyLength;
    private byte[] _publicKey; //from Client Key Exchange, should be 32bytes every time
    public KeyShare(uint length, byte[] extensionData)
    {
        Length = length;
        ExtensionData = extensionData;
        DataReader dr = new(extensionData);

        _keyShareDataLength = dr.ReadBytes(2);
        _keyGroup = dr.ReadBytes(2);
        _publicKeyLength = dr.ReadBytes(2);
        ushort publicKeyLength = Utils.BytesToUShort(_publicKeyLength);
        _publicKey = dr.ReadBytes(publicKeyLength);

        /*  int offset = 4;
          _keyShareDataLength = data[offset..(offset + 2)];
          offset += 2;
          _keyGroup = data[offset..(offset + 2)];
          offset += 2;
          _publicKeyLength = data[offset..(offset + 2)];
          offset += 2;
          _publicKey = data[offset..(offset + Utils.BytesToUShort(_publicKeyLength[0], _publicKeyLength[1]))];*/

    }


    public byte[] getPublicKey => _publicKey;

    public SupportedGroupsName GetKeyGroup => GetSupportedGroupsName(_keyGroup);


    override public string ToString()
    {
        return $"KEYSHARE -> {BitConverter.ToString(_publicKey)}";
    }
}


