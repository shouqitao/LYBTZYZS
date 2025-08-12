using System;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Core.Models.Prescriptions
{
    /// <summary>
    /// 处方项目信息 - 前端显示模型
    /// </summary>
    public class PrescriptionItemInfo : PrescriptionItemDto
    {
        /// <summary>
        /// 药材名称（冗余字段，提高显示性能）
        /// </summary>
        public string HerbName { get; set; } = string.Empty;

        /// <summary>
        /// 单价
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 小计金额
        /// </summary>
        public decimal Subtotal { get; set; }

        /// <summary>
        /// 是否选中（用于批量操作）
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// 是否有效（用于验证）
        /// </summary>
        public bool IsValid => Quantity > 0 && Price >= 0;

        /// <summary>
        /// 显示文本
        /// </summary>
        public string DisplayText => $"{HerbName} {Quantity}{Unit} × ¥{Price:F2} = ¥{Subtotal:F2}";

        /// <summary>
        /// 计算小计
        /// </summary>
        public void CalculateSubtotal()
        {
            Subtotal = Quantity * Price;
        }
    }
}