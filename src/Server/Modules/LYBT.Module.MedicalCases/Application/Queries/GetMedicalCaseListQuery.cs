using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Queries;

/// <summary>
/// 查询医案列表（分页，返回 MedicalCaseListDto）。
/// </summary>
public sealed record GetMedicalCaseListQuery(
    MedicalCaseStatus? Status,
    Guid? PatientId,
    int Page,
    int PageSize,
    Guid? CurrentDoctorId = null,
    bool IsAdmin = false,
    string? Keyword = null
) : IRequest<PagedResult<MedicalCaseListDto>>;
