using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Enums
{

    /// <summary>
    /// 性别枚举 - 前后端共享
    /// </summary>
    [Description("性别")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Gender
    {

        /// <summary>未知</summary>
        [Description("未知")]
        Unknown = 0,

        /// <summary>男</summary>
        [Description("男")]
        Male = 1,

        /// <summary>女</summary>
        [Description("女")]
        Female = 2
    }
}