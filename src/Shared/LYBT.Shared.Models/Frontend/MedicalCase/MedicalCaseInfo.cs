using LYBT.Shared.Models.Frontend.Consultation;

namespace LYBT.Shared.Models.Frontend.MedicalCase
{
    /// <summary>
    /// 医疗案例前端模型 - 简化中医诊所版本
    /// </summary>
    public class MedicalCaseInfo
    {
        /// <summary>
        /// ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 患者ID
        /// </summary>
        public Guid PatientId { get; set; }

        /// <summary>
        /// 患者姓名
        /// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>
        /// 中医师用户ID
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 中医师姓名
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 状态显示名称
        /// </summary>
        public string StatusName { get; set; } = string.Empty;

        /// <summary>
        /// 中医诊断摘要
        /// </summary>
        public string DiagnosisSummary { get; set; } = string.Empty;

        /// <summary>
        /// 主要症状
        /// </summary>
        public string MainSymptoms { get; set; } = string.Empty;

        /// <summary>
        /// 治疗效果
        /// </summary>
        public string TreatmentEffect { get; set; } = string.Empty;

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime? CompleteTime { get; set; }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// 医疗案例详情前端模型 - 简化中医诊所版本
    /// </summary>
    public class MedicalCaseDetailInfo : MedicalCaseInfo
    {
        /// <summary>
        /// 看诊信息
        /// </summary>
        public ConsultationInfo? Consultation { get; set; }

        /// <summary>
        /// 相关处方数量
        /// </summary>
        public int PrescriptionCount { get; set; }

        /// <summary>
        /// 复诊次数
        /// </summary>
        public int FollowUpCount { get; set; }

        /// <summary>
        /// 病程记录
        /// </summary>
        public List<string> ProgressNotes { get; set; } = new();
    }
}