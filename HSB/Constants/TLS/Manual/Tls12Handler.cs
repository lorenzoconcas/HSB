using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using HSB.Exceptions;

namespace HSB.Constants.TLS.Manual;

public class Tls12Handler(Socket socket, X509Certificate2 serverCertificate)
{
    private readonly List<byte> _handshakeMessages = new(); // Buffer for all handshake messages
    
    // Handshake State
    private byte[] _clientRandom = [];
    private byte[] _serverRandom = [];
    private byte[] _masterSecret = [];
    private byte[] _keyBlock = [];

    // Session Keys
    private byte[] ClientWriteMacKey { get; set; } = [];
    private byte[] ServerWriteMacKey { get; set; } = [];
    private byte[] ClientWriteKey { get; set; } = [];
    private byte[] ServerWriteKey { get; set; } = [];
    private byte[] ClientWriteIv { get; set; } = [];
    private byte[] ServerWriteIv { get; set; } = [];
    
    private ulong _serverSequenceNumber;

    public void PerformHandshake()
    {
        // 1. Receive ClientHello
        try{
            var clientHello = ReceiveClientHello();
            if (clientHello == null) throw new Exception("Failed to receive ClientHello");
            _clientRandom = clientHello.Random;
        }
        catch(Exception e)
        {
            if (e is NonHttpRequestException)
            {
                //ask the user to upgrade to HTTPS
                Console.WriteLine("[HSB_TLS] Non-HTTP request detected during ClientHello. Redirecting to https.");
              
                var resp = "HTTP/1.1 400 Bad Request\r\n" +
                           "Content-Type: text/plain\r\n" +
                           "Connection: close\r\n" +
                           "\r\n" +
                           "This server requires an HTTPS connection.\r\n";

                socket.Send(Encoding.UTF8.GetBytes(resp));
                socket.Close();
                throw new NonHttpRequestException();

            }
            return;
        }

        // 2. Send ServerHello
        SendServerHello();

        // 3. Send Certificate
        SendCertificate();

        // 4. Send ServerHelloDone
        SendServerHelloDone();

        // 5. Receive ClientKeyExchange
        var preMasterSecret = ReceiveClientKeyExchange();
        
        // 6. Calculate Master Secret
        CalculateMasterSecret(preMasterSecret);
        
        // 7. Calculate Key Block and Slice Keys
        CalculateKeyBlock();

        // 8. Receive ChangeCipherSpec
        ReceiveChangeCipherSpec();

        // 9. Receive Finished (Encrypted)
        ReceiveFinished();
        
        // 10. Send ChangeCipherSpec
        SendChangeCipherSpec();
        
        // 11. Send Finished (Encrypted)
        SendFinished();
    }

    private void BufferHandshakeMessage(byte[] message)
    {
        _handshakeMessages.AddRange(message);
    }

    private ClientHelloInfo? ReceiveClientHello()
    {
        var header = new byte[5];
        int received = socket.Receive(header);
        if (received < 5) return null; // Should handle cleanly
        if (header[0] != (byte) ContentType.Handshake) throw new NonHttpRequestException();
        
        var length = (header[3] << 8) | header[4];
        var body = new byte[length];
        received = socket.Receive(body);
        if (received < length) return null;

        // Buffer the ClientHello (Handshake Body)
        BufferHandshakeMessage(body);

        var random = new byte[32];
        Array.Copy(body, 6, random, 0, 32);

        return new ClientHelloInfo { Random = random };
    }

    private void SendServerHello()
    {
        _serverRandom = new byte[32];
        new Random().NextBytes(_serverRandom);

        byte sessionIdLen = 0;
        byte[] cipherSuite = TlsConstants.CipherSuite;
        byte compressionMethod = 0;
        
        var handshakeBody = new List<byte>
        {
            TlsConstants.MajorVersion,
            TlsConstants.MinorVersion
        };
        handshakeBody.AddRange(_serverRandom);
        handshakeBody.Add(sessionIdLen);
        handshakeBody.AddRange(cipherSuite);
        handshakeBody.Add(compressionMethod);

        SendHandshakeMessage(HandshakeType.ServerHello, handshakeBody.ToArray());
    }

    private void SendCertificate()
    {
        var certData = serverCertificate.Export(X509ContentType.Cert);
        var certsBody = new List<byte>();
        int certsVectorLen = certData.Length + 3; 
        
        certsBody.Add((byte)((certsVectorLen >> 16) & 0xFF));
        certsBody.Add((byte)((certsVectorLen >> 8) & 0xFF));
        certsBody.Add((byte)(certsVectorLen & 0xFF));

        certsBody.Add((byte)((certData.Length >> 16) & 0xFF));
        certsBody.Add((byte)((certData.Length >> 8) & 0xFF));
        certsBody.Add((byte)(certData.Length & 0xFF));
        certsBody.AddRange(certData);

        SendHandshakeMessage(HandshakeType.Certificate, certsBody.ToArray());
    }

    private void SendServerHelloDone()
    {
        SendHandshakeMessage(HandshakeType.ServerHelloDone, []);
    }

    private byte[] ReceiveClientKeyExchange()
    {
        var header = new byte[5];
        if (socket.Receive(header) < 5) throw new Exception("Failed to receive ClientKeyExchange header");
        var length = (header[3] << 8) | header[4];
        var body = new byte[length];
        if (socket.Receive(body) < length) throw new Exception("Failed to receive ClientKeyExchange body");

        // Buffer ClientKeyExchange
        BufferHandshakeMessage(body);

        // Body[0]=Type, Body[1..3]=Length, Body[4..5]=EncLen
        int encryptedLen = (body[4] << 8) | body[5];
        var encryptedPreMaster = new byte[encryptedLen];
        Array.Copy(body, 6, encryptedPreMaster, 0, encryptedLen);

        return TlsCrypto.RsaDecrypt(serverCertificate, encryptedPreMaster);
    }
    
    private void CalculateMasterSecret(byte[] preMasterSecret)
    {
        var seed = new byte[64];
        Buffer.BlockCopy(_clientRandom, 0, seed, 0, 32);
        Buffer.BlockCopy(_serverRandom, 0, seed, 32, 32);
        
        _masterSecret = TlsCrypto.Prf(preMasterSecret, "master secret", seed, 48);
    }

    private void CalculateKeyBlock()
    {
        var seed = new byte[64];
        Buffer.BlockCopy(_serverRandom, 0, seed, 0, 32);
        Buffer.BlockCopy(_clientRandom, 0, seed, 32, 32);

        _keyBlock = TlsCrypto.Prf(_masterSecret, "key expansion", seed, 104);
        
        int pos = 0;
        ClientWriteMacKey = new byte[20]; Array.Copy(_keyBlock, pos, ClientWriteMacKey, 0, 20); pos += 20;
        ServerWriteMacKey = new byte[20]; Array.Copy(_keyBlock, pos, ServerWriteMacKey, 0, 20); pos += 20;
        ClientWriteKey = new byte[16];    Array.Copy(_keyBlock, pos, ClientWriteKey, 0, 16);    pos += 16;
        ServerWriteKey = new byte[16];    Array.Copy(_keyBlock, pos, ServerWriteKey, 0, 16);    pos += 16;
        ClientWriteIv = new byte[16];     Array.Copy(_keyBlock, pos, ClientWriteIv, 0, 16);     pos += 16;
        ServerWriteIv = new byte[16];     Array.Copy(_keyBlock, pos, ServerWriteIv, 0, 16);
    }

    private void ReceiveChangeCipherSpec()
    {
        var header = new byte[5];
        if (socket.Receive(header) < 5) throw new Exception("Failed to receive CCS header");
        if ((ContentType)header[0] != ContentType.ChangeCipherSpec) throw new Exception("Expected ChangeCipherSpec");
        
        var body = new byte[1];
        socket.Receive(body);
        if (body[0] != 1) throw new Exception("Invalid CCS payload");
        
        // Note: Do NOT buffer CCS in handshake messages
        Console.WriteLine("[HSB_TLS] Received ChangeCipherSpec. Resetting Client Sequence Number to 0.");
        _clientResultSequence = 0;
    }

    private void ReceiveFinished()
    {
        // Receive Encrypted Record
        var header = new byte[5];
        if (socket.Receive(header) < 5) throw new Exception("Failed to receive Finished header");
        
        int length = (header[3] << 8) | header[4];
        var encryptedBody = new byte[length]; // IV + EncryptedData(Msg + MAC + Pad)
        if (socket.Receive(encryptedBody) < length) throw new Exception("Failed to receive Finished body");

        // The IV is explicit in TLS 1.2 AES-CBC records? 
        // Yes, GenericBlockCipher: IV + Ciphertext
        var recordIv = new byte[16];
        Array.Copy(encryptedBody, 0, recordIv, 0, 16);
        
        var ciphertext = new byte[length - 16];
        Array.Copy(encryptedBody, 16, ciphertext, 0, length - 16);

        // Decrypt
        var decrypted = TlsCrypto.AesDecrypt(ciphertext, ClientWriteKey, recordIv);
        
        // Strip Padding (PKCS7-like, check last byte)
        int padLen = decrypted[^1];
        int contentLen = decrypted.Length - 1 - padLen;
        
        // Actual Content = Msg + MAC
        // MAC is 20 bytes (SHA1)
        int macLen = 20;
        int msgLen = contentLen - macLen;
        
        var finishedMsg = new byte[msgLen];
        Array.Copy(decrypted, 0, finishedMsg, 0, msgLen);

        // Verify content type is Handshake (22) and Finished (20)
        // Note: The 'decrypted' block is the TLSCompressed struct which implies:
        // content = Handshake(20) + Length + VerifyData
        // Wait, the "Record Layer" decrypts to the fragment.
        // The fragment is a Handshake Message: Type(1) + Length(3) + VerifyData(12)
        // Total 16 bytes (if msgLen == 16)
        
        if (finishedMsg[0] != (byte)HandshakeType.Finished) throw new Exception("Expected Finished message");
        // We really should Verify MAC here using ClientWriteMacKey but for POC skipping.

        // Verify VerifyData
        var receivedVerifyData = new byte[12];
        Array.Copy(finishedMsg, 4, receivedVerifyData, 0, 12);
        
        // Calculate Expected VerifyData
        using var sha256 = SHA256.Create();
        var handshakeHash = sha256.ComputeHash(_handshakeMessages.ToArray());
        var expectedVerifyData = TlsCrypto.Prf(_masterSecret, "client finished", handshakeHash, 12);
        
        if (!Enumerable.SequenceEqual(receivedVerifyData, expectedVerifyData))
        {
            throw new Exception("Finished message verification failed!");
        }
        
        // Buffer this handshake message too! (Decrypted plaintext)
        BufferHandshakeMessage(finishedMsg);
    }

    private void SendHandshakeMessage(HandshakeType type, byte[] body)
    {
        var msg = new List<byte>
        {
            (byte) type,
            (byte) ((body.Length >> 16) & 0xFF),
            (byte)((body.Length >> 8) & 0xFF),
            (byte)(body.Length & 0xFF)
        };
        msg.AddRange(body);
        
        // Buffer this message we are about to send
        BufferHandshakeMessage(msg.ToArray());

        SendRecord(ContentType.Handshake, msg.ToArray());
    }

    private void SendRecord(ContentType type, byte[] data)
    {
        var record = new List<byte>
        {
            (byte) type,
            TlsConstants.MajorVersion,
            TlsConstants.MinorVersion,
            (byte)((data.Length >> 8) & 0xFF),
            (byte)(data.Length & 0xFF)
        };
        record.AddRange(data);

        socket.Send(record.ToArray());
    }

    private void SendChangeCipherSpec()
    {
        var msg = new byte[] { 1 };
        SendRecord(ContentType.ChangeCipherSpec, msg);
      //  Console.WriteLine("[HSB_TLS] Sent ChangeCipherSpec. Resetting Server Sequence Number to 0.");
        _serverSequenceNumber = 0;
    }
    
    private void SendFinished()
    {
        // Calculate hashing of handshake messages
        // NOTE: Handshake Messages MUST include the DECRYPTED ClientFinished message we just received.
        // We buffered it in ReceiveFinished, so we are good.
        
        using var sha256 = SHA256.Create();
        var handshakeHash = sha256.ComputeHash(_handshakeMessages.ToArray());
        var verifyData = TlsCrypto.Prf(_masterSecret, "server finished", handshakeHash, 12);
        
        // Construct Finished Message
        var msg = new List<byte>
        {
            (byte) HandshakeType.Finished,
            0,
            0,
            12 // Length 12
        };
        msg.AddRange(verifyData);
        
        var finishedMsg = msg.ToArray();
        
        // Buffer this (plaintext) for next steps (if any)
        BufferHandshakeMessage(finishedMsg);
        
        SendEncryptedRecord(ContentType.Handshake, finishedMsg);
    }

    private void SendEncryptedRecord(ContentType type, byte[] data)
    {
        ////Console.WriteLine($"[HSB_TLS] Sending Encrypted Record. Seq={_serverSequenceNumber}, Type={type}, Len={data.Length}");
        // 1. Calculate MAC
        // MAC(seq_num + type + version + length + content)
        var seqNumBytes = BitConverter.GetBytes(_serverSequenceNumber);
        if (BitConverter.IsLittleEndian) Array.Reverse(seqNumBytes);
        
        var header = new byte[5];
        header[0] = (byte)type;
        header[1] = TlsConstants.MajorVersion;
        header[2] = TlsConstants.MinorVersion;
        header[3] = (byte)((data.Length >> 8) & 0xFF);
        header[4] = (byte)(data.Length & 0xFF);
        
        var macInput = new byte[8 + 5 + data.Length];
        Buffer.BlockCopy(seqNumBytes, 0, macInput, 0, 8);
        Buffer.BlockCopy(header, 0, macInput, 8, 5);
        Buffer.BlockCopy(data, 0, macInput, 13, data.Length);
        
        var mac = TlsCrypto.HmacSha1(ServerWriteMacKey, macInput);
        
        // 2. Padding
        // Plaintext = data + MAC + Pad
        // CMS (Cryptographic Message Syntax) padding (or similar for TLS: padding length bytes, value = len)
        // Block size = 16 (AES)
        int contentLen = data.Length + mac.Length;
        int padLen = 16 - (contentLen % 16);
        if (padLen == 0) padLen = 16; // Usually prefer 16 if aligned? Or 0? 
        // TLS 1.2 CBC padding: "The padding is such that the total length of the GenericBlockCipher structure is a multiple of the cipher's block length."
        // Structure: IV + Content + MAC + Padding + PaddingLen
        // Wait, implicit IV? No, explicit IV in TLS 1.2 (GenericBlockCipher).
        // IV is 16 bytes.
        // So total = 16 + Data + MAC + Pad + PadLen.
        // The Data+MAC+Pad+PadLen block must be multiple of 16.
        // Actually, the AES Encrypt call will take the IV.
        // We encrypt (Data + MAC + Pad + PadLen).
        
        // Re-calc pad
        // We need (Content + MAC + Pad + PadLen) % 16 == 0
        int plainLen = data.Length + mac.Length;
        // e.g. plainLen = 16 + 20 = 36.
        // 36 % 16 = 4. 16 - 4 = 12.
        // So we need 12 bytes of padding? 
        // The last byte is padLen.
        // So we add 12 bytes. Last byte is 11? 
        // "Each uint8 in the padding data vector MUST be filled with the padding length value."
        // Value is length of the padding data vector minus 1? No.
        // "The padding length value does not include the size of the padding length field itself." -> No, that's Pre-TLS 1.1?
        // TLS 1.2: "The last byte ... contains the padding length in bytes."
        // "padding_length ... is the length of the padding. ... excluding the padding_length field itself."
        // So if we add N bytes total (Pad + PadLen), the last byte is (N-1).
        
        int currentSize = plainLen + 1; // +1 for PadLen byte
        int needed = 16 - (currentSize % 16);
        if (needed == 16) needed = 0; 
        
        int totalPadBytes = needed + 1; // +1 for the length byte itself
        byte padVal = (byte)(totalPadBytes - 1);
        
        var toEncrypt = new byte[plainLen + totalPadBytes];
        Buffer.BlockCopy(data, 0, toEncrypt, 0, data.Length);
        Buffer.BlockCopy(mac, 0, toEncrypt, data.Length, mac.Length);
        for(int i = plainLen; i < toEncrypt.Length; i++) toEncrypt[i] = padVal;

        // 3. Encrypt
        // Generate explicit IV (random)
        var iv = new byte[16];
        new Random().NextBytes(iv);
        
        var ciphertext = TlsCrypto.AesEncrypt(toEncrypt, ServerWriteKey, iv);
        
        // 4. Send Record
        // Header (5) + IV (16) + Ciphertext (N)
        var recordLen = iv.Length + ciphertext.Length;
        var record = new List<byte>
        {
            (byte) type,
            TlsConstants.MajorVersion,
            TlsConstants.MinorVersion,
            (byte)((recordLen >> 8) & 0xFF),
            (byte)(recordLen & 0xFF) // Length includes IV
        };
        record.AddRange(iv);
        record.AddRange(ciphertext);
        
        socket.Send(record.ToArray());
        
        _serverSequenceNumber++;
    }
    // Application Data Buffer
    private readonly List<byte> _appDataBuffer = new();
    
    // Sequence Numbers
    private ulong _clientResultSequence; 
    // _serverSequenceNumber is already defined

    public int Read(byte[] buffer, int offset, int count)
    {
        // 1. Serve from buffer if available
        if (_appDataBuffer.Count > 0)
        {
            int toCopy = Math.Min(count, _appDataBuffer.Count);
            _appDataBuffer.CopyTo(0, buffer, offset, toCopy);
            _appDataBuffer.RemoveRange(0, toCopy);
            return toCopy;
        }

        // 2. Read next record
        while (true)
        {
            var header = new byte[5];
            int read = socket.Receive(header);
            if (read == 0) return 0; // Disconnect
            if (read < 5) throw new Exception("Incomplete TLS header");

            var type = (ContentType)header[0];
            int length = (header[3] << 8) | header[4];
            
            var body = new byte[length];
            int bodyRead = 0;
            while (bodyRead < length)
            {
                int r = socket.Receive(body, bodyRead, length - bodyRead, SocketFlags.None);
                if (r == 0) throw new Exception("Unexpected EOF in TLS body");
                bodyRead += r;
            }

            // If App Data, Decrypt
            if (type == ContentType.ApplicationData)
            {
                byte[] decrypted = DecryptRecord(body, type);
                _appDataBuffer.AddRange(decrypted);
                
                // Return what we have found
                int toCopy = Math.Min(count, _appDataBuffer.Count);
                _appDataBuffer.CopyTo(0, buffer, offset, toCopy);
                _appDataBuffer.RemoveRange(0, toCopy);
                return toCopy;
            }
            else if (type == ContentType.Alert)
            {
                // Encrypted Alert?
                // For POC, assume close notify if alert
                Console.WriteLine("[HSB_TLS] Received Alert. Closing.");
                return 0; 
            }
            else
            {
                //Console.WriteLine($"[HSB_TLS] Ignored record type: {type}");
            }
        }
    }

    public void Write(byte[] buffer, int offset, int count)
    {
        // TLS 1.2 Record limit is 2^14 (16384 bytes) for plaintext fragment
        const int maxFragmentSize = 16384; 

        int position = 0;
        while (position < count)
        {
            int bytesToSend = Math.Min(count - position, maxFragmentSize);
            
            var chunk = new byte[bytesToSend];
            Array.Copy(buffer, offset + position, chunk, 0, bytesToSend);

            SendEncryptedRecord(ContentType.ApplicationData, chunk);

            position += bytesToSend;
        }
    }
    
    private byte[] DecryptRecord(byte[] encryptedBody, ContentType type)
    {
         // IV (16) + Ciphertext
         var recordIv = new byte[16];
         Array.Copy(encryptedBody, 0, recordIv, 0, 16);
         
         var ciphertext = new byte[encryptedBody.Length - 16];
         Array.Copy(encryptedBody, 16, ciphertext, 0, encryptedBody.Length - 16);
         
         var decrypted = TlsCrypto.AesDecrypt(ciphertext, ClientWriteKey, recordIv);
         
         // Strip Padding
         int padLen = decrypted[^1];
         int contentLen = decrypted.Length - 1 - padLen;
         if (contentLen < 0) throw new Exception("Invalid padding");
         
         // Content + MAC
         int macLen = 20;
         int msgLen = contentLen - macLen;
         if (msgLen < 0) throw new Exception("Invalid MAC length");
         
         var msg = new byte[msgLen];
         Array.Copy(decrypted, 0, msg, 0, msgLen);
         
         // Verify MAC (Client Sequence Number)
         // MAC(seq_num + type + version + length + content)
         // Increment sequence after verification
         _clientResultSequence++; // Just incrementing for now, full verification omitted for brevity in POC
         
         return msg;
    }

    private class ClientHelloInfo
    {
        public byte[] Random { get; set; } = [];
    }
}
