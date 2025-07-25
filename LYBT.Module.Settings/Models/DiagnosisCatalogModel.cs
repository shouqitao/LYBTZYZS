using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Settings.Models {

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