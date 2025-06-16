using System;

namespace LYBT.Models.Settings {
    /// <summary>
    /// 系统设置模型
    /// </summary>
    public class SettingsModel {
        /// <summary>设置键（唯一标识）</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>设置值</summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>中文说明</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>类型（bool/int/string）</summary>
        public string ValueType { get; set; } = string.Empty;

        /// <summary>更新时间</summary>
        public DateTime UpdateTime { get; set; }

        /// <summary>主键ID</summary>
        public Guid Id { get; set; }
    }
}

