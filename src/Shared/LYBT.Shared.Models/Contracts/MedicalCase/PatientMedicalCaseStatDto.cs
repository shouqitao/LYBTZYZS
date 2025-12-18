using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 患者医案统计DTO
    /// </summary>
    public class PatientMedicalCaseStatDto
    {
        public Guid PatientId { get; set; }

        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        [DisplayName("总医案数")]
        public int TotalMedicalCases { get; set; }

        [DisplayName("完成医案数")]
        public int CompletedCases { get; set; }

        [DisplayName("完成率")]
        public decimal CompletionRate { get; set; }

        [DisplayName("首次就诊时间")]
        public DateTime? FirstVisitDate { get; set; }

        [DisplayName("最近就诊时间")]
        public DateTime? LastVisitDate { get; set; }

        [DisplayName("平均就诊间隔(天)")]
        public decimal AverageVisitInterval { get; set; }
    }
}
