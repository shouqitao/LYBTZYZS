using System.ComponentModel;
namespace LYBT.Module.Settings.Dtos {

    /// <summary>
    /// 设置项详情 DTO
    /// </summary>
    public class SettingsDetailDto {

        /// <summary>设置项ID</summary>
        [DisplayName("设置项ID")]
        public Guid Id { get; set; }

        /// <summary>设置项键名</summary>
        [DisplayName("设置项键名")]
        public string Key { get; set; } = string.Empty;

        /// <summary>设置项值</summary>
        [DisplayName("设置项值")]
        public string Value { get; set; } = string.Empty;

        /// <summary>设置项说明</summary>
        [DisplayName("设置项说明")]
        public string? Description { get; set; }
    }
}