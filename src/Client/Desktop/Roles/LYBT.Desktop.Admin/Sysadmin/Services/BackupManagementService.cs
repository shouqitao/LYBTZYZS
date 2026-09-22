using LYBT.Desktop.Admin.Services;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Backup;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Admin.Sysadmin.Services;

/// <summary>
/// 备份/恢复服务实现——封装 <see cref="IApiClientBackup"/>（D6: DP10 收口）。
/// 远程模式走服务端 BackupController（远程 SQL Server），本地模式走内嵌 LocalWebAPI（本机 LocalDB），
/// 路由切换由 SwitchingApiClient 按当前连接模式完成。
/// </summary>
public class BackupManagementService : IBackupManagementService
{
    private readonly IApiClientBackup _backupApi;
    private readonly ILogger<BackupManagementService> _logger;

    public BackupManagementService(IApiClientBackup backupApi, ILogger<BackupManagementService> logger)
    {
        _backupApi = backupApi ?? throw new ArgumentNullException(nameof(backupApi));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<BackupStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        var response = await _backupApi.GetStatusAsync(ct);
        if (response.Success && response.Data != null)
            return response.Data;

        _logger.LogWarning("获取备份状态失败: {Message}", response.Message);
        return new BackupStatusDto();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BackupFileDto>> GetBackupsAsync(CancellationToken ct = default)
    {
        var response = await _backupApi.GetBackupsAsync(ct);
        if (response.Success && response.Data != null)
            return response.Data;

        _logger.LogWarning("获取备份列表失败: {Message}", response.Message);
        return Array.Empty<BackupFileDto>();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BackupTableDto>> GetTablesAsync(CancellationToken ct = default)
    {
        var response = await _backupApi.GetTablesAsync(ct);
        if (response.Success && response.Data != null)
            return response.Data;

        _logger.LogWarning("获取可恢复表清单失败: {Message}", response.Message);
        return Array.Empty<BackupTableDto>();
    }

    /// <inheritdoc />
    public async Task<BackupOperationResultDto> CreateAsync(BackupCreateRequestDto request, CancellationToken ct = default)
        => Unwrap(await _backupApi.CreateAsync(request, ct), "备份失败");

    /// <inheritdoc />
    public async Task<BackupOperationResultDto> RestoreAsync(string backupId, RestoreRequestDto request, CancellationToken ct = default)
        => Unwrap(await _backupApi.RestoreAsync(backupId, request, ct), "恢复失败");

    /// <inheritdoc />
    public async Task<BackupOperationResultDto> DeleteAsync(string backupId, CancellationToken ct = default)
        => Unwrap(await _backupApi.DeleteAsync(backupId, ct), "删除失败");

    /// <inheritdoc />
    public async Task<BackupOperationResultDto> CleanupAsync(CancellationToken ct = default)
        => Unwrap(await _backupApi.CleanupAsync(ct), "清理失败");

    /// <summary>解包 ApiResponse 信封；失败时保留服务端消息供 VM 展示</summary>
    private static BackupOperationResultDto Unwrap(
        ApiResponse<BackupOperationResultDto> response,
        string fallbackMessage)
    {
        if (response.Success && response.Data != null)
            return response.Data;

        var message = string.IsNullOrWhiteSpace(response.Message) ? fallbackMessage : response.Message;
        return new BackupOperationResultDto
        {
            Success = false,
            Error = message,
            Message = message
        };
    }
}
