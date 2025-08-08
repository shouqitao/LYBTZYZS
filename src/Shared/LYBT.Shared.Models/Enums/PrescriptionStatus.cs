using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Enums
{

    /// <summary>
    /// 处方状态枚举 - 前后端共享（简化版）
    /// </summary>
    [Description("处方状态")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PrescriptionStatus
    {

        /// <summary>编辑中 - 处方正在编辑中</summary>
        [Description("编辑中")]
        Draft = 0,

        /// <summary>已完成 - 处方已完成</summary>
        [Description("已完成")]
        Completed = 1
    }
}