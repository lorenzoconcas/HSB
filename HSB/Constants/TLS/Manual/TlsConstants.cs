namespace HSB.Constants.TLS.Manual;

public enum ContentType : byte
{
    ChangeCipherSpec = 20,
    Alert = 21,
    Handshake = 22,
    ApplicationData = 23
}

public enum HandshakeType : byte
{
    ClientHello = 1,
    ServerHello = 2,
    Certificate = 11,
    ServerKeyExchange = 12,
    CertificateRequest = 13,
    ServerHelloDone = 14,
    CertificateVerify = 15,
    ClientKeyExchange = 16,
    Finished = 20
}

public static class TlsConstants
{
    public const byte MajorVersion = 3;
    public const byte MinorVersion = 3; // TLS 1.2

    // Cipher Suite: TLS_RSA_WITH_AES_128_CBC_SHA
    public static readonly byte[] CipherSuite = { 0x00, 0x2F }; 
}
