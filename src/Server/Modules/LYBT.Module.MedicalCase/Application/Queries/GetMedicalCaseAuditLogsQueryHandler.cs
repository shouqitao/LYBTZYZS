using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Queries;

public class GetMedicalCaseAuditLogsQueryHandler(
    IMedicalCaseRepository repository
) : IRequestHandler<GetMedicalCaseAuditLogsQuery, Result<PagedResult<AuditLogDto>>>
{
    public async Task<Result<PagedResult<AuditLogDto>>> Handle(
        GetMedicalCaseAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var medicalCase = await repository.GetByIdWithDetailsAsync(request.CaseId, cancellationToken);
        if (medicalCase == null)
            return Result<PagedResult<AuditLogDto>>.Failure(ErrorCode.NotFound, "医案不存在");

        var totalCount = await repository.CountAuditLogsAsync(request.CaseId, cancellationToken);
        var logs = await repository.GetAuditLogsAsync(request.CaseId, request.Page, request.PageSize, cancellationToken);

        var items = logs.Select(l => new AuditLogDto
        {
            Timestamp = l.CreatedAt,
            Action = l.OperationType switch
            {
                0 => "医案创建",
                1 => "医案更新",
                2 => "状态变更",
                3 => "医案删除",
                _ => "未知操作"
            },
            PerformedBy = l.OperatorId.ToString("D"),
            Details = l.Reason ?? $"操作人: {l.OperatorName}"
        }).ToList();

        var paged = new PagedResult<AuditLogDto>(items, totalCount, request.Page, request.PageSize);
        return Result<PagedResult<AuditLogDto>>.Success(paged);
    }
}
