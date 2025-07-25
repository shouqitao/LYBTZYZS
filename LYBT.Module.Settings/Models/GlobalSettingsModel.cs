using LYBT.Common.Enums;
using System.ComponentModel;

namespace LYBT.Module.Settings.Models {

    /// <summary>
    /// 全局系统设置
    /// </summary>
    public class GlobalSettingsModel {

        [DisplayName("Id")]
        public Guid Id { get; set; }

        [DisplayName("DefaultRecordSharing")]
        public string DefaultRecordSharing { get; set; } = "Private"; // Private or Public

        [DisplayName("SyncMode")]
        public SyncMode SyncMode { get; set; } = SyncMode.Auto;
    }
}