using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace LYBT.Models.Prescriptions {
    /// <summary>
    /// 处方明细实体
    /// </summary>
    public class PrescriptionItemModel {
        [Key]
        [DisplayName("Id")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        [Required]
        [DisplayName("PrescriptionId")]
/// <summary>
/// PrescriptionId 属性。
/// </summary>
        public Guid PrescriptionId { get; set; }

        [Required]
        [DisplayName("HerbId")]
/// <summary>
/// HerbId 属性。
/// </summary>
        public Guid HerbId { get; set; }

        [Required, StringLength(64)]
        [DisplayName("HerbName")]
/// <summary>
/// HerbName 属性。
/// </summary>
        public string HerbName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("Quantity")]
/// <summary>
/// Quantity 属性。
/// </summary>
        public decimal Quantity { get; set; }

        [StringLength(16)]
        [DisplayName("Unit")]
/// <summary>
/// Unit 属性。
/// </summary>
        public string? Unit { get; set; }

        [StringLength(64)]
        [DisplayName("Usage")]
/// <summary>
/// Usage 属性。
/// </summary>
        public string? Usage { get; set; }
    }
}
