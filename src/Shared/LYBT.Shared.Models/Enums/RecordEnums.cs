using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Enums {
    /// <summary>
    /// 诊疗状态枚举 - 前后端共享
    /// </summary>
    [Description("诊疗状态")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ConsultationStatus {
        /// <summary>诊疗中</summary>
        [Description("诊疗中")]
        InProgress = 1,

        /// <summary>已完成</summary>
        [Description("已完成")]
        Completed = 2,

        /// <summary>已取消</summary>
        [Description("已取消")]
        Cancelled = 3
    }

}