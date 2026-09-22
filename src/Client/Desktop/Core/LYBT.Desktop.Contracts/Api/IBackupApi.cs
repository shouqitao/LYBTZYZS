using LYBT.Shared.Models.Contracts.Backup;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// 备份/恢复 API 客户端接口 - 对应双端 BackupController（B-06 / US-SHELL-013）。
/// </summary>
/// <remarks>
/// 功能范围: 备份列表/状态/表清单、创建备份（全量/差异）、恢复（整库/选择性）、删除、清理、登录自动备份
/// 权限要求: 管理操作 SysAdminOnly；<c>auto</c> 仅要求已认证（登录后自动备份）
/// 路由: <c>/api/v1/backup</c>（远程与本地 LocalWebAPI 同路由）
/// </remarks>
internal interface IBackupApi
{
    /// <summary>备份文件列表（按备份时间倒序）</summary>
    [Refit.Get("/api/v1/backup")]
    Task<ApiResponse<List<BackupFileDto>>> GetBackupsAsync(CancellationToken ct = default);

    /// <summary>备份状态（上次备份/数量/总大小/进行中作业与进度）</summary>
    [Refit.Get("/api/v1/backup/status")]
    Task<ApiResponse<BackupStatusDto>> GetStatusAsync(CancellationToken ct = default);

    /// <summary>可选择性恢复的表清单</summary>
    [Refit.Get("/api/v1/backup/tables")]
    Task<ApiResponse<List<BackupTableDto>>> GetTablesAsync(CancellationToken ct = default);

    /// <summary>创建备份（全量/差异，可选压缩与加密）</summary>
    [Refit.Post("/api/v1/backup")]
    Task<ApiResponse<BackupOperationResultDto>> CreateAsync(
        [Refit.Body] BackupCreateRequestDto request,
        CancellationToken ct = default);

    /// <summary>恢复指定备份（整库覆盖或按表/记录选择性回写）</summary>
    [Refit.Post("/api/v1/backup/{id}/restore")]
    Task<ApiResponse<BackupOperationResultDto>> RestoreAsync(
        string id,
        [Refit.Body] RestoreRequestDto request,
        CancellationToken ct = default);

    /// <summary>删除指定备份文件</summary>
    [Refit.Delete("/api/v1/backup/{id}")]
    Task<ApiResponse<BackupOperationResultDto>> DeleteAsync(string id, CancellationToken ct = default);

    /// <summary>清理超过保留期的备份文件</summary>
    [Refit.Post("/api/v1/backup/cleanup")]
    Task<ApiResponse<BackupOperationResultDto>> CleanupAsync(CancellationToken ct = default);

    /// <summary>自动备份（登录后触发；距上次备份未满间隔时为空操作）</summary>
    [Refit.Post("/api/v1/backup/auto")]
    Task<ApiResponse<BackupOperationResultDto>> AutoBackupAsync(CancellationToken ct = default);
}
