using System;
using HSB.TLS.Constants;

namespace HSB.TLS.Extensions;
//source https://www.iana.org/assignments/tls-extensiontype-values/tls-extensiontype-values.xhtml#tls-extensiontype-values-1
public static class ExtensionsSuites
{
    public static readonly Dictionary<ExtensionName, byte[]> extensions = new(){
        {ExtensionName.SERVER_NAME, new byte[]{0x00, 0x00}},
        {ExtensionName.MAX_FRAGMENT_LENGTH, new byte[]{0x00, 0x01}},
        {ExtensionName.CLIENT_CERTIFICATE_URL, new byte[]{0x00, 0x02}},
        {ExtensionName.TRUSTED_CA_KEYS, new byte[]{0x00, 0x03}},
        {ExtensionName.TRUNCATED_HMAC, new byte[]{0x00, 0x04}},
        {ExtensionName.STATUS_REQUEST, new byte[]{0x00, 0x05}},
        {ExtensionName.USER_MAPPING, new byte[]{0x00, 0x06}},
        {ExtensionName.CLIENT_AUTHZ, new byte[]{0x00, 0x07}},
        {ExtensionName.SERVER_AUTHZ, new byte[]{0x00, 0x08}},
        {ExtensionName.CERT_TYPE, new byte[]{0x00, 0x09}},
        {ExtensionName.SUPPORTED_GROUPS, new byte[]{0x00, 0x0a}},
        {ExtensionName.EC_POINT_FORMATS, new byte[]{0x00, 0x0b}},
        {ExtensionName.SRP, new byte[]{0x00, 0x0c}},
        {ExtensionName.SIGNATURE_ALGORITHMS, new byte[]{0x00, 0x0d}},
        {ExtensionName.USE_SRTP, new byte[]{0x00, 0x0e}},
        {ExtensionName.HEARTBEAT, new byte[]{0x00, 0x0f}},
        {ExtensionName.APPLICATION_LAYER_PROTOCOL_NEGOTIATION, new byte[]{0x00, 0x10}},
        {ExtensionName.STATUS_REQUEST_V2, new byte[]{0x00, 0x11}},
        {ExtensionName.SIGNED_CERTIFICATE_TIMESTAMP, new byte[]{0x00, 0x12}},
        {ExtensionName.CLIENT_CERTIFICATE_TYPE, new byte[]{0x00, 0x13}},
        {ExtensionName.SERVER_CERTIFICATE_TYPE, new byte[]{0x00, 0x14}},
        {ExtensionName.PADDING, new byte[]{0x00, 0x15}},
        {ExtensionName.ENCRYPT_THEN_MAC, new byte[]{0x00, 0x16}},
        {ExtensionName.EXTENDED_MASTER_SECRET, new byte[]{0x0, 0x17}},
        {ExtensionName.TOKEN_BINDING, new byte[]{0x0, 0x18}},
        {ExtensionName.CACHED_INFO, new byte[]{0x0, 0x19}},
        {ExtensionName.TLS_LTS, new byte[]{0x0, 0x1a}},
        {ExtensionName.COMPRESS_CERTIFICATE, new byte[]{0x0, 0x1b}},
        {ExtensionName.RECORD_SIZE_LIMIT, new byte[]{0x0, 0x1c}},
        {ExtensionName.PWD_PROTECT, new byte[]{0x0, 0x1d}},
        {ExtensionName.PWD_CLEAR, new byte[]{0x0, 0x1e}},
        {ExtensionName.PASSWORD_SALT, new byte[]{0x0, 0x1f}},
        {ExtensionName.TICKET_PINNING, new byte[]{0x0, 0x20}},
        {ExtensionName.TLS_CERT_WITH_EXTERN_PSK, new byte[]{0x0, 0x21}},
        {ExtensionName.DELEGATED_CREDENTIAL, new byte[]{0x0, 0x22}},
        {ExtensionName.SESSION_TICKET, new byte[]{0x0, 0x23}},
        {ExtensionName.TLMSP, new byte[]{0x0, 0x24}},
        {ExtensionName.TLMSP_PROXYING, new byte[]{0x0, 0x25}},
        {ExtensionName.TLMSP_DELEGATE, new byte[]{0x0, 0x26}},
        {ExtensionName.SUPPORTED_EKT_CIPHERS, new byte[]{0x0, 0x27}},
        //0x0, 0x28 is Reserved
        {ExtensionName.PRE_SHARED_KEY, new byte[]{0x0, 0x29}},
        {ExtensionName.EARLY_DATA, new byte[]{0x0, 0x2a}},
        {ExtensionName.SUPPORTED_VERSIONS, new byte[]{0x0, 0x2b}},
        {ExtensionName.COOKIE, new byte[]{0x0, 0x2c}},
        {ExtensionName.PSK_KEY_EXCHANGE_MODES, new byte[]{0x0, 0x2d}},
        //0x0 0x2e is Reserved
        {ExtensionName.CERTIFICATE_AUTHORITIES, new byte[]{0x0, 0x2f}},
        {ExtensionName.OID_FILTERS, new byte[]{0x0, 0x30}},
        {ExtensionName.POST_HANDSHAKE_AUTH, new byte[]{0x0, 0x31}},
        {ExtensionName.SIGNATURE_ALGORITHMS_CERT, new byte[]{0x0, 0x32}},
        {ExtensionName.KEY_SHARE, new byte[]{0x0, 0x33}},
        {ExtensionName.TRANSPARENCY_INFO, new byte[]{0x0, 0x34}},
        {ExtensionName.CONNECTION_ID, new byte[]{0x0, 0x36}}, //0x0, 0x35 is deprecated (old connection_id)
        {ExtensionName.EXTERNAL_ID_HASH, new byte[]{0x0, 0x37}},
        {ExtensionName.EXTERNAL_SESSION_ID, new byte[]{0x0, 0x38}},
        {ExtensionName.QUIC_TRANSPORT_PARAMETERS, new byte[]{0x0, 0x39}},
        {ExtensionName.TICKET_REQUEST, new byte[]{0x0, 0x3a}},
        {ExtensionName.DNSSEC_CHAIN, new byte[]{0x0, 0x3b}},
        {ExtensionName.SEQUENCE_NUMBER_ENCRYPTION_ALGORITHMS, new byte[]{0x0, 0x3c}},

    };

    public static byte[] GetExtension(ExtensionName name)
    {
        return extensions[name];
    }

    public static ExtensionName GetExtensionName(byte[] extension)
    {
        return extensions.FirstOrDefault(x => x.Value.SequenceEqual(extension)).Key;
    }

    public static string GetExtensionNameString(byte[] extension)
    {
        return GetExtensionName(extension).ToString();
    }
}

