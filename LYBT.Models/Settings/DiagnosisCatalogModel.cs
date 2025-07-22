using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Models.Settings {

    /// <summary>
    /// 诊断目录项
    /// </summary>
    public class DiagnosisCatalogModel {
        [Key]
        [DisplayName("Id")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        [Required, StringLength(64)]
        [DisplayName("Name")]
/// <summary>
/// Name 属性。
/// </summary>
        public string Name { get; set; } = string.Empty;

        [StringLength(128)]
        [DisplayName("Description")]
/// <summary>
/// Description 属性。
/// </summary>
        public string? Description { get; set; }

        [DisplayName("IsEnabled")]
/// <summary>
/// IsEnabled 属性。
/// </summary>
        public bool IsEnabled { get; set; } = true;
    }
}
