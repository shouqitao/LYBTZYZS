using LYBT.Common.Enums;
using System.ComponentModel;

namespace LYBT.Models.Settings {

    /// <summary>
    /// 全局系统设置
    /// </summary>
    public class GlobalSettingsModel {
        [DisplayName("Id")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }
        [DisplayName("DefaultRecordSharing")]
/// <summary>
/// DefaultRecordSharing 属性。
/// </summary>
        public string DefaultRecordSharing { get; set; } = "Private"; // Private or Public
        [DisplayName("SyncMode")]
/// <summary>
/// SyncMode 属性。
/// </summary>
        public SyncMode SyncMode { get; set; } = SyncMode.Auto;
    }
}
