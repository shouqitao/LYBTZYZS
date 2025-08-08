using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Herbs
{

    /// <summary>
    /// 药材库存预警DTO
    /// </summary>
    public class HerbStockWarningDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? PinYinCode { get; set; }
        public decimal Stock { get; set; }
        public decimal StockWarningLevel { get; set; }
        public string Unit { get; set; } = "克";
        public string? Supplier { get; set; }

        /// <summary>
        /// 缺货数量（预警值 - 当前库存）
        /// </summary>
        public decimal ShortageQuantity => StockWarningLevel - Stock;

        /// <summary>
        /// 预警级别：Critical(严重缺货) < 10%, Low(缺货) < 50%, Warning(预警) < 100%
        /// </summary>
        public string WarningLevel
        {
            get
            {
                var percentage = Stock / StockWarningLevel * 100;
                if (percentage < 10) return "Critical";
                if (percentage < 50) return "Low";
                return "Warning";
            }
        }
    }

    /// <summary>
    /// 药材库存统计DTO
    /// </summary>
    public class HerbStockStatisticsDto
    {
        /// <summary>
        /// 药材总数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 缺货药材数（库存为0）
        /// </summary>
        public int OutOfStockCount { get; set; }

        /// <summary>
        /// 预警药材数（低于预警值）
        /// </summary>
        public int WarningCount { get; set; }

        /// <summary>
        /// 充足药材数
        /// </summary>
        public int SufficientCount { get; set; }

        /// <summary>
        /// 库存总价值（库存量 × 成本价）
        /// </summary>
        public decimal TotalStockValue { get; set; }

        /// <summary>
        /// 即将过期药材数（30天内）
        /// </summary>
        public int ExpiringCount { get; set; }

        /// <summary>
        /// 已过期药材数
        /// </summary>
        public int ExpiredCount { get; set; }
    }

    /// <summary>
    /// 药材库存更新DTO
    /// </summary>
    public class HerbStockUpdateDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "库存量必须大于等于0")]
        public decimal NewStock { get; set; }

        /// <summary>
        /// 更新原因
        /// </summary>
        public string? Reason { get; set; }
    }

    /// <summary>
    /// 药材过期预警DTO
    /// </summary>
    public class HerbExpiryWarningDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? BatchNumber { get; set; }
        public decimal Stock { get; set; }
        public string Unit { get; set; } = "克";
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// 剩余天数
        /// </summary>
        public int DaysRemaining
        {
            get
            {
                if (!ExpiryDate.HasValue) return int.MaxValue;
                return (int)(ExpiryDate.Value - DateTime.Now).TotalDays;
            }
        }

        /// <summary>
        /// 是否已过期
        /// </summary>
        public bool IsExpired => DaysRemaining < 0;

        /// <summary>
        /// 预警级别
        /// </summary>
        public string WarningLevel
        {
            get
            {
                if (IsExpired) return "Expired";
                if (DaysRemaining <= 7) return "Critical";
                if (DaysRemaining <= 30) return "Warning";
                return "Normal";
            }
        }
    }
}