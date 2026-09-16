using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Queries;

/// <summary>
/// 获取待看诊队列查询处理器 — 委托 <see cref="IMedicalCaseQueryService.QueryAsync"/>（QueryType=Pending）。
/// </summary>
public sealed class GetPendingCasesQueryHandler
    : IRequestHandler<GetPendingCasesQuery, PagedResult<MedicalCaseListDto>>
{
    private readonly IMedicalCaseQueryService _queryService;

    public GetPendingCasesQueryHandler(IMedicalCaseQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<PagedResult<MedicalCaseListDto>> Handle(
        GetPendingCasesQuery request, CancellationToken cancellationToken)
    {
        var query = new MedicalCaseQueryDto
        {
            QueryType = MedicalCaseQueryType.Pending,
            PatientId = request.PatientId,
            IncludeAllDoctors = request.IsAdmin
        };
        if (!request.IsAdmin)
            query.DoctorId = request.OperatorId;

        return await _queryService.QueryAsync(query, cancellationToken);
    }
}
