using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Settings {

    /// <summary>
    /// 系统设置模型
    /// </summary>
    public class SettingsModel {

        /// <summary>主键ID</summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>设置键（唯一标识）</summary>
        [Required, StringLength(64)]
        public string Key { get; set; } = string.Empty;

        /// <summary>设置值</summary>
        [Required, StringLength(256)]
        public string Value { get; set; } = string.Empty;

        /// <summary>中文说明</summary>
        [StringLength(128)]
        public string Description { get; set; } = string.Empty;

        /// <summary>类型（bool/int/string）</summary>
        [StringLength(32)]
        public string ValueType { get; set; } = string.Empty;

        /// <summary>更新时间</summary>
        [Required]
        public DateTime UpdateTime { get; set; }
    }
}