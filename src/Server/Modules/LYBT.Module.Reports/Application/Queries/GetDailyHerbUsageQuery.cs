using LYBT.Shared.Models.Contracts.Reports;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.Reports.Application.Queries;

/// <summary>
/// 获取每日草药使用汇总查询。
/// </summary>
public record GetDailyHerbUsageQuery(DateTime? StartDate = null, DateTime? EndDate = null) : IRequest<Result<DailyHerbUsageDto>>;


