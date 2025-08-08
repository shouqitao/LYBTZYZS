namespace LYBT.Infrastructure.Options
{

    /// <summary>
    /// 存储配置选项
    /// </summary>
    public class StorageOptions
    {

        /// <summary>
        /// 存储类型：Local, Azure, AWS, MinIO
        /// </summary>
        public string StorageType { get; set; } = "Local";

        /// <summary>
        /// 本地存储配置
        /// </summary>
        public LocalStorageConfig LocalStorage { get; set; } = new();

        /// <summary>
        /// Azure Blob存储配置
        /// </summary>
        public AzureStorageConfig AzureStorage { get; set; } = new();

        /// <summary>
        /// AWS S3存储配置
        /// </summary>
        public AwsStorageConfig AwsStorage { get; set; } = new();

        /// <summary>
        /// MinIO存储配置
        /// </summary>
        public MinIOStorageConfig MinIOStorage { get; set; } = new();

        /// <summary>
        /// 文件上传限制
        /// </summary>
        public UploadLimits UploadLimits { get; set; } = new();

        /// <summary>
        /// 安全配置
        /// </summary>
        public StorageSecurityConfig Security { get; set; } = new();
    }

    /// <summary>
    /// 本地存储配置
    /// </summary>
    public class LocalStorageConfig
    {

        /// <summary>
        /// 根路径
        /// </summary>
        public string RootPath { get; set; } = "uploads";

        /// <summary>
        /// 是否创建按日期分组的子目录
        /// </summary>
        public bool CreateDateFolders { get; set; } = true;

        /// <summary>
        /// 是否启用文件去重
        /// </summary>
        public bool EnableDeduplication { get; set; } = false;
    }

    /// <summary>
    /// Azure存储配置
    /// </summary>
    public class AzureStorageConfig
    {

        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// 容器名称
        /// </summary>
        public string ContainerName { get; set; } = "uploads";

        /// <summary>
        /// 是否启用CDN
        /// </summary>
        public bool EnableCdn { get; set; } = false;

        /// <summary>
        /// CDN端点
        /// </summary>
        public string? CdnEndpoint { get; set; }
    }

    /// <summary>
    /// AWS S3存储配置
    /// </summary>
    public class AwsStorageConfig
    {

        /// <summary>
        /// 访问密钥ID
        /// </summary>
        public string AccessKeyId { get; set; } = string.Empty;

        /// <summary>
        /// 密钥
        /// </summary>
        public string SecretAccessKey { get; set; } = string.Empty;

        /// <summary>
        /// 区域
        /// </summary>
        public string Region { get; set; } = "us-east-1";

        /// <summary>
        /// 存储桶名称
        /// </summary>
        public string BucketName { get; set; } = "uploads";

        /// <summary>
        /// 是否使用HTTPS
        /// </summary>
        public bool UseHttps { get; set; } = true;
    }

    /// <summary>
    /// MinIO存储配置
    /// </summary>
    public class MinIOStorageConfig
    {

        /// <summary>
        /// 服务端点
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// 访问密钥
        /// </summary>
        public string AccessKey { get; set; } = string.Empty;

        /// <summary>
        /// 密钥
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// 存储桶名称
        /// </summary>
        public string BucketName { get; set; } = "uploads";

        /// <summary>
        /// 是否使用SSL
        /// </summary>
        public bool UseSSL { get; set; } = true;
    }

    /// <summary>
    /// 文件上传限制
    /// </summary>
    public class UploadLimits
    {

        /// <summary>
        /// 最大文件大小（MB）
        /// </summary>
        public int MaxFileSizeMB { get; set; } = 10;

        /// <summary>
        /// 允许的文件扩展名
        /// </summary>
        public List<string> AllowedExtensions { get; set; } = new() {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp",
            ".pdf", ".doc", ".docx", ".xls", ".xlsx",
            ".txt", ".csv", ".zip", ".rar"
        };

        /// <summary>
        /// 禁止的文件扩展名
        /// </summary>
        public List<string> ForbiddenExtensions { get; set; } = new() {
            ".exe", ".bat", ".cmd", ".com", ".scr",
            ".vbs", ".js", ".jar", ".class"
        };

        /// <summary>
        /// 每日上传限制（MB）
        /// </summary>
        public int DailyUploadLimitMB { get; set; } = 100;

        /// <summary>
        /// 单用户最大文件数量
        /// </summary>
        public int MaxFilesPerUser { get; set; } = 1000;
    }

    /// <summary>
    /// 存储安全配置
    /// </summary>
    public class StorageSecurityConfig
    {

        /// <summary>
        /// 是否启用病毒扫描
        /// </summary>
        public bool EnableVirusScanning { get; set; } = false;

        /// <summary>
        /// 是否启用文件内容验证
        /// </summary>
        public bool EnableContentValidation { get; set; } = true;

        /// <summary>
        /// 是否启用文件加密
        /// </summary>
        public bool EnableEncryption { get; set; } = false;

        /// <summary>
        /// 加密密钥
        /// </summary>
        public string? EncryptionKey { get; set; }

        /// <summary>
        /// 是否生成缩略图
        /// </summary>
        public bool GenerateThumbnails { get; set; } = true;

        /// <summary>
        /// 缩略图大小
        /// </summary>
        public int ThumbnailSize { get; set; } = 150;
    }
}