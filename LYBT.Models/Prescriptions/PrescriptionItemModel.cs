using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Models.Prescriptions {
    /// <summary>
    /// 处方明细实体
    /// </summary>
    public class PrescriptionItemModel {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PrescriptionId { get; set; }

        [Required]
        public Guid HerbId { get; set; }

        [Required, StringLength(64)]
        public string HerbName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [StringLength(16)]
        public string? Unit { get; set; }

        [StringLength(64)]
        public string? Usage { get; set; }
    }
}
