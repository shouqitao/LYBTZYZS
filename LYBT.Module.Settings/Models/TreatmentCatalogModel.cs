using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Settings.Models {

    /// <summary>
    /// 治疗项目目录项
    /// </summary>
    public class TreatmentCatalogModel {

        [Key]
        [DisplayName("Id")]
        public Guid Id { get; set; }

        [Required, StringLength(64)]
        [DisplayName("Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [DisplayName("Price")]
        public decimal Price { get; set; }

        [StringLength(128)]
        [DisplayName("Description")]
        public string? Description { get; set; }

        [DisplayName("IsEnabled")]
        public bool IsEnabled { get; set; } = true;
    }
}