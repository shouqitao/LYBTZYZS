using LYBT.Shared.Models.Contracts.Common;
using System;

namespace LYBT.Desktop.Core.Models.Backup
{
    /// <summary>
    /// 数据备份信息
    /// </summary>
    public class BackupInfo
    {
        /// <summary>备份ID</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>备份名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>备份类型</summary>
        public BackupType Type { get; set; }

        /// <summary>备份文件路径</summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>文件大小（字节）</summary>
        public long FileSize { get; set; }

        /// <summary>备份时间</summary>
        public DateTime BackupTime { get; set; } = DateTime.Now;

        /// <summary>备份说明</summary>
        public string? Description { get; set; }

        /// <summary>操作员</summary>
        public string Operator { get; set; } = string.Empty;

        /// <summary>备份状态</summary>
        public BackupStatus Status { get; set; }

        /// <summary>是否自动备份</summary>
        public bool IsAutoBackup { get; set; }

        /// <summary>数据库版本</summary>
        public string? DatabaseVersion { get; set; }

        /// <summary>应用版本</summary>
        public string? AppVersion { get; set; }

        #region 显示属性

        /// <summary>备份类型名称</summary>
        public string TypeName => GetTypeName();

        /// <summary>状态名称</summary>
        public string StatusName => GetStatusName();

        /// <summary>状态颜色</summary>
        public string StatusColor => GetStatusColor();

        /// <summary>文件大小显示</summary>
        public string FileSizeDisplay => FormatFileSize();

        /// <summary>是否可以恢复</summary>
        public bool CanRestore => Status == BackupStatus.Success;

        /// <summary>是否可以删除</summary>
        public bool CanDelete => Status != BackupStatus.InProgress;

        #endregion

        #region 私有方法

        private string GetTypeName()
        {
            return Type switch
            {
                BackupType.Full => "完全备份",
                BackupType.Incremental => "增量备份",
                BackupType.Differential => "差异备份",
                BackupType.Manual => "手动备份",
                BackupType.Scheduled => "计划备份",
                _ => "未知类型"
            };
        }

        private string GetStatusName()
        {
            return Status switch
            {
                BackupStatus.InProgress => "备份中",
                BackupStatus.Success => "成功",
                BackupStatus.Failed => "失败",
                BackupStatus.Cancelled => "已取消",
                BackupStatus.Verifying => "验证中",
                BackupStatus.Verified => "已验证",
                _ => "未知"
            };
        }

        private string GetStatusColor()
        {
            return Status switch
            {
                BackupStatus.InProgress => "#007BFF",
                BackupStatus.Success => "#28A745",
                BackupStatus.Failed => "#DC3545",
                BackupStatus.Cancelled => "#6C757D",
                BackupStatus.Verifying => "#17A2B8",
                BackupStatus.Verified => "#28A745",
                _ => "#6C757D"
            };
        }

        private string FormatFileSize()
        {
            if (FileSize < 1024)
                return $"{FileSize} B";
            else if (FileSize < 1024 * 1024)
                return $"{FileSize / 1024.0:F2} KB";
            else if (FileSize < 1024 * 1024 * 1024)
                return $"{FileSize / (1024.0 * 1024.0):F2} MB";
            else
                return $"{FileSize / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }

        #endregion
    }

    /// <summary>
    /// 备份类型枚举
    /// </summary>
    public enum BackupType
    {
        /// <summary>完全备份</summary>
        Full = 0,

        /// <summary>增量备份</summary>
        Incremental = 1,

        /// <summary>差异备份</summary>
        Differential = 2,

        /// <summary>手动备份</summary>
        Manual = 3,

        /// <summary>计划备份</summary>
        Scheduled = 4
    }

    /// <summary>
    /// 备份状态枚举
    /// </summary>
    public enum BackupStatus
    {
        /// <summary>备份中</summary>
        InProgress = 0,

        /// <summary>成功</summary>
        Success = 1,

        /// <summary>失败</summary>
        Failed = 2,

        /// <summary>已取消</summary>
        Cancelled = 3,

        /// <summary>验证中</summary>
        Verifying = 4,

        /// <summary>已验证</summary>
        Verified = 5
    }
}