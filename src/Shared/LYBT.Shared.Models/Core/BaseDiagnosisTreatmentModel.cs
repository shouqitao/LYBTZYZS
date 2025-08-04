using System.ComponentModel;

namespace LYBT.Shared.Models.Core {

    /// <summary>
    /// 诊疗基础模型 - 前后端共享核心字段
    /// 包含所有通用的诊疗信息字段，各层可基于此模型扩展
    /// </summary>
    public class BaseDiagnosisTreatmentModel {

        /// <summary>诊疗唯一标识</summary>
        [DisplayName("诊疗ID")]
        public Guid Id { get; set; }

        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>主诉</summary>
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        /// <summary>诊断类型ID</summary>
        [DisplayName("诊断类型ID")]
        public Guid DiagnosisCatalogId { get; set; }

        /// <summary>诊断内容</summary>
        [DisplayName("诊断内容")]
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>创建时间（统一命名）</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}