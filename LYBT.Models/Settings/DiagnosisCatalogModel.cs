using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Models.Settings {

    /// <summary>
    /// 诊断目录项
    /// </summary>
    public class DiagnosisCatalogModel {
        [Key]
        [DisplayName("Id")]
        public Guid Id { get; set; }

        [Required, StringLength(64)]
        [DisplayName("Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(128)]
        [DisplayName("Description")]
        public string? Description { get; set; }

        [DisplayName("IsEnabled")]
        public bool IsEnabled { get; set; } = true;
    }
}