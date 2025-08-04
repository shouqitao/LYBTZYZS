namespace LYBT.WPF.Client.Core.Models.Prescriptions {
    /// <summary>
    /// 处方药材项信息模型 - 前端专用
    /// </summary>
    public class PrescriptionItemInfo {
        /// <summary>处方项ID</summary>
        public Guid Id { get; set; }

        /// <summary>处方ID</summary>
        public Guid PrescriptionId { get; set; }

        /// <summary>药材ID</summary>
        public Guid HerbId { get; set; }

        /// <summary>药材名称</summary>
        public string HerbName { get; set; } = string.Empty;

        /// <summary>用量</summary>
        public decimal Quantity { get; set; }

        /// <summary>单位</summary>
        public string Unit { get; set; } = "g";

        /// <summary>单价</summary>
        public decimal UnitPrice { get; set; } = 0;

        /// <summary>小计金额</summary>
        public decimal Amount => UnitPrice * Quantity;

        /// <summary>用法说明</summary>
        public string? Usage { get; set; }

        /// <summary>备注信息</summary>
        public string? Remark { get; set; }

        /// <summary>产地（前端显示字段）</summary>
        public string? Origin { get; set; }

        /// <summary>规格（前端显示字段）</summary>
        public string? Specification { get; set; }

        /// <summary>是否缺货（前端状态字段）</summary>
        public bool IsOutOfStock { get; set; }

        /// <summary>是否选中（用于批量操作）</summary>
        public bool IsSelected { get; set; }
    }
}