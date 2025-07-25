using System.ComponentModel;

namespace LYBT.Module.Settings.Models.Dtos {

    /// <summary>
    /// 表示DiagnosisCatalogDto。
    /// </summary>
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

    /// <summary>
    /// 表示DiagnosisCatalogCreateDto。
    /// </summary>
    public class DiagnosisCatalogCreateDto {

        [DisplayName("Name")]
        public string Name { get; set; } = string.Empty;

        [DisplayName("Description")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// 表示DiagnosisCatalogEditDto。
    /// </summary>
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