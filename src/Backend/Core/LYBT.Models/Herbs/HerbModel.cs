using LYBT.Shared.Models.Core;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Herbs {

    /// <summary>
    /// 药材信息实体 - 中药材基础信息管理，支持软删除策略和快速检索
    /// 继承BaseHerbModel，添加后端特有字段
    /// </summary>
    public class HerbModel : BaseHerbModel {

        /// <summary>
        /// 药材基础规格数值（如：1，用于计算实际用量）
        /// </summary>
        [DisplayName("规格")]
        public decimal Specification { get; set; } = 1;

        /// <summary>
        /// 当前库存量（单位根据Unit字段）
        /// </summary>
        [DisplayName("库存量")]
        public decimal Stock { get; set; } = 0;

        /// <summary>
        /// 库存预警值（低于此值时需要预警）
        /// </summary>
        [DisplayName("库存预警值")]
        public decimal StockWarningLevel { get; set; } = 100;

        /// <summary>
        /// 最高库存限制（超过此值需要预警）
        /// </summary>
        [DisplayName("最高库存限制")]
        public decimal MaxStock { get; set; } = 10000;

        /// <summary>
        /// 成本价（元/单位，用于进货成本计算）
        /// </summary>
        [DisplayName("成本价")]
        public decimal CostPrice { get; set; } = 0;

        /// <summary>
        /// 会员价（元/单位，会员优惠价格）
        /// </summary>
        [DisplayName("会员价")]
        public decimal MemberPrice { get; set; } = 0;

        /// <summary>
        /// 特价（元/单位，促销价格）
        /// </summary>
        [DisplayName("特价")]
        public decimal? SpecialPrice { get; set; }

        /// <summary>
        /// 特价开始时间
        /// </summary>
        [DisplayName("特价开始时间")]
        public DateTime? SpecialPriceStartTime { get; set; }

        /// <summary>
        /// 特价结束时间
        /// </summary>
        [DisplayName("特价结束时间")]
        public DateTime? SpecialPriceEndTime { get; set; }

        /// <summary>
        /// 供应商信息
        /// </summary>
        [StringLength(200)]
        [DisplayName("供应商")]
        public string? Supplier { get; set; }

        /// <summary>
        /// 批号
        /// </summary>
        [StringLength(100)]
        [DisplayName("批号")]
        public string? BatchNumber { get; set; }

        /// <summary>
        /// 生产日期
        /// </summary>
        [DisplayName("生产日期")]
        public DateTime? ProductionDate { get; set; }

        /// <summary>
        /// 有效期至
        /// </summary>
        [DisplayName("有效期至")]
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// 最后操作者ID
        /// </summary>
        [DisplayName("最后操作者ID")]
        public Guid? LastOperatorId { get; set; }

        /// <summary>
        /// 最后操作者姓名
        /// </summary>
        [StringLength(50)]
        [DisplayName("最后操作者姓名")]
        public string? LastOperatorName { get; set; }
    }
}