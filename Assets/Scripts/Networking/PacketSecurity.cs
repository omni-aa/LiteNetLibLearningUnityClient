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

        public static bool Verify(byte[] data, byte[] hash)
        {
            return StructuralComparisons.StructuralEqualityComparer.Equals(HMAC(data), hash);
        }
    }
}
