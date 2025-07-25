using System.ComponentModel;

namespace LYBT.Module.Settings.Models.Dtos {

    /// <summary>
    /// 设置项列表 DTO（简要信息）
    /// </summary>
    public class SettingsDto {

        /// <summary>设置项ID</summary>
        [DisplayName("设置项ID")]
        public Guid Id { get; set; }

        /// <summary>设置项键名</summary>
        [DisplayName("设置项键名")]
        public string Key { get; set; } = string.Empty;

        /// <summary>设置项值</summary>
        [DisplayName("设置项值")]
        public string Value { get; set; } = string.Empty;
    }
}