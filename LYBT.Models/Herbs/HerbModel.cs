using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Models {

    /// <summary>
    /// 药材主表实体
    /// </summary>
    public class HerbModel {

        [Key]
        public Guid Id { get; set; }

        [Required, StringLength(64)]
        public string Name { get; set; } = string.Empty;

        [StringLength(32)]
        public string? Pinyin { get; set; }

        [StringLength(64)]
        public string? Origin { get; set; }

        [StringLength(32)]
        public string? Spec { get; set; }

        [StringLength(16)]
        public string? Unit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [StringLength(128)]
        public string? Effect { get; set; }

        [StringLength(256)]
        public string? Remark { get; set; }
    }
}