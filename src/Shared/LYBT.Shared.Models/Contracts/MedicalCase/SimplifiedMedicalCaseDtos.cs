using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 简化的医疗案例DTO - 去除过度复杂的业务逻辑方法
    /// </summary>
    public class SimplifiedMedicalCaseDto : StatusDto, IRemarkable
    {
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        [DisplayName("医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

        [DisplayName("诊疗时间")]
        public DateTime ConsultationDate { get; set; } = DateTime.Now;

        [DisplayName("案例状态")]
        public MedicalCaseStatus CaseStatus { get; set; } = MedicalCaseStatus.Active;

        [DisplayName("备注")]
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get; set; }

        // 简化的状态检查方法 - Epic #1612修正版
        public bool CanEdit() => CaseStatus == MedicalCaseStatus.Active || CaseStatus == MedicalCaseStatus.Draft;
        public bool IsCompleted() => CaseStatus == MedicalCaseStatus.Completed;
    }

    /// <summary>
    /// 简化的医疗案例详情DTO
    /// </summary>
    public class SimplifiedMedicalCaseDetailDto : SimplifiedMedicalCaseDto
    {
        [DisplayName("诊疗记录")]
        public ConsultationDto? Consultation { get; set; }

        [DisplayName("处方信息")]
        public PrescriptionDto? Prescription { get; set; }
    }

    /// <summary>
    /// 简化医疗案例输入DTO - 统一创建和更新
    /// Phase 3: 合并SimplifiedMedicalCaseCreateDto和SimplifiedMedicalCaseUpdateDto
    /// </summary>
    public class SimplifiedMedicalCaseInputDto
    {
        /// <summary>医疗案例ID（更新时必填，创建时为null）</summary>
        [DisplayName("医疗案例ID")]
        public Guid? Id { get; set; }

        /// <summary>患者ID（创建时必填）</summary>
        [DisplayName("患者ID")]
        public Guid? PatientId { get; set; }

        /// <summary>医生ID（创建时必填）</summary>
        [DisplayName("医生ID")]
        public Guid? DoctorId { get; set; }

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>案例状态（更新时可选）</summary>
        [DisplayName("状态")]
        public MedicalCaseStatus? CaseStatus { get; set; }
    }

    /// <summary>
    /// 简化的聚合创建DTO - 只包含核心组合逻辑
    /// </summary>
    public class SimplifiedMedicalCaseAggregateCreateDto
    {
        [Required(ErrorMessage = "医案信息不能为空")]
        [DisplayName("医案信息")]
        public SimplifiedMedicalCaseInputDto MedicalCase { get; set; } = new();

        [Required(ErrorMessage = "诊疗信息不能为空")]
        [DisplayName("诊疗信息")]
        public ConsultationInputDto Consultation { get; set; } = new();

        [DisplayName("处方信息")]
        public PrescriptionInputDto? Prescription { get; set; }
    }

    /// <summary>
    /// 简化的查询DTO
    /// </summary>
    public class SimplifiedMedicalCaseQueryDto : PagedQueryBaseDto
    {
        [DisplayName("患者ID")]
        public Guid? PatientId { get; set; }

        [DisplayName("医生ID")]
        public Guid? DoctorId { get; set; }

        [DisplayName("案例状态")]
        public MedicalCaseStatus? CaseStatus { get; set; }
    }
}
