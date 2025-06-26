using LYBT.Module.Herbs.Dtos;

namespace LYBT.Module.DiagnosisTreatment.Models.Dtos {

    /// <summary>
    /// 药方 DTO（治疗方），含药材明细
    /// </summary>
    public class FormulaDto {

        /// <summary>药方名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>药材组成</summary>
        public List<HerbDto> Herbs { get; set; } = new();

        /// <summary>药方总价（自动计算）</summary>
        public decimal TotalPrice => Herbs?.Sum(x => x.TotalPrice) ?? 0;
    }

    /// <summary>
    /// 单味药材明细 DTO（引用 LYBT.Module.Herbs.Models.Dtos）
    /// </summary>
}