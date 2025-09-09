using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LYBT.Desktop.Services
{

    /// <summary>
    /// 凭据管理服务 - 安全地保存和加载用户凭据
    /// </summary>
    public class CredentialService : ICredentialService
    {
        private readonly string _credentialFilePath;
        private readonly byte[] _entropy;

        public CredentialService()
        {
            // 获取应用程序数据目录
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appDirectory = Path.Combine(appDataPath, "LYBT.WPF.Client");

            if (!Directory.Exists(appDirectory))
            {
                Directory.CreateDirectory(appDirectory);
            }

            _credentialFilePath = Path.Combine(appDirectory, "credentials.dat");

            // 使用固定的熵值（在实际应用中，这个值应该更复杂）
            _entropy = Encoding.UTF8.GetBytes("LYBT-Credential-Entropy-2024");
        }

        /// <summary>
        /// 保存用户凭据
        /// </summary>
        public void SaveCredentials(string username, string password, bool rememberMe)
        {
            try
            {
                if (!rememberMe)
                {
                    // 如果不记住密码，删除已保存的凭据
                    DeleteCredentials();
                    return;
                }

                var credentials = new SavedCredentials
                {
                    Username = username,
                    Password = password,
                    RememberMe = rememberMe,
                    SavedAt = DateTime.Now
                };

                var json = JsonSerializer.Serialize(credentials);
                var data = Encoding.UTF8.GetBytes(json);

                // 使用Windows数据保护API加密
                var encryptedData = ProtectedData.Protect(data, _entropy, DataProtectionScope.CurrentUser);

                File.WriteAllBytes(_credentialFilePath, encryptedData);
            }
            catch (Exception ex)
            {
                // 记录错误但不抛出异常，避免影响登录流程
                System.Diagnostics.Debug.WriteLine($"保存凭据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载保存的凭据
        /// </summary>
        public SavedCredentials? LoadCredentials()
        {
            try
            {
                if (!File.Exists(_credentialFilePath))
                {
                    return null;
                }

                var encryptedData = File.ReadAllBytes(_credentialFilePath);

                // 解密数据
                var data = ProtectedData.Unprotect(encryptedData, _entropy, DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(data);

                var credentials = JsonSerializer.Deserialize<SavedCredentials>(json);

                // 检查凭据是否过期（30天）
                if (credentials != null && (DateTime.Now - credentials.SavedAt).TotalDays > 30)
                {
                    DeleteCredentials();
                    return null;
                }

                return credentials;
            }
            catch (Exception ex)
            {
                // 如果解密失败，删除损坏的文件
                System.Diagnostics.Debug.WriteLine($"加载凭据失败: {ex.Message}");
                DeleteCredentials();
                return null;
            }
        }

        /// <summary>
        /// 删除保存的凭据
        /// </summary>
        public void DeleteCredentials()
        {
            try
            {
                if (File.Exists(_credentialFilePath))
                {
                    File.Delete(_credentialFilePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除凭据失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 凭据服务接口
    /// </summary>
    public interface ICredentialService
    {

        void SaveCredentials(string username, string password, bool rememberMe);

        SavedCredentials? LoadCredentials();

        void DeleteCredentials();
    }

    /// <summary>
    /// 保存的凭据信息
    /// </summary>
    public class SavedCredentials
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
        public DateTime SavedAt { get; set; }
    }
}
