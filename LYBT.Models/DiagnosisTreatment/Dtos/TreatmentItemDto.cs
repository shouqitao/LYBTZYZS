using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.DiagnosisTreatment.Models.Dtos {

    /// <summary>
    /// 单个治疗项目 DTO（如针灸、正骨，每项含次数与价格）
    /// </summary>
    public class TreatmentItemDto {

        /// <summary>项目名称（如“针灸”）</summary>
        [Required(ErrorMessage = "治疗项目名称不能为空")]
        [DisplayName("项目名称（如“针灸”）")]
/// <summary>
/// Name 属性。
/// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>次数</summary>
        [Range(1, 99, ErrorMessage = "次数必须大于0")]
        [DisplayName("次数")]
/// <summary>
/// Count 属性。
/// </summary>
        public int Count { get; set; }

        /// <summary>单价</summary>
        [Range(0, double.MaxValue, ErrorMessage = "单价不能为负数")]
        [DisplayName("单价")]
/// <summary>
/// Price 属性。
/// </summary>
        public decimal Price { get; set; }

        /// <summary>小计（自动计算：单价 × 次数）</summary>
        public decimal Subtotal => Price * Count;
    }
}
