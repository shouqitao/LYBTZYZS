using System.Security.Cryptography;
using System.Text;

namespace LYBT.Infrastructure.Serialization;

/// <summary>
/// 敏感字段确定性哈希（R-6 盲索引）。
/// AES-GCM 为非确定性加密（随机 nonce），无法用于 SQL 等值查询；
/// 本 helper 用与加密同一密钥做 HMAC-SHA256，产生可索引的确定性摘要。
/// </summary>
public static class SensitiveDataHashHelper
{
    /// <summary>
    /// 计算明文的 HMAC-SHA256（小写 hex，64 字符）。空值返回 null。
    /// </summary>
    public static string? ComputeHmacSha256Hex(string? plain)
    {
        if (string.IsNullOrEmpty(plain))
            return null;

        var key = AesGcmValueConverter.ResolveKey();
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(plain));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
