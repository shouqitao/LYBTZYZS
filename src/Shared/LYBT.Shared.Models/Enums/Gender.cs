using System.ComponentModel;

namespace LYBT.Shared.Models.Enums
{

    /// <summary>
    /// 性别枚举 - 前后端共享
    /// OpenSpec: unify-enums-to-shared - 移除冗余JsonConverter（已全局配置）
    /// </summary>
    [Description("性别")]
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
