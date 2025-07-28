using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.FormulaTemplates.Models {

    /// <summary>
    /// 经验方模板主表实体
    /// </summary>
    public class FormulaTemplateModel {

        /// <summary>
        /// 模板ID（主键）
        /// </summary>
        [Key]
        [DisplayName("模板ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 模板名称
        /// </summary>
        [Required]
        [StringLength(200)]
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
        [StringLength(1000)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>
        /// 创建者ID
        /// </summary>
        [Required]
        [DisplayName("创建者ID")]
        public Guid CreatedById { get; set; }

        /// <summary>
        /// 创建者姓名
        /// </summary>
        [Required]
        [StringLength(100)]
        [DisplayName("创建者姓名")]
        public string CreatedByName { get; set; } = string.Empty;

        /// <summary>
        /// 是否共享（共享后所有医生可见，未共享仅创建者可见）
        /// </summary>
        [DisplayName("是否共享")]
        public bool IsShared { get; set; } = false;

        /// <summary>
        /// 共享时间
        /// </summary>
        [DisplayName("共享时间")]
        public DateTime? SharedAt { get; set; }

        /// <summary>
        /// 共享操作人ID
        /// </summary>
        [DisplayName("共享操作人ID")]
        public Guid? SharedById { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [DisplayName("创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        [Required]
        [DisplayName("更新时间")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 是否启用
        /// </summary>
        [DisplayName("是否启用")]
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// 经验方模板药材项
    /// </summary>
    public class FormulaTemplateHerbItem {
        /// <summary>药材ID</summary>
        public Guid HerbId { get; set; }
        
        /// <summary>药材名称</summary>
        public string HerbName { get; set; } = string.Empty;
        
        /// <summary>剂量</summary>
        public decimal Quantity { get; set; }
        
        /// <summary>单位</summary>
        public string Unit { get; set; } = string.Empty;
        
        /// <summary>用法</summary>
        public string? Usage { get; set; }
    }
}