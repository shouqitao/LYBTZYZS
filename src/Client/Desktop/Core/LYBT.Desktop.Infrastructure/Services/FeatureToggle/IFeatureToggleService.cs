using LYBT.Desktop.Controls.Models;

namespace LYBT.Desktop.Infrastructure.Services.FeatureToggle;

/// <summary>
/// 功能开关服务契约（T8: US-CFG-004）。
/// 策略级开关（非逐按钮）——VM/业务经本服务读取，配置变更热更新无需重启。
/// </summary>
public interface IFeatureToggleService
{
    /// <summary>读取布尔开关</summary>
    bool IsEnabled(string key);

    /// <summary>读取字符串策略值</summary>
    string? GetValue(string key);

    /// <summary>
    /// 当前重复药材合并策略（F-01: DuplicateHerbMergeStrategy 消费——缺省/非法值回退 Max）
    /// </summary>
    DuplicateDosageStrategy GetDuplicateMergeStrategy();

    /// <summary>配置变更通知（热更新）</summary>
    event EventHandler? TogglesChanged;
}
