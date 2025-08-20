using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;

namespace LYBT.Desktop.Core.Models.Prescriptions
{
    /// <summary>
    /// 处方数据
    /// </summary>
    public class PrescriptionData
    {
        public List<PrescriptionItem> Items { get; set; } = new();
        public int Dosage { get; set; } = 7;  // 默认7剂
        public string Usage { get; set; } = "每日1剂，水煎服，分早晚两次温服";
        public decimal TotalPrice { get; set; }
        public decimal Discount { get; set; } = 1.0m;
    }

    /// <summary>
    /// 处方项
    /// </summary>
    public class PrescriptionItem
    {
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = "";
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "g";
        public decimal UnitPrice { get; set; }
        public string? ImportSource { get; set; }
        public string Remark { get; set; } = "";
        
        // 计算属性
        public decimal Subtotal => Quantity * UnitPrice;
        public string DisplayText => $"{HerbName} {Quantity}{Unit}";
        public string PriceText => $"￥{Subtotal:F2}";
    }
}
