using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.Settings.Dtos {

    /// <summary>
    /// 新增设置项 DTO
    /// </summary>
    public class SettingsCreateDto {

        /// <summary>设置项键名</summary>
        [Required(ErrorMessage = "键名不能为空")]
        [DisplayName("设置项键名")]
/// <summary>
/// Key 属性。
/// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>设置项值</summary>
        [Required(ErrorMessage = "值不能为空")]
        [DisplayName("设置项值")]
/// <summary>
/// Value 属性。
/// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>设置项说明</summary>
        [DisplayName("设置项说明")]
/// <summary>
/// Description 属性。
/// </summary>
        public string? Description { get; set; }
    }
}
