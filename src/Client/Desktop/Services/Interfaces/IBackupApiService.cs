using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models.Backup;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using Refit;
using LYBT.Desktop.Core.Models;

namespace LYBT.Desktop.Services.Interfaces
{
    /// <summary>
    /// 数据备份API服务接口
    /// </summary>
    public interface IBackupApiService
    {
        /// <summary>
        /// 获取备份历史列表
        /// </summary>
        [Get("/api/Backup/history")]
        Task<Refit.ApiResponse<PagedResult<BackupInfo>>> GetBackupHistoryAsync(
            [Query] int page = 1,
            [Query] int pageSize = 20,
            [Query] string? keyword = null,
            [Query] BackupType? type = null,
            [Query] BackupStatus? status = null,
            [Query] DateTime? startTime = null,
            [Query] DateTime? endTime = null);

        /// <summary>
        /// 创建手动备份
        /// </summary>
        [Post("/api/Backup/manual")]
        Task<Refit.ApiResponse<BackupInfo>> CreateManualBackupAsync([Body] CreateBackupRequest request);

        /// <summary>
        /// 恢复备份
        /// </summary>
        [Post("/api/Backup/{backupId}/restore")]
        Task<Refit.ApiResponse<bool>> RestoreBackupAsync(Guid backupId);

        /// <summary>
        /// 删除备份
        /// </summary>
        [Delete("/api/Backup/{backupId}")]
        Task<Refit.ApiResponse<bool>> DeleteBackupAsync(Guid backupId);

        /// <summary>
        /// 验证备份文件
        /// </summary>
        [Post("/api/Backup/{backupId}/verify")]
        Task<Refit.ApiResponse<bool>> VerifyBackupAsync(Guid backupId);

        /// <summary>
        /// 获取备份计划列表
        /// </summary>
        [Get("/api/Backup/schedules")]
        Task<Refit.ApiResponse<List<BackupScheduleInfo>>> GetBackupSchedulesAsync();

        /// <summary>
        /// 创建备份计划
        /// </summary>
        [Post("/api/Backup/schedules")]
        Task<Refit.ApiResponse<BackupScheduleInfo>> CreateBackupScheduleAsync([Body] BackupScheduleInfo schedule);

        /// <summary>
        /// 更新备份计划
        /// </summary>
        [Put("/api/Backup/schedules/{scheduleId}")]
        Task<Refit.ApiResponse<BackupScheduleInfo>> UpdateBackupScheduleAsync(Guid scheduleId, [Body] BackupScheduleInfo schedule);

        /// <summary>
        /// 删除备份计划
        /// </summary>
        [Delete("/api/Backup/schedules/{scheduleId}")]
        Task<Refit.ApiResponse<bool>> DeleteBackupScheduleAsync(Guid scheduleId);

        /// <summary>
        /// 启用/禁用备份计划
        /// </summary>
        [Patch("/api/Backup/schedules/{scheduleId}/toggle")]
        Task<Refit.ApiResponse<bool>> ToggleBackupScheduleAsync(Guid scheduleId, [Body] bool isEnabled);

        /// <summary>
        /// 获取备份统计信息
        /// </summary>
        [Get("/api/Backup/statistics")]
        Task<Refit.ApiResponse<BackupStatistics>> GetBackupStatisticsAsync();

        /// <summary>
        /// 获取备份配置
        /// </summary>
        [Get("/api/Backup/configuration")]
        Task<Refit.ApiResponse<BackupConfiguration>> GetBackupConfigurationAsync();

        /// <summary>
        /// 更新备份配置
        /// </summary>
        [Put("/api/Backup/configuration")]
        Task<Refit.ApiResponse<bool>> UpdateBackupConfigurationAsync([Body] BackupConfiguration configuration);
    }

    /// <summary>
    /// 创建备份请求
    /// </summary>
    public class CreateBackupRequest
    {
        /// <summary>备份名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>备份类型</summary>
        public BackupType Type { get; set; }

        /// <summary>备份说明</summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// 备份统计信息
    /// </summary>
    public class BackupStatistics
    {
        /// <summary>总备份数</summary>
        public int TotalBackups { get; set; }

        /// <summary>成功备份数</summary>
        public int SuccessfulBackups { get; set; }

        /// <summary>失败备份数</summary>
        public int FailedBackups { get; set; }

        /// <summary>总备份大小（字节）</summary>
        public long TotalBackupSize { get; set; }

        /// <summary>最后备份时间</summary>
        public DateTime? LastBackupTime { get; set; }

        /// <summary>下次计划备份时间</summary>
        public DateTime? NextScheduledBackupTime { get; set; }

        /// <summary>活跃计划数</summary>
        public int ActiveSchedules { get; set; }
    }

    /// <summary>
    /// 备份配置
    /// </summary>
    public class BackupConfiguration
    {
        /// <summary>默认备份路径</summary>
        public string DefaultBackupPath { get; set; } = string.Empty;

        /// <summary>是否启用自动备份</summary>
        public bool EnableAutoBackup { get; set; }

        /// <summary>自动备份间隔（小时）</summary>
        public int AutoBackupInterval { get; set; } = 24;

        /// <summary>备份保留天数</summary>
        public int BackupRetentionDays { get; set; } = 30;

        /// <summary>最大备份数量</summary>
        public int MaxBackupCount { get; set; } = 100;

        /// <summary>是否压缩备份文件</summary>
        public bool CompressBackupFiles { get; set; } = true;

        /// <summary>是否加密备份文件</summary>
        public bool EncryptBackupFiles { get; set; } = false;
    }
}