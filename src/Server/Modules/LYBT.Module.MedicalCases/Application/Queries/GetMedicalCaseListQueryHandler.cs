using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Queries;

/// <summary>
/// 查询医案列表（分页）查询处理器 — 委托 <see cref="IMedicalCaseQueryService.GetListDtoAsync"/>。
/// </summary>
public sealed class GetMedicalCaseListQueryHandler
    : IRequestHandler<GetMedicalCaseListQuery, PagedResult<MedicalCaseListDto>>
{
    private readonly IMedicalCaseQueryService _queryService;

    public GetMedicalCaseListQueryHandler(IMedicalCaseQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<PagedResult<MedicalCaseListDto>> Handle(
        GetMedicalCaseListQuery request, CancellationToken cancellationToken)
    {
        return await _queryService.GetListDtoAsync(
            request.Status,
            request.PatientId,
            request.Page,
            request.PageSize,
            request.CurrentDoctorId,
            request.IsAdmin,
            request.Keyword,
            cancellationToken);
    }
}
