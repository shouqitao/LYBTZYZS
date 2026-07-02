using LYBT.Module.Reports.Domain;
using LYBT.Module.Reports.Interfaces;
using LYBT.SharedKernel.Common;
using MediatR;

namespace LYBT.Module.Reports.Application.Queries;

/// <summary>
/// 获取每日草药使用汇总查询处理器。
/// </summary>
public class GetDailyHerbUsageQueryHandler : IRequestHandler<GetDailyHerbUsageQuery, Result<DailyHerbUsage>>
{
    private readonly IReportRepository _reportRepository;

    public GetDailyHerbUsageQueryHandler(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<Result<DailyHerbUsage>> Handle(
        GetDailyHerbUsageQuery request, CancellationToken cancellationToken)
    {
        var herbUsageItems = await _reportRepository.GetTodayHerbUsageAsync(cancellationToken);

        var items = herbUsageItems
            .Select(h => new HerbUsageItem(h.HerbName, h.UsageCount, h.TotalDosage))
            .ToList();

        var dailyHerbUsage = new DailyHerbUsage(Items: items);

        return Result<DailyHerbUsage>.Success(dailyHerbUsage);
    }
}


