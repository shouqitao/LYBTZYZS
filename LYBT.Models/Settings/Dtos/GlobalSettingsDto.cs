using LYBT.Common.Enums;
using System.ComponentModel;

namespace LYBT.Module.Settings.Dtos {

/// <summary>
/// 表示GlobalSettingsDto。
/// </summary>
    public class GlobalSettingsDto {
        [DisplayName("Id")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }
        [DisplayName("DefaultRecordSharing")]
/// <summary>
/// DefaultRecordSharing 属性。
/// </summary>
        public string DefaultRecordSharing { get; set; } = "Private";
        [DisplayName("SyncMode")]
/// <summary>
/// SyncMode 属性。
/// </summary>
        public SyncMode SyncMode { get; set; } = SyncMode.Auto;
    }
}
