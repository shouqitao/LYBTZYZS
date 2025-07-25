using System.ComponentModel;

namespace LYBT.Common.Enums {

    /// <summary>
    /// 药房处方状态枚举
    /// </summary>
    [Description("药房状态")]
    public enum PharmacyStatus {

        /// <summary>待抓药</summary>
        Waiting = 0,

        /// <summary>抓药中</summary>
        Preparing = 1,

        /// <summary>已抓药</summary>
        Prepared = 2,

        /// <summary>待代煎</summary>
        DecoctionWaiting = 3,

        /// <summary>代煎完成</summary>
        DecoctionCompleted = 4,

        /// <summary>抓药完毕</summary>
        Completed = 5
    }
}