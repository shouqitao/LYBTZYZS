using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Configuration {

    /// <summary>
    /// 治疗目录模型
    /// </summary>
    public class TreatmentCatalogModel {

        /// <summary>
        /// 主键ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 治疗项目编码
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// 治疗项目名称
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 治疗项目描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 标准价格
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 上级分类ID
        /// </summary>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 分类级别
        /// </summary>
        public int Level { get; set; } = 1;

        /// <summary>
        /// 排序序号
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// 治疗时长（分钟）
        /// </summary>
        public int? Duration { get; set; }

        /// <summary>
        /// 适应症
        /// </summary>
        public string? Indications { get; set; }

        /// <summary>
        /// 禁忌症
        /// </summary>
        public string? Contraindications { get; set; }

        /// <summary>
        /// 注意事项
        /// </summary>
        public string? Precautions { get; set; }

        /// <summary>
        /// 是否需要预约
        /// </summary>
        public bool RequireAppointment { get; set; } = false;

        /// <summary>
        /// 是否为常用项目
        /// </summary>
        public bool IsCommon { get; set; } = false;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }
    }
}