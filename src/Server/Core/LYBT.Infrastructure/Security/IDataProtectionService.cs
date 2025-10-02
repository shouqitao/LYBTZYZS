namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// 数据保护服务接口 - 提供数据的加密和解密功能
    /// </summary>
    public interface IDataProtectionService
    {
        /// <summary>
        /// 保护（加密）数据
        /// </summary>
        string Protect(string plainText);

        /// <summary>
        /// 解除保护（解密）数据
        /// </summary>
        string Unprotect(string protectedText);

        /// <summary>
        /// 保护数据并设置过期时间
        /// </summary>
        string ProtectWithExpiry(string plainText, TimeSpan expiry);

        /// <summary>
        /// 尝试解除保护（不抛出异常）
        /// </summary>
        bool TryUnprotect(string protectedText, out string? plainText);
    }
}
