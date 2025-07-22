using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.Settings.Dtos {

    /// <summary>
    /// 编辑设置项 DTO
    /// </summary>
    public class SettingsEditDto {

        /// <summary>设置项ID</summary>
        [Required(ErrorMessage = "ID不能为空")]
        [DisplayName("设置项ID")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

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
