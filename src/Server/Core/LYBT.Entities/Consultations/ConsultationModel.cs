using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Common;

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

        // 诊断核心字段（精简版 - OpenSpec: refactor-diagnosis-fields）

        /// <summary>现病史</summary>
        [StringLength(2000)]
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        /// <summary>舌诊</summary>
        [StringLength(500)]
        [DisplayName("舌诊")]
        public string? TongueDiagnosis { get; set; }

        /// <summary>脉诊</summary>
        [StringLength(500)]
        [DisplayName("脉诊")]
        public string? PulseDiagnosis { get; set; }

        /// <summary>中医辨证（必填）</summary>
        [StringLength(500)]
        [DisplayName("中医辨证")]
        public string? TCMDiagnosis { get; set; }

        // OpenSpec: consultation-field-alignment - PrescriptionEnabled已移除
        // 处方开关统一使用MedicalCase.NeedsPrescription

        // RowVersion、IsDeleted等字段继承自BaseEntity

        // 导航属性已移除 - OpenSpec: refactor-server-ddd-aggregates
        // Consultation作为MedicalCase聚合的内部实体，不应有反向导航
        // 需要MedicalCase信息时，通过Id查询或使用Query Service
    }
}
