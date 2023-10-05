using System;
namespace HSB.TLS.Constants;

public class SupportedGroupsTypes{
    public enum SupportedGroupsName{
        x25519 = 0x001D,
        secp256r1 = 0x0017,
        secp384r1 = 0x0018,
        secp521r1 = 0x0019,
        x448 = 0x001E,
        ffdhe2048 = 0x0100,
        ffdhe3072 = 0x0101,
        ffdhe4096 = 0x0102,
        ffdhe6144 = 0x0103,
        ffdhe8192 = 0x0104,
       
    }

    public static SupportedGroupsName GetSupportedGroupsName(byte[] data){
        if(data.Length != 2){
            throw new Exception("data length is not 2");
        }
        ushort supportedGroupsName = Utils.BytesToUShort(data);
        return (SupportedGroupsName)supportedGroupsName;
    }

    public static SupportedGroupsName GetSupportedGroupsName(byte byte1, byte byte2){
        ushort supportedGroupsName = Utils.BytesToUShort(byte1, byte2);
        return (SupportedGroupsName)supportedGroupsName;
    }

    public static byte[] GetBytes(SupportedGroupsName supportedGroupsName){
        return Utils.UShortToBytes((ushort)supportedGroupsName);       
    }
}
