namespace LYBT.Models.Settings {

    /// <summary>
    /// 诊断目录项
    /// </summary>
    public class DiagnosisCatalogModel {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}