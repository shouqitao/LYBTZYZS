using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// 数据加密服务 - UltraThink重构安全加固
    /// 提供AES-256加密、哈希和数字签名功能
    /// </summary>
    public class EncryptionService : IEncryptionService
    {
        private readonly string _encryptionKey;
        private readonly string _initializationVector;
        
        public EncryptionService(IConfiguration configuration)
        {
            _encryptionKey = configuration["Security:EncryptionKey"] ?? GenerateSecureKey();
            _initializationVector = configuration["Security:InitializationVector"] ?? GenerateSecureIV();
        }

        /// <summary>
        /// AES-256加密
        /// </summary>
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                using var aes = Aes.Create();
                aes.Key = Convert.FromBase64String(_encryptionKey);
                aes.IV = Convert.FromBase64String(_initializationVector);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor();
                using var msEncrypt = new MemoryStream();
                using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
                using var swEncrypt = new StreamWriter(csEncrypt);
                
                swEncrypt.Write(plainText);
                swEncrypt.Close();
                
                return Convert.ToBase64String(msEncrypt.ToArray());
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"加密失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// AES-256解密
        /// </summary>
        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                using var aes = Aes.Create();
                aes.Key = Convert.FromBase64String(_encryptionKey);
                aes.IV = Convert.FromBase64String(_initializationVector);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                using var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText));
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var srDecrypt = new StreamReader(csDecrypt);
                
                return srDecrypt.ReadToEnd();
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"解密失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// SHA-256哈希
        /// </summary>
        public string Hash(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashedBytes);
        }

        /// <summary>
        /// HMAC-SHA256签名
        /// </summary>
        public string Sign(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(signature);
        }

        /// <summary>
        /// 验证HMAC-SHA256签名
        /// </summary>
        public bool VerifySignature(string data, string signature, string key)
        {
            var expectedSignature = Sign(data, key);
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(signature),
                Convert.FromBase64String(expectedSignature));
        }

        /// <summary>
        /// 生成安全随机密钥
        /// </summary>
        public string GenerateSecureKey()
        {
            using var rng = RandomNumberGenerator.Create();
            var keyBytes = new byte[32]; // 256 bits
            rng.GetBytes(keyBytes);
            return Convert.ToBase64String(keyBytes);
        }

        /// <summary>
        /// 生成安全随机初始化向量
        /// </summary>
        private string GenerateSecureIV()
        {
            using var rng = RandomNumberGenerator.Create();
            var ivBytes = new byte[16]; // 128 bits
            rng.GetBytes(ivBytes);
            return Convert.ToBase64String(ivBytes);
        }

        /// <summary>
        /// 加密敏感配置项
        /// </summary>
        public string EncryptConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return connectionString;

            // 对密码部分进行特殊处理
            var parts = connectionString.Split(';');
            var encryptedParts = new List<string>();

            foreach (var part in parts)
            {
                if (part.ToLower().Contains("password=") || part.ToLower().Contains("pwd="))
                {
                    var keyValue = part.Split('=');
                    if (keyValue.Length == 2)
                    {
                        var encryptedValue = Encrypt(keyValue[1]);
                        encryptedParts.Add($"{keyValue[0]}={encryptedValue}");
                    }
                    else
                    {
                        encryptedParts.Add(part);
                    }
                }
                else
                {
                    encryptedParts.Add(part);
                }
            }

            return string.Join(";", encryptedParts);
        }

        /// <summary>
        /// 解密敏感配置项
        /// </summary>
        public string DecryptConnectionString(string encryptedConnectionString)
        {
            if (string.IsNullOrEmpty(encryptedConnectionString))
                return encryptedConnectionString;

            var parts = encryptedConnectionString.Split(';');
            var decryptedParts = new List<string>();

            foreach (var part in parts)
            {
                if (part.ToLower().Contains("password=") || part.ToLower().Contains("pwd="))
                {
                    var keyValue = part.Split('=');
                    if (keyValue.Length == 2)
                    {
                        try
                        {
                            var decryptedValue = Decrypt(keyValue[1]);
                            decryptedParts.Add($"{keyValue[0]}={decryptedValue}");
                        }
                        catch
                        {
                            // 如果解密失败，说明可能不是加密的，直接使用原值
                            decryptedParts.Add(part);
                        }
                    }
                    else
                    {
                        decryptedParts.Add(part);
                    }
                }
                else
                {
                    decryptedParts.Add(part);
                }
            }

            return string.Join(";", decryptedParts);
        }
    }
}