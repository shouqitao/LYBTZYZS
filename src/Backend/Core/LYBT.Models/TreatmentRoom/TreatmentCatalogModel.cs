using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.TreatmentRoom
{
    /// <summary>
    /// 理疗项目目录实体 - 数据库映射
    /// </summary>
    public class TreatmentCatalogModel
    {
        /// <summary>项目唯一标识</summary>
        [Key]
        [DisplayName("项目ID")]
        public Guid Id { get; set; }

        /// <summary>项目编码</summary>
        [Required]
        [MaxLength(50)]
        [DisplayName("项目编码")]
        public string Code { get; set; } = string.Empty;

        /// <summary>项目名称</summary>
        [Required]
        [MaxLength(100)]
        [DisplayName("项目名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>项目分类</summary>
        [Required]
        [MaxLength(50)]
        [DisplayName("项目分类")]
        public string Category { get; set; } = string.Empty; // 针灸、推拿、理疗、拔罐等

        /// <summary>项目描述</summary>
        [MaxLength(500)]
        [DisplayName("项目描述")]
        public string? Description { get; set; }

        /// <summary>价格</summary>
        [Required]
        [DisplayName("价格")]
        public decimal Price { get; set; }

        /// <summary>时长(分钟)</summary>
        [Required]
        [DisplayName("时长")]
        public int Duration { get; set; }

        /// <summary>注意事项</summary>
        [MaxLength(500)]
        [DisplayName("注意事项")]
        public string? Precautions { get; set; }

        /// <summary>适应症</summary>
        [MaxLength(500)]
        [DisplayName("适应症")]
        public string? Indications { get; set; }

        /// <summary>禁忌症</summary>
        [MaxLength(500)]
        [DisplayName("禁忌症")]
        public string? Contraindications { get; set; }

        /// <summary>是否启用</summary>
        [DisplayName("是否启用")]
        public bool IsActive { get; set; } = true;

        /// <summary>排序号</summary>
        [DisplayName("排序号")]
        public int SortOrder { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>创建人</summary>
        [MaxLength(50)]
        [DisplayName("创建人")]
        public string? CreatedBy { get; set; }

        /// <summary>更新人</summary>
        [MaxLength(50)]
        [DisplayName("更新人")]
        public string? UpdatedBy { get; set; }
    }
}