using HSB.TLS.Constants;
namespace HSB.TLS;
public class ProtocolVersion
{
    readonly TLSVersion version;

    public ProtocolVersion(byte[] clientHello)
    {
        version = (TLSVersion)Utils.BytesToUShort(clientHello);
    }
    public ProtocolVersion(TLSVersion tlsVersion)
    {
        version = tlsVersion;
    }

    /// <summary>
    /// Returns the TLS version as human readable string
    /// </summary>
    /// <returns></returns>
    public override string ToString() => version switch
    {
        TLSVersion.TLS_1_0 => "1.0",
        TLSVersion.TLS_1_1 => "1.1",
        TLSVersion.TLS_1_2 => "1.2",
        TLSVersion.TLS_1_3 => "1.3",
        _ => "Unknown",
    };

    /// <summary>
    /// Returns the TLS version as integer values (major, minor)
    /// </summary>
    /// <returns></returns>
    public uint[] GetVersion()
    {
        return new uint[] { (uint)version >> 8, (uint)version & 0xFF };
    }
}