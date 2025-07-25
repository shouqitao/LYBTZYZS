using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using LYBT.Common.Enums.Herbs;

namespace LYBT.Models.Herbs {

    /// <summary>
    /// 药材主表实体
    /// </summary>
    public class HerbModel {

        [Key]
        [DisplayName("编号")]
        public Guid Id { get; set; }

        [Required, StringLength(64)]
        [DisplayName("名称")]
        public string Name { get; set; } = string.Empty;

        [StringLength(32)]
        [DisplayName("拼音")]
        public string? Pinyin { get; set; }

        [StringLength(64)]
        [DisplayName("产地")]
        public string? Origin { get; set; }

        [StringLength(32)]
        [DisplayName("规格")]
        public string? Spec { get; set; }

        [StringLength(16)]
        [DisplayName("单位")]
        public string? Unit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("价格")]
        public decimal Price { get; set; }

        /// <summary>
        /// 库存数量
        /// </summary>
        [DisplayName("库存数量")]
        public int Stock { get; set; }

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

        [StringLength(128)]
        [DisplayName("功效")]
        public string? Effect { get; set; }

        [StringLength(256)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>
        /// 药材状态
        /// </summary>
        [DisplayName("药材状态")]
        public HerbStatus Status { get; set; } = HerbStatus.Active;

        /// <summary>
        /// 创建时间
        /// </summary>
        [DisplayName("创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 更新时间
        /// </summary>
        [DisplayName("更新时间")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 最后操作人ID
        /// </summary>
        [DisplayName("最后操作人ID")]
        public Guid? LastOperatorId { get; set; }

        /// <summary>
        /// 最后操作人姓名
        /// </summary>
        [StringLength(50)]
        [DisplayName("最后操作人姓名")]
        public string? LastOperatorName { get; set; }
    }
}