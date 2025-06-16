using System;
using System.Security.Cryptography;
using System.Text;

namespace LYBT.Infrastructure.Helpers {
    /// <summary>
    /// 密码加密帮助类（使用 SHA256）
    /// </summary>
    public static class EncryptHelper {
        public static string HashPassword(string password) {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        public static bool VerifyPassword(string password, string hashed) {
            return HashPassword(password) == hashed;
        }
    }
}
