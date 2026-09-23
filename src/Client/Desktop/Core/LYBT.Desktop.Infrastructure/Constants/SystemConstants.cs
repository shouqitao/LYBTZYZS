using System.IO;
using LYBT.Shared.Models.Primitives;

namespace LYBT.Desktop.Infrastructure.Constants
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
        /// 应用程序版本（运行时从程序集元数据读取，单源 = Directory.Build.props 的 VersionPrefix）
        /// </summary>
        public static string ApplicationVersion => AppVersion.Current;

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
        /// 超级管理员用户名（权威定义见 LYBT.Shared.Models.Primitives.UserConstants）
        /// </summary>
        public const string SuperAdminUsername = UserConstants.SysAdminUsername;

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
        /// 用户数据根目录（<c>%LOCALAPPDATA%\LYBT\Desktop</c>）。
        /// </summary>
        /// <remarks>
        /// <para><b>必须位于应用安装目录之外</b>：经 Velopack 安装时，安装根为
        /// <c>%LOCALAPPDATA%\{packId}</c>（即 <c>%LOCALAPPDATA%\LYBTZYZS</c>），
        /// 更新/卸载会管理该目录内容——把用户设置或备份放在其中会在更新或卸载时丢失。</para>
        /// <para>本目录同时是凭据、照片、系统设置、首次运行标记的既有约定目录
        /// （<c>CredentialStorage</c>/<c>DpapiPhotoStorageService</c>/<c>UsernameStorageService</c>/
        /// <c>LoginViewModel</c>/<c>SystemSettingsService</c>），此处收敛为单一常量。</para>
        /// </remarks>
        public static string UserDataDirectory => AppDataPaths.DesktopDataDirectory;


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
    }
}
