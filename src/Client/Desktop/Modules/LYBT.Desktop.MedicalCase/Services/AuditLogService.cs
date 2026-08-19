using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Results;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// 医案审计日志Service实现 - 封装医案审计日志（对齐 VM→Service→Repository→IApiClient 分层，P0-2）
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly IMedicalCaseRepository _repository;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(IMedicalCaseRepository repository, ILogger<AuditLogService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<CommandResult<PagedResult<AuditLogDto>>> GetAuditLogsAsync(Guid medicalCaseId, int page, int pageSize, CancellationToken ct = default)
    {
        try
        {
            // Repository 已解包信封（Data==null → 空分页），与 IMedicalCaseQueryService 约定一致
            var result = await _repository.GetAuditLogsAsync(medicalCaseId, page, pageSize, ct);
            return CommandResult<PagedResult<AuditLogDto>>.Succeeded(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] AuditLog.Get failed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return CommandResult<PagedResult<AuditLogDto>>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载审计日志", ex));
        }
    }
}
