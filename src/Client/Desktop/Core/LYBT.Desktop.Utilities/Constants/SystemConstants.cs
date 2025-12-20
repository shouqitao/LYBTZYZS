namespace LYBT.Desktop.Utilities.Constants
{
    /// <summary>
    /// 系统常量
    /// UltraThink架构优化 - 统一系统配置
    /// </summary>
    public static class SystemConstants
    {
        /// <summary>
        /// 应用程序名称
        /// </summary>
        public const string ApplicationName = "凌隐宝堂中医诊所管理系统";

        /// <summary>
        /// 系统标题
        /// </summary>
        public const string SystemTitle = "凌隐宝堂中医诊所管理系统";

        /// <summary>
        /// 应用程序版本
        /// </summary>
        public const string ApplicationVersion = "2.0.0";

        /// <summary>
        /// 配置文件名称
        /// </summary>
        public const string ConfigFileName = "appsettings.json";

        /// <summary>
        /// 默认语言
        /// </summary>
        public const string DefaultLanguage = "zh-CN";

        /// <summary>
        /// 默认主题
        /// </summary>
        public const string DefaultTheme = "Light";

        /// <summary>
        /// 超级管理员用户名
        /// </summary>
        public const string SuperAdminUsername = "sysadmin";

        /// <summary>
        /// 会话超时时间（分钟）
        /// </summary>
        public const int SessionTimeoutMinutes = 30;

        /// <summary>
        /// 最大登录尝试次数
        /// </summary>
        public const int MaxLoginAttempts = 3;

        /// <summary>
        /// 默认页面大小
        /// </summary>
        public const int DefaultPageSize = 20;

        /// <summary>
        /// 最大页面大小
        /// </summary>
        public const int MaxPageSize = 100;

        /// <summary>
        /// 文件上传最大大小（MB）
        /// </summary>
        public const int MaxFileUploadSizeMB = 10;

        /// <summary>
        /// 备份文件保留天数
        /// </summary>
        public const int BackupRetentionDays = 30;

        /// <summary>
        /// 日志文件保留天数
        /// </summary>
        public const int LogRetentionDays = 7;

        /// <summary>
        /// 自动保存间隔（秒）
        /// </summary>
        public const int AutoSaveIntervalSeconds = 60;

        /// <summary>
        /// API连接超时（毫秒）
        /// </summary>
        public const int ApiTimeoutMilliseconds = 30000;

        /// <summary>
        /// 数据库连接超时（秒）
        /// </summary>
        public const int DatabaseTimeoutSeconds = 30;

        /// <summary>
        /// 默认密码策略
        /// </summary>
        public static class PasswordPolicy
        {
            public const int MinLength = 8;
            public const int MaxLength = 128;
            public const bool RequireUppercase = true;
            public const bool RequireLowercase = true;
            public const bool RequireDigit = true;
            public const bool RequireSpecialChar = true;
        }

        /// <summary>
        /// 文件路径
        /// </summary>
        public static class FilePaths
        {
            public const string ConfigDirectory = "Config";
            public const string LogDirectory = "Logs";
            public const string TempDirectory = "Temp";
            public const string BackupDirectory = "Backup";
            public const string ExportDirectory = "Export";
        }

        /// <summary>
        /// 文件扩展名
        /// </summary>
        public static class FileExtensions
        {
            public const string Config = ".json";
            public const string Log = ".log";
            public const string Backup = ".bak";
            public const string Export = ".xlsx";
            public const string Pdf = ".pdf";
        }

        /// <summary>
        /// 错误代码
        /// </summary>
        public static class ErrorCodes
        {
            public const string AuthenticationFailed = "AUTH_001";
            public const string AuthorizationFailed = "AUTH_002";
            public const string SessionExpired = "AUTH_003";
            public const string InvalidInput = "VALID_001";
            public const string DataNotFound = "DATA_001";
            public const string DatabaseError = "DB_001";
            public const string NetworkError = "NET_001";
            public const string UnknownError = "SYS_001";
        }
    }
}
