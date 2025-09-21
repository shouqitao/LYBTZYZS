using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services
{

    /// <summary>
    /// 安全凭据服务 - 使用DPAPI+随机熵加密存储凭据
    /// 统一的安全凭据实现，取代所有弱加密版本
    /// </summary>
    public class SecureCredentialService : ICredentialService
    {
        private readonly ILogger<SecureCredentialService>? _logger;
        private readonly string _credentialFilePath;
        private readonly byte[] _entropy;
        private const int ENTROPY_SIZE = 64; // 增强熵值长度到512位

        public SecureCredentialService(ILogger<SecureCredentialService>? logger = null)
        {
            _logger = logger;

            // 获取用户数据目录
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appDirectory = Path.Combine(appDataPath, "LYBT", "Credentials");
            Directory.CreateDirectory(appDirectory);

            _credentialFilePath = Path.Combine(appDirectory, "user.cred");

            // 生成或加载熵值（增加安全性）
            _entropy = GenerateOrLoadEntropy(appDirectory);
        }

        /// <inheritdoc/>
        public SavedCredentials? LoadCredentials()
        {
            try
            {
                if (!File.Exists(_credentialFilePath))
                {
                    _logger?.LogDebug("凭据文件不存在");
                    return null;
                }

                var encryptedData = File.ReadAllBytes(_credentialFilePath);
                var decryptedData = ProtectedData.Unprotect(encryptedData, _entropy, DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(decryptedData);

                var credentials = JsonSerializer.Deserialize<SavedCredentials>(json);
                _logger?.LogDebug("成功加载保存的凭据");

                return credentials;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载凭据失败");
                return null;
            }
        }

        /// <inheritdoc/>
        public void SaveCredentials(string username, string password, bool rememberMe)
        {
            try
            {
                var credentials = new SavedCredentials
                {
                    Username = username,
                    Password = rememberMe ? password : string.Empty,
                    RememberMe = rememberMe,
                    SavedAt = DateTime.Now
                };

                var json = JsonSerializer.Serialize(credentials);
                var data = Encoding.UTF8.GetBytes(json);
                var encryptedData = ProtectedData.Protect(data, _entropy, DataProtectionScope.CurrentUser);

                File.WriteAllBytes(_credentialFilePath, encryptedData);
                _logger?.LogDebug("成功保存凭据");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存凭据失败");
            }
        }

        public void ClearCredentials()
        {
            try
            {
                if (File.Exists(_credentialFilePath))
                {
                    // 安全删除：多次覆盖后删除（DoD 5220.22-M 标准）
                    SecureDeleteFile(_credentialFilePath);
                    _logger?.LogDebug("成功安全清除凭据");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "清除凭据失败");
            }
        }

        private byte[] GenerateOrLoadEntropy(string directory)
        {
            var entropyFile = Path.Combine(directory, ".entropy");

            if (File.Exists(entropyFile))
            {
                try
                {
                    // 使用 DPAPI 保护熵值文件
                    var encryptedEntropy = File.ReadAllBytes(entropyFile);
                    return ProtectedData.Unprotect(encryptedEntropy, null, DataProtectionScope.CurrentUser);
                }
                catch
                {
                    // 如果解密失败（用户变更），重新生成
                    _logger?.LogWarning("熵值解密失败，将重新生成");
                }
            }

            // 生成增强的随机熵值
            var entropy = new byte[ENTROPY_SIZE];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(entropy);
            }

            // 使用 DPAPI 保护熵值
            var protectedEntropy = ProtectedData.Protect(entropy, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(entropyFile, protectedEntropy);

            // 设置文件属性
            File.SetAttributes(entropyFile, FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReadOnly);

            return entropy;
        }

        /// <inheritdoc/>
        public void DeleteCredentials()
        {
            ClearCredentials(); // 使用安全清除方法
        }

        /// <summary>
        /// 安全删除文件 - DoD 5220.22-M 标准
        /// </summary>
        private void SecureDeleteFile(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            var fileInfo = new FileInfo(filePath);
            var fileSize = fileInfo.Length;

            // 三次覆盖：0x00, 0xFF, 随机数据
            byte[][] patterns = new byte[][]
            {
                new byte[fileSize],  // 0x00
                Enumerable.Repeat((byte)0xFF, (int)fileSize).ToArray(),  // 0xFF
                new byte[fileSize]    // 随机
            };

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(patterns[2]);
            }

            foreach (var pattern in patterns)
            {
                File.WriteAllBytes(filePath, pattern);
            }

            // 最后删除文件
            File.Delete(filePath);
        }

        /// <summary>
        /// 检测并清理旧版本凭据文件
        /// </summary>
        public void MigrateAndCleanupLegacyCredentials()
        {
            try
            {
                // 旧版本路径
                var legacyPaths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "LYBT.WPF.Client", "credentials.dat"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "LYBT", "credentials.dat")
                };

                foreach (var legacyPath in legacyPaths)
                {
                    if (File.Exists(legacyPath))
                    {
                        _logger?.LogInformation("发现旧版本凭据文件，正在安全清理: {Path}", legacyPath);
                        SecureDeleteFile(legacyPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "清理旧版本凭据失败");
            }
        }
    }
}
