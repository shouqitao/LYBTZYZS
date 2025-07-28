using LYBT.Common.Enums.Herbs;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Models.Herbs {

    /// <summary>
    /// 药材信息实体 - 中药材基础信息管理，支持软删除策略和快速检索
    /// </summary>
    public class HerbModel {

        /// <summary>
        /// 药材唯一标识（主键）
        /// </summary>
        [Key]
        [DisplayName("药材ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 药材名称（如：当归、黄芪等）
        /// </summary>
        [Required, StringLength(64)]
        [DisplayName("药材名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 药材基础规格数值（如：1，用于计算实际用量）
        /// </summary>
        [DisplayName("规格")]
        public decimal Specification { get; set; } = 1;

        /// <summary>
        /// 计量单位（如：克、钱、两等）
        /// </summary>
        [StringLength(16)]
        [DisplayName("单位")]
        public string? Unit { get; set; }

        /// <summary>
        /// 药材单价
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("单价")]
        public decimal Price { get; set; }

        /// <summary>
        /// 使用方法说明
        /// </summary>
        [StringLength(256)]
        [DisplayName("用法")]
        public string? Usage { get; set; }

        /// <summary>
        /// 其他补充信息
        /// </summary>
        [StringLength(256)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>
        /// 药材名称拼音简码（用于快速搜索）
        /// </summary>
        [StringLength(32)]
        [DisplayName("拼音码")]
        public string? PinyinCode { get; set; }

        /// <summary>
        /// 药材名称五笔码（用于快速搜索）
        /// </summary>
        [StringLength(32)]
        [DisplayName("五笔码")]
        public string? WuBiCode { get; set; }

        /// <summary>
        /// 药材状态（启用/禁用，支持软删除策略）
        /// </summary>
        [DisplayName("药材状态")]
        public HerbStatus Status { get; set; } = HerbStatus.Active;

        /// <summary>
        /// 药材产地
        /// </summary>
        [StringLength(64)]
        [DisplayName("产地")]
        public string? Origin { get; set; }

        /// <summary>
        /// 药材功效
        /// </summary>
        [StringLength(256)]
        [DisplayName("功效")]
        public string? Effect { get; set; }

        /// <summary>
        /// 库存数量
        /// </summary>
        [DisplayName("库存数量")]
        public int Stock { get; set; } = 0;

        /// <summary>
        /// 批号
        /// </summary>
        [StringLength(32)]
        [DisplayName("批号")]
        public string? BatchNo { get; set; }

        /// <summary>
        /// 有效期
        /// </summary>
        [DisplayName("有效期")]
        public DateTime? ExpireDate { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [DisplayName("创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        [DisplayName("更新时间")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

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