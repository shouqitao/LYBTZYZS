using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 通用状态枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CommonStatus
    {
        /// <summary>禁用</summary>
        [Description("禁用")]
        Disabled = 0,

        /// <summary>启用</summary>
        [Description("启用")]
        Enabled = 1
    }

    /// <summary>
    /// 是否删除枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DeleteStatus
    {
        /// <summary>正常</summary>
        [Description("正常")]
        Normal = 0,

        /// <summary>已删除</summary>
        [Description("已删除")]
        Deleted = 1
    }

    /// <summary>
    /// 操作结果枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OperationResult
    {
        /// <summary>失败</summary>
        [Description("失败")]
        Failed = 0,

        /// <summary>成功</summary>
        [Description("成功")]
        Success = 1
    }

    /// <summary>
    /// 数据状态枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DataStatus
    {
        /// <summary>草稿</summary>
        [Description("草稿")]
        Draft = 0,

        /// <summary>正常</summary>
        [Description("正常")]
        Normal = 1,

        /// <summary>锁定</summary>
        [Description("锁定")]
        Locked = 2,

        /// <summary>归档</summary>
        [Description("归档")]
        Archived = 3
    }

    /// <summary>
    /// 审核状态枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AuditStatus
    {
        /// <summary>待审核</summary>
        [Description("待审核")]
        Pending = 0,

        /// <summary>审核通过</summary>
        [Description("审核通过")]
        Approved = 1,

        /// <summary>审核拒绝</summary>
        [Description("审核拒绝")]
        Rejected = 2
    }

    /// <summary>
    /// 支付状态枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentStatus
    {
        /// <summary>未支付</summary>
        [Description("未支付")]
        Unpaid = 0,

        /// <summary>已支付</summary>
        [Description("已支付")]
        Paid = 1,

        /// <summary>部分支付</summary>
        [Description("部分支付")]
        PartialPaid = 2,

        /// <summary>已退款</summary>
        [Description("已退款")]
        Refunded = 3
    }

    /// <summary>
    /// 支付方式枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentMethod
    {
        /// <summary>现金</summary>
        [Description("现金")]
        Cash = 0,

        /// <summary>银行卡</summary>
        [Description("银行卡")]
        BankCard = 1,

        /// <summary>微信支付</summary>
        [Description("微信支付")]
        WeChat = 2,

        /// <summary>支付宝</summary>
        [Description("支付宝")]
        Alipay = 3,

        /// <summary>医保卡</summary>
        [Description("医保卡")]
        MedicalCard = 4
    }

    /// <summary>
    /// 工作日枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum WorkDay
    {
        /// <summary>周一</summary>
        [Description("周一")]
        Monday = 1,

        /// <summary>周二</summary>
        [Description("周二")]
        Tuesday = 2,

        /// <summary>周三</summary>
        [Description("周三")]
        Wednesday = 3,

        /// <summary>周四</summary>
        [Description("周四")]
        Thursday = 4,

        /// <summary>周五</summary>
        [Description("周五")]
        Friday = 5,

        /// <summary>周六</summary>
        [Description("周六")]
        Saturday = 6,

        /// <summary>周日</summary>
        [Description("周日")]
        Sunday = 7
    }

    /// <summary>
    /// 时间段枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TimeSlot
    {
        /// <summary>上午</summary>
        [Description("上午")]
        Morning = 0,

        /// <summary>下午</summary>
        [Description("下午")]
        Afternoon = 1,

        /// <summary>晚上</summary>
        [Description("晚上")]
        Evening = 2,

        /// <summary>全天</summary>
        [Description("全天")]
        AllDay = 3
    }
}