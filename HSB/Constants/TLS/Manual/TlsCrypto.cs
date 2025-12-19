using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace HSB.Constants.TLS.Manual;

public static class TlsCrypto
{
    /// <summary>
    /// Computes the TLS 1.2 Pseudo-Random Function (PRF) using HMAC-SHA256.
    /// PRF(secret, label, seed) = P_SHA256(secret, label + seed)
    /// </summary>
    public static byte[] Prf(byte[] secret, string label, byte[] seed, int length)
    {
        byte[] labelBytes = Encoding.ASCII.GetBytes(label);
        
        // Seed for P_hash is label + seed
        byte[] labelAndSeed = new byte[labelBytes.Length + seed.Length];
        Buffer.BlockCopy(labelBytes, 0, labelAndSeed, 0, labelBytes.Length);
        Buffer.BlockCopy(seed, 0, labelAndSeed, labelBytes.Length, seed.Length);

        return P_Hash(secret, labelAndSeed, length);
    }

    /// <summary>
    /// P_hash(secret, seed) = HMAC_hash(secret, A(1) + seed) +
    ///                        HMAC_hash(secret, A(2) + seed) + ...
    /// Where A(0) = seed
    ///       A(i) = HMAC_hash(secret, A(i-1))
    /// </summary>
    private static byte[] P_Hash(byte[] secret, byte[] seed, int length)
    {
        using var hmac = new HMACSHA256(secret);
        var result = new byte[length];
        int currentPos = 0;
        
        // A(0) is seed
        byte[] a = seed;

        while (currentPos < length)
        {
            // A(i) = HMAC_hash(secret, A(i-1))
            a = hmac.ComputeHash(a);

            // Output = HMAC_hash(secret, A(i) + seed)
            // Need to concatenate A(i) + seed
            byte[] input = new byte[a.Length + seed.Length];
            Buffer.BlockCopy(a, 0, input, 0, a.Length);
            Buffer.BlockCopy(seed, 0, input, a.Length, seed.Length);

            byte[] output = hmac.ComputeHash(input);

            // Copy to result
            int bytesToCopy = Math.Min(output.Length, length - currentPos);
            Buffer.BlockCopy(output, 0, result, currentPos, bytesToCopy);
            currentPos += bytesToCopy;
        }

        return result;
    }

    /// <summary>
    /// Decrypts data using the private key of the certificate.
    /// Used for decrypting the Pre-Master Secret in RSA Key Exchange.
    /// </summary>
    public static byte[] RsaDecrypt(X509Certificate2 cert, byte[] data)
    {
        using (RSA? rsa = cert.GetRSAPrivateKey())
        {
            if (rsa == null) throw new Exception("Certificate does not have a private key or is not RSA.");
            // TLS 1.2 RSA Key Exchange uses PKCS#1 v1.5 padding
            return rsa.Decrypt(data, RSAEncryptionPadding.Pkcs1);
        }
    }

    public static byte[] AesDecrypt(byte[] data, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None; // We handle padding manually or use standard if exact

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(data, 0, data.Length);
    }

    public static byte[] HmacSha1(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA1(key);
        return hmac.ComputeHash(data);
    }

    public static byte[] AesEncrypt(byte[] data, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None; // Manual padding

        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(data, 0, data.Length);
    }
}
