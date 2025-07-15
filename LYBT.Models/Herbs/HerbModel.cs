using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace LYBT.Models {

    /// <summary>
    /// 药材主表实体
    /// </summary>
    public class HerbModel {

        [Key]
        [DisplayName("Id")]
        public Guid Id { get; set; }

        [Required, StringLength(64)]
        [DisplayName("Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(32)]
        [DisplayName("Pinyin")]
        public string? Pinyin { get; set; }

        [StringLength(64)]
        [DisplayName("Origin")]
        public string? Origin { get; set; }

        [StringLength(32)]
        [DisplayName("Spec")]
        public string? Spec { get; set; }

        [StringLength(16)]
        [DisplayName("Unit")]
        public string? Unit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("Price")]
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
        [DisplayName("Effect")]
        public string? Effect { get; set; }

        [StringLength(256)]
        [DisplayName("Remark")]
        public string? Remark { get; set; }
    }
}