using System;
using System.Collections.Generic;

namespace LYBT.Models.Billing {
    /// <summary>
    /// 账单主表实体
    /// </summary>
    public class BillingModel {
        /// <summary>
        /// 主键ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 账单业务编码（如流水号，可选）
        /// </summary>
        public string BillingId { get; set; } = string.Empty;

        /// <summary>
        /// 病人ID
        /// </summary>
        public Guid PatientId { get; set; }

        /// <summary>
        /// 对应处方ID
        /// </summary>
        public Guid PrescriptionId { get; set; }

        /// <summary>
        /// 账单明细项目（建议用 Json 字段保存）
        /// </summary>
        public List<BillingItem> Items { get; set; } = new();

        /// <summary>
        /// 账单总金额
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// 已缴金额
        /// </summary>
        public decimal PaidAmount { get; set; }

        /// <summary>
        /// 缴费状态（未缴费、已缴费、已取消等）
        /// </summary>
        public string PaymentStatus { get; set; } = "未缴费";

        /// <summary>
        /// 缴费方式（现金、微信等）
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// 开单医生ID
        /// </summary>
        public Guid DoctorId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 账单时间（如有二次缴费等场景可与 CreateTime 区分）
        /// </summary>
        public DateTime BillingTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 账单明细实体（可单独为表，也可作为 Json 字段保存）
    /// </summary>
    public class BillingItem {
        /// <summary>
        /// 明细主键ID（如不用单独建表可省略）
        /// </summary>
        public Guid ItemId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 项目名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 单价
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// 小计（单价 × 数量，自动计算）
        /// </summary>
        public decimal SubTotal => UnitPrice * Quantity;
    }
}
