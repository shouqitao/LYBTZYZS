using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Entities.Herbs
{

    /// <summary>
    /// 中药材实体 - UltraThink v2.0架构简化版
    /// 合并了原BaseHerb和HerbModel，不包含库存管理功能
    /// 只保留药材基础信息和价格信息，用于处方开具
    /// 继承BaseEntity实现审计字段自动化
    /// </summary>
    [Table("Herbs")]
    public class Herb : BaseEntity
    {

        // Id字段继承自BaseEntity

        /// <summary>药材名称</summary>
        [Required]
        [StringLength(100)]
        [DisplayName("药材名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>拼音码（用于快速搜索）</summary>
        [StringLength(50)]
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>产地</summary>
        [StringLength(100)]
        [DisplayName("产地")]
        public string? Origin { get; set; }

        /// <summary>规格</summary>
        [StringLength(100)]
        [DisplayName("规格")]
        public string? Spec { get; set; }

        /// <summary>单位（如：克、两、钱）</summary>
        [Required]
        [StringLength(10)]
        [DisplayName("单位")]
        public string Unit { get; set; } = "克";

        /// <summary>单价（元/单位）</summary>
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("单价")]
        public decimal Price { get; set; }

        /// <summary>成本价（元/单位）</summary>
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("成本价")]
        public decimal? CostPrice { get; set; }

        /// <summary>功效说明</summary>
        [StringLength(500)]
        [DisplayName("功效说明")]
        public string? Effect { get; set; }

        /// <summary>用法用量</summary>
        [StringLength(500)]
        [DisplayName("用法用量")]
        public string? Usage { get; set; }

        /// <summary>备注</summary>
        [StringLength(500)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>药材状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;
    }
}
