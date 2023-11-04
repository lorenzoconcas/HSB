using HSB.Constants.TLS;
using HSB;

public class DeprecatedTLSVersionException : Exception
{
    public DeprecatedTLSVersionException(TLSVersion version)
    {
        Terminal.ERROR($"Deprecated TLS version -> {version}");
    }
 
}

