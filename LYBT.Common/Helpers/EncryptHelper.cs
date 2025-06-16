using System.Security.Cryptography;
using System.Text;

namespace LYBT.Infrastructure.Utilities {

    public static class EncryptHelper {

        /// <summary>
        /// 对字符串进行 MD5 哈希，不建议用于密码等安全场景
        /// </summary>
        public static string ToMd5(string input) {
            using var md5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = md5.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// 使用 SHA256 进行更安全的哈希运算，适用于密码哈希等场景
        /// </summary>
        public static string ToSha256(string input) {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}