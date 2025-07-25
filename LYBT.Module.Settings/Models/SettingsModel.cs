using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Settings.Models {

    /// <summary>
    /// 系统设置模型
    /// </summary>
    public class SettingsModel {

        /// <summary>主键ID</summary>
        [Key]
        [DisplayName("主键ID")]
        public Guid Id { get; set; }

        /// <summary>设置键（唯一标识）</summary>
        [Required, StringLength(64)]
        [DisplayName("设置键（唯一标识）")]
        public string Key { get; set; } = string.Empty;

        /// <summary>设置值</summary>
        [Required, StringLength(256)]
        [DisplayName("设置值")]
        public string Value { get; set; } = string.Empty;

        /// <summary>中文说明</summary>
        [StringLength(128)]
        [DisplayName("中文说明")]
        public string Description { get; set; } = string.Empty;

        /// <summary>类型（bool/int/string）</summary>
        [StringLength(32)]
        [DisplayName("类型（bool/int/string）")]
        public string ValueType { get; set; } = string.Empty;

        /// <summary>更新时间</summary>
        [Required]
        [DisplayName("更新时间")]
        public DateTime UpdateTime { get; set; }
    }
}