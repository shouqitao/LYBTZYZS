using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Models.Settings {

    /// <summary>
    /// 系统设置模型
    /// </summary>
    public class SettingsModel {

        /// <summary>主键ID</summary>
        [Key]
        [DisplayName("主键ID")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>设置键（唯一标识）</summary>
        [Required, StringLength(64)]
        [DisplayName("设置键（唯一标识）")]
/// <summary>
/// Key 属性。
/// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>设置值</summary>
        [Required, StringLength(256)]
        [DisplayName("设置值")]
/// <summary>
/// Value 属性。
/// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>中文说明</summary>
        [StringLength(128)]
        [DisplayName("中文说明")]
/// <summary>
/// Description 属性。
/// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>类型（bool/int/string）</summary>
        [StringLength(32)]
        [DisplayName("类型（bool/int/string）")]
/// <summary>
/// ValueType 属性。
/// </summary>
        public string ValueType { get; set; } = string.Empty;

        /// <summary>更新时间</summary>
        [Required]
        [DisplayName("更新时间")]
/// <summary>
/// UpdateTime 属性。
/// </summary>
        public DateTime UpdateTime { get; set; }
    }
}
