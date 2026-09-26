using System.Collections.ObjectModel;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Constants;

/// <summary>
/// 重复处理策略下拉项（US-SHELL-021 AC②：Skip/Update/Error 的中文展示与取值）。
/// </summary>
/// <param name="Value">请求 DTO 使用的策略值</param>
/// <param name="Display">界面显示文本</param>
public sealed record DuplicateStrategyOption(DuplicateStrategy Value, string Display);

/// <summary>
/// 批量导入重复处理策略选项（患者/药材/验方三类导入共用）。
/// </summary>
public static class DuplicateStrategyOptions
{
    /// <summary>全部策略选项（顺序与 <see cref="DuplicateStrategy"/> 一致：跳过/更新/报错）</summary>
    public static ReadOnlyCollection<DuplicateStrategyOption> All { get; } = new(new[]
    {
        new DuplicateStrategyOption(DuplicateStrategy.Skip, "跳过重复"),
        new DuplicateStrategyOption(DuplicateStrategy.Update, "更新重复"),
        new DuplicateStrategyOption(DuplicateStrategy.Error, "重复即报错")
    });

    /// <summary>取指定策略的界面显示文本（如确认提示中的「跳过重复」）</summary>
    /// <param name="strategy">重复处理策略</param>
    public static string GetDisplay(DuplicateStrategy strategy)
        => All.First(option => option.Value == strategy).Display;
}
