using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Herbs {

    /// <summary>
    /// 药材价格更新DTO
    /// </summary>
    public class HerbPriceUpdateDto {
        [Required]
        public Guid Id { get; set; }
        
        /// <summary>
        /// 成本价（元/单位）
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "成本价必须大于等于0")]
        public decimal? CostPrice { get; set; }
        
        /// <summary>
        /// 零售价（元/单位）
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "零售价必须大于等于0")]
        public decimal? Price { get; set; }
        
        /// <summary>
        /// 会员价（元/单位）
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "会员价必须大于等于0")]
        public decimal? MemberPrice { get; set; }
        
        /// <summary>
        /// 更新原因
        /// </summary>
        public string? Reason { get; set; }
    }

    /// <summary>
    /// 药材价格历史记录DTO
    /// </summary>
    public class HerbPriceHistoryDto {
        public Guid Id { get; set; }
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        
        /// <summary>
        /// 原成本价
        /// </summary>
        public decimal OldCostPrice { get; set; }
        
        /// <summary>
        /// 新成本价
        /// </summary>
        public decimal NewCostPrice { get; set; }
        
        /// <summary>
        /// 原零售价
        /// </summary>
        public decimal OldPrice { get; set; }
        
        /// <summary>
        /// 新零售价
        /// </summary>
        public decimal NewPrice { get; set; }
        
        /// <summary>
        /// 原会员价
        /// </summary>
        public decimal OldMemberPrice { get; set; }
        
        /// <summary>
        /// 新会员价
        /// </summary>
        public decimal NewMemberPrice { get; set; }
        
        /// <summary>
        /// 变更时间
        /// </summary>
        public DateTime ChangeTime { get; set; }
        
        /// <summary>
        /// 操作者
        /// </summary>
        public string? OperatorName { get; set; }
        
        /// <summary>
        /// 变更原因
        /// </summary>
        public string? Reason { get; set; }
        
        /// <summary>
        /// 价格变化百分比（零售价）
        /// </summary>
        public decimal PriceChangePercentage {
            get {
                if (OldPrice == 0) return 0;
                return Math.Round((NewPrice - OldPrice) / OldPrice * 100, 2);
            }
        }
    }

    /// <summary>
    /// 特价设置DTO
    /// </summary>
    public class HerbSpecialPriceDto {
        [Required]
        public Guid Id { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "特价必须大于0")]
        public decimal SpecialPrice { get; set; }
        
        [Required]
        public DateTime StartTime { get; set; }
        
        [Required]
        public DateTime EndTime { get; set; }
        
        /// <summary>
        /// 促销说明
        /// </summary>
        public string? Description { get; set; }
    }
}