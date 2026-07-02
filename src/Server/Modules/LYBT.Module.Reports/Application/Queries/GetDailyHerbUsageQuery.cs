using LYBT.Module.Reports.Domain;
using LYBT.SharedKernel.Common;
using MediatR;

namespace LYBT.Module.Reports.Application.Queries;

/// <summary>
/// 获取每日草药使用汇总查询。
/// </summary>
public record GetDailyHerbUsageQuery : IRequest<Result<DailyHerbUsage>>;


