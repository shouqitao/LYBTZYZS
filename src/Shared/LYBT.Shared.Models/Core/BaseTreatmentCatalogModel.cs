using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Core
{

    /// <summary>
    /// 治疗目录基础模型 - 前后端共享
    /// </summary>
    public abstract class BaseTreatmentCatalogModel
    {

        /// <summary>主键ID</summary>
        [DisplayName("主键ID")]
        public Guid Id { get; set; }

        /// <summary>治疗项目编码</summary>
        [DisplayName("治疗项目编码")]
        public string? Code { get; set; }

        /// <summary>治疗项目名称</summary>
        [Required]
        [DisplayName("治疗项目名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>治疗项目描述</summary>
        [DisplayName("治疗项目描述")]
        public string? Description { get; set; }

        /// <summary>标准价格</summary>
        [DisplayName("标准价格")]
        public decimal Price { get; set; }

        /// <summary>上级分类ID</summary>
        [DisplayName("上级分类ID")]
        public Guid? ParentId { get; set; }

        /// <summary>分类级别</summary>
        [DisplayName("分类级别")]
        public int Level { get; set; } = 1;

        /// <summary>排序序号</summary>
        [DisplayName("排序序号")]
        public int SortOrder { get; set; } = 0;

        /// <summary>治疗时长（分钟）</summary>
        [DisplayName("治疗时长（分钟）")]
        public int? Duration { get; set; }

        /// <summary>适应症</summary>
        [DisplayName("适应症")]
        public string? Indications { get; set; }

        /// <summary>禁忌症</summary>
        [DisplayName("禁忌症")]
        public string? Contraindications { get; set; }

        /// <summary>注意事项</summary>
        [DisplayName("注意事项")]
        public string? Precautions { get; set; }

        /// <summary>是否需要预约</summary>
        [DisplayName("是否需要预约")]
        public bool RequireAppointment { get; set; } = false;

        /// <summary>是否为常用项目</summary>
        [DisplayName("是否为常用项目")]
        public bool IsCommon { get; set; } = false;

        /// <summary>是否启用</summary>
        [DisplayName("是否启用")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime UpdateTime { get; set; } = DateTime.Now;

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}