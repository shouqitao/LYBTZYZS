using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Models.Prescriptions {

    /// <summary>
    /// 处方明细实体
    /// </summary>
    public class PrescriptionItemModel {

        [Key]
        [DisplayName("Id")]
        public Guid Id { get; set; }

        [Required]
        [DisplayName("PrescriptionId")]
        public Guid PrescriptionId { get; set; }

        [Required]
        [DisplayName("HerbId")]
        public Guid HerbId { get; set; }

        [Required, StringLength(64)]
        [DisplayName("HerbName")]
        public string HerbName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("Quantity")]
        public decimal Quantity { get; set; }

        [StringLength(16)]
        [DisplayName("Unit")]
        public string? Unit { get; set; }

        [StringLength(64)]
        [DisplayName("Usage")]
        public string? Usage { get; set; }
    }
}