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
        /// <summary>
        /// Id 属性。
        /// </summary>
        public Guid Id { get; set; }

        [Required, StringLength(64)]
        [DisplayName("名称")]
        /// <summary>
        /// Name 属性。
        /// </summary>
        public string Name { get; set; } = string.Empty;

        [StringLength(32)]
        [DisplayName("拼音")]
        /// <summary>
        /// Pinyin 属性。
        /// </summary>
        public string? Pinyin { get; set; }

        [StringLength(64)]
        [DisplayName("产地")]
        /// <summary>
        /// Origin 属性。
        /// </summary>
        public string? Origin { get; set; }

        [StringLength(32)]
        [DisplayName("规格")]
        /// <summary>
        /// Spec 属性。
        /// </summary>
        public string? Spec { get; set; }

        [StringLength(16)]
        [DisplayName("单位")]
        /// <summary>
        /// Unit 属性。
        /// </summary>
        public string? Unit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("价格")]
        /// <summary>
        /// Price 属性。
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 库存数量
        /// </summary>
        [DisplayName("库存数量")]
        /// <summary>
        /// Stock 属性。
        /// </summary>
        public int Stock { get; set; }

        /// <summary>
        /// 批号
        /// </summary>
        [StringLength(32)]
        [DisplayName("批号")]
        /// <summary>
        /// BatchNo 属性。
        /// </summary>
        public string? BatchNo { get; set; }

        /// <summary>
        /// 有效期
        /// </summary>
        [DisplayName("有效期")]
        /// <summary>
        /// ExpireDate 属性。
        /// </summary>
        public DateTime? ExpireDate { get; set; }

        [StringLength(128)]
        [DisplayName("功效")]
        /// <summary>
        /// Effect 属性。
        /// </summary>
        public string? Effect { get; set; }

        [StringLength(256)]
        [DisplayName("备注")]
        /// <summary>
        /// Remark 属性。
        /// </summary>
        public string? Remark { get; set; }

        public HerbStatus Status { get; set; } = HerbStatus.Active;
    }
}
