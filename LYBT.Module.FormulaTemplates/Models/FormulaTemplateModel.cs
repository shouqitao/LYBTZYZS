using System.ComponentModel;

namespace LYBT.Module.FormulaTemplates.Models {

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