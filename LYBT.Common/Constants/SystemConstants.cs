namespace LYBT.Common.Constants {

    /// <summary>
    /// 系统常量定义
    /// </summary>
    public static class SystemConstants {

        /// <summary>
        /// 系统名称
        /// </summary>
        public const string SystemName = "LYBT中医诊所管理系统";

        /// <summary>
        /// 系统版本
        /// </summary>
        public const string Version = "1.0.0";

        /// <summary>
        /// 默认页面大小
        /// </summary>
        public const int DefaultPageSize = 10;

        /// <summary>
        /// 最大页面大小
        /// </summary>
        public const int MaxPageSize = 100;

        /// <summary>
        /// 默认超时时间（秒）
        /// </summary>
        public const int DefaultTimeoutSeconds = 30;

        /// <summary>
        /// 系统管理员用户名
        /// </summary>
        public const string SystemAdminUserName = "admin";

        /// <summary>
        /// 默认密码最小长度
        /// </summary>
        public const int MinPasswordLength = 6;

        /// <summary>
        /// 默认密码最大长度
        /// </summary>
        public const int MaxPasswordLength = 50;
    }

    /// <summary>
    /// 缓存键常量
    /// </summary>
    public static class CacheKeys {

        /// <summary>
        /// 用户信息缓存键前缀
        /// </summary>
        public const string UserPrefix = "user:";

        /// <summary>
        /// 患者信息缓存键前缀
        /// </summary>
        public const string PatientPrefix = "patient:";

        /// <summary>
        /// 医生信息缓存键前缀
        /// </summary>
        public const string DoctorPrefix = "doctor:";

        /// <summary>
        /// 药材信息缓存键前缀
        /// </summary>
        public const string HerbPrefix = "herb:";

        /// <summary>
        /// 处方信息缓存键前缀
        /// </summary>
        public const string PrescriptionPrefix = "prescription:";

        /// <summary>
        /// 系统配置缓存键前缀
        /// </summary>
        public const string ConfigPrefix = "config:";

        /// <summary>
        /// 枚举数据缓存键前缀
        /// </summary>
        public const string EnumPrefix = "enum:";

        /// <summary>
        /// 统计数据缓存键前缀
        /// </summary>
        public const string StatsPrefix = "stats:";
    }

    /// <summary>
    /// API响应消息常量
    /// </summary>
    public static class Messages {

        /// <summary>
        /// 操作成功
        /// </summary>
        public const string Success = "操作成功";

        /// <summary>
        /// 操作失败
        /// </summary>
        public const string Failed = "操作失败";

        /// <summary>
        /// 数据不存在
        /// </summary>
        public const string NotFound = "数据不存在";

        /// <summary>
        /// 参数错误
        /// </summary>
        public const string InvalidParameter = "参数错误";

        /// <summary>
        /// 权限不足
        /// </summary>
        public const string AccessDenied = "权限不足";

        /// <summary>
        /// 登录失败
        /// </summary>
        public const string LoginFailed = "用户名或密码错误";

        /// <summary>
        /// 用户已存在
        /// </summary>
        public const string UserExists = "用户已存在";

        /// <summary>
        /// 密码强度不够
        /// </summary>
        public const string WeakPassword = "密码强度不够";

        /// <summary>
        /// 系统异常
        /// </summary>
        public const string SystemError = "系统异常，请稍后重试";
    }

    /// <summary>
    /// 文件相关常量
    /// </summary>
    public static class FileConstants {

        /// <summary>
        /// 允许上传的图片格式
        /// </summary>
        public static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };

        /// <summary>
        /// 允许上传的文档格式
        /// </summary>
        public static readonly string[] AllowedDocExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv" };

        /// <summary>
        /// 最大文件大小（MB）
        /// </summary>
        public const int MaxFileSizeMB = 10;

        /// <summary>
        /// 默认头像路径
        /// </summary>
        public const string DefaultAvatar = "/images/default-avatar.png";

        /// <summary>
        /// 文件上传目录
        /// </summary>
        public const string UploadDirectory = "uploads";
    }

    /// <summary>
    /// 日期时间格式常量
    /// </summary>
    public static class DateTimeFormats {

        /// <summary>
        /// 标准日期时间格式
        /// </summary>
        public const string StandardDateTime = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// 标准日期格式
        /// </summary>
        public const string StandardDate = "yyyy-MM-dd";

        /// <summary>
        /// 标准时间格式
        /// </summary>
        public const string StandardTime = "HH:mm:ss";

        /// <summary>
        /// 中文日期格式
        /// </summary>
        public const string ChineseDate = "yyyy年MM月dd日";

        /// <summary>
        /// 中文日期时间格式
        /// </summary>
        public const string ChineseDateTime = "yyyy年MM月dd日 HH:mm:ss";

        /// <summary>
        /// 文件名时间戳格式
        /// </summary>
        public const string FileNameTimestamp = "yyyyMMddHHmmss";
    }

    /// <summary>
    /// 正则表达式常量
    /// </summary>
    public static class RegexPatterns {

        /// <summary>
        /// 手机号正则表达式
        /// </summary>
        public const string PhoneNumber = @"^1[3-9]\d{9}$";

        /// <summary>
        /// 身份证号正则表达式
        /// </summary>
        public const string IdCard = @"^\d{17}[\dXx]$";

        /// <summary>
        /// 邮箱正则表达式
        /// </summary>
        public const string Email = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

        /// <summary>
        /// 用户名正则表达式（字母、数字、下划线，3-20位）
        /// </summary>
        public const string UserName = @"^[a-zA-Z0-9_]{3,20}$";

        /// <summary>
        /// 中文姓名正则表达式
        /// </summary>
        public const string ChineseName = @"^[\u4e00-\u9fa5]{2,10}$";

        /// <summary>
        /// IP地址正则表达式
        /// </summary>
        public const string IPAddress = @"^((25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(25[0-5]|2[0-4]\d|[01]?\d\d?)$";
    }
}