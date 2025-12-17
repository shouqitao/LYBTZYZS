using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Shared.Models.Enums;

namespace LYBT.Entities.Formulas
{

    /// <summary>
    /// 验方明细 - 验方中的药材组成，包含药材名称和剂量
    /// 根据用户要求：剂量使用整数，不继承IHerbItem接口
    /// 支持延迟绑定：允许先保存原始药材名称，稍后再绑定到药材库
    /// </summary>
    [Table("FormulaHerbItems")]
    public class FormulaHerbItem
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        [DisplayName("ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 所属验方ID
        /// </summary>
        [DisplayName("验方ID")]
        public Guid FormulaId { get; set; }

        /// <summary>
        /// 关联的验方实体
        /// </summary>
        [ForeignKey("FormulaId")]
        public virtual Formula? Formula { get; set; }

        /// <summary>
        /// 药材ID（可空，支持延迟绑定）
        /// </summary>
        [DisplayName("药材ID")]
        public Guid? HerbId { get; set; }

        /// <summary>
        /// 原始药材名称（从老系统导入时保存，用于延迟绑定）
        /// </summary>
        [StringLength(100)]
        [DisplayName("原始药材名称")]
        public string? OriginalHerbName { get; set; }

        /// <summary>
        /// 是否已验证绑定（true表示HerbId已绑定到药材库，默认false）
        /// </summary>
        [DisplayName("已验证")]
        public bool IsValidated { get; set; } = false;

        /// <summary>
        /// Gets or sets 药材名称.
        /// </summary>
        [Required]
        [StringLength(100)]
        [DisplayName("药材名称")]
        public string HerbName { get; set; } = string.Empty;

        /// <summary>
        /// 剂量（整数）
        /// </summary>
        [DisplayName("剂量")]
        public int Dosage { get; set; } = 1;

        /// <summary>
        /// Gets or sets 单位（从药材库继承，如：克、钱、两等）.
        /// </summary>
        [StringLength(16)]
        [DisplayName("单位")]
        public string Unit { get; set; } = "g";

        /// <summary>
        /// Gets or sets 用法说明（该药材的特殊用法）.
        /// </summary>
        [StringLength(200)]
        [DisplayName("用法")]
        public string? Usage { get; set; }

        /// <summary>
        /// Gets or sets 备注信息.
        /// </summary>
        [StringLength(200)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>
        /// Gets or sets 炮制方法.
        /// </summary>
        [StringLength(100)]
        [DisplayName("炮制方法")]
        public string? ProcessingMethod { get; set; }

        /// <summary>
        /// 煎法（先煎、后下等）
        /// </summary>
        [DisplayName("煎法")]
        public DecocteMethod DecocteMethod { get; set; } = DecocteMethod.Default;
    }
}
