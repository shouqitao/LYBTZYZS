using LYBT.Common.Enums;
using System.ComponentModel;

namespace LYBT.Module.Settings.Dtos {

    public class GlobalSettingsDto {
        [DisplayName("Id")]
        public Guid Id { get; set; }
        [DisplayName("DefaultRecordSharing")]
        public string DefaultRecordSharing { get; set; } = "Private";
        [DisplayName("SyncMode")]
        public SyncMode SyncMode { get; set; } = SyncMode.Auto;
    }
}