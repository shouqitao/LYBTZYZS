using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Queries;

/// <summary>
/// 获取医案详情查询（含 NotFound 语义）。
/// </summary>
public sealed record GetMedicalCaseQuery(
    Guid Id,
    Guid? OperatorId = null,
    bool IsAdmin = false
) : IRequest<Result<MedicalCaseDetailDto>>;
