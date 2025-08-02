using LYBT.Shared.Models.Contracts.DiagnosisTreatment;
using LYBT.Shared.Models.Contracts.Herbs;
using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Records {

    /// <summary>
    /// 病历详情 DTO
    /// </summary>
    public class RecordDetailDto {

        /// <summary>病历ID</summary>
        [DisplayName("病历ID")]
        public Guid Id { get; set; }

        /// <summary>病人ID</summary>
        [DisplayName("病人ID")]
        public string PatientId { get; set; } = string.Empty;

        /// <summary>病人姓名</summary>
        [DisplayName("病人姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>挂号ID</summary>
        [DisplayName("挂号ID")]
        public Guid RegistrationId { get; set; }

        /// <summary>诊断内容</summary>
        [DisplayName("诊断内容")]
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>主诉</summary>
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        /// <summary>诊疗建议</summary>
        [DisplayName("诊疗建议")]
        public string? TreatmentAdvice { get; set; }

        /// <summary>开方信息（如药方ID）</summary>
        [DisplayName("开方信息（如药方ID）")]
        public Guid? PrescriptionId { get; set; }

        /// <summary>辩证结果列表</summary>
        [DisplayName("辩证结果列表")]
        public List<string> DiagnosisResults { get; set; } = new();

        /// <summary>药材组成</summary>
        [DisplayName("药材组成")]
        public List<FormulaIngredientDto>? HerbalFormula { get; set; }

        /// <summary>辅助治疗方案</summary>
        [DisplayName("辅助治疗方案")]
        public List<TreatmentItemDto>? TreatmentPlans { get; set; }

        /// <summary>是否共享</summary>
        [DisplayName("是否共享")]
        public bool IsShared { get; set; }

        /// <summary>共享给医生ID列表</summary>
        [DisplayName("共享给医生ID列表")]
        public List<string> SharedToDoctorIds { get; set; } = new();

        /// <summary>创建医生ID</summary>
        [DisplayName("创建医生ID")]
        public string? CreatedBy { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreatedTime { get; set; }

        /// <summary>病历创建/修改时间</summary>
        [DisplayName("病历创建/修改时间")]
        public DateTime RecordTime { get; set; }
    }
}