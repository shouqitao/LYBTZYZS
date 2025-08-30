using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// 安全凭据服务 - 使用加密存储凭据
    /// </summary>
    public class SecureCredentialService : ICredentialService
    {
        private readonly ILogger<SecureCredentialService>? _logger;
        private readonly string _credentialFilePath;
        private readonly byte[] _entropy;
        
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
                    // 安全删除：先覆盖再删除
                    var size = new FileInfo(_credentialFilePath).Length;
                    var random = new byte[size];
                    using (var rng = RandomNumberGenerator.Create())
                    {
                        rng.GetBytes(random);
                    }
                    File.WriteAllBytes(_credentialFilePath, random);
                    File.Delete(_credentialFilePath);
                    
                    _logger?.LogDebug("成功清除凭据");
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
                return File.ReadAllBytes(entropyFile);
            }
            
            // 生成新的熵值
            var entropy = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(entropy);
            }
            
            File.WriteAllBytes(entropyFile, entropy);
            
            // 隐藏文件
            File.SetAttributes(entropyFile, FileAttributes.Hidden | FileAttributes.System);
            
            return entropy;
        }
        
        public void DeleteCredentials()
        {
            try
            {
                if (File.Exists(_credentialFilePath))
                {
                    File.Delete(_credentialFilePath);
                    _logger?.LogDebug("凭据已删除");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "删除凭据失败");
            }
        }
    }
}