using System.ComponentModel;
namespace LYBT.Models.DiagnosisTreatment {

    /// <summary>
    /// 治疗项目实体（如针灸等，一条诊疗可有多个项目）
    /// </summary>
    public class TreatmentItemModel {

        /// <summary>
        /// 治疗项目名称（如“针灸”）
        /// </summary>
        [DisplayName("治疗项目名称（如“针灸”）")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 次数
        /// </summary>
        [DisplayName("次数")]
        public int Count { get; set; }

        /// <summary>
        /// 单价
        /// </summary>
        [DisplayName("单价")]
        public decimal Price { get; set; }

        /// <summary>
        /// 小计（单价 × 次数）
        /// </summary>
        public decimal Subtotal => Price * Count;
    }
}