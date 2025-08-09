using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LYBT.Infrastructure.Configuration
{
    /// <summary>
    /// 秘钥管理器接口
    /// </summary>
    public interface ISecretManager
    {
        /// <summary>
        /// 获取秘钥
        /// </summary>
        string GetSecret(string key);

        /// <summary>
        /// 设置秘钥
        /// </summary>
        Task SetSecretAsync(string key, string value);

        /// <summary>
        /// 删除秘钥
        /// </summary>
        Task DeleteSecretAsync(string key);

        /// <summary>
        /// 加密字符串
        /// </summary>
        string Encrypt(string plainText);

        /// <summary>
        /// 解密字符串
        /// </summary>
        string Decrypt(string encryptedText);

        /// <summary>
        /// 验证秘钥完整性
        /// </summary>
        bool ValidateSecrets();
    }

    /// <summary>
    /// 秘钥管理器实现
    /// </summary>
    public class SecretManager : ISecretManager
    {
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;
        private readonly string _secretsPath;
        private readonly byte[] _encryptionKey;

        public SecretManager(IConfiguration configuration, IHostEnvironment environment)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            
            _secretsPath = Path.Combine(environment.ContentRootPath, "secrets.json");
            _encryptionKey = GetOrCreateEncryptionKey();
        }

        /// <summary>
        /// 获取秘钥
        /// </summary>
        public string GetSecret(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            // 首先尝试从环境变量获取
            var envValue = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(envValue))
            {
                return envValue;
            }

            // 然后尝试从配置文件获取
            var configValue = _configuration[key];
            if (!string.IsNullOrEmpty(configValue))
            {
                return ProcessConfigValue(configValue);
            }

            // 最后尝试从加密的秘钥文件获取
            var secrets = LoadSecretsFromFile();
            if (secrets.ContainsKey(key))
            {
                return Decrypt(secrets[key]);
            }

            // 如果是生产环境且找不到必需的秘钥，抛出异常
            if (_environment.IsProduction() && IsRequiredSecret(key))
            {
                throw new InvalidOperationException($"生产环境中必需的秘钥 '{key}' 未找到");
            }

            return string.Empty;
        }

        /// <summary>
        /// 设置秘钥
        /// </summary>
        public async Task SetSecretAsync(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value));

            var secrets = LoadSecretsFromFile();
            secrets[key] = Encrypt(value);
            await SaveSecretsToFileAsync(secrets);
        }

        /// <summary>
        /// 删除秘钥
        /// </summary>
        public async Task DeleteSecretAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            var secrets = LoadSecretsFromFile();
            if (secrets.ContainsKey(key))
            {
                secrets.Remove(key);
                await SaveSecretsToFileAsync(secrets);
            }
        }

        /// <summary>
        /// 加密字符串
        /// </summary>
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                using var aes = Aes.Create();
                aes.Key = _encryptionKey;
                
                var iv = aes.IV;
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                
                using var encryptor = aes.CreateEncryptor();
                var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                
                // 组合IV和加密数据
                var result = new byte[iv.Length + encryptedBytes.Length];
                Array.Copy(iv, 0, result, 0, iv.Length);
                Array.Copy(encryptedBytes, 0, result, iv.Length, encryptedBytes.Length);
                
                return Convert.ToBase64String(result);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("加密失败", ex);
            }
        }

        /// <summary>
        /// 解密字符串
        /// </summary>
        public string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            try
            {
                var encryptedBytes = Convert.FromBase64String(encryptedText);
                
                using var aes = Aes.Create();
                aes.Key = _encryptionKey;
                
                // 提取IV
                var iv = new byte[aes.IV.Length];
                Array.Copy(encryptedBytes, 0, iv, 0, iv.Length);
                aes.IV = iv;
                
                // 提取加密数据
                var encrypted = new byte[encryptedBytes.Length - iv.Length];
                Array.Copy(encryptedBytes, iv.Length, encrypted, 0, encrypted.Length);
                
                using var decryptor = aes.CreateDecryptor();
                var decryptedBytes = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
                
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("解密失败", ex);
            }
        }

        /// <summary>
        /// 验证秘钥完整性
        /// </summary>
        public bool ValidateSecrets()
        {
            try
            {
                var requiredSecrets = GetRequiredSecrets();
                foreach (var secretKey in requiredSecrets)
                {
                    var value = GetSecret(secretKey);
                    if (string.IsNullOrEmpty(value))
                    {
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 处理配置值（支持环境变量替换）
        /// </summary>
        private string ProcessConfigValue(string configValue)
        {
            if (string.IsNullOrEmpty(configValue))
                return configValue;

            // 处理 ${VAR_NAME} 格式的环境变量
            var pattern = @"\$\{([^}]+)\}";
            return System.Text.RegularExpressions.Regex.Replace(configValue, pattern, match =>
            {
                var envVarName = match.Groups[1].Value;
                var envValue = Environment.GetEnvironmentVariable(envVarName);
                
                if (string.IsNullOrEmpty(envValue))
                {
                    if (_environment.IsProduction())
                    {
                        throw new InvalidOperationException($"生产环境中环境变量 '{envVarName}' 未设置");
                    }
                    return match.Value; // 开发环境返回原始值
                }
                
                return envValue;
            });
        }

        /// <summary>
        /// 获取或创建加密密钥
        /// </summary>
        private byte[] GetOrCreateEncryptionKey()
        {
            var keyPath = Path.Combine(_environment.ContentRootPath, ".encryption-key");
            
            if (File.Exists(keyPath))
            {
                var keyData = File.ReadAllText(keyPath);
                return Convert.FromBase64String(keyData);
            }
            
            // 创建新的加密密钥
            using var aes = Aes.Create();
            aes.GenerateKey();
            var keyBase64 = Convert.ToBase64String(aes.Key);
            
            // 保存密钥（仅开发环境）
            if (_environment.IsDevelopment())
            {
                File.WriteAllText(keyPath, keyBase64);
            }
            
            return aes.Key;
        }

        /// <summary>
        /// 从文件加载秘钥
        /// </summary>
        private Dictionary<string, string> LoadSecretsFromFile()
        {
            if (!File.Exists(_secretsPath))
            {
                return new Dictionary<string, string>();
            }

            try
            {
                var json = File.ReadAllText(_secretsPath);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) 
                       ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// 保存秘钥到文件
        /// </summary>
        private async Task SaveSecretsToFileAsync(Dictionary<string, string> secrets)
        {
            var json = JsonSerializer.Serialize(secrets, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_secretsPath, json);
        }

        /// <summary>
        /// 判断是否为必需的秘钥
        /// </summary>
        private bool IsRequiredSecret(string key)
        {
            var requiredSecrets = GetRequiredSecrets();
            return requiredSecrets.Contains(key, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 获取必需的秘钥列表
        /// </summary>
        private List<string> GetRequiredSecrets()
        {
            return new List<string>
            {
                "JWT_SECRET",
                "ConnectionStrings:DefaultConnection",
                "ADMIN_DEFAULT_PASSWORD",
                "USER_DEFAULT_PASSWORD"
            };
        }
    }
}