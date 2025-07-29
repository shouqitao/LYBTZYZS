using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.FormulaTemplates {

    /// <summary>
    /// 经验方模板实体 - 传统中医经验方管理，支持软删除策略和验方共享
    /// </summary>
    public class FormulaTemplateModel {

        /// <summary>
        /// 模板唯一标识（主键）
        /// </summary>
        [DisplayName("模板ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 经验方名称（如：逍遥散、六味地黄丸等）
        /// </summary>
        [Required, StringLength(200)]
        [DisplayName("经验方名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 方剂主要功效和治疗作用
        /// </summary>
        [StringLength(500)]
        [DisplayName("功效")]
        public string? Effect { get; set; }

        /// <summary>
        /// 服用方法、煎煮方式等用法说明
        /// </summary>
        [StringLength(500)]
        [DisplayName("用法")]
        public string? Usage { get; set; }

        /// <summary>
        /// 方剂适应症、注意事项等补充信息
        /// </summary>
        [StringLength(1000)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>
        /// 药材组成（方剂中包含的药材列表）
        /// </summary>
        [DisplayName("药材组成")]
        public List<FormulaTemplateHerbItem> Herbs { get; set; } = new();

        /// <summary>
        /// 配方性味归经（中医理论属性）
        /// </summary>
        [StringLength(200)]
        [DisplayName("性味归经")]
        public string? Property { get; set; }

        /// <summary>
        /// 是否启用（支持软删除策略）
        /// </summary>
        [DisplayName("是否启用")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 是否共享给其他医生使用
        /// </summary>
        [DisplayName("是否共享")]
        public bool IsShared { get; set; } = false;

        /// <summary>
        /// 创建者ID
        /// </summary>
        [DisplayName("创建者ID")]
        public Guid? CreatedById { get; set; }

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
        /// 共享时间
        /// </summary>
        [DisplayName("共享时间")]
        public DateTime? SharedAt { get; set; }

        /// <summary>
        /// 共享者ID
        /// </summary>
        [DisplayName("共享者ID")]
        public Guid? SharedById { get; set; }
    }
}