using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LYBT.Infrastructure.Options;
using System.Security.Cryptography;

namespace LYBT.Infrastructure.Storage {

    /// <summary>
    /// 本地文件存储服务实现
    /// </summary>
    public class LocalFileStorageService : IFileStorageService {
        private readonly StorageOptions _options;
        private readonly ILogger<LocalFileStorageService> _logger;

        public LocalFileStorageService(
            IOptions<StorageOptions> options,
            ILogger<LocalFileStorageService> logger) {
            _options = options.Value;
            _logger = logger;

            // 确保根目录存在
            EnsureDirectoryExists(_options.LocalStorage.RootPath);
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        public async Task<string> UploadAsync(string fileName, Stream stream, string? contentType = null) {
            try {
                var sanitizedFileName = SanitizeFileName(fileName);
                var relativePath = GenerateFilePath(sanitizedFileName);
                var fullPath = Path.Combine(_options.LocalStorage.RootPath, relativePath);

                // 确保目录存在
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory)) {
                    EnsureDirectoryExists(directory);
                }

                using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
                await stream.CopyToAsync(fileStream);

                _logger.LogInformation("File uploaded successfully: {FilePath}", relativePath);
                return relativePath;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error uploading file: {FileName}", fileName);
                throw;
            }
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        public async Task<Stream?> DownloadAsync(string filePath) {
            try {
                var fullPath = Path.Combine(_options.LocalStorage.RootPath, filePath);
                if (!File.Exists(fullPath)) {
                    return null;
                }

                var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                await Task.CompletedTask;
                return stream;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error downloading file: {FilePath}", filePath);
                return null;
            }
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        public async Task<bool> DeleteAsync(string filePath) {
            try {
                var fullPath = Path.Combine(_options.LocalStorage.RootPath, filePath);
                if (File.Exists(fullPath)) {
                    File.Delete(fullPath);
                    _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                    await Task.CompletedTask;
                    return true;
                }
                return false;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
                return false;
            }
        }

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        public async Task<bool> ExistsAsync(string filePath) {
            try {
                var fullPath = Path.Combine(_options.LocalStorage.RootPath, filePath);
                await Task.CompletedTask;
                return File.Exists(fullPath);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error checking file existence: {FilePath}", filePath);
                return false;
            }
        }

        /// <summary>
        /// 获取文件信息
        /// </summary>
        public async Task<FileMetadata?> GetMetadataAsync(string filePath) {
            try {
                var fullPath = Path.Combine(_options.LocalStorage.RootPath, filePath);
                if (!File.Exists(fullPath)) {
                    return null;
                }

                var fileInfo = new FileInfo(fullPath);
                var hash = await CalculateFileHashAsync(fullPath);

                return new FileMetadata {
                    FilePath = filePath,
                    FileName = fileInfo.Name,
                    Size = fileInfo.Length,
                    ContentType = GetContentType(fileInfo.Extension),
                    CreatedAt = fileInfo.CreationTime,
                    ModifiedAt = fileInfo.LastWriteTime,
                    Hash = hash
                };
            } catch (Exception ex) {
                _logger.LogError(ex, "Error getting file metadata: {FilePath}", filePath);
                return null;
            }
        }

        /// <summary>
        /// 列出目录下的文件
        /// </summary>
        public async Task<IEnumerable<FileMetadata>> ListFilesAsync(string directoryPath, string searchPattern = "*") {
            try {
                var fullPath = Path.Combine(_options.LocalStorage.RootPath, directoryPath);
                if (!Directory.Exists(fullPath)) {
                    return Enumerable.Empty<FileMetadata>();
                }

                var files = Directory.GetFiles(fullPath, searchPattern, SearchOption.TopDirectoryOnly);
                var result = new List<FileMetadata>();

                foreach (var file in files) {
                    var relativePath = Path.GetRelativePath(_options.LocalStorage.RootPath, file);
                    var metadata = await GetMetadataAsync(relativePath);
                    if (metadata != null) {
                        result.Add(metadata);
                    }
                }

                return result;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error listing files in directory: {DirectoryPath}", directoryPath);
                return Enumerable.Empty<FileMetadata>();
            }
        }

        /// <summary>
        /// 复制文件
        /// </summary>
        public async Task<bool> CopyAsync(string sourceFilePath, string destinationFilePath) {
            try {
                var sourceFullPath = Path.Combine(_options.LocalStorage.RootPath, sourceFilePath);
                var destinationFullPath = Path.Combine(_options.LocalStorage.RootPath, destinationFilePath);

                if (!File.Exists(sourceFullPath)) {
                    return false;
                }

                var destinationDirectory = Path.GetDirectoryName(destinationFullPath);
                if (!string.IsNullOrEmpty(destinationDirectory)) {
                    EnsureDirectoryExists(destinationDirectory);
                }

                File.Copy(sourceFullPath, destinationFullPath, true);
                await Task.CompletedTask;
                return true;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error copying file from {Source} to {Destination}", sourceFilePath, destinationFilePath);
                return false;
            }
        }

        /// <summary>
        /// 移动文件
        /// </summary>
        public async Task<bool> MoveAsync(string sourceFilePath, string destinationFilePath) {
            try {
                var sourceFullPath = Path.Combine(_options.LocalStorage.RootPath, sourceFilePath);
                var destinationFullPath = Path.Combine(_options.LocalStorage.RootPath, destinationFilePath);

                if (!File.Exists(sourceFullPath)) {
                    return false;
                }

                var destinationDirectory = Path.GetDirectoryName(destinationFullPath);
                if (!string.IsNullOrEmpty(destinationDirectory)) {
                    EnsureDirectoryExists(destinationDirectory);
                }

                File.Move(sourceFullPath, destinationFullPath);
                await Task.CompletedTask;
                return true;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error moving file from {Source} to {Destination}", sourceFilePath, destinationFilePath);
                return false;
            }
        }

        /// <summary>
        /// 生成文件访问URL（本地存储不支持）
        /// </summary>
        public async Task<string?> GenerateAccessUrlAsync(string filePath, TimeSpan? expiry = null) {
            await Task.CompletedTask;
            // 本地存储可以返回相对路径或null
            return filePath;
        }

        #region 私有方法

        /// <summary>
        /// 确保目录存在
        /// </summary>
        private static void EnsureDirectoryExists(string directoryPath) {
            if (!Directory.Exists(directoryPath)) {
                Directory.CreateDirectory(directoryPath);
            }
        }

        /// <summary>
        /// 清理文件名
        /// </summary>
        private static string SanitizeFileName(string fileName) {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// 生成文件路径
        /// </summary>
        private string GenerateFilePath(string fileName) {
            var timestamp = DateTime.UtcNow.ToString("yyyy/MM/dd");
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var extension = Path.GetExtension(fileName);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            
            return Path.Combine(timestamp, $"{nameWithoutExt}_{uniqueId}{extension}");
        }

        /// <summary>
        /// 获取内容类型
        /// </summary>
        private static string? GetContentType(string extension) {
            return extension.ToLower() switch {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".html" => "text/html",
                ".json" => "application/json",
                ".xml" => "application/xml",
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// 计算文件哈希值
        /// </summary>
        private static async Task<string> CalculateFileHashAsync(string filePath) {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var md5 = MD5.Create();
            var hash = await md5.ComputeHashAsync(stream);
            return Convert.ToHexString(hash);
        }

        #endregion
    }
}