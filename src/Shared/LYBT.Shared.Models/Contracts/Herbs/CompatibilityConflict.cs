namespace LYBT.Shared.Models.Contracts.Herbs
{

    /// <summary>
    /// 配伍冲突信息
    /// </summary>
    public class CompatibilityConflict
    {
        public Guid Herb1Id { get; set; }
        public string Herb1Name { get; set; } = string.Empty;
        public Guid Herb2Id { get; set; }
        public string Herb2Name { get; set; } = string.Empty;
        public string ConflictType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
    }

}
