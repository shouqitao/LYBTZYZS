namespace LYBT.Desktop.Shell.Services.Diagnostics;

/// <summary>
/// 启动诊断接口
/// 记录各启动阶段的耗时，便于性能分析和问题定位
/// </summary>
public interface IStartupDiagnostics
{
    /// <summary>
    /// 开始记录一个启动步骤
    /// </summary>
    /// <param name="stepName">步骤名称</param>
    void BeginStep(string stepName);

    /// <summary>
    /// 结束当前启动步骤
    /// </summary>
    /// <param name="success">步骤是否成功</param>
    /// <param name="errorMessage">错误信息（失败时）</param>
    void EndStep(bool success = true, string? errorMessage = null);

    /// <summary>
    /// 记录启动开始
    /// </summary>
    void BeginStartup();

    /// <summary>
    /// 记录启动完成
    /// </summary>
    void EndStartup();

    /// <summary>
    /// 获取启动报告
    /// </summary>
    StartupReport GetReport();

    /// <summary>
    /// 记录一个自定义标记点
    /// </summary>
    /// <param name="markerName">标记名称</param>
    void RecordMarker(string markerName);
}

/// <summary>
/// 启动步骤记录
/// </summary>
public record StartupStepRecord(
    string StepName,
    DateTime StartTime,
    DateTime EndTime,
    TimeSpan Duration,
    bool Success,
    string? ErrorMessage = null
)
{
    /// <summary>
    /// 是否为慢步骤（超过3秒）
    /// </summary>
    public bool IsSlow => Duration.TotalSeconds > 3;
}

/// <summary>
/// 启动报告
/// </summary>
public class StartupReport
{
    /// <summary>
    /// 启动开始时间
    /// </summary>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// 启动结束时间
    /// </summary>
    public DateTime? EndTime { get; init; }

    /// <summary>
    /// 总启动时间
    /// </summary>
    public TimeSpan? TotalDuration => EndTime.HasValue ? EndTime.Value - StartTime : null;

    /// <summary>
    /// 所有步骤记录
    /// </summary>
    public IReadOnlyList<StartupStepRecord> Steps { get; init; } = [];

    /// <summary>
    /// 所有标记点
    /// </summary>
    public IReadOnlyList<StartupMarker> Markers { get; init; } = [];

    /// <summary>
    /// 慢步骤列表
    /// </summary>
    public IReadOnlyList<StartupStepRecord> SlowSteps => Steps.Where(s => s.IsSlow).ToList();

    /// <summary>
    /// 失败步骤列表
    /// </summary>
    public IReadOnlyList<StartupStepRecord> FailedSteps => Steps.Where(s => !s.Success).ToList();

    /// <summary>
    /// 启动是否成功
    /// </summary>
    public bool IsSuccess => !FailedSteps.Any();
}

/// <summary>
/// 启动标记点
/// </summary>
public record StartupMarker(
    string Name,
    DateTime Timestamp,
    TimeSpan ElapsedSinceStart
);
