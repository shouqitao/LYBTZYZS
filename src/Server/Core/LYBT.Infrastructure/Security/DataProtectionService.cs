using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// 数据保护服务实现 - 使用ASP.NET Core Data Protection API
    /// </summary>
    public class DataProtectionService : IDataProtectionService
    {
        private readonly IDataProtector _protector;
        private readonly IDataProtectionProvider _provider;
        private readonly ILogger<DataProtectionService> _logger;

        // 用于不同目的的保护器
        private readonly IDataProtector _secretsProtector;
        private readonly IDataProtector _connectionStringProtector;
        private readonly IDataProtector _personalDataProtector;

        public DataProtectionService(
            IDataProtectionProvider provider,
            ILogger<DataProtectionService> logger)
        {
            _provider = provider;
            _logger = logger;

            // 创建不同用途的保护器
            _protector = _provider.CreateProtector("LYBT.General.v1");
            _secretsProtector = _provider.CreateProtector("LYBT.Secrets.v1");
            _connectionStringProtector = _provider.CreateProtector("LYBT.ConnectionStrings.v1");
            _personalDataProtector = _provider.CreateProtector("LYBT.PersonalData.v1");
        }

        /// <summary>
        /// 保护（加密）数据
        /// </summary>
        public string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                throw new ArgumentNullException(nameof(plainText));
            }

            try
            {
                return _secretsProtector.Protect(plainText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据保护失败");
                throw new CryptographicException("数据保护失败", ex);
            }
        }

        /// <summary>
        /// 解除保护（解密）数据
        /// </summary>
        public string Unprotect(string protectedText)
        {
            if (string.IsNullOrEmpty(protectedText))
            {
                throw new ArgumentNullException(nameof(protectedText));
            }

            try
            {
                return _secretsProtector.Unprotect(protectedText);
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "数据解密失败 - 密钥可能已更改或数据已损坏");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据解除保护失败");
                throw new CryptographicException("数据解除保护失败", ex);
            }
        }

        /// <summary>
        /// 保护数据并设置过期时间
        /// </summary>
        public string ProtectWithExpiry(string plainText, TimeSpan expiry)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                throw new ArgumentNullException(nameof(plainText));
            }

            try
            {
                var timeLimitedProtector = _secretsProtector.ToTimeLimitedDataProtector();
                return timeLimitedProtector.Protect(plainText, expiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "带过期时间的数据保护失败");
                throw new CryptographicException("带过期时间的数据保护失败", ex);
            }
        }

        /// <summary>
        /// 尝试解除保护（不抛出异常）
        /// </summary>
        public bool TryUnprotect(string protectedText, out string? plainText)
        {
            plainText = null;

            if (string.IsNullOrEmpty(protectedText))
            {
                return false;
            }

            try
            {
                plainText = _secretsProtector.Unprotect(protectedText);
                return true;
            }
            catch (CryptographicException ex)
            {
                _logger.LogWarning(ex, "数据解密失败（预期的失败）");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "尝试解除保护时发生意外错误");
                return false;
            }
        }

        #region 专用保护方法

        /// <summary>
        /// 保护连接字符串
        /// </summary>
        public string ProtectConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            try
            {
                return _connectionStringProtector.Protect(connectionString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "连接字符串保护失败");
                throw;
            }
        }

        /// <summary>
        /// 解除连接字符串保护
        /// </summary>
        public string UnprotectConnectionString(string protectedConnectionString)
        {
            if (string.IsNullOrEmpty(protectedConnectionString))
            {
                throw new ArgumentNullException(nameof(protectedConnectionString));
            }

            try
            {
                return _connectionStringProtector.Unprotect(protectedConnectionString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "连接字符串解密失败");
                throw;
            }
        }

        /// <summary>
        /// 保护个人数据（符合GDPR要求）
        /// </summary>
        public string ProtectPersonalData(string personalData)
        {
            if (string.IsNullOrEmpty(personalData))
            {
                throw new ArgumentNullException(nameof(personalData));
            }

            try
            {
                return _personalDataProtector.Protect(personalData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "个人数据保护失败");
                throw;
            }
        }

        /// <summary>
        /// 解除个人数据保护
        /// </summary>
        public string UnprotectPersonalData(string protectedPersonalData)
        {
            if (string.IsNullOrEmpty(protectedPersonalData))
            {
                throw new ArgumentNullException(nameof(protectedPersonalData));
            }

            try
            {
                return _personalDataProtector.Unprotect(protectedPersonalData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "个人数据解密失败");
                throw;
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 生成加密密钥
        /// </summary>
        public static byte[] GenerateKey(int keySizeInBits = 256)
        {
            using var rng = RandomNumberGenerator.Create();
            var key = new byte[keySizeInBits / 8];
            rng.GetBytes(key);
            return key;
        }

        /// <summary>
        /// 生成初始化向量
        /// </summary>
        public static byte[] GenerateIV(int blockSizeInBits = 128)
        {
            using var rng = RandomNumberGenerator.Create();
            var iv = new byte[blockSizeInBits / 8];
            rng.GetBytes(iv);
            return iv;
        }

        /// <summary>
        /// 计算数据的哈希值
        /// </summary>
        public static string ComputeHash(string data, HashAlgorithmName algorithm = default)
        {
            if (algorithm == default)
            {
                algorithm = HashAlgorithmName.SHA256;
            }

            HashAlgorithm hashAlgorithm = algorithm.Name switch
            {
                "SHA256" => SHA256.Create(),
                "SHA384" => SHA384.Create(),
                "SHA512" => SHA512.Create(),
                _ => SHA256.Create()
            };

            using (hashAlgorithm)
            {
                var bytes = Encoding.UTF8.GetBytes(data);
                var hash = hashAlgorithm.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        #endregion
    }
}