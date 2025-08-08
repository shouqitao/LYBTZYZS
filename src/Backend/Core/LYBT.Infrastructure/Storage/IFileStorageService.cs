namespace LYBT.Infrastructure.Storage
{

    /// <summary>
    /// 文件存储服务接口
    /// </summary>
    public interface IFileStorageService
    {

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="stream">文件流</param>
        /// <param name="contentType">内容类型</param>
        /// <returns>文件路径或URL</returns>
        Task<string> UploadAsync(string fileName, Stream stream, string? contentType = null);

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件流</returns>
        Task<Stream?> DownloadAsync(string filePath);

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否成功</returns>
        Task<bool> DeleteAsync(string filePath);

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否存在</returns>
        Task<bool> ExistsAsync(string filePath);

        /// <summary>
        /// 获取文件信息
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件信息</returns>
        Task<FileMetadata?> GetMetadataAsync(string filePath);

        /// <summary>
        /// 列出目录下的文件
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <param name="searchPattern">搜索模式</param>
        /// <returns>文件列表</returns>
        Task<IEnumerable<FileMetadata>> ListFilesAsync(string directoryPath, string searchPattern = "*");

        /// <summary>
        /// 复制文件
        /// </summary>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="destinationFilePath">目标文件路径</param>
        /// <returns>是否成功</returns>
        Task<bool> CopyAsync(string sourceFilePath, string destinationFilePath);

        /// <summary>
        /// 移动文件
        /// </summary>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="destinationFilePath">目标文件路径</param>
        /// <returns>是否成功</returns>
        Task<bool> MoveAsync(string sourceFilePath, string destinationFilePath);

        /// <summary>
        /// 生成文件访问URL（仅适用于云存储）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="expiry">过期时间</param>
        /// <returns>访问URL</returns>
        Task<string?> GenerateAccessUrlAsync(string filePath, TimeSpan? expiry = null);
    }

    /// <summary>
    /// 文件元数据
    /// </summary>
    public class FileMetadata
    {

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// 内容类型
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime ModifiedAt { get; set; }

        /// <summary>
        /// 文件哈希值
        /// </summary>
        public string? Hash { get; set; }

        /// <summary>
        /// 自定义标签
        /// </summary>
        public Dictionary<string, string> Tags { get; set; } = new();
    }
}