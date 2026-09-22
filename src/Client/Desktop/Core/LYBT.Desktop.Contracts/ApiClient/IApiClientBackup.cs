using LYBT.Shared.Models.Contracts.Backup;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Contracts.ApiClient;

/// <summary>备份/恢复端点（B-06 / US-SHELL-013；远程与本地模式同路由）。</summary>
public interface IApiClientBackup
{
    /// <summary>备份文件列表（按备份时间倒序）</summary>
    Task<ApiResponse<List<BackupFileDto>>> GetBackupsAsync(CancellationToken ct = default);

    /// <summary>备份状态（上次备份/数量/总大小/进行中作业与进度）</summary>
    Task<ApiResponse<BackupStatusDto>> GetStatusAsync(CancellationToken ct = default);

    /// <summary>可选择性恢复的表清单</summary>
    Task<ApiResponse<List<BackupTableDto>>> GetTablesAsync(CancellationToken ct = default);

    /// <summary>创建备份（全量/差异，可选压缩与加密）</summary>
    Task<ApiResponse<BackupOperationResultDto>> CreateAsync(BackupCreateRequestDto request, CancellationToken ct = default);

    /// <summary>恢复指定备份（整库覆盖或按表/记录选择性回写）</summary>
    Task<ApiResponse<BackupOperationResultDto>> RestoreAsync(string backupId, RestoreRequestDto request, CancellationToken ct = default);

    /// <summary>删除指定备份文件</summary>
    Task<ApiResponse<BackupOperationResultDto>> DeleteAsync(string backupId, CancellationToken ct = default);

    /// <summary>清理超过保留期的备份文件</summary>
    Task<ApiResponse<BackupOperationResultDto>> CleanupAsync(CancellationToken ct = default);

    /// <summary>自动备份（登录后触发；距上次备份未满间隔时为空操作）</summary>
    Task<ApiResponse<BackupOperationResultDto>> AutoBackupAsync(CancellationToken ct = default);
}
