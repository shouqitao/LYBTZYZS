using LYBT.Module.Reports.Interfaces;
using LYBT.Shared.Models.Contracts.Reports;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.Reports.Application.Queries;

/// <summary>
/// 获取每日收入汇总查询处理器。
/// </summary>
public class GetDailyIncomeQueryHandler : IRequestHandler<GetDailyIncomeQuery, Result<DailyIncomeDto>>
{
    private readonly IReportRepository _reportRepository;

    public GetDailyIncomeQueryHandler(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<Result<DailyIncomeDto>> Handle(
        GetDailyIncomeQuery request, CancellationToken cancellationToken)
    {
        var startDate = request.StartDate ?? DateTime.Today;
        var endDate = request.EndDate ?? DateTime.Today;

        var registrationFeeTotal = await _reportRepository.GetRegistrationFeeTotalAsync(startDate, endDate, cancellationToken);
        var medicineFeeTotal = await _reportRepository.GetMedicineFeeTotalAsync(startDate, endDate, cancellationToken);

        var dto = new DailyIncomeDto
        {
            TotalIncome = registrationFeeTotal + medicineFeeTotal,
            RegistrationFeeTotal = registrationFeeTotal,
            MedicineFeeTotal = medicineFeeTotal
        };

        return Result<DailyIncomeDto>.Success(dto);
    }
}


