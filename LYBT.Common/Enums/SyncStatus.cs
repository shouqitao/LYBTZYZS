using System.ComponentModel;

namespace LYBT.Common.Enums {

    /// <summary>
    /// 数据同步结果状态
    /// </summary>
    [Description("同步状态")]
    public enum SyncStatus {

        [Description("成功")]
        Success = 0,

        [Description("失败")]
        Failed = 1,

        [Description("部分成功")]
        Partial = 2
    }
}