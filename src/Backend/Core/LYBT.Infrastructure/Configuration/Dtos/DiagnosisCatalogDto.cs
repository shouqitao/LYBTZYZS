using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Infrastructure.Configuration.Dtos
{

    /// <summary>
    /// 诊断目录传输对象
    /// </summary>
    public class DiagnosisCatalogDto
    {

        /// <summary>
        /// 主键ID
        /// </summary>
        [DisplayName("主键ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 诊断编码
        /// </summary>
        [StringLength(20, ErrorMessage = "诊断编码长度不能超过20个字符")]
        [DisplayName("诊断编码")]
        public string? Code { get; set; }

        /// <summary>
        /// 诊断名称
        /// </summary>
        [Required(ErrorMessage = "诊断名称不能为空")]
        [StringLength(100, ErrorMessage = "诊断名称长度不能超过100个字符")]
        [DisplayName("诊断名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 诊断描述
        /// </summary>
        [StringLength(500, ErrorMessage = "诊断描述长度不能超过500个字符")]
        [DisplayName("诊断描述")]
        public string? Description { get; set; }

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
        /// 是否启用
        /// </summary>
        [DisplayName("是否启用")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// ICD编码
        /// </summary>
        [StringLength(20, ErrorMessage = "ICD编码长度不能超过20个字符")]
        [DisplayName("ICD编码")]
        public string? IcdCode { get; set; }

        /// <summary>
        /// 中医证型
        /// </summary>
        [StringLength(100, ErrorMessage = "中医证型长度不能超过100个字符")]
        [DisplayName("中医证型")]
        public string? TcmSyndrome { get; set; }

        /// <summary>
        /// 常用诊断
        /// </summary>
        [DisplayName("常用诊断")]
        public bool IsCommon { get; set; } = false;

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