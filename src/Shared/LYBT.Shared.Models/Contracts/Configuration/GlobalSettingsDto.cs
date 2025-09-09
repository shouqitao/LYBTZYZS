using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Configuration
{

    /// <summary>
    /// 全局设置传输对象
    /// </summary>
    public class GlobalSettingsDto
    {

        /// <summary>
        /// ID
        /// </summary>
        [DisplayName("ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 系统名称
        /// </summary>
        [StringLength(100, ErrorMessage = "系统名称长度不能超过100个字符")]
        [DisplayName("系统名称")]
        public string? SystemName { get; set; }

        /// <summary>
        /// 系统版本
        /// </summary>
        [StringLength(20, ErrorMessage = "系统版本长度不能超过20个字符")]
        [DisplayName("系统版本")]
        public string? SystemVersion { get; set; }

        /// <summary>
        /// 系统logo路径
        /// </summary>
        [StringLength(255, ErrorMessage = "logo路径长度不能超过255个字符")]
        [DisplayName("系统logo路径")]
        public string? SystemLogo { get; set; }

        /// <summary>
        /// 默认病历共享模式
        /// </summary>
        [Required(ErrorMessage = "默认病历共享模式不能为空")]
        [StringLength(20, ErrorMessage = "共享模式长度不能超过20个字符")]
        [DisplayName("默认病历共享模式")]
        public string DefaultRecordSharing { get; set; } = "Private";

        /// <summary>
        /// 同步模式
        /// </summary>
        [Required(ErrorMessage = "同步模式不能为空")]
        [StringLength(20, ErrorMessage = "同步模式长度不能超过20个字符")]
        [DisplayName("同步模式")]
        public string SyncMode { get; set; } = "Auto";

        /// <summary>
        /// 数据备份间隔（小时）
        /// </summary>
        [Range(1, 168, ErrorMessage = "备份间隔必须在1-168小时之间")]
        [DisplayName("数据备份间隔（小时）")]
        public int BackupInterval { get; set; } = 24;

        /// <summary>
        /// 日志保留天数
        /// </summary>
        [Range(1, 365, ErrorMessage = "日志保留天数必须在1-365天之间")]
        [DisplayName("日志保留天数")]
        public int LogRetentionDays { get; set; } = 90;

        /// <summary>
        /// 会话超时时间（分钟）
        /// </summary>
        [Range(5, 1440, ErrorMessage = "会话超时时间必须在5-1440分钟之间")]
        [DisplayName("会话超时时间（分钟）")]
        public int SessionTimeoutMinutes { get; set; } = 30;

        /// <summary>
        /// 最大文件上传大小（MB）
        /// </summary>
        [Range(1, 100, ErrorMessage = "文件上传大小必须在1-100MB之间")]
        [DisplayName("最大文件上传大小（MB）")]
        public int MaxFileUploadSizeMB { get; set; } = 10;

        /// <summary>
        /// 是否启用审计日志
        /// </summary>
        [DisplayName("是否启用审计日志")]
        public bool EnableAuditLog { get; set; } = true;

        /// <summary>
        /// 是否启用性能监控
        /// </summary>
        [DisplayName("是否启用性能监控")]
        public bool EnablePerformanceMonitoring { get; set; } = true;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        [DisplayName("最后更新时间")]
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// 更新者姓名
        /// </summary>
        [DisplayName("更新者姓名")]
        public string? UpdatedByName { get; set; }
    }
}
