using System.ComponentModel;

namespace LYBT.Module.Settings.Models.Dtos {

    /// <summary>
    /// 表示TreatmentCatalogDto。
    /// </summary>
    public class TreatmentCatalogDto {

        [DisplayName("Id")]
        public Guid Id { get; set; }

        [DisplayName("Name")]
        public string Name { get; set; } = string.Empty;

        [DisplayName("Price")]
        public decimal Price { get; set; }

        [DisplayName("Description")]
        public string? Description { get; set; }

        [DisplayName("IsEnabled")]
        public bool IsEnabled { get; set; }
    }

    /// <summary>
    /// 表示TreatmentCatalogCreateDto。
    /// </summary>
    public class TreatmentCatalogCreateDto {

        [DisplayName("Name")]
        public string Name { get; set; } = string.Empty;

        [DisplayName("Price")]
        public decimal Price { get; set; }

        [DisplayName("Description")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// 表示TreatmentCatalogEditDto。
    /// </summary>
    public class TreatmentCatalogEditDto {

        [DisplayName("Id")]
        public Guid Id { get; set; }

        [DisplayName("Name")]
        public string Name { get; set; } = string.Empty;

        [DisplayName("Price")]
        public decimal Price { get; set; }

        [DisplayName("Description")]
        public string? Description { get; set; }

        [DisplayName("IsEnabled")]
        public bool IsEnabled { get; set; }
    }
}