namespace HSB.TLS.Constants;

public enum MessageType
{
    HELLLO_REQUEST = 0x0,
    CLIENT_REQUEST = 0x01,
    server_hello = 0x02,
    certificate = 0x0B,
    server_key_exchange = 0x0C,
    certificate_request = 0x0D,
    server_hello_done = 0x0E,
    certificate_verify = 0x0F,
    client_key_exchange = 0x10,
    finished = 20
}