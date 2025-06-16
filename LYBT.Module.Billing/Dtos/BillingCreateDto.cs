using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Billing.Dtos {
    /// <summary>
    /// 新增账单 DTO
    /// </summary>
    public class BillingCreateDto {
        /// <summary>病人ID</summary>
        [Required(ErrorMessage = "病人ID不能为空")]
        public string PatientId { get; set; } = string.Empty;

        /// <summary>处方ID（可选）</summary>
        public string? PrescriptionId { get; set; }

        /// <summary>开单医生ID</summary>
        [Required(ErrorMessage = "开单医生ID不能为空")]
        public string DoctorId { get; set; } = string.Empty;

        /// <summary>账单明细列表</summary>
        public List<BillingItemDto> Items { get; set; } = new();

        /// <summary>账单总金额</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>已缴金额</summary>
        public decimal PaidAmount { get; set; }

        /// <summary>缴费状态</summary>
        public string PaymentStatus { get; set; } = "未缴费";

        /// <summary>缴费方式</summary>
        public string? PaymentMethod { get; set; }

        /// <summary>账单时间</summary>
        public DateTime BillingTime { get; set; } = DateTime.Now;

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 账单明细 DTO
    /// </summary>
    public class BillingItemDto {
        /// <summary>项目名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>单价</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>数量</summary>
        public decimal Quantity { get; set; }

        /// <summary>小计</summary>
        public decimal SubTotal => UnitPrice * Quantity;
    }
}
