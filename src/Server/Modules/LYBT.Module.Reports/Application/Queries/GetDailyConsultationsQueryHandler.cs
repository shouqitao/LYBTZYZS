using LYBT.Module.Reports.Domain;
using LYBT.Module.Reports.Interfaces;
using LYBT.SharedKernel.Common;
using MediatR;

namespace LYBT.Module.Reports.Application.Queries;

/// <summary>
/// 获取每日问诊汇总查询处理器。
/// </summary>
public class GetDailyConsultationsQueryHandler : IRequestHandler<GetDailyConsultationsQuery, Result<DailyConsultation>>
{
    private readonly IReportRepository _reportRepository;

    public GetDailyConsultationsQueryHandler(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<Result<DailyConsultation>> Handle(
        GetDailyConsultationsQuery request, CancellationToken cancellationToken)
    {
        var startDate = request.StartDate ?? DateTime.Today;
        var endDate = request.EndDate ?? DateTime.Today;

        var totalCount = await _reportRepository.GetConsultationCountAsync(startDate, endDate, cancellationToken);
        var byDoctorDtos = await _reportRepository.GetConsultationsByDoctorAsync(startDate, endDate, cancellationToken);

        var byDoctor = byDoctorDtos
            .Select(d => new DoctorCount(d.DoctorName, d.Count))
            .ToList();

        var dailyConsultation = new DailyConsultation(
            TotalCount: totalCount,
            ByDoctor: byDoctor);

        return Result<DailyConsultation>.Success(dailyConsultation);
    }
}


