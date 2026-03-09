using System.IO;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Security;

/// <summary>
/// C2: DPAPI 照片加密存储服务
/// 使用 DataProtectionScope.CurrentUser 加密，仅当前 Windows 用户可解密
/// 存储路径: %LOCALAPPDATA%\LYBT\Desktop\Photos\{identifier}.enc
/// </summary>
public class DpapiPhotoStorageService : IPhotoStorageService
{
    private readonly ILogger<DpapiPhotoStorageService> _logger;
    private readonly string _photoDirectory;

    public DpapiPhotoStorageService(ILogger<DpapiPhotoStorageService> logger)
    {
        _logger = logger;

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _photoDirectory = Path.Combine(appDataPath, "LYBT", "Desktop", "Photos");

        if (!Directory.Exists(_photoDirectory))
        {
            Directory.CreateDirectory(_photoDirectory);
        }
    }

    /// <summary>
    /// 加密并保存照片到文件
    /// </summary>
    public Task<string> SavePhotoAsync(byte[] photoData, string identifier)
    {
        ArgumentNullException.ThrowIfNull(photoData);
        if (photoData.Length == 0)
            throw new ArgumentException("照片数据不能为空", nameof(photoData));
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("标识符不能为空", nameof(identifier));

        return Task.Run(() =>
        {
            try
            {
                var encryptedData = ProtectedData.Protect(
                    photoData,
                    null,
                    DataProtectionScope.CurrentUser);

                var safeFileName = ComputeFileHash(identifier);
                var filePath = Path.Combine(_photoDirectory, $"{safeFileName}.enc");

                File.WriteAllBytes(filePath, encryptedData);

                _logger.LogInformation("照片已加密保存: {FilePath} ({Size} bytes -> {EncSize} bytes)",
                    filePath, photoData.Length, encryptedData.Length);

                return filePath;
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "DPAPI 加密照片失败");
                throw;
            }
        });
    }

    /// <summary>
    /// 加载并解密照片
    /// </summary>
    public Task<byte[]?> LoadPhotoAsync(string encryptedFilePath)
    {
        if (string.IsNullOrWhiteSpace(encryptedFilePath))
            return Task.FromResult<byte[]?>(null);

        return Task.Run<byte[]?>(() =>
        {
            try
            {
                if (!File.Exists(encryptedFilePath))
                {
                    _logger.LogDebug("加密照片文件不存在: {FilePath}", encryptedFilePath);
                    return null;
                }

                var encryptedData = File.ReadAllBytes(encryptedFilePath);
                var decryptedData = ProtectedData.Unprotect(
                    encryptedData,
                    null,
                    DataProtectionScope.CurrentUser);

                _logger.LogDebug("照片已解密加载: {FilePath} ({Size} bytes)", encryptedFilePath, decryptedData.Length);
                return decryptedData;
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "DPAPI 解密照片失败 (可能是其他 Windows 用户的数据): {FilePath}", encryptedFilePath);
                return null;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "读取加密照片文件失败: {FilePath}", encryptedFilePath);
                return null;
            }
        });
    }

    /// <summary>
    /// 删除加密照片文件
    /// </summary>
    public Task<bool> DeletePhotoAsync(string encryptedFilePath)
    {
        if (string.IsNullOrWhiteSpace(encryptedFilePath))
            return Task.FromResult(false);

        return Task.Run(() =>
        {
            try
            {
                if (!File.Exists(encryptedFilePath))
                    return true;

                File.Delete(encryptedFilePath);
                _logger.LogInformation("已删除加密照片: {FilePath}", encryptedFilePath);
                return true;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "删除加密照片失败: {FilePath}", encryptedFilePath);
                return false;
            }
        });
    }

    /// <summary>
    /// 检查加密照片是否存在
    /// </summary>
    public bool PhotoExists(string encryptedFilePath)
    {
        return !string.IsNullOrWhiteSpace(encryptedFilePath) && File.Exists(encryptedFilePath);
    }

    /// <summary>
    /// 计算安全文件名 (SHA256 哈希前16字符)
    /// </summary>
    private static string ComputeFileHash(string identifier)
    {
        var hashBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(identifier));
        return Convert.ToHexString(hashBytes)[..16].ToLowerInvariant();
    }
}
