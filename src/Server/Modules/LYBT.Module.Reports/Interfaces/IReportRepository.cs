using LYBT.Shared.Models.Contracts.Reports;

namespace LYBT.Module.Reports.Interfaces;

public interface IReportRepository
{
    Task<decimal> GetRegistrationFeeTotalAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<decimal> GetMedicineFeeTotalAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<int> GetConsultationCountAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<List<DoctorCountDto>> GetConsultationsByDoctorAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<List<HerbUsageItemDto>> GetHerbUsageAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}


