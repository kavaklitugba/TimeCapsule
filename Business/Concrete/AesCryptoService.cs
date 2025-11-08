using Business.Abstract;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

public class AesCryptoService : ICryptoService
{
    private readonly byte[] _key; // 32 byte (256 bit)

    public AesCryptoService(IConfiguration config)
    {
        var keyBase64 = config["Crypto:AesKey"];
        if (string.IsNullOrEmpty(keyBase64))
            throw new InvalidOperationException("Crypto:AesKey tanımlı değil.");

        _key = Convert.FromBase64String(keyBase64);
        if (_key.Length != 32)
            throw new InvalidOperationException("AesKey 256 bit olmalı (32 byte).");
    }

    public (byte[] cipherText, byte[] iv) Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        return (cipherBytes, aes.IV);
    }

    public string Decrypt(byte[] cipherText, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }

    public byte[] HashToken(string token)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes(token));
    }
}
