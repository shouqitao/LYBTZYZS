using LYBT.Shared.Models.Contracts.Backup;

namespace LYBT.Desktop.Admin.Services;

/// <summary>
/// 备份/恢复服务门面（B-06 / US-SHELL-013）。
/// VM 经本门面访问 <c>IApiClient.Backup</c>（DP10：VM 禁注入 IApiClient 子接口）。
/// </summary>
public interface IBackupManagementService
{
    /// <summary>备份状态（上次备份/数量/总大小/目录/保留期/进行中作业与进度）</summary>
    Task<BackupStatusDto> GetStatusAsync(CancellationToken ct = default);

    /// <summary>备份文件列表（按备份时间倒序）</summary>
    Task<IReadOnlyList<BackupFileDto>> GetBackupsAsync(CancellationToken ct = default);

    /// <summary>可选择性恢复的表清单</summary>
    Task<IReadOnlyList<BackupTableDto>> GetTablesAsync(CancellationToken ct = default);

    /// <summary>创建备份（全量/差异，可选压缩与加密）</summary>
    Task<BackupOperationResultDto> CreateAsync(BackupCreateRequestDto request, CancellationToken ct = default);

    /// <summary>恢复指定备份（整库覆盖或按表/记录选择性回写）</summary>
    Task<BackupOperationResultDto> RestoreAsync(string backupId, RestoreRequestDto request, CancellationToken ct = default);

    /// <summary>删除指定备份文件</summary>
    Task<BackupOperationResultDto> DeleteAsync(string backupId, CancellationToken ct = default);

    /// <summary>清理超过保留期的备份文件</summary>
    Task<BackupOperationResultDto> CleanupAsync(CancellationToken ct = default);
}
