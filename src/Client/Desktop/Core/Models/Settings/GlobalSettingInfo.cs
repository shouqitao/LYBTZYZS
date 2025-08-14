namespace LYBT.Desktop.Core.Models.Settings
{
    /// <summary>
    /// 全局设置信息模型 - 前端专用
    /// </summary>
    public class GlobalSettingInfo
    {
        /// <summary>ID</summary>
        public Guid Id { get; set; }

        /// <summary>系统名称</summary>
        public string? SystemName { get; set; }

        /// <summary>系统版本</summary>
        public string? SystemVersion { get; set; }

        /// <summary>系统logo路径</summary>
        public string? SystemLogo { get; set; }

        /// <summary>默认病历共享模式</summary>
        public string DefaultRecordSharing { get; set; } = "Private";

        /// <summary>同步模式</summary>
        public string SyncMode { get; set; } = "Auto";

        /// <summary>数据备份间隔（小时）</summary>
        public int BackupInterval { get; set; } = 24;

        /// <summary>日志保留天数</summary>
        public int LogRetentionDays { get; set; } = 90;

        /// <summary>会话超时时间（分钟）</summary>
        public int SessionTimeoutMinutes { get; set; } = 30;

        /// <summary>最大文件上传大小（MB）</summary>
        public int MaxFileUploadSizeMB { get; set; } = 10;

        /// <summary>是否启用审计日志</summary>
        public bool EnableAuditLog { get; set; } = true;

        /// <summary>是否启用性能监控</summary>
        public bool EnablePerformanceMonitoring { get; set; } = true;

        /// <summary>最后更新时间</summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>更新者姓名</summary>
        public string? UpdatedByName { get; set; }

        /// <summary>病历共享模式显示名称（前端显示字段）</summary>
        public string RecordSharingDisplayName => GetRecordSharingDisplayName();

        /// <summary>同步模式显示名称（前端显示字段）</summary>
        public string SyncModeDisplayName => GetSyncModeDisplayName();

        /// <summary>备份间隔描述（前端显示字段）</summary>
        public string BackupIntervalText => $"{BackupInterval} 小时";

        /// <summary>日志保留描述（前端显示字段）</summary>
        public string LogRetentionText => $"{LogRetentionDays} 天";

        /// <summary>会话超时描述（前端显示字段）</summary>
        public string SessionTimeoutText => $"{SessionTimeoutMinutes} 分钟";

        /// <summary>文件上传大小描述（前端显示字段）</summary>
        public string MaxFileUploadText => $"{MaxFileUploadSizeMB} MB";

        /// <summary>审计日志状态文本（前端显示字段）</summary>
        public string AuditLogStatusText => EnableAuditLog ? "启用" : "禁用";

        /// <summary>性能监控状态文本（前端显示字段）</summary>
        public string PerformanceMonitoringStatusText => EnablePerformanceMonitoring ? "启用" : "禁用";

        private string GetRecordSharingDisplayName()
        {
            return DefaultRecordSharing switch
            {
                "Private" => "私有",
                "Public" => "公开",
                "Selective" => "选择性共享",
                _ => DefaultRecordSharing
            };
        }

        private string GetSyncModeDisplayName()
        {
            return SyncMode switch
            {
                "Auto" => "自动同步",
                "Manual" => "手动同步",
                "Disabled" => "禁用同步",
                _ => SyncMode
            };
        }
    }
}