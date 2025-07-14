namespace LYBT.Module.Settings.Dtos {

    /// <summary>
    /// 设置项列表 DTO（简要信息）
    /// </summary>
    public class SettingsDto {

        /// <summary>设置项ID</summary>
        public Guid Id { get; set; }

        /// <summary>设置项键名</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>设置项值</summary>
        public string Value { get; set; } = string.Empty;
    }
}