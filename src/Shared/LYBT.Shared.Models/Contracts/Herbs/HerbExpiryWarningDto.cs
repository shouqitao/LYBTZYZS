namespace LYBT.Shared.Models.Contracts.Herbs
{

    /// <summary>
    /// 药材过期预警DTO
    /// </summary>
    public class HerbExpiryWarningDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? BatchNumber { get; set; }
        public string Unit { get; set; } = "克";
        public DateTime? ExpiryDate { get; set; }

        /// <summary>剩余天数（由Service层计算）</summary>
        public int DaysRemaining { get; set; }

        /// <summary>是否已过期（由Service层计算）</summary>
        public bool IsExpired { get; set; }

        /// <summary>预警级别（由Service层计算）</summary>
        public string WarningLevel { get; set; } = "Normal";
    }

}
