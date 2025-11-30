using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Entities.Consultations
{

    /// <summary>
    /// 诊疗实体 - UltraThink v2.0架构简化版
    /// 合并了原BaseConsultation和ConsultationModel
    /// 专注于中医诊疗，包含中医四诊和辨证论治
    /// 作为MedicalCase的一部分，使用共享主键
    /// </summary>
    [Table("Consultations")]
    public class Consultation : BaseEntity
    {
        // Id字段与MedicalCase共享主键
        // 通过EF Core配置建立一对一关系

        // PatientId和UserId通过MedicalCase获取，不需要重复存储

        /// <summary>创建人ID（医生用户ID）</summary>
        // 审计字段（CreatedBy等）继承自BaseEntity

        /// <summary>主诉</summary>
        [StringLength(500)]
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        [StringLength(1000)]
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        // 中医四诊

        /// <summary>望诊</summary>
        [StringLength(500)]
        [DisplayName("望诊")]
        public string? Inspection { get; set; }

        /// <summary>闻诊</summary>
        [StringLength(500)]
        [DisplayName("闻诊")]
        public string? AuscultationOlfaction { get; set; }

        /// <summary>问诊</summary>
        [StringLength(500)]
        [DisplayName("问诊")]
        public string? Inquiry { get; set; }

        /// <summary>切诊（包含脉诊、舌诊等）</summary>
        [StringLength(500)]
        [DisplayName("切诊")]
        public string? Palpation { get; set; }

        // 中医诊断结果

        /// <summary>中医辨证</summary>
        [StringLength(500)]
        [DisplayName("中医辨证")]
        public string? TCMDiagnosis { get; set; }

        /// <summary>治疗原则</summary>
        [StringLength(500)]
        [DisplayName("治疗原则")]
        public string? TreatmentPrinciple { get; set; }

        /// <summary>医嘱</summary>
        [StringLength(1000)]
        [DisplayName("医嘱")]
        public string? MedicalAdvice { get; set; }

        /// <summary>备注信息</summary>
        [StringLength(500)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>
        /// 处方开关（true=开处方，false=不开处方）
        /// 注：与MedicalCase.NeedsPrescription同步，保留用于兼容
        /// </summary>
        [DisplayName("处方开关")]
        public bool PrescriptionEnabled { get; set; } = true;

        // RowVersion、IsDeleted等字段继承自BaseEntity

        // 导航属性

        /// <summary>
        /// 所属医疗案例（必需的，通过共享主键关联）
        /// </summary>
        [Required]
        public virtual MedicalCases.MedicalCase MedicalCase { get; set; } = null!;
    }
}
