using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Infrastructure.Configuration.Dtos
{

    /// <summary>
    /// 治疗目录传输对象
    /// </summary>
    public class TreatmentCatalogDto
    {

        /// <summary>
        /// 主键ID
        /// </summary>
        [DisplayName("主键ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 治疗项目编码
        /// </summary>
        [StringLength(20, ErrorMessage = "治疗项目编码长度不能超过20个字符")]
        [DisplayName("治疗项目编码")]
        public string? Code { get; set; }

        /// <summary>
        /// 治疗项目名称
        /// </summary>
        [Required(ErrorMessage = "治疗项目名称不能为空")]
        [StringLength(100, ErrorMessage = "治疗项目名称长度不能超过100个字符")]
        [DisplayName("治疗项目名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 治疗项目描述
        /// </summary>
        [StringLength(500, ErrorMessage = "治疗项目描述长度不能超过500个字符")]
        [DisplayName("治疗项目描述")]
        public string? Description { get; set; }

        /// <summary>
        /// 标准价格
        /// </summary>
        [Required(ErrorMessage = "标准价格不能为空")]
        [Range(0, 999999.99, ErrorMessage = "价格必须在0-999999.99之间")]
        [DisplayName("标准价格")]
        public decimal Price { get; set; }

        /// <summary>
        /// 上级分类ID
        /// </summary>
        [DisplayName("上级分类ID")]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 分类级别
        /// </summary>
        [DisplayName("分类级别")]
        public int Level { get; set; } = 1;

        /// <summary>
        /// 排序序号
        /// </summary>
        [DisplayName("排序序号")]
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// 治疗时长（分钟）
        /// </summary>
        [Range(1, 1440, ErrorMessage = "治疗时长必须在1-1440分钟之间")]
        [DisplayName("治疗时长（分钟）")]
        public int? Duration { get; set; }

        /// <summary>
        /// 适应症
        /// </summary>
        [StringLength(500, ErrorMessage = "适应症长度不能超过500个字符")]
        [DisplayName("适应症")]
        public string? Indications { get; set; }

        /// <summary>
        /// 禁忌症
        /// </summary>
        [StringLength(500, ErrorMessage = "禁忌症长度不能超过500个字符")]
        [DisplayName("禁忌症")]
        public string? Contraindications { get; set; }

        /// <summary>
        /// 注意事项
        /// </summary>
        [StringLength(1000, ErrorMessage = "注意事项长度不能超过1000个字符")]
        [DisplayName("注意事项")]
        public string? Precautions { get; set; }

        /// <summary>
        /// 是否需要预约
        /// </summary>
        [DisplayName("是否需要预约")]
        public bool RequireAppointment { get; set; } = false;

        /// <summary>
        /// 是否为常用项目
        /// </summary>
        [DisplayName("是否为常用项目")]
        public bool IsCommon { get; set; } = false;

        /// <summary>
        /// 是否启用
        /// </summary>
        [DisplayName("是否启用")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 更新时间
        /// </summary>
        [DisplayName("更新时间")]
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}