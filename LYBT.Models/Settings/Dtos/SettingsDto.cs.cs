using System.ComponentModel;
namespace LYBT.Module.Settings.Dtos {

    /// <summary>
    /// 设置项列表 DTO（简要信息）
    /// </summary>
    public class SettingsDto {

        /// <summary>设置项ID</summary>
        [DisplayName("设置项ID")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>设置项键名</summary>
        [DisplayName("设置项键名")]
/// <summary>
/// Key 属性。
/// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>设置项值</summary>
        [DisplayName("设置项值")]
/// <summary>
/// Value 属性。
/// </summary>
        public string Value { get; set; } = string.Empty;
    }
}
