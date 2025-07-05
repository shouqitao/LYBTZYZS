using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Settings {

    /// <summary>
    /// 诊断目录项
    /// </summary>
    public class DiagnosisCatalogModel {
        [Key]
        public Guid Id { get; set; }

        [Required, StringLength(64)]
        public string Name { get; set; } = string.Empty;

        [StringLength(128)]
        public string? Description { get; set; }

        public bool IsEnabled { get; set; } = true;
    }
}