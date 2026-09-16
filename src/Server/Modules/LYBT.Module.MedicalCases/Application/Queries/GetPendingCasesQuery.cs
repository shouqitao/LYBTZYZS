using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Queries;

/// <summary>
/// 获取待看诊队列查询（QueryType=Pending 统一查询）。
/// </summary>
public sealed record GetPendingCasesQuery(
    Guid OperatorId,
    Guid? PatientId = null,
    bool IsAdmin = false
) : IRequest<PagedResult<MedicalCaseListDto>>;
