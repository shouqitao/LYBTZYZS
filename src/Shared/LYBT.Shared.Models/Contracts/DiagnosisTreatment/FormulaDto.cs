using LYBT.Shared.Models.Contracts.Herbs;
using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.DiagnosisTreatment {

    /// <summary>
    /// 药方 DTO（治疗方），含药材明细
    /// </summary>
    public class FormulaDto {

        /// <summary>药方名称</summary>
        [DisplayName("药方名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>药材组成</summary>
        [DisplayName("药材组成")]
        public List<FormulaIngredientDto> Herbs { get; set; } = new();

        /// <summary>药方总价（自动计算）</summary>
        public decimal TotalPrice => Herbs?.Sum(x => x.TotalPrice) ?? 0;
    }
}