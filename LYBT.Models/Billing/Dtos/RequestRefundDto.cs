using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.Billing.Dtos {
    /// <summary>
    /// ˿ DTO
    /// </summary>
    public class RequestRefundDto {
        [Required]
        [DisplayName("BillingId")]
/// <summary>
/// BillingId 属性。
/// </summary>
        public Guid BillingId { get; set; }

        [Required]
        [StringLength(128, ErrorMessage = "˿ɲܳ128ַ")]
        [DisplayName("Reason")]
/// <summary>
/// Reason 属性。
/// </summary>
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// ˿ DTOͬ/ܾ
    /// </summary>
    public class RefundActionDto {
        [Required]
        [DisplayName("BillingId")]
/// <summary>
/// BillingId 属性。
/// </summary>
        public Guid BillingId { get; set; }
    }
}
