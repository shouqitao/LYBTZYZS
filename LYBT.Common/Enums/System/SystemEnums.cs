namespace LYBT.Common.Enums.System {

    /// <summary>
    /// 计费状态
    /// </summary>
    public enum BillingStatus {

        /// <summary>
        /// 待付款
        /// </summary>
        Pending = 0,

        /// <summary>
        /// 已付款
        /// </summary>
        Paid = 1,

        /// <summary>
        /// 部分付款
        /// </summary>
        PartiallyPaid = 2,

        /// <summary>
        /// 已退款
        /// </summary>
        Refunded = -1,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = -2
    }

    /// <summary>
    /// 药房处方状态
    /// </summary>
    public enum PharmacyStatus {

        /// <summary>
        /// 待抓药
        /// </summary>
        Pending = 0,

        /// <summary>
        /// 抓药中
        /// </summary>
        InProgress = 1,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed = 2,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = -1
    }

    /// <summary>
    /// 性别
    /// </summary>
    public enum Gender {

        /// <summary>
        /// 未知
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// 男
        /// </summary>
        Male = 1,

        /// <summary>
        /// 女
        /// </summary>
        Female = 2
    }

    /// <summary>
    /// 用户资料模式
    /// </summary>
    public enum ProfileMode {

        /// <summary>
        /// 公开
        /// </summary>
        Public = 1,

        /// <summary>
        /// 仅医生可见
        /// </summary>
        DoctorsOnly = 2,

        /// <summary>
        /// 私密
        /// </summary>
        Private = 3
    }

    /// <summary>
    /// 同步模式
    /// </summary>
    public enum SyncMode {

        /// <summary>
        /// 手动
        /// </summary>
        Manual = 0,

        /// <summary>
        /// 自动
        /// </summary>
        Auto = 1,

        /// <summary>
        /// 定时
        /// </summary>
        Scheduled = 2
    }

    /// <summary>
    /// 同步状态
    /// </summary>
    public enum SyncStatus {

        /// <summary>
        /// 待同步
        /// </summary>
        Pending = 0,

        /// <summary>
        /// 同步中
        /// </summary>
        Syncing = 1,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed = 2,

        /// <summary>
        /// 失败
        /// </summary>
        Failed = -1,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = -2
    }
}