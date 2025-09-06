using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Infrastructure.Configuration {

    /// <summary>
    /// 全局设置实体模型（整合原 Module.Settings）
    /// </summary>
    public class GlobalSettingsModel {

        /// <summary>
        /// ID
        /// </summary>
        [DisplayName("ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 系统名称
        /// </summary>
        [StringLength(100)]
        [DisplayName("系统名称")]
        public string? SystemName { get; set; }

        /// <summary>
        /// 系统版本
        /// </summary>
        [StringLength(20)]
        [DisplayName("系统版本")]
        public string? SystemVersion { get; set; }

        /// <summary>
        /// 系统logo路径
        /// </summary>
        [StringLength(255)]
        [DisplayName("系统logo路径")]
        public string? SystemLogo { get; set; }

        /// <summary>
        /// 默认病历共享模式（Private/Public）
        /// </summary>
        [StringLength(20)]
        [DisplayName("默认病历共享模式")]
        public string DefaultRecordSharing { get; set; } = "Private";

        /// <summary>
        /// 同步模式（Auto/Manual）
        /// </summary>
        [StringLength(20)]
        [DisplayName("同步模式")]
        public string SyncMode { get; set; } = "Auto";

        /// <summary>
        /// 数据备份间隔（小时）
        /// </summary>
        [DisplayName("数据备份间隔（小时）")]
        public int BackupInterval { get; set; } = 24;

        /// <summary>
        /// 日志保留天数
        /// </summary>
        [DisplayName("日志保留天数")]
        public int LogRetentionDays { get; set; } = 90;

        /// <summary>
        /// 会话超时时间（分钟）
        /// </summary>
        [DisplayName("会话超时时间（分钟）")]
        public int SessionTimeoutMinutes { get; set; } = 30;

        /// <summary>
        /// 最大文件上传大小（MB）
        /// </summary>
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
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新者ID
        /// </summary>
        [DisplayName("更新者ID")]
        public Guid? UpdatedBy { get; set; }

        /// <summary>
        /// 更新者姓名
        /// </summary>
        [StringLength(50)]
        [DisplayName("更新者姓名")]
        public string? UpdatedByName { get; set; }
    }
}
