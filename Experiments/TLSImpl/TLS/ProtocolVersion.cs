namespace HSB.TLS;
public class ProtocolVersion
{

    public enum TLSVersionEnum
    {
        TLS_1_0 = 0x0301,
        TLS_1_1 = 0x0302,
        TLS_1_2 = 0x0303,
        TLS_1_3 = 0x0304  
    }

    uint major;
    uint minor;

    public ProtocolVersion(byte[] clientHello)
    {
        major = clientHello[1];
        minor = clientHello[2];
    }

    public ProtocolVersion(uint major, uint minor)
    {
        this.major = major;
        this.minor = minor;
    }

    public ProtocolVersion(TLSVersionEnum tlsVersion)
    {
        major = (uint)tlsVersion >> 8;
        minor = (uint)tlsVersion & 0xFF;
    }


    public string GetVersionName()
    {
        if (major == 0x03 && minor == 0x01)
            return "1.0";

        if (major == 0x03 && minor == 0x02)
            return "1.1";

        if (major == 0x03 && minor == 0x03)
            return "1.2";

        else if (major == 0x03 && minor == 0x04)
            return "1.3";                

        return "Unknown";

    }


    public uint[] GetVersion()
    {
        return new uint[] { major, minor };
    }



    public override string ToString()
    {
        return $"TLS v{GetVersionName()} ({major}.{minor})";
    }
}