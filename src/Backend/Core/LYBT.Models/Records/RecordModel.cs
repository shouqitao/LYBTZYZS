using LYBT.Models.DiagnosisTreatment;
using LYBT.Shared.Models.Core;
using System.ComponentModel;

namespace LYBT.Models.Records {

    /// <summary>
    /// 病历实体 - 继承共享基础模型，数据库映射
    /// </summary>
    public class RecordModel : BaseRecordModel {

        /// <summary>辩证结果列表</summary>
        [DisplayName("辩证结果列表")]
        public List<string> DiagnosisResults { get; set; } = new();

        /// <summary>药材组成</summary>
        [DisplayName("药材组成")]
        public List<HerbItemModel>? HerbalFormula { get; set; }

        /// <summary>辅助治疗方案</summary>
        [DisplayName("辅助治疗方案")]
        public List<TreatmentItemModel>? TreatmentPlans { get; set; }

        /// <summary>关联的验方模板ID</summary>
        [DisplayName("验方模板ID")]
        public Guid? FormulaTemplateId { get; set; }

        /// <summary>关联的理疗项目ID列表</summary>
        [DisplayName("理疗项目ID列表")]
        public List<Guid>? TreatmentRoomIds { get; set; }

        /// <summary>共享给医生ID列表</summary>
        [DisplayName("共享给医生ID列表")]
        public List<string> SharedToDoctorIds { get; set; } = new();
    }
}