using LYBT.Desktop.Contracts.Results;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using System.Threading;

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 医案审计日志Service接口
/// 封装 IApiClient.MedicalCases.GetAuditLogsAsync，VM 只注入本接口不直连 API 客户端
/// </summary>
public interface IAuditLogService
{
    /// <summary>获取医案审计日志（分页）</summary>
    Task<CommandResult<PagedResult<AuditLogDto>>> GetAuditLogsAsync(Guid medicalCaseId, int page, int pageSize, CancellationToken ct = default);
}
