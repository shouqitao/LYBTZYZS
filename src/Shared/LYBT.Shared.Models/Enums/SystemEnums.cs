using System.ComponentModel;

namespace LYBT.Shared.Models.Enums {

    /// <summary>
    /// 计费状态枚举 - 前后端共享
    /// </summary>
    [Description("计费状态")]
    public enum BillingStatus {

        /// <summary>待付款</summary>
        [Description("待付款")]
        Pending = 0,

        /// <summary>已付款</summary>
        [Description("已付款")]
        Paid = 1,

        /// <summary>部分付款</summary>
        [Description("部分付款")]
        PartiallyPaid = 2,

        /// <summary>已退款</summary>
        [Description("已退款")]
        Refunded = -1,

        /// <summary>已取消</summary>
        [Description("已取消")]
        Cancelled = -2
    }

    /// <summary>
    /// 药房处方状态枚举 - 前后端共享
    /// </summary>
    [Description("药房处方状态")]
    public enum PharmacyStatus {

        /// <summary>待抓药</summary>
        [Description("待抓药")]
        Pending = 0,

        /// <summary>抓药中</summary>
        [Description("抓药中")]
        InProgress = 1,

        /// <summary>已完成</summary>
        [Description("已完成")]
        Completed = 2,

        /// <summary>已取消</summary>
        [Description("已取消")]
        Cancelled = -1
    }

    /// <summary>
    /// 用户资料模式枚举 - 前后端共享
    /// </summary>
    [Description("用户资料模式")]
    public enum ProfileMode {

        /// <summary>公开</summary>
        [Description("公开")]
        Public = 1,

        /// <summary>仅医生可见</summary>
        [Description("仅医生可见")]
        DoctorsOnly = 2,

        /// <summary>私密</summary>
        [Description("私密")]
        Private = 3
    }

    /// <summary>
    /// 同步模式枚举 - 前后端共享
    /// </summary>
    [Description("同步模式")]
    public enum SyncMode {

        /// <summary>手动</summary>
        [Description("手动")]
        Manual = 0,

        /// <summary>自动</summary>
        [Description("自动")]
        Auto = 1,

        /// <summary>定时</summary>
        [Description("定时")]
        Scheduled = 2
    }

    /// <summary>
    /// 同步状态枚举 - 前后端共享
    /// </summary>
    [Description("同步状态")]
    public enum SyncStatus {

        /// <summary>待同步</summary>
        [Description("待同步")]
        Pending = 0,

        /// <summary>同步中</summary>
        [Description("同步中")]
        Syncing = 1,

        /// <summary>已完成</summary>
        [Description("已完成")]
        Completed = 2,

        /// <summary>失败</summary>
        [Description("失败")]
        Failed = -1,

        /// <summary>已取消</summary>
        [Description("已取消")]
        Cancelled = -2
    }
}