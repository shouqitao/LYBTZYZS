using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.Billing.Dtos {
    /// <summary>
    /// 退款申请 DTO
    /// </summary>
    public class RequestRefundDto {
        [Required]
        [DisplayName("BillingId")]
        public Guid BillingId { get; set; }

        [Required]
        [StringLength(128, ErrorMessage = "原因长度不能超过128个字符")]
        [DisplayName("Reason")]
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 退款审核 DTO
    /// </summary>
    public class RefundActionDto {
        [Required]
        [DisplayName("BillingId")]
        public Guid BillingId { get; set; }
    }
}
