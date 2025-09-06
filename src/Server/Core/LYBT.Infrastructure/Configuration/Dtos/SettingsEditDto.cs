using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Infrastructure.Configuration.Dtos {

    /// <summary>
    /// 系统设置编辑传输对象
    /// </summary>
    public class SettingsEditDto {

        /// <summary>
        /// 主键ID
        /// </summary>
        [Required(ErrorMessage = "ID不能为空")]
        [DisplayName("主键ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 设置值
        /// </summary>
        [Required(ErrorMessage = "设置值不能为空")]
        [StringLength(1000, ErrorMessage = "设置值长度不能超过1000个字符")]
        [DisplayName("设置值")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 中文说明
        /// </summary>
        [StringLength(200, ErrorMessage = "说明长度不能超过200个字符")]
        [DisplayName("中文说明")]
        public string? Description { get; set; }

        /// <summary>
        /// 设置分组
        /// </summary>
        [StringLength(50, ErrorMessage = "设置分组长度不能超过50个字符")]
        [DisplayName("设置分组")]
        public string? Group { get; set; }

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
        /// 备注
        /// </summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}
