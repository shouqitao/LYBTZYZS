using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Common;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Herbs
{
    /// <summary>
    /// 方剂药材成分DTO - 前后端共享API契约
    /// 用于在药方中表示单味药材的用量和计价信息
    /// </summary>
    public class FormulaIngredientDto
    {
        /// <summary>药材ID</summary>
        [Required(ErrorMessage = "药材ID不能为空")]
        [DisplayName("药材ID")]
        public Guid HerbId { get; set; }

        /// <summary>药材名称</summary>
        [DisplayName("药材名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>单位</summary>
        [DisplayName("单位")]
        public string? Unit { get; set; }

        /// <summary>单价</summary>
        [DisplayName("单价")]
        public decimal Price { get; set; }

        /// <summary>数量</summary>
        [Required(ErrorMessage = "数量不能为空")]
        [Range(0.1, 999999, ErrorMessage = "数量必须大于0")]
        [DisplayName("数量")]
        public decimal Quantity { get; set; }

        /// <summary>小计（自动计算）</summary>
        [DisplayName("小计")]
        public decimal TotalPrice => Price * Quantity;

        /// <summary>备注</summary>
        [StringLength(200, ErrorMessage = "备注长度不能超过200个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 中药材导入DTO - 前后端共享API契约
    /// 用于批量导入中药材档案的请求模型
    /// </summary>
    public class HerbImportDto
    {
        /// <summary>药材名称</summary>
        [Required(ErrorMessage = "药材名称不能为空")]
        [StringLength(100, ErrorMessage = "药材名称长度不能超过100个字符")]
        [DisplayName("药材名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>产地</summary>
        [StringLength(100, ErrorMessage = "产地长度不能超过100个字符")]
        [DisplayName("产地")]
        public string? Origin { get; set; }

        /// <summary>规格</summary>
        [StringLength(50, ErrorMessage = "规格长度不能超过50个字符")]
        [DisplayName("规格")]
        public string? Spec { get; set; }

        /// <summary>单位</summary>
        [StringLength(20, ErrorMessage = "单位长度不能超过20个字符")]
        [DisplayName("单位")]
        public string? Unit { get; set; }

        /// <summary>单价</summary>
        [Range(0, 999999.99, ErrorMessage = "单价必须在0-999999.99之间")]
        [DisplayName("单价")]
        public decimal Price { get; set; }

        /// <summary>库存数量</summary>
        [Range(0, int.MaxValue, ErrorMessage = "库存数量不能为负数")]
        [DisplayName("库存数量")]
        public int Stock { get; set; }

        /// <summary>批号</summary>
        [StringLength(50, ErrorMessage = "批号长度不能超过50个字符")]
        [DisplayName("批号")]
        public string? BatchNo { get; set; }

        /// <summary>有效期</summary>
        [DisplayName("有效期")]
        public DateTime? ExpireDate { get; set; }

        /// <summary>功效说明</summary>
        [StringLength(1000, ErrorMessage = "功效说明长度不能超过1000个字符")]
        [DisplayName("功效说明")]
        public string? Effect { get; set; }

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 中药材状态更新DTO - 前后端共享API契约
    /// 用于更新中药材状态的请求模型
    /// </summary>
    public class CommonStatusUpdateDto
    {
        /// <summary>药材ID</summary>
        [Required(ErrorMessage = "药材ID不能为空")]
        [DisplayName("药材ID")]
        public Guid Id { get; set; }

        /// <summary>药材状态</summary>
        [DisplayName("药材状态")]
        public CommonStatus Status { get; set; }

        /// <summary>是否启用</summary>
        [DisplayName("是否启用")]
        public bool IsEnabled { get; set; }

        /// <summary>更新原因</summary>
        [StringLength(500, ErrorMessage = "更新原因长度不能超过500个字符")]
        [DisplayName("更新原因")]
        public string? Reason { get; set; }

        /// <summary>更新备注</summary>
        [StringLength(500, ErrorMessage = "更新备注长度不能超过500个字符")]
        [DisplayName("更新备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 药材价格更新DTO
    /// </summary>
    public class HerbPriceUpdateDto
    {
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
    public class HerbPriceHistoryDto
    {
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
        public decimal PriceChangePercentage
        {
            get
            {
                if (OldPrice == 0) return 0;
                return Math.Round((NewPrice - OldPrice) / OldPrice * 100, 2);
            }
        }
    }

    /// <summary>
    /// 特价设置DTO
    /// </summary>
    public class HerbSpecialPriceDto
    {
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

    /// <summary>
    /// 库存更新请求
    /// </summary>
    public class StockUpdateRequest
    {
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
    public class WarningLevelRequest
    {
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
    public class SpecialPriceRequest
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "特价必须大于0")]
        public decimal SpecialPrice { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public string? Description { get; set; }

        // 验证结束时间必须大于开始时间
        public bool IsValid()
        {
            return EndTime > StartTime && StartTime >= DateTime.Now.Date;
        }
    }

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
    /// 药材库存统计DTO - 继承HerbStatisticsDto保持UltraThink架构统一
    /// </summary>
    public class HerbStockStatisticsDto : HerbStatisticsDto
    {
        /// <summary>
        /// 缺货药材数（库存为0） - 重写基类属性
        /// </summary>
        public new int OutOfStockCount { get; set; }

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
        /// 即将过期药材数（30天内） - 重写基类属性
        /// </summary>
        public new int NearExpiryCount { get; set; }

        /// <summary>
        /// 即将过期药材数（30天内） - UltraThink兼容性别名
        /// </summary>
        public int ExpiringCount { get => NearExpiryCount; set => NearExpiryCount = value; }

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