namespace LYBT.WPF.Client.Core.Models.DiagnosisTreatment {
    /// <summary>
    /// 治疗项目信息模型 - 前端专用
    /// </summary>
    public class TreatmentItemInfo {
        /// <summary>治疗项目名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>次数</summary>
        public int Count { get; set; }

        /// <summary>单价</summary>
        public decimal Price { get; set; }

        /// <summary>小计（单价 × 次数）</summary>
        public decimal Subtotal => Price * Count;

        /// <summary>备注（前端扩展字段）</summary>
        public string? Remark { get; set; }

        /// <summary>是否选中（用于批量操作）</summary>
        public bool IsSelected { get; set; }
    }
}