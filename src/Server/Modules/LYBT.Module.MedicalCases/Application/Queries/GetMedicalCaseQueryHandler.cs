using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Queries;

/// <summary>
/// 获取医案详情查询处理器 — 委托 <see cref="IMedicalCaseQueryService.GetDetailDtoAsync"/>。
/// </summary>
public sealed class GetMedicalCaseQueryHandler
    : IRequestHandler<GetMedicalCaseQuery, Result<MedicalCaseDetailDto>>
{
    private readonly IMedicalCaseQueryService _queryService;

    public GetMedicalCaseQueryHandler(IMedicalCaseQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<Result<MedicalCaseDetailDto>> Handle(
        GetMedicalCaseQuery request, CancellationToken cancellationToken)
    {
        return await _queryService.GetDetailDtoAsync(
            request.Id, request.OperatorId, request.IsAdmin, cancellationToken);
    }
}
