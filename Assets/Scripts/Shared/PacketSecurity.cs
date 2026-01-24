using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;

namespace Shared
{
    public static class PacketSecurity
    {
        // ==========================
        // KEYS MUST BE CORRECT SIZE
        // ==========================
        private static readonly byte[] HmacKey = Encoding.UTF8.GetBytes("12345678901234567890123456789012"); // 32 bytes
        private static readonly byte[] AesKey = Encoding.UTF8.GetBytes("12345678901234567890123456789012"); // 32 bytes for AES-256
        private static readonly byte[] AesIV = Encoding.UTF8.GetBytes("1234567890123456");                 // 16 bytes IV

        // --------------------------
        // ENCRYPT AND SIGN (for sending)
        // --------------------------
        public static byte[] EncryptAndSign(byte[] data)
        {
            byte[] encrypted = Encrypt(data);
            byte[] hmac = HMAC(encrypted);
            
            byte[] result = new byte[hmac.Length + encrypted.Length];
            Buffer.BlockCopy(hmac, 0, result, 0, hmac.Length);
            Buffer.BlockCopy(encrypted, 0, result, hmac.Length, encrypted.Length);
            
            return result;
        }

        // --------------------------
        // VERIFY AND DECRYPT (for receiving)
        // --------------------------
        public static bool Verify(byte[] encryptedData, byte[] signature)
        {
            byte[] expectedHmac = HMAC(encryptedData);
            return StructuralComparisons.StructuralEqualityComparer.Equals(expectedHmac, signature);
        }

        // --------------------------
        // AES-256-CBC ENCRYPTION
        // --------------------------
        public static byte[] Encrypt(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = AesKey;
            aes.IV = AesIV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(data, 0, data.Length);
        }

        // --------------------------
        // AES-256-CBC DECRYPTION
        // --------------------------
        public static byte[] Decrypt(byte[] encrypted)
        {
            using var aes = Aes.Create();
            aes.Key = AesKey;
            aes.IV = AesIV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
        }

        // --------------------------
        // HMAC-SHA256
        // --------------------------
        public static byte[] HMAC(byte[] data)
        {
            using var hmac = new HMACSHA256(HmacKey);
            return hmac.ComputeHash(data);
        }
        
        // --------------------------
        // DEBUG METHOD: Get key info
        // --------------------------
        public static string GetKeyInfo()
        {
            return $"HMAC Key: {BitConverter.ToString(HmacKey)}\nAES Key: {BitConverter.ToString(AesKey)}\nAES IV: {BitConverter.ToString(AesIV)}";
        }
    }
}