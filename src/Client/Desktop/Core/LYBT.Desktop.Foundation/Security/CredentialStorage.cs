using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// 凭据文件存储 - 管理 vault.dat / credentials.dat 文件的加载、保存、路径解析
    ///
    /// 存储路径: %LOCALAPPDATA%\LYBT\Desktop\vault.dat
    /// </summary>
    internal sealed class CredentialStorage
    {
        private readonly string _vaultFilePath;
        private readonly string _oldCredentialsPath;
        private readonly object _lock = new();
        private readonly ILogger _logger;

        /// <summary>
        /// vault.dat 文件完整路径
        /// </summary>
        public string VaultFilePath => _vaultFilePath;

        /// <summary>
        /// 旧版 credentials.dat 文件完整路径
        /// </summary>
        public string OldCredentialsFilePath => _oldCredentialsPath;

        public CredentialStorage(ILogger logger)
        {
            _logger = logger;

            // 存储路径: %LOCALAPPDATA%\LYBT\Desktop\vault.dat
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var lybtFolder = Path.Combine(appDataPath, "LYBT", "Desktop");

            // 确保目录存在
            if (!Directory.Exists(lybtFolder))
            {
                Directory.CreateDirectory(lybtFolder);
            }

            _vaultFilePath = Path.Combine(lybtFolder, "vault.dat");
            _oldCredentialsPath = Path.Combine(lybtFolder, "credentials.dat");
        }

        /// <summary>
        /// 加载Vault数据（线程安全，文件不存在或格式错误返回null）
        /// </summary>
        public Task<VaultStorage?> LoadVaultAsync()
        {
            return Task.Run(() =>
            {
                lock (_lock)
                {
                    if (!File.Exists(_vaultFilePath))
                    {
                        return null;
                    }

                    try
                    {
                        var json = File.ReadAllText(_vaultFilePath, Encoding.UTF8);
                        return JsonSerializer.Deserialize<VaultStorage>(json);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Vault文件格式错误，将重置");
                        return null;
                    }
                }
            });
        }

        /// <summary>
        /// 保存Vault数据（线程安全）
        /// </summary>
        public Task SaveVaultAsync(VaultStorage vault)
        {
            return Task.Run(() =>
            {
                var json = JsonSerializer.Serialize(vault, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                lock (_lock)
                {
                    File.WriteAllText(_vaultFilePath, json, Encoding.UTF8);
                }
            });
        }

        /// <summary>
        /// 删除Vault文件（清除所有凭据），返回文件是否曾存在
        /// </summary>
        public bool DeleteVaultFile()
        {
            lock (_lock)
            {
                if (File.Exists(_vaultFilePath))
                {
                    File.Delete(_vaultFilePath);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 检查Vault文件是否存在
        /// </summary>
        public bool VaultFileExists() => File.Exists(_vaultFilePath);

        /// <summary>
        /// 检查旧格式凭据文件是否存在
        /// </summary>
        public bool OldCredentialsFileExists() => File.Exists(_oldCredentialsPath);

        /// <summary>
        /// 读取并反序列化旧格式凭据文件
        /// </summary>
        public async Task<OldCredentialFormat?> ReadOldCredentialsAsync()
        {
            var json = await File.ReadAllTextAsync(_oldCredentialsPath, Encoding.UTF8);
            return JsonSerializer.Deserialize<OldCredentialFormat>(json);
        }
    }

    /// <summary>
    /// Vault存储结构
    /// </summary>
    internal sealed class VaultStorage
    {
        public int Version { get; set; } = 1;
        public List<VaultEntry> Entries { get; set; } = new();
        public bool MigratedFromOldFormat { get; set; }
        public string? MigrationUsername { get; set; }
    }

    /// <summary>
    /// 单个用户的Vault条目
    /// </summary>
    internal sealed class VaultEntry
    {
        public string Username { get; set; } = string.Empty;
        public string EncryptedAutoLoginToken { get; set; } = string.Empty;
        public string Hmac { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string EncryptedPassword { get; set; } = string.Empty;
        public string PasswordHmac { get; set; } = string.Empty;
        public DateTime? PasswordSavedAt { get; set; }
    }

    /// <summary>
    /// 旧格式凭据结构（用于迁移）
    /// </summary>
    internal sealed class OldCredentialFormat
    {
        public string Username { get; set; } = string.Empty;
        public string EncryptedPassword { get; set; } = string.Empty;
        public bool RememberPassword { get; set; }
    }
}
