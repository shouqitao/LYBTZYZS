using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Herbs {

    /// <summary>
    /// 库存更新请求
    /// </summary>
    public class StockUpdateRequest {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "数量必须大于0")]
        public decimal Quantity { get; set; }
        
        [Required]
        public bool IsIncrease { get; set; }
        
        public string? Reason { get; set; }
    }

    /// <summary>
    /// 预警值设置请求
    /// </summary>
    public class WarningLevelRequest {
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "预警值必须大于等于0")]
        public decimal WarningLevel { get; set; }
        
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "最大库存必须大于等于0")]
        public decimal MaxStock { get; set; }
    }

    /// <summary>
    /// 特价设置请求
    /// </summary>
    public class SpecialPriceRequest {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "特价必须大于0")]
        public decimal SpecialPrice { get; set; }
        
        [Required]
        public DateTime StartTime { get; set; }
        
        [Required]
        public DateTime EndTime { get; set; }
        
        public string? Description { get; set; }
        
        // 验证结束时间必须大于开始时间
        public bool IsValid() {
            return EndTime > StartTime && StartTime >= DateTime.Now.Date;
        }
    }
}