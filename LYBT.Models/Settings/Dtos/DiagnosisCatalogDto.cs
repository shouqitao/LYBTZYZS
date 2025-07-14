namespace LYBT.Module.Settings.Dtos {

    public class DiagnosisCatalogDto {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class DiagnosisCatalogCreateDto {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class DiagnosisCatalogEditDto {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsEnabled { get; set; }
    }
}