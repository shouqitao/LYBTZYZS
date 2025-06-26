namespace LYBT.Models.Settings {

    /// <summary>
    /// 治疗项目目录项
    /// </summary>
    public class TreatmentCatalogModel {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}