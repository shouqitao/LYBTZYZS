using System.ComponentModel;
namespace LYBT.Module.Settings.Dtos {

    public class DiagnosisCatalogDto {
        [DisplayName("Id")]
        public Guid Id { get; set; }
        [DisplayName("Name")]
        public string Name { get; set; } = string.Empty;
        [DisplayName("Description")]
        public string? Description { get; set; }
        [DisplayName("IsEnabled")]
        public bool IsEnabled { get; set; }
    }

    public class DiagnosisCatalogCreateDto {
        [DisplayName("Name")]
        public string Name { get; set; } = string.Empty;
        [DisplayName("Description")]
        public string? Description { get; set; }
    }

    public class DiagnosisCatalogEditDto {
        [DisplayName("Id")]
        public Guid Id { get; set; }
        [DisplayName("Name")]
        public string Name { get; set; } = string.Empty;
        [DisplayName("Description")]
        public string? Description { get; set; }
        [DisplayName("IsEnabled")]
        public bool IsEnabled { get; set; }
    }
}