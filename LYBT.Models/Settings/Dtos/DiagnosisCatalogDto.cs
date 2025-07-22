using System.ComponentModel;
namespace LYBT.Module.Settings.Dtos {

/// <summary>
/// 表示DiagnosisCatalogDto。
/// </summary>
    public class DiagnosisCatalogDto {
        [DisplayName("Id")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }
        [DisplayName("Name")]
/// <summary>
/// Name 属性。
/// </summary>
        public string Name { get; set; } = string.Empty;
        [DisplayName("Description")]
/// <summary>
/// Description 属性。
/// </summary>
        public string? Description { get; set; }
        [DisplayName("IsEnabled")]
/// <summary>
/// IsEnabled 属性。
/// </summary>
        public bool IsEnabled { get; set; }
    }

/// <summary>
/// 表示DiagnosisCatalogCreateDto。
/// </summary>
    public class DiagnosisCatalogCreateDto {
        [DisplayName("Name")]
/// <summary>
/// Name 属性。
/// </summary>
        public string Name { get; set; } = string.Empty;
        [DisplayName("Description")]
/// <summary>
/// Description 属性。
/// </summary>
        public string? Description { get; set; }
    }

/// <summary>
/// 表示DiagnosisCatalogEditDto。
/// </summary>
    public class DiagnosisCatalogEditDto {
        [DisplayName("Id")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }
        [DisplayName("Name")]
/// <summary>
/// Name 属性。
/// </summary>
        public string Name { get; set; } = string.Empty;
        [DisplayName("Description")]
/// <summary>
/// Description 属性。
/// </summary>
        public string? Description { get; set; }
        [DisplayName("IsEnabled")]
/// <summary>
/// IsEnabled 属性。
/// </summary>
        public bool IsEnabled { get; set; }
    }
}
