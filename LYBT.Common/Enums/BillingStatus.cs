using System.ComponentModel;

namespace LYBT.Common.Enums {

    /// <summary>
    /// 结算状态枚举
    /// </summary>
    [Description("结算状态")]
    public enum BillingStatus {

        /// <summary>待支付</summary>
        [Description("待支付")]
        Pending = 0,

        /// <summary>已支付</summary>
        [Description("已支付")]
        Paid = 1,

        /// <summary>已完成</summary>
        [Description("已完成")]
        Completed = 2,

        /// <summary>申请退款</summary>
        [Description("申请退款")]
        RefundRequested = 3,

        /// <summary>已退款</summary>
        [Description("已退款")]
        Refunded = 4,

        /// <summary>已取消</summary>
        [Description("已取消")]
        Cancelled = 5
    }
}