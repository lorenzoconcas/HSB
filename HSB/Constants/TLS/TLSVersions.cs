namespace HSB.Constants.TLS;

public enum TLSVersion
{
    NOT_SET = 0x0000,
    [Obsolete("TLS 1.0 is deprecated and should not be used.")]
    TLS_1_0 = 0x0301, 
    [Obsolete("TLS 1.1 is deprecated and should not be used.")]
    TLS_1_1 = 0x0302,
    TLS_1_2 = 0x0303,
    TLS_1_3 = 0x0304
}