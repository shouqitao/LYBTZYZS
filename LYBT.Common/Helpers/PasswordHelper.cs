using System;
using System.Security.Cryptography;
using System.Text;

namespace LYBT.Common.Helpers {
    /// <summary>
    /// 密码加密与校验辅助类
    /// </summary>
    public static class PasswordHelper {
        /// <summary>
        /// 对明文密码进行哈希加密（SHA256）
        /// </summary>
        /// <param name="password">明文密码</param>
        /// <returns>加密后的哈希字符串</returns>
        public static string HashPassword(string password) {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// 校验明文密码与加密密码是否匹配
        /// </summary>
        /// <param name="plainPassword">用户输入的明文密码</param>
        /// <param name="hashPassword">数据库中保存的加密密码</param>
        /// <returns>是否匹配</returns>
        public static bool VerifyPassword(string plainPassword, string hashPassword) {
            return HashPassword(plainPassword) == hashPassword;
        }
    }
}
