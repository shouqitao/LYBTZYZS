using LYBT.Common.Enums;
using System.ComponentModel;

namespace LYBT.Module.Billing.Models.Dtos {

    /// <summary>
    /// 账单详情 DTO
    /// </summary>
    public class BillingDetailDto {

        /// <summary>账单ID</summary>
        [DisplayName("账单ID")]
        public Guid Id { get; set; }

        /// <summary>病人ID</summary>
        [DisplayName("病人ID")]
        public Guid PatientId { get; set; }

        /// <summary>病人姓名</summary>
        [DisplayName("病人姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>处方ID</summary>
        [DisplayName("处方ID")]
        public Guid? PrescriptionId { get; set; }

        /// <summary>开单医生ID</summary>
        [DisplayName("开单医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>账单明细</summary>
        [DisplayName("账单明细")]
        public List<BillingItemDto> Items { get; set; } = new();

        /// <summary>账单总金额</summary>
        [DisplayName("账单总金额")]
        public decimal TotalAmount { get; set; }

        /// <summary>已缴金额</summary>
        [DisplayName("已缴金额")]
        public decimal PaidAmount { get; set; }

        /// <summary>账单状态</summary>
        [DisplayName("账单状态")]
        public BillingStatus Status { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreatedTime { get; set; }

        /// <summary>支付时间</summary>
        [DisplayName("支付时间")]
        public DateTime? PaidTime { get; set; }

        /// <summary>完成时间</summary>
        [DisplayName("完成时间")]
        public DateTime? CompletedTime { get; set; }

        /// <summary>退款时间</summary>
        [DisplayName("退款时间")]
        public DateTime? RefundTime { get; set; }

        /// <summary>退款理由</summary>
        [DisplayName("退款理由")]
        public string? RefundReason { get; set; }

        /// <summary>缴费方式</summary>
        [DisplayName("缴费方式")]
        public string? PaymentMethod { get; set; }

        /// <summary>账单时间</summary>
        [DisplayName("账单时间")]
        public DateTime BillingTime { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}