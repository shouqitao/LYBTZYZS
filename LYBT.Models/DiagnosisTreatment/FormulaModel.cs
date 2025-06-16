using System;
using System.Collections.Generic;
using System.Linq;

namespace LYBT.Models.DiagnosisTreatment {
    /// <summary>
    /// 药方（治疗方）实体
    /// </summary>
    public class FormulaModel {
        /// <summary>
        /// 药方名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 药材明细列表
        /// </summary>
        public List<HerbItemModel> Herbs { get; set; } = new();

        /// <summary>
        /// 药方总价（所有药材总价）
        /// </summary>
        public decimal TotalPrice => Herbs?.Sum(x => x.TotalPrice) ?? 0;
    }
}
