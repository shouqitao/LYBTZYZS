namespace LYBT.Shared.Models.Contracts.Reports;

public class DailyConsultationDto
{
    public int TotalCount { get; set; }
    public List<DoctorCountDto> ByDoctor { get; set; } = [];
}
