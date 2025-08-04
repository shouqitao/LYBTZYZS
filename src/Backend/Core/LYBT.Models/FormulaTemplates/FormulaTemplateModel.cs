using LYBT.Shared.Models.Core;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.FormulaTemplates {

    /// <summary>
    /// 经验方模板实体 - 继承共享基础模型，数据库映射
    /// </summary>
    public class FormulaTemplateModel : BaseFormulaTemplateModel {

        /// <summary>
        /// 药材组成（方剂中包含的药材列表）
        /// </summary>
        [DisplayName("药材组成")]
        public List<FormulaTemplateHerbItem> Herbs { get; set; } = new();
    }
}