using System.ComponentModel;
namespace LYBT.Module.Settings.Dtos {

/// <summary>
/// 表示TreatmentCatalogDto。
/// </summary>
    public class TreatmentCatalogDto {
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
        [DisplayName("Price")]
/// <summary>
/// Price 属性。
/// </summary>
        public decimal Price { get; set; }
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
/// 表示TreatmentCatalogCreateDto。
/// </summary>
    public class TreatmentCatalogCreateDto {
        [DisplayName("Name")]
/// <summary>
/// Name 属性。
/// </summary>
        public string Name { get; set; } = string.Empty;
        [DisplayName("Price")]
/// <summary>
/// Price 属性。
/// </summary>
        public decimal Price { get; set; }
        [DisplayName("Description")]
/// <summary>
/// Description 属性。
/// </summary>
        public string? Description { get; set; }
    }

/// <summary>
/// 表示TreatmentCatalogEditDto。
/// </summary>
    public class TreatmentCatalogEditDto {
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
        [DisplayName("Price")]
/// <summary>
/// Price 属性。
/// </summary>
        public decimal Price { get; set; }
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
