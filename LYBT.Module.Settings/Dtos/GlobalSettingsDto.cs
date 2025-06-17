using System;
using LYBT.Common.Enums;

namespace LYBT.Module.Settings.Dtos {
    public class GlobalSettingsDto {
        public Guid Id { get; set; }
        public string DefaultRecordSharing { get; set; } = "Private";
        public SyncMode SyncMode { get; set; } = SyncMode.Auto;
    }
}
