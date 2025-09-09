using System.Security.Cryptography;
using System.Text;
using LYBT.Entities.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// 数据加密服务 - Epic 05-P0-03: 数据安全保障
    /// 提供敏感数据的加密、解密和脱敏功能
    /// </summary>
    public interface IDataEncryptionService
    {
        /// <summary>
        /// 加密敏感数据
        /// </summary>
        /// <param name="plaintext">明文</param>
        /// <param name="dataType">敏感数据类型</param>
        /// <returns>加密后的密文</returns>
        string Encrypt(string plaintext, SensitiveDataType dataType = SensitiveDataType.PersonalInfo);

        /// <summary>
        /// 解密敏感数据
        /// </summary>
        /// <param name="ciphertext">密文</param>
        /// <param name="dataType">敏感数据类型</param>
        /// <returns>解密后的明文</returns>
        string Decrypt(string ciphertext, SensitiveDataType dataType = SensitiveDataType.PersonalInfo);

        /// <summary>
        /// 数据脱敏
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="maskingMode">脱敏模式</param>
        /// <returns>脱敏后的数据</returns>
        string MaskData(string data, MaskingMode maskingMode);
    }

    /// <summary>
    /// 数据加密服务实现
    /// </summary>
    public class DataEncryptionService : IDataEncryptionService, IDisposable
    {
        private readonly ILogger<DataEncryptionService> _logger;
        private readonly string _encryptionKey;
        private readonly Dictionary<SensitiveDataType, byte[]> _typeSpecificKeys;
        private bool _disposed = false;

        public DataEncryptionService(IConfiguration configuration, ILogger<DataEncryptionService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // 从配置中获取主加密密钥
            _encryptionKey = configuration["Security:EncryptionKey"] 
                ?? throw new InvalidOperationException("未配置数据加密密钥");

            // 验证密钥长度
            if (_encryptionKey.Length < 32)
            {
                throw new InvalidOperationException("加密密钥长度必须至少32字符");
            }

            // 为不同类型的敏感数据生成专用密钥
            _typeSpecificKeys = GenerateTypeSpecificKeys();
            
            _logger.LogInformation("数据加密服务初始化完成，支持 {DataTypes} 种敏感数据类型", 
                Enum.GetValues<SensitiveDataType>().Length);
        }

        public string Encrypt(string plaintext, SensitiveDataType dataType = SensitiveDataType.PersonalInfo)
        {
            if (string.IsNullOrEmpty(plaintext))
                return plaintext;

            try
            {
                using var aes = Aes.Create();
                aes.Key = _typeSpecificKeys[dataType];
                aes.GenerateIV();

                using var encryptor = aes.CreateEncryptor();
                using var msEncrypt = new MemoryStream();
                using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
                using var swEncrypt = new StreamWriter(csEncrypt);

                swEncrypt.Write(plaintext);
                swEncrypt.Close();

                // 返回 IV + 密文 的 Base64 编码
                var iv = aes.IV;
                var encrypted = msEncrypt.ToArray();
                var result = new byte[iv.Length + encrypted.Length];
                Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                Buffer.BlockCopy(encrypted, 0, result, iv.Length, encrypted.Length);

                var base64Result = Convert.ToBase64String(result);
                _logger.LogDebug("敏感数据加密完成，数据类型: {DataType}, 长度: {Length}", 
                    dataType, plaintext.Length);
                
                return base64Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加密敏感数据失败，数据类型: {DataType}", dataType);
                throw new InvalidOperationException("数据加密失败", ex);
            }
        }

        public string Decrypt(string ciphertext, SensitiveDataType dataType = SensitiveDataType.PersonalInfo)
        {
            if (string.IsNullOrEmpty(ciphertext))
                return ciphertext;

            try
            {
                var buffer = Convert.FromBase64String(ciphertext);
                
                using var aes = Aes.Create();
                aes.Key = _typeSpecificKeys[dataType];

                // 提取 IV（前16字节）
                var iv = new byte[16];
                Buffer.BlockCopy(buffer, 0, iv, 0, iv.Length);
                aes.IV = iv;

                // 提取密文（剩余字节）
                var encrypted = new byte[buffer.Length - iv.Length];
                Buffer.BlockCopy(buffer, iv.Length, encrypted, 0, encrypted.Length);

                using var decryptor = aes.CreateDecryptor();
                using var msDecrypt = new MemoryStream(encrypted);
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var srDecrypt = new StreamReader(csDecrypt);

                var result = srDecrypt.ReadToEnd();
                _logger.LogDebug("敏感数据解密完成，数据类型: {DataType}", dataType);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解密敏感数据失败，数据类型: {DataType}", dataType);
                throw new InvalidOperationException("数据解密失败", ex);
            }
        }

        public string MaskData(string data, MaskingMode maskingMode)
        {
            if (string.IsNullOrEmpty(data))
                return data;

            return maskingMode switch
            {
                MaskingMode.Default => MaskDefault(data),
                MaskingMode.Partial => MaskPartial(data),
                MaskingMode.Full => new string('*', Math.Min(8, data.Length)),
                MaskingMode.Hash => GetHashMask(data),
                _ => data
            };
        }

        private Dictionary<SensitiveDataType, byte[]> GenerateTypeSpecificKeys()
        {
            var keys = new Dictionary<SensitiveDataType, byte[]>();
            var masterKey = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));

            foreach (SensitiveDataType dataType in Enum.GetValues<SensitiveDataType>())
            {
                // 为每种数据类型生成专用密钥
                using var hmac = new HMACSHA256(masterKey);
                var typeBytes = Encoding.UTF8.GetBytes(dataType.ToString());
                var derivedKey = hmac.ComputeHash(typeBytes);
                keys[dataType] = derivedKey.Take(32).ToArray(); // AES-256 需要32字节密钥
            }

            return keys;
        }

        private static string MaskDefault(string data)
        {
            if (data.Length <= 2)
                return "*";

            var visibleChars = Math.Max(1, data.Length / 4);
            var start = data[..visibleChars];
            var end = data[^visibleChars..];
            var maskLength = data.Length - (2 * visibleChars);
            
            return $"{start}{new string('*', maskLength)}{end}";
        }

        private static string MaskPartial(string data)
        {
            return data.Length switch
            {
                <= 3 => "*",
                <= 6 => $"{data[0]}***{data[^1]}",
                <= 11 => $"{data[..2]}****{data[^2..]}",
                _ => $"{data[..3]}******{data[^3..]}"
            };
        }

        private static string GetHashMask(string data)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            var hashString = Convert.ToHexString(hashBytes);
            return $"#{hashString[..8]}";
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // 清理敏感数据
                foreach (var key in _typeSpecificKeys.Values)
                {
                    Array.Clear(key, 0, key.Length);
                }
                _typeSpecificKeys.Clear();
                
                _disposed = true;
                _logger.LogInformation("数据加密服务已释放资源");
            }
        }
    }
}