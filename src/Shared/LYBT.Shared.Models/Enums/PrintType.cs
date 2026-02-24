using System.ComponentModel;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 打印类型枚举
    /// 标识打印日志记录的来源类型
    /// </summary>
    public enum PrintType
    {
        /// <summary>处方打印</summary>
        [Description("处方打印")]
        Prescription = 1,

        /// <summary>验方打印</summary>
        [Description("验方打印")]
        Formula = 2
    }
}
