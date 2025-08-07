using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 排队状态枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum QueueStatus
    {
        /// <summary>等待中</summary>
        [Description("等待中")]
        Waiting = 0,

        /// <summary>正在服务</summary>
        [Description("正在服务")]
        InService = 1,

        /// <summary>已完成</summary>
        [Description("已完成")]
        Completed = 2,

        /// <summary>已跳过</summary>
        [Description("已跳过")]
        Skipped = 3,

        /// <summary>已取消</summary>
        [Description("已取消")]
        Cancelled = 4,

        /// <summary>过号</summary>
        [Description("过号")]
        Missed = 5
    }

    /// <summary>
    /// 排队类型枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum QueueType
    {
        /// <summary>挂号</summary>
        [Description("挂号")]
        Registration = 0,

        /// <summary>看诊</summary>
        [Description("看诊")]
        Consultation = 1,

        /// <summary>取药</summary>
        [Description("取药")]
        Pharmacy = 2,

        /// <summary>治疗</summary>
        [Description("治疗")]
        Treatment = 3,

        /// <summary>缴费</summary>
        [Description("缴费")]
        Payment = 4
    }

    /// <summary>
    /// 优先级枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum QueuePriority
    {
        /// <summary>普通</summary>
        [Description("普通")]
        Normal = 0,

        /// <summary>优先</summary>
        [Description("优先")]
        High = 1,

        /// <summary>紧急</summary>
        [Description("紧急")]
        Urgent = 2,

        /// <summary>VIP</summary>
        [Description("VIP")]
        VIP = 3
    }
}