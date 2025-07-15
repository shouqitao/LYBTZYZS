using System.ComponentModel;
namespace LYBT.Module.Settings.Dtos {

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

    public class TreatmentCatalogCreateDto {
        [DisplayName("Name")]
        public string Name { get; set; } = string.Empty;
        [DisplayName("Price")]
        public decimal Price { get; set; }
        [DisplayName("Description")]
        public string? Description { get; set; }
    }

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