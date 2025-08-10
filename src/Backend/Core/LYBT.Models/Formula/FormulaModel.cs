using LYBT.Shared.Models.Core;
using System.ComponentModel;

namespace LYBT.Models.Formula
{

    /// <summary>
    /// 验方实体 - 继承共享基础模型，数据库映射
    /// </summary>
    public class FormulaModel : BaseFormula
    {

        /// <summary>
        /// 药材组成（方剂中包含的药材列表）
        /// </summary>
        [DisplayName("药材组成")]
        public List<FormulaHerbItem> Herbs { get; set; } = new();
    }
}