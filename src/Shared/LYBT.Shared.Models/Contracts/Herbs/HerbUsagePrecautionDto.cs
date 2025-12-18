namespace LYBT.Shared.Models.Contracts.Herbs
{

    /// <summary>
    /// 药材使用注意事项DTO
    /// </summary>
    public class HerbUsagePrecautionDto
    {
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public List<string> Precautions { get; set; } = new List<string>();
        public List<string> Contraindications { get; set; } = new List<string>();
        public List<string> SideEffects { get; set; } = new List<string>();
        public string? MaxDailyDosage { get; set; }
        public string? PregnancyCategory { get; set; }
    }

}
