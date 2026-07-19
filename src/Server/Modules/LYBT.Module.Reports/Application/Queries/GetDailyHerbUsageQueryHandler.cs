using LYBT.Module.Reports.Interfaces;
using LYBT.Shared.Models.Contracts.Reports;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.Reports.Application.Queries;

/// <summary>
/// 获取每日草药使用汇总查询处理器。
/// </summary>
public class GetDailyHerbUsageQueryHandler : IRequestHandler<GetDailyHerbUsageQuery, Result<DailyHerbUsageDto>>
{
    private readonly IReportRepository _reportRepository;

    public GetDailyHerbUsageQueryHandler(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<Result<DailyHerbUsageDto>> Handle(
        GetDailyHerbUsageQuery request, CancellationToken cancellationToken)
    {
        var startDate = request.StartDate ?? DateTime.Today;
        var endDate = request.EndDate ?? DateTime.Today;

        var items = await _reportRepository.GetHerbUsageAsync(startDate, endDate, cancellationToken);

        var dto = new DailyHerbUsageDto { Items = items };

        return Result<DailyHerbUsageDto>.Success(dto);
    }
}


