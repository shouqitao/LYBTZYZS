using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Queries;

public record GetMedicalCaseAuditLogsQuery(
    Guid CaseId,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PagedResult<AuditLogDto>>>;
