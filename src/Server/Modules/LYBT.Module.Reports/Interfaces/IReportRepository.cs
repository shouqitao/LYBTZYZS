using LYBT.Shared.Models.Contracts.Reports;

namespace LYBT.Module.Reports.Interfaces;

public interface IReportRepository
{
    Task<decimal> GetTodayRegistrationFeeTotalAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetTodayMedicineFeeTotalAsync(CancellationToken cancellationToken = default);
    Task<int> GetTodayConsultationCountAsync(CancellationToken cancellationToken = default);
    Task<List<DoctorCountDto>> GetTodayConsultationsByDoctorAsync(CancellationToken cancellationToken = default);
    Task<List<HerbUsageItemDto>> GetTodayHerbUsageAsync(CancellationToken cancellationToken = default);
}


