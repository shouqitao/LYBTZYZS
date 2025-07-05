using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Billing.Dtos {
    /// <summary>
    /// 申请退款 DTO
    /// </summary>
    public class RequestRefundDto {
        [Required]
        public Guid BillingId { get; set; }

        [Required]
        [StringLength(128, ErrorMessage = "退款理由不能超过128个字符")]
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 退款操作 DTO（同意/拒绝）
    /// </summary>
    public class RefundActionDto {
        [Required]
        public Guid BillingId { get; set; }
    }
}
