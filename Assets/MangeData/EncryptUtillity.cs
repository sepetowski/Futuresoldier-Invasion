using System;
using System.Security.Cryptography;
using System.Text;

//Advanced Encryption Standard (AES)
public static class EncryptionUtility
{
    private static readonly string EncryptionKey = "6hC4Bg79kG5B2x85";

    public static string Encrypt(string plainText)
    {
        byte[] keyBytes = CreateValidKey(EncryptionKey);

        using (Aes aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.IV = new byte[16];
            ICryptoTransform encryptor = aes.CreateEncryptor();

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            return Convert.ToBase64String(encryptedBytes);
        }
    }

    public static string Decrypt(string encryptedText)
    {
        byte[] keyBytes = CreateValidKey(EncryptionKey);

        using (Aes aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.IV = new byte[16];
            ICryptoTransform decryptor = aes.CreateDecryptor();

            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            byte[] plainBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }

    private static byte[] CreateValidKey(string key)
    {
        if (key.Length < 16) key = key.PadRight(16, '0');
        else if (key.Length > 16 && key.Length < 24) key = key.PadRight(24, '0');
        else if (key.Length > 24 && key.Length < 32) key = key.PadRight(32, '0');
        else if (key.Length > 32) key = key.Substring(0, 32);

        return Encoding.UTF8.GetBytes(key);
    }
}
