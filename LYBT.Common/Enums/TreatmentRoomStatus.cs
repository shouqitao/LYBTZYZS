using System.ComponentModel;

namespace LYBT.Common.Enums {

    /// <summary>
    /// 诊疗室状态
    /// </summary>
    public enum TreatmentRoomStatus {

        /// <summary>空闲</summary>
        [Description("空闲")]
        Idle = 0,

        /// <summary>使用中</summary>
        [Description("使用中")]
        InUse = 1,

        /// <summary>暂停</summary>
        [Description("暂停")]
        Paused = 2,

        /// <summary>已停用</summary>
        [Description("停用")]
        Disabled = 3
    }
}