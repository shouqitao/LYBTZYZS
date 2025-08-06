using System;

namespace LYBT.Shared.Models.Frontend.Cashier
{
    /// <summary>
    /// 收银前端模型（替代BillingInfo）
    /// </summary>
    public class CashierInfo
    {
        /// <summary>
        /// ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 医疗案例ID
        /// </summary>
        public Guid MedicalCaseId { get; set; }

        /// <summary>
        /// 患者姓名
        /// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>
        /// 发票号
        /// </summary>
        public string InvoiceNumber { get; set; } = string.Empty;

        /// <summary>
        /// 总金额
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// 折扣金额
        /// </summary>
        public decimal DiscountAmount { get; set; }

        /// <summary>
        /// 实收金额
        /// </summary>
        public decimal ActualAmount { get; set; }

        /// <summary>
        /// 支付方式
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// 支付方式显示名称
        /// </summary>
        public string PaymentMethodName { get; set; } = string.Empty;

        /// <summary>
        /// 支付状态
        /// </summary>
        public string PaymentStatus { get; set; } = string.Empty;

        /// <summary>
        /// 支付状态显示名称
        /// </summary>
        public string PaymentStatusName { get; set; } = string.Empty;

        /// <summary>
        /// 支付时间
        /// </summary>
        public DateTime? PaymentTime { get; set; }

        /// <summary>
        /// 退费金额
        /// </summary>
        public decimal? RefundAmount { get; set; }

        /// <summary>
        /// 退费原因
        /// </summary>
        public string RefundReason { get; set; } = string.Empty;

        /// <summary>
        /// 退费时间
        /// </summary>
        public DateTime? RefundTime { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// 收银详情前端模型
    /// </summary>
    public class CashierDetailInfo : CashierInfo
    {
        /// <summary>
        /// 医生姓名
        /// </summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>
        /// 费用明细列表
        /// </summary>
        public List<BillItemInfo> BillItems { get; set; } = new List<BillItemInfo>();
    }

    /// <summary>
    /// 费用明细前端模型
    /// </summary>
    public class BillItemInfo
    {
        /// <summary>
        /// ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 项目名称
        /// </summary>
        public string ItemName { get; set; } = string.Empty;

        /// <summary>
        /// 项目类型
        /// </summary>
        public string ItemType { get; set; } = string.Empty;

        /// <summary>
        /// 数量
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// 单价
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 小计
        /// </summary>
        public decimal Subtotal => Quantity * UnitPrice;

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; } = string.Empty;
    }
}