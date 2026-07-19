using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.Patients.Application.Queries;

/// <summary>
/// 获取患者分页列表查询。
/// </summary>
public record GetPatientsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Keyword = null,
    bool FilterDisabled = false
) : IRequest<Result<PagedResult<PatientListDto>>>;


