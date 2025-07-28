using System.ComponentModel;

namespace LYBT.Models.FormulaTemplates {

    /// <summary>
    /// 经验方模板主表实体
    /// </summary>
    public class FormulaTemplateModel {

        /// <summary>
        /// 模板ID（主键）
        /// </summary>
        [DisplayName("模板ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 模板名称
        /// </summary>
        [DisplayName("模板名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 药材组成（结构化列表）
        /// </summary>
        [DisplayName("药材组成")]
        public List<FormulaTemplateHerbItem> Herbs { get; set; } = new();

        /// <summary>
        /// 备注
        /// </summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>
        /// 配方性味归经
        /// </summary>
        [DisplayName("配方性味归经")]
        public string? Property { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        [DisplayName("是否激活")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 是否共享
        /// </summary>
        [DisplayName("是否共享")]
        public bool IsShared { get; set; } = false;

        /// <summary>
        /// 创建者ID
        /// </summary>
        [DisplayName("创建者ID")]
        public Guid? CreatedById { get; set; }

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