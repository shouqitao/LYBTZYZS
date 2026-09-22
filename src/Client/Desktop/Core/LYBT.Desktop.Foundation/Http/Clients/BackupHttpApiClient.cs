// ---------------------------------------------------------------------------
// BackupHttpApiClient — HttpClient adapter for IApiClientBackup
// ---------------------------------------------------------------------------
// LocalWebAPI mode implementation of IApiClientBackup (split from HttpClientApiClient).
// Part of the IApiClient unified abstraction layer.
// ---------------------------------------------------------------------------

using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Backup;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>本地模式备份/恢复 API 客户端（拆分自 HttpClientApiClient）</summary>
internal sealed class BackupHttpApiClient : HttpApiClientBase, IApiClientBackup
{
    public BackupHttpApiClient(IHttpClientFactory httpClientFactory, ILogger logger)
        : base(httpClientFactory, logger)
    {
    }

    /// <inheritdoc />
    public Task<ApiResponse<List<BackupFileDto>>> GetBackupsAsync(CancellationToken ct = default)
        => GetAndWrapAsync<List<BackupFileDto>>("/api/v1/backup", ct);

    /// <inheritdoc />
    public Task<ApiResponse<BackupStatusDto>> GetStatusAsync(CancellationToken ct = default)
        => GetAndWrapAsync<BackupStatusDto>("/api/v1/backup/status", ct);

    /// <inheritdoc />
    public Task<ApiResponse<List<BackupTableDto>>> GetTablesAsync(CancellationToken ct = default)
        => GetAndWrapAsync<List<BackupTableDto>>("/api/v1/backup/tables", ct);

    /// <inheritdoc />
    public Task<ApiResponse<BackupOperationResultDto>> CreateAsync(BackupCreateRequestDto request, CancellationToken ct = default)
        => PostAndWrapAsync<BackupOperationResultDto>("/api/v1/backup", request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<BackupOperationResultDto>> RestoreAsync(string backupId, RestoreRequestDto request, CancellationToken ct = default)
        => PostAndWrapAsync<BackupOperationResultDto>($"/api/v1/backup/{backupId}/restore", request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<BackupOperationResultDto>> DeleteAsync(string backupId, CancellationToken ct = default)
        => SendAndWrapAsync<BackupOperationResultDto>($"/api/v1/backup/{backupId}", HttpMethod.Delete, null, ct);

    /// <inheritdoc />
    public Task<ApiResponse<BackupOperationResultDto>> CleanupAsync(CancellationToken ct = default)
        => PostAndWrapAsync<BackupOperationResultDto>("/api/v1/backup/cleanup", ct: ct);

    /// <inheritdoc />
    public Task<ApiResponse<BackupOperationResultDto>> AutoBackupAsync(CancellationToken ct = default)
        => PostAndWrapAsync<BackupOperationResultDto>("/api/v1/backup/auto", ct: ct);
}
