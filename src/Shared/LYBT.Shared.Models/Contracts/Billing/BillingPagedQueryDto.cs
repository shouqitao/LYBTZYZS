using System;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Billing
{
    /// <summary>
    /// 账单分页查询DTO
    /// </summary>
    public class BillingPagedQueryDto : PagedQueryBaseDto
    {
        /// <summary>账单状态</summary>
        public BillingStatus? Status { get; set; }

        /// <summary>账单类型</summary>
        public string? BillingType { get; set; }

        /// <summary>患者ID</summary>
        public Guid? PatientId { get; set; }

        /// <summary>医生ID</summary>
        public Guid? DoctorId { get; set; }

        /// <summary>开始日期</summary>
        public DateTime? StartDate { get; set; }

        /// <summary>结束日期</summary>
        public DateTime? EndDate { get; set; }

        /// <summary>最小金额</summary>
        public decimal? MinAmount { get; set; }

        /// <summary>最大金额</summary>
        public decimal? MaxAmount { get; set; }

        /// <summary>支付方式</summary>
        public string? PaymentMethod { get; set; }

        /// <summary>是否已开发票</summary>
        public bool? IsInvoiced { get; set; }

        /// <summary>是否包含已删除</summary>
        public bool IncludeDeleted { get; set; } = false;
    }
}