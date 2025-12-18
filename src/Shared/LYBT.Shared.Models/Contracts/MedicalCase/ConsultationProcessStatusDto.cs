using System.ComponentModel;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 诊疗流程状态DTO (Record-Only模式：仅数据记录，无复杂流程控制)
    /// </summary>
    public class ConsultationProcessStatusDto
    {
        public Guid MedicalCaseId { get; set; }

        [DisplayName("当前状态")]
        public MedicalCaseStatus CurrentStatus { get; set; }

        [DisplayName("当前步骤")]
        public string CurrentStep { get; set; } = string.Empty;

        [DisplayName("最后更新时间")]
        public DateTime LastUpdatedAt { get; set; }

        public Guid DoctorId { get; set; }

        [DisplayName("医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

        [DisplayName("已完成步骤")]
        public List<string> CompletedSteps { get; set; } = new();

        [DisplayName("待处理步骤")]
        public List<string> PendingSteps { get; set; } = new();

        [DisplayName("可进行下一步")]
        public bool CanProceedToNext { get; set; }
    }
}
