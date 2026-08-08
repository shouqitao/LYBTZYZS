using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Results;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// 医案审计日志Service实现 - 封装医案域 API 客户端（对齐 VM→Service→IApiClient 分层）
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(IApiClient apiClient, ILogger<AuditLogService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<CommandResult<PagedResult<AuditLogDto>>> GetAuditLogsAsync(Guid medicalCaseId, int page, int pageSize, CancellationToken ct = default)
    {
        try
        {
            var result = await _apiClient.MedicalCases.GetAuditLogsAsync(medicalCaseId, page, pageSize);
            return result.Success && result.Data != null
                ? CommandResult<PagedResult<AuditLogDto>>.Succeeded(result.Data)
                : CommandResult<PagedResult<AuditLogDto>>.Failed(result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] AuditLog.Get failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return CommandResult<PagedResult<AuditLogDto>>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载审计日志", ex));
        }
    }
}
