using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Backup;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>
/// 备份/恢复 API 客户端——包装 IBackupApi（Refit）以实现 IApiClientBackup（远程模式）。
/// </summary>
internal sealed class BackupApiClient : IApiClientBackup
{
    private readonly IBackupApi _api;

    public BackupApiClient(IBackupApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <inheritdoc />
    public Task<ApiResponse<List<BackupFileDto>>> GetBackupsAsync(CancellationToken ct = default)
        => _api.GetBackupsAsync(ct);

    /// <inheritdoc />
    public Task<ApiResponse<BackupStatusDto>> GetStatusAsync(CancellationToken ct = default)
        => _api.GetStatusAsync(ct);

    /// <inheritdoc />
    public Task<ApiResponse<List<BackupTableDto>>> GetTablesAsync(CancellationToken ct = default)
        => _api.GetTablesAsync(ct);

    /// <inheritdoc />
    public Task<ApiResponse<BackupOperationResultDto>> CreateAsync(BackupCreateRequestDto request, CancellationToken ct = default)
        => _api.CreateAsync(request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<BackupOperationResultDto>> RestoreAsync(string backupId, RestoreRequestDto request, CancellationToken ct = default)
        => _api.RestoreAsync(backupId, request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<BackupOperationResultDto>> DeleteAsync(string backupId, CancellationToken ct = default)
        => _api.DeleteAsync(backupId, ct);

    /// <inheritdoc />
    public Task<ApiResponse<BackupOperationResultDto>> CleanupAsync(CancellationToken ct = default)
        => _api.CleanupAsync(ct);

    /// <inheritdoc />
    public Task<ApiResponse<BackupOperationResultDto>> AutoBackupAsync(CancellationToken ct = default)
        => _api.AutoBackupAsync(ct);
}
