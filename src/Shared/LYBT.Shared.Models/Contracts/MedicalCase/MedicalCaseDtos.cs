using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 医疗案例DTO - UltraThink v2.0简化版
    /// 与MedicalCase实体对齐，保留ConsultationDate
    /// </summary>
    public class MedicalCaseDto : StatusDto, IRemarkable
    {
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        [DisplayName("医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

        [DisplayName("诊断ID")]
        public Guid? ConsultationId { get; set; }

        [DisplayName("处方ID")]
        public Guid? PrescriptionId { get; set; }

        [DisplayName("看诊时间")]
        public DateTime ConsultationDate { get; set; } = DateTime.Now;

        /// <summary>医疗案例专用状态</summary>
        [DisplayName("案例状态")]
        public MedicalCaseStatus CaseStatus { get; set; } = MedicalCaseStatus.Registered;

        [DisplayName("备注")]
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get; set; }

        /// <summary>获取优先级 - 基于看诊时间</summary>
        public int GetPriority()
        {
            var hoursElapsed = (DateTime.Now - ConsultationDate).TotalHours;
            if (hoursElapsed > 48) return 3; // 高优先级
            if (hoursElapsed > 24) return 2; // 中优先级
            return 1; // 低优先级
        }

        /// <summary>是否紧急</summary>
        public bool IsUrgent() => GetPriority() >= 3;

        /// <summary>是否需要医生注意 - 基于看诊时间</summary>
        public bool NeedsDoctorAttention() => CaseStatus != MedicalCaseStatus.Completed && (DateTime.Now - ConsultationDate).TotalHours > 24;

        /// <summary>是否可以开始诊疗</summary>
        public bool CanStartConsultation() => CaseStatus == MedicalCaseStatus.Registered;

        /// <summary>是否可以完成</summary>
        public bool CanComplete() => CaseStatus == MedicalCaseStatus.InConsultation;

        /// <summary>是否可以取消</summary>
        public bool CanCancel() => CaseStatus == MedicalCaseStatus.Registered || CaseStatus == MedicalCaseStatus.InConsultation;

        /// <summary>是否可以删除</summary>
        public bool CanDelete() => CaseStatus == MedicalCaseStatus.Cancelled || CaseStatus == MedicalCaseStatus.Completed;

        /// <summary>是否可以编辑</summary>
        public bool CanEdit() => CaseStatus != MedicalCaseStatus.Completed && CaseStatus != MedicalCaseStatus.Cancelled;

        /// <summary>是否已完成</summary>
        public bool IsCompleted() => CaseStatus == MedicalCaseStatus.Completed;


    }

    /// <summary>
    /// 医疗案例详情DTO - 继承基础DTO，添加详细信息
    /// </summary>
    public class MedicalCaseDetailDto : MedicalCaseDto
    {
        [DisplayName("挂号ID")]
        public Guid? RegistrationId { get; set; }

        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        [DisplayName("既往史")]
        public string? PastHistory { get; set; }

        [DisplayName("体格检查")]
        public string? PhysicalExamination { get; set; }

        [DisplayName("辅助检查")]
        public string? AuxiliaryExamination { get; set; }

        [DisplayName("诊断结果")]
        public string? DiagnosisResult { get; set; }

        [DisplayName("治疗方案")]
        public string? TreatmentPlan { get; set; }

        [DisplayName("处方信息")]
        public string? PrescriptionInfo { get; set; }

        [DisplayName("随访计划")]
        public string? FollowUpPlan { get; set; }
    }

    /// <summary>
    /// 医疗案例输入基础DTO - 提供通用输入字段验证规则
    /// </summary>
    public abstract class MedicalCaseInputBaseDto : IRemarkable
    {
        [Required(ErrorMessage = "患者ID不能为空")]
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        [Required(ErrorMessage = "医生ID不能为空")]
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        [DisplayName("挂号ID")]
        public Guid? RegistrationId { get; set; }

        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 创建医疗案例DTO - 继承医疗案例输入基础DTO
    /// </summary>
    public class MedicalCaseCreateDto : MedicalCaseInputBaseDto
    {
        [StringLength(200, ErrorMessage = "诊断摘要长度不能超过200个字符")]
        [DisplayName("诊断摘要")]
        public string? DiagnosisSummary { get; set; }
    }

    /// <summary>
    /// 编辑医疗案例DTO - 继承医疗案例输入基础DTO并添加ID字段
    /// </summary>
    public class MedicalCaseEditDto : MedicalCaseInputBaseDto, IIdentifiable<Guid>
    {
        [Required(ErrorMessage = "医疗案例ID不能为空")]
        [DisplayName("医疗案例ID")]
        public Guid Id { get; set; }

        [StringLength(200, ErrorMessage = "诊断摘要长度不能超过200个字符")]
        [DisplayName("诊断摘要")]
        public string? DiagnosisSummary { get; set; }

        [StringLength(1000, ErrorMessage = "主诉长度不能超过1000个字符")]
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        [StringLength(2000, ErrorMessage = "现病史长度不能超过2000个字符")]
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        [StringLength(1000, ErrorMessage = "既往史长度不能超过1000个字符")]
        [DisplayName("既往史")]
        public string? PastHistory { get; set; }

        [StringLength(1000, ErrorMessage = "诊断结果长度不能超过1000个字符")]
        [DisplayName("诊断结果")]
        public string? DiagnosisResult { get; set; }

        [StringLength(1000, ErrorMessage = "治疗方案长度不能超过1000个字符")]
        [DisplayName("治疗方案")]
        public string? TreatmentPlan { get; set; }

        [DisplayName("状态")]
        public string? Status { get; set; }


    }

    /// <summary>
    /// 更新医疗案例DTO - 继承编辑DTO，用于更复杂的更新操作
    /// </summary>
    public class MedicalCaseUpdateDto : MedicalCaseEditDto
    {

        [StringLength(1000, ErrorMessage = "体格检查长度不能超过1000个字符")]
        [DisplayName("体格检查")]
        public string? PhysicalExamination { get; set; }

        [StringLength(1000, ErrorMessage = "辅助检查长度不能超过1000个字符")]
        [DisplayName("辅助检查")]
        public string? AuxiliaryExamination { get; set; }

        [StringLength(1000, ErrorMessage = "处方信息长度不能超过1000个字符")]
        [DisplayName("处方信息")]
        public string? PrescriptionInfo { get; set; }

        [StringLength(1000, ErrorMessage = "随访计划长度不能超过1000个字符")]
        [DisplayName("随访计划")]
        public string? FollowUpPlan { get; set; }
    }

    /// <summary>
    /// 医疗案例查询DTO - 继承完整分页查询DTO，提供分页、时间范围、关键词搜索功能
    /// </summary>
    public class MedicalCaseQueryDto : FullPagedQueryDto
    {
        [DisplayName("患者ID")]
        public Guid? PatientId { get; set; }

        [DisplayName("医生ID")]
        public Guid? DoctorId { get; set; }

        [DisplayName("案例状态")]
        public string? CaseStatus { get; set; }

        [DisplayName("排序字段")]
        public string OrderBy { get; set; } = "CreateTime";

        [DisplayName("升序排序")]
        public bool IsAscending { get; set; } = false;
    }

    /// <summary>
    /// 医疗案例统计DTO - 继承统计DTO基础类
    /// </summary>
    public class MedicalCaseStatisticsDto : StatisticsDto
    {
        [DisplayName("进行中案例数量")]
        public int InProgressCount { get; set; }

        [DisplayName("已完成案例数量")]
        public int CompletedCount { get; set; }

        [DisplayName("已取消案例数量")]
        public int CancelledCount { get; set; }

        [DisplayName("平均完成时间(天)")]
        public double AverageCompletionDays { get; set; }

        [DisplayName("医生案例分布")]
        public Dictionary<string, int> DoctorCaseDistribution { get; set; } = new();

        [DisplayName("月度趋势")]
        public Dictionary<string, int> MonthlyTrend { get; set; } = new();
    }
}