using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Infrastructure.Configuration
{

    /// <summary>
    /// 诊断目录实体模型
    /// </summary>
    public class DiagnosisCatalogModel
    {

        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        [DisplayName("主键ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 诊断编码
        /// </summary>
        [StringLength(20)]
        [DisplayName("诊断编码")]
        public string? Code { get; set; }

        /// <summary>
        /// 诊断名称
        /// </summary>
        [Required, StringLength(100)]
        [DisplayName("诊断名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 诊断描述
        /// </summary>
        [StringLength(500)]
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
        [StringLength(20)]
        [DisplayName("ICD编码")]
        public string? IcdCode { get; set; }

        /// <summary>
        /// 中医证型
        /// </summary>
        [StringLength(100)]
        [DisplayName("中医证型")]
        public string? TcmSyndrome { get; set; }

        /// <summary>
        /// 常用诊断（标记为常用便于快速选择）
        /// </summary>
        [DisplayName("常用诊断")]
        public bool IsCommon { get; set; } = false;

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
        /// 创建者ID
        /// </summary>
        [DisplayName("创建者ID")]
        public Guid? CreatedBy { get; set; }

        /// <summary>
        /// 更新者ID
        /// </summary>
        [DisplayName("更新者ID")]
        public Guid? UpdatedBy { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}