using HSB;
using HSB.Constants.TLS;

namespace HSB.Exceptions;

public class DeprecatedTLSVersionException : Exception
{
    public DeprecatedTLSVersionException(TLSVersion version)
    {
        Terminal.ERROR($"Deprecated TLS version -> {version}");
    }

}