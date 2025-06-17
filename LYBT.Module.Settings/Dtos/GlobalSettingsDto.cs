using System;

namespace LYBT.Module.Settings.Dtos {
    public class GlobalSettingsDto {
        public Guid Id { get; set; }
        public string DefaultRecordSharing { get; set; } = "Private";
        public string SyncMode { get; set; } = "Auto";
    }
}
