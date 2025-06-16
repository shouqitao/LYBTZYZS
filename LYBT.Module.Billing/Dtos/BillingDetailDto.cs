using System;
using System.Collections.Generic;

namespace LYBT.Module.Billing.Dtos {
    /// <summary>
    /// 账单详情 DTO
    /// </summary>
    public class BillingDetailDto {
        /// <summary>账单ID</summary>
        public Guid Id { get; set; }

        /// <summary>病人ID</summary>
        public string PatientId { get; set; } = string.Empty;

        /// <summary>病人姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>处方ID</summary>
        public string? PrescriptionId { get; set; }

        /// <summary>开单医生ID</summary>
        public string DoctorId { get; set; } = string.Empty;

        /// <summary>账单明细</summary>
        public List<BillingItemDto> Items { get; set; } = new();

        /// <summary>账单总金额</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>已缴金额</summary>
        public decimal PaidAmount { get; set; }

        /// <summary>缴费状态</summary>
        public string PaymentStatus { get; set; } = string.Empty;

        /// <summary>缴费方式</summary>
        public string? PaymentMethod { get; set; }

        /// <summary>账单时间</summary>
        public DateTime BillingTime { get; set; }

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}
