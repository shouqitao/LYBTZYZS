namespace LYBT.WPF.Client.Core.Models.DiagnosisTreatment {
    /// <summary>
    /// 药方信息模型 - 前端专用
    /// </summary>
    public class FormulaInfo {
        /// <summary>药方名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>药材明细列表</summary>
        public List<HerbItemInfo> Herbs { get; set; } = new();

        /// <summary>药方总价（所有药材总价）</summary>
        public decimal TotalPrice => Herbs?.Sum(x => x.TotalPrice) ?? 0;

        /// <summary>付数（前端业务字段）</summary>
        public int Dosage { get; set; } = 1;

        /// <summary>用法说明（前端扩展字段）</summary>
        public string? Usage { get; set; }

        /// <summary>备注（前端扩展字段）</summary>
        public string? Remark { get; set; }
    }
}