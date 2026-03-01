using System.Collections.ObjectModel;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Constants;

/// <summary>
/// 共享枚举选项常量，避免各 ViewModel 重复创建
/// </summary>
public static class CommonOptions
{
    /// <summary>通用状态选项 (Enabled/Disabled)</summary>
    public static ReadOnlyCollection<CommonStatus> StatusOptions { get; } =
        new(Enum.GetValues<CommonStatus>());
}
