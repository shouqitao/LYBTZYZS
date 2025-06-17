using System.ComponentModel;

namespace LYBT.Common.Enums {
    /// <summary>
    /// 同步模式（自动/手动）
    /// </summary>
    public enum SyncMode {
        [Description("自动同步")]
        Auto = 0,

        [Description("手动同步")]
        Manual = 1
    }
}
