namespace LYBT.WPF.Client.Core.Models.DiagnosisTreatment {
    /// <summary>
    /// 药材明细信息模型 - 前端专用
    /// </summary>
    public class HerbItemInfo {
        /// <summary>药材ID</summary>
        public Guid HerbId { get; set; }

        /// <summary>药材名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>药材名称（别名）</summary>
        public string HerbName { get; set; } = string.Empty;

        /// <summary>剂量</summary>
        public decimal Amount { get; set; }

        /// <summary>单价</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>小计（单价 × 剂量）</summary>
        public decimal TotalPrice => UnitPrice * Amount;

        /// <summary>单位（前端显示字段）</summary>
        public string? Unit { get; set; }

        /// <summary>备注（前端扩展字段）</summary>
        public string? Remark { get; set; }

        /// <summary>是否选中（用于批量操作）</summary>
        public bool IsSelected { get; set; }
    }
}