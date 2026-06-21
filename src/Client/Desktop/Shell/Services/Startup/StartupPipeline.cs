using System.Collections.Concurrent;
using System.Diagnostics;
using LYBT.Desktop.Contracts.Performance;
using LYBT.Desktop.Shared.Models;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.ExceptionHandling.Mappers;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services.Startup;

/// <summary>
/// 启动管道实现
/// 按顺序执行所有注册的启动步骤
/// </summary>
public class StartupPipeline : IStartupPipeline
{
    private readonly ILogger<StartupPipeline> _logger;
    private readonly IPerformanceMonitor _performanceMonitor;
    private readonly List<IStartupStep> _steps = new();
    private readonly ConcurrentDictionary<string, StartupStepResult> _stepResults = new();
    private readonly object _stateLock = new();

    private StartupPipelineState _state = StartupPipelineState.NotStarted;
    private Stopwatch? _totalStopwatch;
    private int _completedSteps;

    public StartupPipeline(
        ILogger<StartupPipeline> logger,
        IPerformanceMonitor performanceMonitor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
    }

    /// <inheritdoc />
    public StartupPipelineState State
    {
        get
        {
            lock (_stateLock)
            {
                return _state;
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<IStartupStep> Steps => _steps.AsReadOnly();

    /// <inheritdoc />
    public event EventHandler<StartupPipelineStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public event EventHandler<StartupStepCompletedEventArgs>? StepCompleted;

    /// <inheritdoc />
    public void RegisterStep(IStartupStep step)
    {
        ArgumentNullException.ThrowIfNull(step);

        lock (_stateLock)
        {
            if (_state != StartupPipelineState.NotStarted)
            {
                throw new InvalidOperationException("无法在管道启动后注册步骤");
            }

            // 检查是否已存在同名步骤
            if (_steps.Any(s => s.Name == step.Name))
            {
                throw new InvalidOperationException($"步骤 '{step.Name}' 已经注册");
            }

            _steps.Add(step);
            _logger.LogDebug("注册启动步骤: {StepName} (Order: {Order}, Required: {Required})",
                step.Name, step.Order, step.IsRequired);
        }
    }

    /// <inheritdoc />
    public async Task<StartupPipelineResult> ExecuteAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        lock (_stateLock)
        {
            if (_state != StartupPipelineState.NotStarted)
            {
                throw new InvalidOperationException($"管道已在状态 {_state}，无法重复执行");
            }
        }

        _totalStopwatch = Stopwatch.StartNew();
        _completedSteps = 0;

        // 按Order排序步骤
        var sortedSteps = _steps.OrderBy(s => s.Order).ToList();

        _logger.LogInformation("启动管道开始执行，共 {StepCount} 个步骤", sortedSteps.Count);
        TransitionTo(StartupPipelineState.Running);

        try
        {
            // 将排序后的步骤序列拆分为"执行单元"：
            //   - 相邻且 ParallelGroup 相同（非 null）的步骤合并为一个并行单元（Task.WhenAll）
            //   - 其余步骤各自构成顺序单元
            var executionUnits = BuildExecutionUnits(sortedSteps);

            foreach (var unit in executionUnits)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("启动管道被取消");
                    TransitionTo(StartupPipelineState.Cancelled);
                    _totalStopwatch.Stop();

                    return StartupPipelineResult.Failed(
                        unit.Steps[0].Name,
                        "启动过程被取消",
                        _totalStopwatch.Elapsed,
                        new Dictionary<string, StartupStepResult>(_stepResults));
                }

                progress?.Report(unit.Steps.Count == 1
                    ? $"正在执行: {unit.Steps[0].Name}..."
                    : $"正在并行执行: {string.Join(" / ", unit.Steps.Select(s => s.Name))}...");

                var unitResults = await ExecuteUnitAsync(unit, progress, cancellationToken);

                // 处理单元内每个步骤的结果
                IStartupStep? failedRequiredStep = null;
                StartupStepResult? failedRequiredResult = null;

                for (var i = 0; i < unit.Steps.Count; i++)
                {
                    var step = unit.Steps[i];
                    var stepResult = unitResults[i];

                    _stepResults[step.Name] = stepResult;
                    _completedSteps++;

                    // 触发步骤完成事件
                    StepCompleted?.Invoke(this, new StartupStepCompletedEventArgs(
                        step.Name,
                        step.Order,
                        stepResult,
                        _completedSteps,
                        sortedSteps.Count));

                    // 必需步骤失败 → 记录首个失败步骤，准备终止管道
                    if (!stepResult.Success && step.IsRequired && failedRequiredStep == null)
                    {
                        failedRequiredStep = step;
                        failedRequiredResult = stepResult;
                    }

                    // 非必需步骤失败只记录警告
                    if (!stepResult.Success && !step.IsRequired)
                    {
                        _logger.LogWarning("可选步骤 {StepName} 执行失败，继续执行: {ErrorMessage}",
                            step.Name, stepResult.ErrorMessage);
                    }
                }

                // 必需步骤失败 → 终止管道（在事件触发完毕后）
                if (failedRequiredStep != null)
                {
                    _logger.LogError("必需步骤 {StepName} 执行失败，终止启动管道: {ErrorMessage}",
                        failedRequiredStep.Name, failedRequiredResult!.ErrorMessage);

                    TransitionTo(StartupPipelineState.Failed);
                    _totalStopwatch.Stop();

                    return StartupPipelineResult.Failed(
                        failedRequiredStep.Name,
                        failedRequiredResult!.ErrorMessage ?? "未知错误",
                        _totalStopwatch.Elapsed,
                        new Dictionary<string, StartupStepResult>(_stepResults));
                }
            }

            _totalStopwatch.Stop();
            TransitionTo(StartupPipelineState.Completed);

            _logger.LogInformation("启动管道执行完成，总耗时: {TotalDuration}ms",
                _totalStopwatch.ElapsedMilliseconds);

            return StartupPipelineResult.Succeeded(
                _totalStopwatch.Elapsed,
                new Dictionary<string, StartupStepResult>(_stepResults));
        }
        catch (OperationCanceledException)
        {
            _totalStopwatch?.Stop();
            TransitionTo(StartupPipelineState.Cancelled);

            _logger.LogWarning("启动管道被取消");

            return StartupPipelineResult.Failed(
                "Unknown",
                "启动过程被取消",
                _totalStopwatch?.Elapsed ?? TimeSpan.Zero,
                new Dictionary<string, StartupStepResult>(_stepResults));
        }
        catch (Exception ex)
        {
            _totalStopwatch?.Stop();
            TransitionTo(StartupPipelineState.Failed);

            _logger.LogError(ex, "启动管道执行过程中发生未处理异常");

            return StartupPipelineResult.Failed(
                "Unknown",
                ClientErrorMessageMapper.GetSafeOperationFailureMessage("启动", ex),
                _totalStopwatch?.Elapsed ?? TimeSpan.Zero,
                new Dictionary<string, StartupStepResult>(_stepResults));
        }
    }

    /// <inheritdoc />
    public StartupPipelineDiagnostics GetDiagnostics()
    {
        var stepDiagnostics = _steps
            .OrderBy(s => s.Order)
            .Select(s =>
            {
                _stepResults.TryGetValue(s.Name, out var result);
                return new StartupStepDiagnostics(
                    Name: s.Name,
                    Order: s.Order,
                    IsRequired: s.IsRequired,
                    Executed: result != null,
                    Success: result?.Success ?? false,
                    Duration: result?.Duration,
                    ErrorMessage: result?.ErrorMessage
                );
            })
            .ToList();

        return new StartupPipelineDiagnostics(
            CurrentState: _state,
            TotalSteps: _steps.Count,
            CompletedSteps: _completedSteps,
            FailedSteps: _stepResults.Values.Count(r => !r.Success && !r.Skipped),
            TotalDuration: _totalStopwatch?.Elapsed,
            StepDiagnostics: stepDiagnostics
        );
    }

    /// <summary>
    /// 执行单个启动步骤
    /// </summary>
    private async Task<StartupStepResult> ExecuteStepAsync(
        IStartupStep step,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var stepStopwatch = Stopwatch.StartNew();

        // Phase 4 Task 4.4: 开始性能监控
        var timingKey = $"StartupStep_{step.Name}";
        _performanceMonitor.StartTiming(timingKey);

        _logger.LogDebug("开始执行步骤: {StepName}", step.Name);

        try
        {
            var result = await step.ExecuteAsync(progress, cancellationToken);
            stepStopwatch.Stop();

            // Phase 4 Task 4.4: 停止性能监控
            _performanceMonitor.StopTiming(timingKey);

            // 更新结果中的Duration
            var finalResult = result with { Duration = stepStopwatch.Elapsed };

            if (finalResult.Success)
            {
                _logger.LogInformation("步骤 {StepName} 执行成功，耗时: {Duration}ms",
                    step.Name, stepStopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning("步骤 {StepName} 执行失败，耗时: {Duration}ms, 错误: {Error}",
                    step.Name, stepStopwatch.ElapsedMilliseconds, finalResult.ErrorMessage);
            }

            return finalResult;
        }
        catch (OperationCanceledException)
        {
            stepStopwatch.Stop();
            _performanceMonitor.StopTiming(timingKey);
            _logger.LogWarning("步骤 {StepName} 被取消", step.Name);
            return StartupStepResult.Failed("步骤被取消", duration: stepStopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stepStopwatch.Stop();
            _performanceMonitor.StopTiming(timingKey);
            _logger.LogError(ex, "步骤 {StepName} 执行异常", step.Name);
            return StartupStepResult.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("执行步骤", ex), ex, stepStopwatch.Elapsed);
        }
    }

    /// <summary>
    /// 执行单元 —— 一个并行组或单个顺序步骤
    /// </summary>
    private async Task<List<StartupStepResult>> ExecuteUnitAsync(
        ExecutionUnit unit,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        if (unit.Steps.Count == 1)
        {
            var single = await ExecuteStepAsync(unit.Steps[0], progress, cancellationToken);
            return new List<StartupStepResult> { single };
        }

        // 并行执行 —— 保留与 ExecuteStepAsync 相同的监控/异常语义
        var results = await Task.WhenAll(unit.Steps.Select(step => ExecuteStepAsync(step, progress, cancellationToken)));
        return results.ToList();
    }

    /// <summary>
    /// 将排序后的步骤序列拆分为执行单元：
    ///   - 相邻且 ParallelGroup 相同（非 null）的步骤合并为一个并行单元
    ///   - ParallelGroup 为 null 或与前一不同 的步骤各自构成顺序单元
    /// </summary>
    private static List<ExecutionUnit> BuildExecutionUnits(IReadOnlyList<IStartupStep> sortedSteps)
    {
        var units = new List<ExecutionUnit>();
        var i = 0;
        while (i < sortedSteps.Count)
        {
            var current = sortedSteps[i];
            var group = current.ParallelGroup;

            if (string.IsNullOrEmpty(group))
            {
                units.Add(new ExecutionUnit(current));
                i++;
                continue;
            }

            // 收集相邻同组步骤
            var bucket = new List<IStartupStep> { current };
            var j = i + 1;
            while (j < sortedSteps.Count
                   && !string.IsNullOrEmpty(sortedSteps[j].ParallelGroup)
                   && string.Equals(sortedSteps[j].ParallelGroup, group, StringComparison.Ordinal))
            {
                bucket.Add(sortedSteps[j]);
                j++;
            }

            units.Add(new ExecutionUnit(bucket));
            i = j;
        }

        return units;
    }

    /// <summary>执行单元 —— 一个并行组或单个顺序步骤</summary>
    private sealed class ExecutionUnit
    {
        public List<IStartupStep> Steps { get; }

        public ExecutionUnit(IStartupStep single) => Steps = new List<IStartupStep> { single };

        public ExecutionUnit(IReadOnlyList<IStartupStep> parallel) => Steps = parallel.ToList();
    }

    /// <inheritdoc />
    public void Reset()
    {
        lock (_stateLock)
        {
            if (_state == StartupPipelineState.Running)
            {
                throw new InvalidOperationException("无法在管道执行中重置");
            }

            _logger.LogInformation("重置启动管道状态，之前状态: {PreviousState}", _state);

            _state = StartupPipelineState.NotStarted;
            _stepResults.Clear();
            _completedSteps = 0;
            _totalStopwatch = null;
        }
    }

    /// <summary>
    /// 状态转换
    /// </summary>
    private void TransitionTo(StartupPipelineState newState, string? currentStepName = null)
    {
        StartupPipelineState previousState;

        lock (_stateLock)
        {
            if (_state == newState)
            {
                return;
            }

            previousState = _state;
            _state = newState;
        }

        _logger.LogDebug("启动管道状态转换: {From} -> {To}", previousState, newState);
        StateChanged?.Invoke(this, new StartupPipelineStateChangedEventArgs(previousState, newState, currentStepName));
    }
}
