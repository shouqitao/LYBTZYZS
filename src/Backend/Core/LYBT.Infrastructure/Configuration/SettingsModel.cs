using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Infrastructure.Configuration {

    /// <summary>
    /// 系统设置实体模型
    /// </summary>
    public class SettingsModel {

        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        [DisplayName("主键ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 设置键（唯一标识）
        /// </summary>
        [Required, StringLength(128)]
        [DisplayName("设置键（唯一标识）")]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// 设置值
        /// </summary>
        [Required, StringLength(1000)]
        [DisplayName("设置值")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 中文说明
        /// </summary>
        [StringLength(200)]
        [DisplayName("中文说明")]
        public string? Description { get; set; }

        /// <summary>
        /// 数据类型（string/int/bool/decimal/json）
        /// </summary>
        [StringLength(20)]
        [DisplayName("数据类型")]
        public string ValueType { get; set; } = "string";

        /// <summary>
        /// 设置分组
        /// </summary>
        [StringLength(50)]
        [DisplayName("设置分组")]
        public string? Group { get; set; }

        /// <summary>
        /// 排序序号
        /// </summary>
        [DisplayName("排序序号")]
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// 是否为系统设置（系统设置不可删除）
        /// </summary>
        [DisplayName("是否为系统设置")]
        public bool IsSystem { get; set; } = false;

        /// <summary>
        /// 是否启用
        /// </summary>
        [DisplayName("是否启用")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        [Required]
        [DisplayName("更新时间")]
        public DateTime UpdateTime { get; set; } = DateTime.Now;

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