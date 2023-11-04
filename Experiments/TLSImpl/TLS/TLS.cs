using System.Net.Sockets;
using System.Security.Cryptography;
using HSB.TLS.Constants;
using HSB.TLS.Extensions;
using HSB;
using static HSB.TLS.Constants.CipherSuite;
using System.Text;
using HSB.TLS.Messages;

namespace HSB.TLS;

public class CustomTLS
{
    Socket socket;
    //items of the TLS request

    // ProtocolVersion clientVersion;


    byte[] clientRandom;
    ushort sessionID_size;
    byte[] fakeSessionID;
    List<Ciphers> cipherSuites = new();


    public CustomTLS(Socket socket)
    {
        this.socket = socket;
        clientRandom = Array.Empty<byte>();
        fakeSessionID = Array.Empty<byte>();
    }


    //TODO
    public void IsTLSPacket(byte data)
    {
    }

    public void Parse(byte[] data)
    {
        Console.Clear();
        PrintDatatable(data);
        PrintDatatableASCII(data);
        //print each byte explaning what is
        //Record Header
        Console.WriteLine($"(offset : 0 )\tRequest Type: {data[0]:X2} (16 == Handshake record))");
        Console.WriteLine($"(offset: 1)\tTLS Version (Major): {data[1]:X2} (Minor): {data[2]:X2})");
        //handshake length
        int handshakeLength = Utils.BytesToUShort(new byte[] {data[3], data[4]});
        Console.WriteLine(
            $"(offset: 3)\tHandshake length (bytes): {data[3]:X2}{data[4]:X2} (int : {handshakeLength}) handshake ends at row {(handshakeLength + 4) / 16} col {(handshakeLength + 4) % 16})");
        //handshake header
        Console.WriteLine($"(offset 5)\tHandshake Type: {data[5]:X2} (1 == Client Hello))");
        Console.WriteLine(
            $"(offset 6)\tHandshake length (bytes): {data[6]:X2}{data[7]:X2}{data[8]:X2} (int : {Utils.UInt24ToUInt32(new byte[] {data[6], data[7], data[8]})})");
        //Client Version
        Console.WriteLine($"(offset 9)\tClient Version (Major): {data[9]:X2} (Minor): {data[10]:X2})");

        if (data[9] == 0x03 && data[10] == 0x03)
        {
            Console.WriteLine("Detected client TLS v1.3");
            ParseTLS_1_3(data, handshakeLength);
            ParseTLSv1_3(data);
        }
        else
        {
            Console.WriteLine("Detected client TLS up to v1.2");
            //ParseTLS_UP_TO_1_2(data, handshake_length);
        }
    }

    static void PrintDatatable(byte[] data)
    {
        Console.WriteLine("Printing raw request preview\n============================================");
        //print table header (0 -> 15)
        Console.WriteLine("\t 0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15");
        int i = 0;
        int j = 0;
        Console.Write($"{0}\t");
        foreach (byte b in data)
        {
            Console.Write($"{b:X2} ");
            i++;
            if (i == 16)
            {
                j++;
                Console.WriteLine("");
                Console.Write($"{j}\t");
                i = 0;
            }
        }

        Console.WriteLine("\n====================================");
    }

    static void PrintDatatableASCII(byte[] data)
    {
        Console.WriteLine("Printing raw request preview\n============================================");
        //print table header (0 -> 15)
        Console.WriteLine("\t 0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15");
        int i = 0;
        int j = 0;
        Console.Write($"{0}\t");
        var str = Encoding.ASCII.GetString(data).ToCharArray();
        for (int k = 0; k < data.Length; k++)
        {
            Console.Write($"{str[k]} ");
            i++;
            if (i == 16)
            {
                j++;
                Console.WriteLine("");
                Console.Write($"{j}\t");
                i = 0;
            }
        }

        Console.WriteLine("\n====================================");
    }


    /*ProtocolVersion version = new(data);
    Console.WriteLine("Client hello with TLS version: " + version.ToString());*/


    private void ParseTLSv1_3(byte[] data)
    {
        Console.WriteLine("=============================\nNew parser result:");
        DataReader d = new(data);

        byte requestType = d.ReadByte();
        byte[] TLS_Version = d.ReadBytes(DataOffsets.TLS_VERSION.size);
        byte[] msgLength = d.ReadBytes(DataOffsets.MESSAGE_LENGTH.size);
        byte handshakeType = d.ReadByte();
        byte[] handshakeLenght = d.ReadBytes(DataOffsets.HANDSHAKE_DATA_LENGHT.size); //in bytes
        uint handshakeLenghtInt = Utils.UInt24ToUInt32(handshakeLenght);


        uint endPosition =
            handshakeLenghtInt +
            1 +
            DataOffsets.TLS_VERSION.size +
            DataOffsets.MESSAGE_LENGTH.size +
            1 +
            DataOffsets.HANDSHAKE_DATA_LENGHT.size;
        d.SetEndPosition(Math.Min(endPosition, 512));

        byte[] clientVersion = d.ReadBytes(DataOffsets.CLIENT_VERSION.size);
        byte[] clientRandom = d.ReadBytes(32);
        byte sessionIDSize = d.ReadByte(); //should always be 0 in TLS1.3
        byte[] fakeSessionID = d.ReadBytes(sessionIDSize);
        byte[] cipherSuitesLength = d.ReadBytes(2);
        ushort cipherSuitesLengthInt = Utils.BytesToUShort(cipherSuitesLength);
        List<Ciphers> ciphers = new();
        for (int i = 0; i < cipherSuitesLengthInt; i += 2)
        {
            ciphers.Add(GetCipher(d.ReadBytes(2)));
        }

        byte[] compressionData = d.ReadBytes(2);
        byte[] extensionsLength = d.ReadBytes(2);
        extensionsLength[0] = 0x0; //testing
        ushort extensionsLengthInt = Utils.BytesToUShort(extensionsLength); //in bytes
        List<IExtension> extensions = new();

        while (d.DataAvailable() && extensionsLengthInt > 0 && extensionsLengthInt < 512)
        {
            IExtension e = ExtensionUtils.ReadExtension(d);
            extensions.Add(e);
            // Console.WriteLine(d.RemainingData);
        }


        Console.Write(
            $"Request Type: 0x{requestType:X2}" +
            $"\nTLS Version: 0x{BitConverter.ToString(TLS_Version).Replace("-", " 0x")}" +
            $"\nMessage Length: 0x{BitConverter.ToString(msgLength).Replace("-", " 0x")} ({Utils.BytesToUShort(msgLength)})" +
            $"\nHandshake Type: 0x{handshakeType:X2}" +
            $"\nHandshake Length: 0x{BitConverter.ToString(handshakeLenght).Replace("-", " 0x")} ({Utils.UInt24ToUInt32(handshakeLenght)})" +
            $"\nClient Version: 0x{BitConverter.ToString(clientVersion).Replace("-", " 0x")}" +
            $"\nClient Random: 0x{BitConverter.ToString(clientRandom).Replace("-", " 0x")}" +
            $"\nSession ID: 0x{sessionIDSize:X2}" +
            $"\nFake Session ID: 0x{BitConverter.ToString(fakeSessionID).Replace("-", " 0x")}0" +
            $"\nCipher Suites Length: 0x{BitConverter.ToString(cipherSuitesLength).Replace("-", " 0x")} ({cipherSuitesLengthInt} bytes, {cipherSuitesLengthInt / 2} ciphers)" +
            $"\nCipher Suites: \n\t{string.Join(",\n\t", ciphers)}" +
            $"\nCompression Data: 0x{BitConverter.ToString(compressionData).Replace("-", " 0x")}" +
            $"\nExtensions Length: 0x{BitConverter.ToString(extensionsLength).Replace("-", " 0x")} ({extensionsLengthInt} bytes)\n"
        );
        //print extensions
        foreach (IExtension e in extensions)
            Console.WriteLine("\t" + e.ToString());


        ClientHello clientHello = new(clientRandom, fakeSessionID, ciphers, extensions, new(TLS_Version));
        ServerHello serverHello = new(clientHello);
        serverHello.BuildResponse();

        //Generate a private key with the X25519 curve
        /* using ECDiffieHellmanCng ecdh = new(ECCurve.NamedCurves.);
         ecdh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
         ecdh.HashAlgorithm = CngAlgorithm.Sha256;
         byte[] publicKey = ecdh.PublicKey.ToByteArray();
         Console.WriteLine($"Generate public key: 0x{BitConverter.ToString(publicKey).Replace("-", " 0x")}");*/


        Console.WriteLine("Done");
    }

    private void ParseTLS_1_3(byte[] data, int handshake_length)
    {
        //in TLS 1.3+ session id is differente and have a size

        int offset = 11;
        int col = offset % 16;
        int row = offset / 16;


        //client random -> 32 bytes
        clientRandom = new byte[32];
        Array.Copy(data, offset, clientRandom, 0, 32);
        uint clientRandomInt = BitConverter.ToUInt32(clientRandom);
        Console.WriteLine(
            $"(offset {offset} (riga {row}, col {col}))\tRandom (32 bytes): 0x{BitConverter.ToString(clientRandom).Replace("-", " 0x")}");
        Console.WriteLine($"The 32bit random number is : {clientRandomInt}");
        offset += 32;
        col = offset % 16;
        row = offset / 16;
        //Session ID
        sessionID_size = data[offset];
        Console.WriteLine(
            $"(offset {offset} (riga {row}, col {col}))\tSession ID size: {data[offset]:X2} (int : {sessionID_size})");
        fakeSessionID = new byte[sessionID_size];
        Array.Copy(data, offset + 1, fakeSessionID, 0, sessionID_size);
        Console.WriteLine(
            $"(offset {offset} (riga {row}, col {col}))\tSession ID: {BitConverter.ToString(fakeSessionID).Replace("-", " ")}");
        offset += 1 + sessionID_size;
        //Cipher Suites
        col = offset % 16;
        row = offset / 16;
        ushort cipher_suites_length =
            Utils.BytesToUShort(new byte[] {data[offset], data[offset + 1]}); //length in bytes
        Console.WriteLine(
            $"(offset {offset} (riga {row}, col {col}))\tCipher Suites Length: {data[offset]:X2} {data[offset + 1]:X2} (int : {cipher_suites_length / 2})");
        offset += 2;
        col = offset % 16;
        row = offset / 16;
        for (int i = 0; i < cipher_suites_length; i += 2)
        {
            Console.Write(
                $"(offset {offset} (riga {row}, col {col}))\tCipher Suite: 0x{data[offset]:X2}, 0x{data[offset + 1]:X2} -> ");
            var cS = GetCipher(new byte[] {data[offset], data[offset + 1]});
            cipherSuites.Add(cS);
            Console.WriteLine(cS);
            offset += 2;
            col = offset % 16;
            row = offset / 16;
        }

        //Compression data -> not available in TLS 1.3+ so two bytes const 0x01 0x00
        Console.WriteLine(
            $"(offset {offset} (riga {row}, col {col}))\tCompression Data: {data[offset]:X2} {data[offset + 1]:X2}");
        offset += 2;
        //Extensions
        col = offset % 16;
        row = offset / 16;
        ushort extensions_length = Utils.BytesToUShort(new byte[] {data[offset], data[offset + 1]});
        //same as TLS 1.2
        Console.WriteLine(
            $"(offset {offset} (riga {row}, col {col}))\tExtensions Length: {data[offset]:X2} {data[offset + 1]:X2} (int : {extensions_length}))");
        offset += 2;

        /*    List<Extension> extensions = new();

            while (offset < handshake_length)
            {
                //extension range from current two bytes to current two bytes + 4 + extension length
                Extension e;
                offset = ExtensionUtils.ExtensionReaderExtended(data, offset, out e);
                extensions.Add(e);
                Console.WriteLine(e.ToString());

            }*/


        Console.WriteLine("Client Hello (TLS version 1.3) parsed!");
    }

    private void ParseTLS_UP_TO_1_2(byte[] data, int handshake_length)
    {
        int i = 0;
        //Random
        Console.WriteLine($"(offset 11)\tRandom (32 bytes): {BitConverter.ToString(data, 11, 32).Replace("-", " ")}");
        //Session ID
        Console.WriteLine($"(offset 43 (riga {43 / 16}, col {43 % 16}))\tSession ID: 0x{data[43]:X2}");
        //Cipher Suites 
        //two bytes to int
        ushort cipher_suites_length = Utils.BytesToUShort(new byte[] {data[44], data[45]});
        Console.WriteLine(
            $"(offset 44 (riga {44 / 16}, col {44 % 16}))\tCipher Suites Length: {data[44]:X2} {data[45]:X2} (int : {cipher_suites_length / 2})");
        int offset = 46;
        int col = offset % 16;
        int row = offset / 16;

        for (i = 0; i < cipher_suites_length; i += 2)
        {
            offset += i;
            Console.Write(
                $"(offset {offset} (riga {row}, col {col}))\tCipher Suite: 0x{data[46 + i]:X2}, 0x{data[47 + i]:X2} -> ");
            Console.WriteLine(CipherSuite.GetCipher(new byte[] {data[46 + i], data[47 + i]}));
        }

        //compression methods
        offset = 46 + cipher_suites_length;
        col = offset % 16;
        row = offset / 16;
        Console.WriteLine(
            $"(offset {offset} (riga {row}, col {col}))\tCompression Methods Length: {data[offset]:X2} {data[offset + 1]:X2} (int : {Utils.BytesToUShort(new byte[] {data[offset], data[offset + 1]})})");
        offset += 2;
        col = offset % 16;
        row = offset / 16;
        //Extensions length
        int extensionsLength = Utils.BytesToUShort(new byte[] {data[offset], data[offset + 1]});
        Console.WriteLine(
            $"(offset {offset} (riga {row}, col {col}))\tExtensions Length: {data[offset]:X2} {data[offset + 1]:X2} (int : {extensionsLength}))");
        offset += 2;
        //Each extension will start with two bytes that indicate which extension it is, 
        //followed by a two-byte content length field, followed by the contents of the extension.
        //Extensions

        //server name?
        /*   Console.WriteLine("Extension 1");
            byte[] extensionType = new byte[] { data[offset], data[offset + 1] };
            Console.WriteLine($"(offset {offset} (riga {row}, col {col}))\tExtension Type: 0x{data[offset]:X2} 0x{data[offset + 1]:X2}");
            offset += 2;
            ushort extensionLength = TwoBytesToShort(new byte[] { data[offset], data[offset + 1] });
            Console.WriteLine($"(offset {offset} (riga {row}, col {col}))\tExtension Length: 0x{data[offset]:X2} 0x{data[offset + 1]:X2} (int : {extensionLength})");
            offset += 2;
            byte[] extensionData = new byte[extensionLength];
            Array.Copy(data, offset, extensionData, 0, extensionLength);
            Console.WriteLine($"(offset {offset} (riga {row}, col {col}))\tExtension Data: {BitConverter.ToString(extensionData).Replace("-", " ")} -> {Encoding.ASCII.GetString(extensionData)}");

            //next extension
            Console.WriteLine("Extension 2");
            offset += extensionLength;
            extensionType = new byte[] { data[offset], data[offset + 1] };
            Console.WriteLine($"(offset {offset} (riga {row}, col {col}))\tExtension Type: 0x{data[offset]:X2} 0x{data[offset + 1]:X2}");
            offset += 2;
            extensionLength = TwoBytesToShort(new byte[] { data[offset], data[offset + 1] });
            Console.WriteLine($"(offset {offset} (riga {row}, col {col}))\tExtension Length: 0x{data[offset]:X2} 0x{data[offset + 1]:X2} (int : {extensionLength})");
            offset += 2;
            extensionData = new byte[extensionLength];
            Array.Copy(data, offset, extensionData, 0, extensionLength);
            Console.WriteLine($"(offset {offset} (riga {row}, col {col}))\tExtension Data: {BitConverter.ToString(extensionData).Replace("-", " ")} -> {Encoding.ASCII.GetString(extensionData)}");
*/


        Console.WriteLine("Client Hello (TLS version max 1.2) parsed!");
    }
}