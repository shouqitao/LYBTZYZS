using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.Billing.Dtos {
    /// <summary>
    /// ˿ DTO
    /// </summary>
    public class RequestRefundDto {
        [Required]
        [DisplayName("BillingId")]
        public Guid BillingId { get; set; }

        [Required]
        [StringLength(128, ErrorMessage = "˿ɲܳ128ַ")]
        [DisplayName("Reason")]
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// ˿ DTOͬ/ܾ
    /// </summary>
    public class RefundActionDto {
        [Required]
        [DisplayName("BillingId")]
        public Guid BillingId { get; set; }
    }
}
