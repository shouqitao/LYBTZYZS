using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Settings {

    /// <summary>
    /// 治疗项目目录项
    /// </summary>
    public class TreatmentCatalogModel {
        [Key]
        public Guid Id { get; set; }

        [Required, StringLength(64)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; }

        [StringLength(128)]
        public string? Description { get; set; }

        public bool IsEnabled { get; set; } = true;
    }
}