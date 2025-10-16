using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Entities.Formula
{

    /// <summary>
    /// 验方实体 - UltraThink v2.0架构简化版
    /// 合并了原BaseFormula和FormulaModel，包含完整的验方信息
    /// 验方为模板，不含价格计算，只定义药材组成和剂量
    /// </summary>
    [Table("Formulas")]
    public class Formula : BaseEntity
    {

        /// <summary>验方名称</summary>
        [Required]
        [StringLength(100)]
        [DisplayName("验方名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>功效</summary>
        [StringLength(500)]
        [DisplayName("功效")]
        public string? Effect { get; set; }

        /// <summary>用法</summary>
        [StringLength(500)]
        [DisplayName("用法")]
        public string? Usage { get; set; }

        /// <summary>备注</summary>
        [StringLength(500)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>性味归经</summary>
        [StringLength(200)]
        [DisplayName("性味归经")]
        public string? Property { get; set; }

        /// <summary>验方状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;

        /// <summary>是否共享</summary>
        [DisplayName("是否共享")]
        public bool IsShared { get; set; } = false;

        /// <summary>
        /// 验证状态 - 标识验方是否已验证（Draft=草稿/未验证，Validated=已验证）
        /// 从老系统导入的验方初始为Draft状态，经过医生审核后标记为Validated
        /// </summary>
        [DisplayName("验证状态")]
        public FormulaValidationStatus ValidationStatus { get; set; } = FormulaValidationStatus.Draft;

        /// <summary>方剂分类</summary>
        [StringLength(50)]
        [DisplayName("分类")]
        public string? Category { get; set; }

        /// <summary>方剂类型（经典方/经验方）</summary>
        [DisplayName("方剂类型")]
        public FormulaType FormulaType { get; set; } = FormulaType.Experience;

        /// <summary>创建用户ID</summary>
        [DisplayName("创建用户")]
        public Guid? UserId { get; set; }

        /// <summary>
        /// 药材组成（方剂中包含的药材列表）
        /// </summary>
        [DisplayName("药材组成")]
        public List<FormulaHerbItem> Herbs { get; set; } = new();
    }

    /// <summary>
    /// 方剂类型枚举
    /// </summary>
    public enum FormulaType
    {
        /// <summary>经典方</summary>
        Classic = 1,
        /// <summary>经验方</summary>
        Experience = 2
    }
}
