using System.Security.Cryptography;
using System.Text;

namespace LYBT.Module.Identity.Services;

/// <summary>
/// 令牌哈希工具 — 提取自 Login/Logout/RefreshToken/ValidateToken 四处重复的 ComputeTokenHash
/// </summary>
public static class TokenHashHelper
{
    public static string ComputeTokenHash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
