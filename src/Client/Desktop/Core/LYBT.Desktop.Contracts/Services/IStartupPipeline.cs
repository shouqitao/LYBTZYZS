namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 启动步骤接口
/// 定义应用程序启动过程中的单个步骤
/// </summary>
public interface IStartupStep
{
    /// <summary>步骤名称（用于日志和进度显示）</summary>
    string Name { get; }

    /// <summary>步骤执行顺序（数字越小越先执行）</summary>
    int Order { get; }

    /// <summary>是否为必需步骤（必需步骤失败将终止启动流程）</summary>
    bool IsRequired { get; }

    /// <summary>
    /// 执行启动步骤
    /// </summary>
    /// <param name="progress">进度报告接口</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行结果</returns>
    Task<StartupStepResult> ExecuteAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// 启动管道接口
/// 协调和执行所有启动步骤
/// </summary>
public interface IStartupPipeline
{
    /// <summary>当前执行状态</summary>
    StartupPipelineState State { get; }

    /// <summary>已注册的启动步骤列表</summary>
    IReadOnlyList<IStartupStep> Steps { get; }

    /// <summary>管道状态变更事件</summary>
    event EventHandler<StartupPipelineStateChangedEventArgs>? StateChanged;

    /// <summary>单个步骤完成事件</summary>
    event EventHandler<StartupStepCompletedEventArgs>? StepCompleted;

    /// <summary>
    /// 注册启动步骤
    /// </summary>
    /// <param name="step">要注册的步骤</param>
    void RegisterStep(IStartupStep step);

    /// <summary>
    /// 执行所有已注册的启动步骤
    /// </summary>
    /// <param name="progress">进度报告接口</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>管道执行结果</returns>
    Task<StartupPipelineResult> ExecuteAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取管道诊断信息
    /// </summary>
    StartupPipelineDiagnostics GetDiagnostics();

    /// <summary>
    /// 重置管道状态，允许重新执行
    /// enhance-shell-connection-dialog: 支持连接失败后重试
    /// </summary>
    void Reset();
}

/// <summary>
/// 启动管道状态
/// </summary>
public enum StartupPipelineState
{
    /// <summary>未开始</summary>
    NotStarted,
    /// <summary>执行中</summary>
    Running,
    /// <summary>成功完成</summary>
    Completed,
    /// <summary>失败</summary>
    Failed,
    /// <summary>已取消</summary>
    Cancelled
}

/// <summary>
/// 启动步骤执行结果
/// </summary>
public record StartupStepResult
{
    /// <summary>是否成功</summary>
    public bool Success { get; init; }

    /// <summary>错误消息（失败时）</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>异常（失败时）</summary>
    public Exception? Exception { get; init; }

    /// <summary>执行耗时</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>是否被跳过</summary>
    public bool Skipped { get; init; }

    public static StartupStepResult Succeeded(TimeSpan duration) => new()
    {
        Success = true,
        Duration = duration
    };

    public static StartupStepResult Failed(string errorMessage, Exception? exception = null, TimeSpan duration = default) => new()
    {
        Success = false,
        ErrorMessage = errorMessage,
        Exception = exception,
        Duration = duration
    };

    public static StartupStepResult SkippedResult() => new()
    {
        Success = true,
        Skipped = true
    };
}

/// <summary>
/// 启动管道执行结果
/// </summary>
public record StartupPipelineResult
{
    /// <summary>是否成功</summary>
    public bool Success { get; init; }

    /// <summary>总耗时</summary>
    public TimeSpan TotalDuration { get; init; }

    /// <summary>各步骤执行结果</summary>
    public IReadOnlyDictionary<string, StartupStepResult> StepResults { get; init; } = new Dictionary<string, StartupStepResult>();

    /// <summary>失败的步骤名称（如果有）</summary>
    public string? FailedStepName { get; init; }

    /// <summary>错误消息（失败时）</summary>
    public string? ErrorMessage { get; init; }

    public static StartupPipelineResult Succeeded(TimeSpan totalDuration, IReadOnlyDictionary<string, StartupStepResult> stepResults) => new()
    {
        Success = true,
        TotalDuration = totalDuration,
        StepResults = stepResults
    };

    public static StartupPipelineResult Failed(string failedStepName, string errorMessage, TimeSpan totalDuration, IReadOnlyDictionary<string, StartupStepResult> stepResults) => new()
    {
        Success = false,
        FailedStepName = failedStepName,
        ErrorMessage = errorMessage,
        TotalDuration = totalDuration,
        StepResults = stepResults
    };
}

/// <summary>
/// 启动管道状态变更事件参数
/// </summary>
public class StartupPipelineStateChangedEventArgs : EventArgs
{
    public StartupPipelineState PreviousState { get; }
    public StartupPipelineState CurrentState { get; }
    public string? CurrentStepName { get; }

    public StartupPipelineStateChangedEventArgs(
        StartupPipelineState previousState,
        StartupPipelineState currentState,
        string? currentStepName = null)
    {
        PreviousState = previousState;
        CurrentState = currentState;
        CurrentStepName = currentStepName;
    }
}

/// <summary>
/// 启动步骤完成事件参数
/// </summary>
public class StartupStepCompletedEventArgs : EventArgs
{
    public string StepName { get; }
    public int StepOrder { get; }
    public StartupStepResult Result { get; }
    public int CompletedCount { get; }
    public int TotalCount { get; }

    public StartupStepCompletedEventArgs(
        string stepName,
        int stepOrder,
        StartupStepResult result,
        int completedCount,
        int totalCount)
    {
        StepName = stepName;
        StepOrder = stepOrder;
        Result = result;
        CompletedCount = completedCount;
        TotalCount = totalCount;
    }
}

/// <summary>
/// 启动管道诊断信息
/// </summary>
public record StartupPipelineDiagnostics(
    StartupPipelineState CurrentState,
    int TotalSteps,
    int CompletedSteps,
    int FailedSteps,
    TimeSpan? TotalDuration,
    IReadOnlyList<StartupStepDiagnostics> StepDiagnostics
);

/// <summary>
/// 单个启动步骤诊断信息
/// </summary>
public record StartupStepDiagnostics(
    string Name,
    int Order,
    bool IsRequired,
    bool Executed,
    bool Success,
    TimeSpan? Duration,
    string? ErrorMessage
);
