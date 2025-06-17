using System;

namespace LYBT.Models.Settings {
    /// <summary>
    /// 全局系统设置
    /// </summary>
    public class GlobalSettingsModel {
        public Guid Id { get; set; }
        public string DefaultRecordSharing { get; set; } = "Private"; // Private or Public
        public string SyncMode { get; set; } = "Auto"; // Auto or Manual
    }
}
