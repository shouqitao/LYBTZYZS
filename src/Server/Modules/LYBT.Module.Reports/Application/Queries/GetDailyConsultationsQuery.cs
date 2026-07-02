using LYBT.Module.Reports.Domain;
using LYBT.SharedKernel.Common;
using MediatR;

namespace LYBT.Module.Reports.Application.Queries;

/// <summary>
/// 获取每日问诊汇总查询。
/// </summary>
public record GetDailyConsultationsQuery : IRequest<Result<DailyConsultation>>;


