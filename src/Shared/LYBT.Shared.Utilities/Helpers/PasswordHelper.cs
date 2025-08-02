using Microsoft.AspNetCore.Identity;
using System.ComponentModel;

namespace LYBT.Shared.Utilities.Helpers {

    /// <summary>
    /// 提供密码哈希工具，使用ASP.NET Core Identity实现
    /// </summary>
    [Description("密码工具类")]
    public static class PasswordHelper {
        private static readonly PasswordHasher<object> _hasher = new();

        /// <summary>
        /// 对明文密码进行哈希
        /// </summary>
        /// <param name="password">明文密码</param>
        /// <returns>哈希后的密码</returns>
        public static string Hash(string password) {
            return _hasher.HashPassword(null!, password);
        }

        /// <summary>
        /// 验证密码与存储的哈希是否匹配
        /// </summary>
        /// <param name="hash">存储的密码哈希</param>
        /// <param name="password">待验证的明文密码</param>
        /// <returns>验证结果</returns>
        public static bool Verify(string hash, string password) {
            var result = _hasher.VerifyHashedPassword(null!, hash, password);
            return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}