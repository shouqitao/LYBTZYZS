using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LYBT.Infrastructure.Serialization;

/// <summary>
/// AES-GCM 透明加解密 ValueConverter（P1-9）
/// 透明加密：明文 → Base64(nonce(12) + tag(16) + ciphertext)；加密失败抛 CryptographicException（R-18 fail-closed，禁止静默返回明文）
/// 透明解密：非 Base64 / 密文长度不足 / 认证失败 → 抛 CryptographicException（T1.4 fail-closed，
/// 4d58c5464：禁止静默回退明文——密钥不匹配时防敏感数据以明文泄漏；EF 读取抛错由
/// BusinessExceptionHandler 映射 ERR-00013 422）
/// 密钥来源：SecurityOptions.AesKey (Base64 32B) 或环境变量 Security__AesKey
/// R-5: 非 Development 环境密钥缺失时 fail-fast 抛异常；Development 环境回退测试密钥并 LogWarning
/// </summary>
public sealed class AesGcmValueConverter : ValueConverter<string?, string?>
{
    private static readonly byte[] FallbackKey = Convert.FromBase64String("MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDE="); // 32B test key

    private static bool IsDevelopment()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
               ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        return string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
    }

    internal static byte[] ResolveKey()
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

        // R-5: 非 Development 环境密钥缺失 fail-fast，防止生产环境静默使用测试密钥
        if (!IsDevelopment())
            throw new InvalidOperationException(
                "AES-GCM 密钥缺失：必须通过环境变量 Security__AesKey 或 Security:AesKey 提供 Base64 编码的 32 字节密钥。" +
                "非 Development 环境禁止回退到内置测试密钥。");

        Trace.TraceWarning(
            "[AesGcmValueConverter] AES-GCM 环境变量密钥缺失，Development 环境回退到内置测试密钥。生产部署必须配置 Security__AesKey。");
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
        catch (CryptographicException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // R-18: 加密失败必须 fail-closed 抛异常，禁止静默返回明文（防敏感数据落库明文）
            throw new CryptographicException("敏感数据加密失败", ex);
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
