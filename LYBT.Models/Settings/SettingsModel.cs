using System;

namespace LYBT.Models.Settings {
    /// <summary>
    /// 系统设置模型
    /// </summary>
    public class SettingsModel {
        public string Key { get; set; }            // 设置键（唯一标识）
        public string Value { get; set; }          // 设置值
        public string Description { get; set; }    // 中文说明
        public string ValueType { get; set; }      // 类型（bool/int/string）
        public DateTime UpdateTime { get; set; }
        public Guid Id { get; set; }
    }
}
