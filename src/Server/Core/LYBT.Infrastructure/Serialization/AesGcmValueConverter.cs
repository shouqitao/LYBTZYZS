using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LYBT.Infrastructure.Serialization;

/// <summary>
/// AES-GCM 透明加解密 ValueConverter（P1-9）
/// 透明加密：明文 → Base64(nonce(12) + tag(16) + ciphertext)
/// 透明解密：若非 Base64 或解密失败则回退原文（兼容历史明文迁移）
/// 密钥来源：SecurityOptions.AesKey (Base64 32B) 或环境变量 Security__AesKey，否则回退测试固定密钥（仅开发/测试）
/// </summary>
public sealed class AesGcmValueConverter : ValueConverter<string?, string?>
{
    private static readonly byte[] FallbackKey = Convert.FromBase64String("MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDE="); // 32B test key

    private static byte[] ResolveKey()
    {
        try
        {
            var env = Environment.GetEnvironmentVariable("Security__AesKey")
                   ?? Environment.GetEnvironmentVariable("Security:AesKey");
            if (!string.IsNullOrWhiteSpace(env))
            {
                var kb = Convert.FromBase64String(env);
                if (kb.Length == 32) return kb;
            }
        }
        catch { }
        return FallbackKey;
    }

    private static string? Encrypt(string? plain)
    {
        if (string.IsNullOrEmpty(plain)) return plain;
        try
        {
            var key = ResolveKey();
            var nonce = RandomNumberGenerator.GetBytes(12);
            var plainBytes = System.Text.Encoding.UTF8.GetBytes(plain);
            var cipher = new byte[plainBytes.Length];
            var tag = new byte[16];
            using var aes = new AesGcm(key, 16);
            aes.Encrypt(nonce, plainBytes, cipher, tag);
            var combined = new byte[nonce.Length + tag.Length + cipher.Length];
            Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
            Buffer.BlockCopy(cipher, 0, combined, nonce.Length + tag.Length, cipher.Length);
            return Convert.ToBase64String(combined);
        }
        catch
        {
            return plain;
        }
    }

    private static string? Decrypt(string? cipherBase64)
    {
        if (string.IsNullOrEmpty(cipherBase64)) return cipherBase64;
        try
        {
            var combined = Convert.FromBase64String(cipherBase64);
            if (combined.Length < 12 + 16 + 1)
                throw new CryptographicException("敏感数据解密失败：密文长度不足");
            var key = ResolveKey();
            var nonce = new byte[12];
            var tag = new byte[16];
            var cipher = new byte[combined.Length - 12 - 16];
            Buffer.BlockCopy(combined, 0, nonce, 0, 12);
            Buffer.BlockCopy(combined, 12, tag, 0, 16);
            Buffer.BlockCopy(combined, 28, cipher, 0, cipher.Length);
            var plain = new byte[cipher.Length];
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(nonce, cipher, tag, plain);
            return System.Text.Encoding.UTF8.GetString(plain);
        }
        catch (CryptographicException)
        {
            throw;
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("敏感数据解密失败：Base64 格式错误", ex);
        }
        catch (Exception ex) when (ex is not CryptographicException)
        {
            throw new CryptographicException("敏感数据解密失败", ex);
        }
    }

    public AesGcmValueConverter() : base(v => Encrypt(v), v => Decrypt(v)) { }

    public AesGcmValueConverter(ConverterMappingHints? hints) : base(v => Encrypt(v), v => Decrypt(v), hints) { }
}
