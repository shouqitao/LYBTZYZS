using System;

namespace LYBT.Module.Settings.Dtos {
    public class TreatmentCatalogDto {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class TreatmentCatalogCreateDto {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Description { get; set; }
    }

    public class TreatmentCatalogEditDto {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public bool IsEnabled { get; set; }
    }
}
