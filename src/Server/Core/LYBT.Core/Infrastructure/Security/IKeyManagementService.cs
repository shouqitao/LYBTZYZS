using System.Security.Cryptography.X509Certificates;

namespace LYBT.Core.Infrastructure.Security
{
    /// <summary>
    /// 密钥管理服务接口 - 提供密钥的生成、存储、旋转和管理功能
    /// </summary>
    public interface IKeyManagementService
    {
        /// <summary>
        /// 获取当前JWT签名密钥
        /// </summary>
        Task<string> GetCurrentJwtSecretAsync();

        /// <summary>
        /// 获取所有有效的JWT密钥（包括当前和上一个密钥，用于验证）
        /// </summary>
        Task<IEnumerable<string>> GetValidJwtSecretsAsync();

        /// <summary>
        /// 旋转JWT密钥
        /// </summary>
        Task<string> RotateJwtSecretAsync();

        /// <summary>
        /// 获取数据库连接字符串（解密后）
        /// </summary>
        Task<string> GetDatabaseConnectionStringAsync();

        /// <summary>
        /// 更新数据库连接字符串（加密存储）
        /// </summary>
        Task UpdateDatabaseConnectionStringAsync(string connectionString);

        /// <summary>
        /// 获取API密钥
        /// </summary>
        Task<string> GetApiKeyAsync(string keyName);

        /// <summary>
        /// 生成新的API密钥
        /// </summary>
        Task<string> GenerateApiKeyAsync(string keyName, int expiryDays = 365);

        /// <summary>
        /// 验证API密钥
        /// </summary>
        Task<bool> ValidateApiKeyAsync(string keyName, string apiKey);

        /// <summary>
        /// 获取证书
        /// </summary>
        Task<X509Certificate2?> GetCertificateAsync(string thumbprint);

        /// <summary>
        /// 获取密钥最后旋转时间
        /// </summary>
        Task<DateTime?> GetLastRotationTimeAsync(string keyType);

        /// <summary>
        /// 检查是否需要旋转密钥
        /// </summary>
        Task<bool> IsRotationRequiredAsync(string keyType);

        /// <summary>
        /// 获取密钥元数据
        /// </summary>
        Task<KeyMetadata?> GetKeyMetadataAsync(string keyType);
    }

    /// <summary>
    /// 密钥元数据
    /// </summary>
    public class KeyMetadata
    {
        public string KeyType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastRotatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int RotationIntervalDays { get; set; }
        public string Version { get; set; } = "1.0";
        public bool IsActive { get; set; } = true;
        public Dictionary<string, string> Tags { get; set; } = new();
    }
}