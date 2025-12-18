using System.ComponentModel;

namespace LYBT.Shared.Models.Enums;

/// <summary>
/// 方剂类型枚举
/// </summary>
public enum FormulaType
{
    /// <summary>经典方</summary>
    [Description("经典方")]
    Classic = 1,

    /// <summary>经验方</summary>
    [Description("经验方")]
    Experience = 2
}
