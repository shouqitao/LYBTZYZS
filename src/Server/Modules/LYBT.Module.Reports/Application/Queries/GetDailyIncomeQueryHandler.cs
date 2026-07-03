using LYBT.Module.Reports.Domain;
using LYBT.Module.Reports.Interfaces;
using LYBT.SharedKernel.Common;
using MediatR;

namespace LYBT.Module.Reports.Application.Queries;

/// <summary>
/// 获取每日收入汇总查询处理器。
/// </summary>
public class GetDailyIncomeQueryHandler : IRequestHandler<GetDailyIncomeQuery, Result<DailyIncome>>
{
    private readonly IReportRepository _reportRepository;

    public GetDailyIncomeQueryHandler(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<Result<DailyIncome>> Handle(
        GetDailyIncomeQuery request, CancellationToken cancellationToken)
    {
        var startDate = request.StartDate ?? DateTime.Today;
        var endDate = request.EndDate ?? DateTime.Today;

        var registrationFeeTotal = await _reportRepository.GetRegistrationFeeTotalAsync(startDate, endDate, cancellationToken);
        var medicineFeeTotal = await _reportRepository.GetMedicineFeeTotalAsync(startDate, endDate, cancellationToken);

        var dailyIncome = new DailyIncome(
            TotalIncome: registrationFeeTotal + medicineFeeTotal,
            RegistrationFeeTotal: registrationFeeTotal,
            MedicineFeeTotal: medicineFeeTotal);

        return Result<DailyIncome>.Success(dailyIncome);
    }
}


