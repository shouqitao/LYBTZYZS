using LYBT.Module.Reports.Domain;
using LYBT.Shared.Models.Contracts.Reports;

namespace LYBT.Module.Reports.Application.Mappers;

/// <summary>
/// 报表数据映射器。静态类，用于 Domain 值对象与 DTO 之间的转换。
/// </summary>
public static class ReportMapper
{
    /// <summary>
    /// DailyIncome 值对象转换为 DailyIncomeDto。
    /// </summary>
    public static DailyIncomeDto ToDto(DailyIncome income) => new()
    {
        TotalIncome = income.TotalIncome,
        RegistrationFeeTotal = income.RegistrationFeeTotal,
        MedicineFeeTotal = income.MedicineFeeTotal
    };

    /// <summary>
    /// DailyConsultation 值对象转换为 DailyConsultationDto。
    /// </summary>
    public static DailyConsultationDto ToDto(DailyConsultation consultation) => new()
    {
        TotalCount = consultation.TotalCount,
        ByDoctor = consultation.ByDoctor.Select(d => new DoctorCountDto
        {
            DoctorName = d.DoctorName,
            Count = d.Count
        }).ToList()
    };

    /// <summary>
    /// DailyHerbUsage 值对象转换为 DailyHerbUsageDto。
    /// </summary>
    public static DailyHerbUsageDto ToDto(DailyHerbUsage herbUsage) => new()
    {
        Items = herbUsage.Items.Select(h => new HerbUsageItemDto
        {
            HerbName = h.HerbName,
            UsageCount = h.UsageCount,
            TotalDosage = h.TotalDosage
        }).ToList()
    };
}


