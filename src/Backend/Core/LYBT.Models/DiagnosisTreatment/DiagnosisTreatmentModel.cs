using LYBT.Shared.Models.Core;
using System.ComponentModel;

namespace LYBT.Models.DiagnosisTreatment {

    /// <summary>
    /// 诊疗实体 - 继承共享基础模型，数据库映射
    /// </summary>
    public class DiagnosisTreatmentModel : BaseDiagnosisTreatmentModel {

        /// <summary>
        /// 治疗项目（如针灸、正骨等）
        /// </summary>
        [DisplayName("治疗项目")]
        public List<TreatmentItemModel> Treatments { get; set; } = new();

        /// <summary>
        /// 本次形成的独立治疗药方
        /// </summary>
        [DisplayName("治疗药方")]
        public FormulaModel? Formula { get; set; }
    }
}