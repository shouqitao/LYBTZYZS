using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Enums
{

    /// <summary>
    /// 医疗案例状态枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MedicalCaseStatus
    {

        /// <summary>挂号完成</summary>
        [Description("挂号完成")]
        Registered = 0,

        /// <summary>看诊中</summary>
        [Description("看诊中")]
        InConsultation = 1,

        /// <summary>已完成</summary>
        [Description("已完成")]
        Completed = 2,

        /// <summary>已取消</summary>
        [Description("已取消")]
        Cancelled = 3,

        /// <summary>暂停</summary>
        [Description("暂停")]
        Suspended = 4,

        /// <summary>已归档</summary>
        [Description("已归档")]
        Archived = 5
    }
}
