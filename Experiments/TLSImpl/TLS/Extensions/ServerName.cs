using System;
using HSB;
using HSB.TLS.Constants;
namespace HSB.TLS.Extensions;


public class ServerName : IExtension
{
    //Generic extension fields
    public ExtensionName ExtensionName = ExtensionName.SERVER_NAME;
    public byte[] ExtensionData;
    public uint Length;

    ExtensionName IExtension.ExtensionName => ExtensionName;

    byte[] IExtension.ExtensionData => ExtensionData;

    uint IExtension.Length => Length;

    //specific for ServerName extension
    private readonly byte[] _listEntryLength;
    private readonly byte _isDnsHostname;
    private readonly byte[] _hostnameLength;
    private readonly byte[] _hostname;


    public ServerName(uint length, byte[] extensionData)
    {
        Length = length;
        ExtensionData = extensionData;

        if (extensionData.Length == 0)
        {
            _isDnsHostname = 0;
            _hostnameLength = Array.Empty<byte>();
            _hostname = Array.Empty<byte>();
            _listEntryLength = Array.Empty<byte>();
            // extensionData = Array.Empty<byte>();
            return;
        }

        DataReader dr = new(extensionData);
        _listEntryLength = dr.ReadBytes(2);
        _isDnsHostname = dr.ReadByte();
        _hostnameLength = dr.ReadBytes(2);
        ushort hostnameLength = Utils.BytesToUShort(_hostnameLength);
        _hostname = dr.ReadBytes(hostnameLength);
    }

    public string Hostname => System.Text.Encoding.UTF8.GetString(_hostname);
    public bool IsDnsHostname => _isDnsHostname == 0;

    public override string ToString()
    {
        return "SERVER_NAME -> \'" + Hostname + "\' is DNS hostname? : " + IsDnsHostname;
    }

}


