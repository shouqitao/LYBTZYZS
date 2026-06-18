using System.Security.Cryptography;
using System.Text;

namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// DPAPI加密保护器 - 封装 Windows 数据保护API 调用和 HMAC 完整性计算
    ///
    /// 安全特性：
    /// - 使用 DPAPI（DataProtectionScope.CurrentUser）加解密
    /// - 只有当前 Windows 用户能解密
    /// - HMAC-SHA256 密钥基于机器名+用户名派生，防止跨机复制
    /// </summary>
    internal sealed class DpapiProtector
    {
        private readonly byte[] _hmacKeySource;

        public DpapiProtector()
        {
            // 派生HMAC密钥源（基于机器和用户上下文）
            var keyMaterial = $"LYBT_VAULT_{Environment.MachineName}_{Environment.UserName}";
            _hmacKeySource = SHA256.HashData(Encoding.UTF8.GetBytes(keyMaterial));
        }

        /// <summary>
        /// 使用DPAPI加密明文字符串，返回Base64编码的密文
        /// </summary>
        public string Encrypt(string plainText)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = ProtectedData.Protect(
                plainBytes,
                null,
                DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// 使用DPAPI解密Base64密文字符串，返回明文
        /// </summary>
        public string Decrypt(string encryptedBase64)
        {
            var encryptedBytes = Convert.FromBase64String(encryptedBase64);
            var decryptedBytes = ProtectedData.Unprotect(
                encryptedBytes,
                null,
                DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        /// <summary>
        /// 计算HMAC-SHA256完整性校验值（Base64）
        /// </summary>
        /// <param name="username">用户名（参与签名，不区分大小写）</param>
        /// <param name="encryptedData">已加密的Base64数据</param>
        public string ComputeHmac(string username, string encryptedData)
        {
            var dataToSign = $"{username.ToLowerInvariant()}:{encryptedData}";
            var dataBytes = Encoding.UTF8.GetBytes(dataToSign);

            using var hmac = new HMACSHA256(_hmacKeySource);
            var hashBytes = hmac.ComputeHash(dataBytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
