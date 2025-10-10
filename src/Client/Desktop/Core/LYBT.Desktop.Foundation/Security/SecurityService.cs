using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Security;

/// <summary>
/// 安全服务接口 - 简化版本，遵循适度设计原则
/// </summary>
public interface ISecurityService
{
    /// <summary>
    /// 加密敏感数据
    /// </summary>
    Task<string> EncryptAsync(string plainText);

    /// <summary>
    /// 解密敏感数据
    /// </summary>
    Task<string> DecryptAsync(string encryptedText);

    /// <summary>
    /// 生成密钥哈希
    /// </summary>
    string GenerateHash(string input);

    /// <summary>
    /// 验证密钥哈希
    /// </summary>
    bool VerifyHash(string input, string hash);
}

/// <summary>
/// 安全服务实现 - 简化版本，避免过度工程
/// </summary>
public class SecurityService : ISecurityService
{
    private readonly ILogger<SecurityService> _logger;
    private readonly byte[] _key;

    public SecurityService(ILogger<SecurityService> logger)
    {
        _logger = logger;
        _key = GetOrCreateKey();
    }

    public async Task<string> EncryptAsync(string plainText)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = await Task.Run(() => encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length));

            // 组合IV和密文
            var result = new byte[aes.IV.Length + encryptedBytes.Length];
            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加密失败");
            throw;
        }
    }

    public async Task<string> DecryptAsync(string encryptedText)
    {
        try
        {
            var data = Convert.FromBase64String(encryptedText);

            using var aes = Aes.Create();
            aes.Key = _key;

            // 提取IV
            var iv = new byte[16];
            var encryptedBytes = new byte[data.Length - 16];
            Array.Copy(data, 0, iv, 0, 16);
            Array.Copy(data, 16, encryptedBytes, 0, encryptedBytes.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = await Task.Run(() => decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length));

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解密失败");
            throw;
        }
    }

    public string GenerateHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public bool VerifyHash(string input, string hash)
    {
        var computedHash = GenerateHash(input);
        return computedHash == hash;
    }

    private byte[] GetOrCreateKey()
    {
        // 简化的密钥管理：使用固定长度的密钥
        // 在实际生产环境中，这应该来自安全的密钥管理服务
        var keyString = $"{Environment.MachineName}-{Environment.UserName}-LYBT-2025";
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
    }
}
