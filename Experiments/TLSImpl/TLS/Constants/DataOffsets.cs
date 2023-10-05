namespace HSB.TLS.Constants;

public static class DataOffsets
{
    public static readonly (uint size, uint offset) HANDSHAKE_RECORD = (size: 1, offset:0);
    public static readonly (uint size, uint offset) TLS_VERSION = (size: 2, offset:1);
    public static readonly (uint size, uint offset) MESSAGE_LENGTH = (size: 2, offset:3);
    public static readonly (uint size, uint offset) HANDSHAKE_TYPE = (size: 1, offset:5);
    public static readonly (uint size, uint offset) HANDSHAKE_DATA_LENGHT = (size: 3, offset:6);
    public static readonly (uint size, uint offset) CLIENT_VERSION = (size : 2, offset: 9);
}