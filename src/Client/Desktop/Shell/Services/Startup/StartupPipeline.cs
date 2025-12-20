using System.Collections.Concurrent;
using System.Diagnostics;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Localization;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services.Startup;

/// <summary>
/// 启动管道实现
/// 按顺序执行所有注册的启动步骤
/// </summary>
public class StartupPipeline : IStartupPipeline
{
    private readonly ILogger<StartupPipeline> _logger;
    private readonly List<IStartupStep> _steps = new();
    private readonly ConcurrentDictionary<string, StartupStepResult> _stepResults = new();
    private readonly object _stateLock = new();

    private StartupPipelineState _state = StartupPipelineState.NotStarted;
    private Stopwatch? _totalStopwatch;
    private int _completedSteps;

    public StartupPipeline(ILogger<StartupPipeline> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            foreach (var step in sortedSteps)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("启动管道被取消");
                    TransitionTo(StartupPipelineState.Cancelled);
                    _totalStopwatch.Stop();

                    return StartupPipelineResult.Failed(
                        step.Name,
                        "启动过程被取消",
                        _totalStopwatch.Elapsed,
                        new Dictionary<string, StartupStepResult>(_stepResults));
                }

                progress?.Report($"正在执行: {step.Name}...");

                var stepResult = await ExecuteStepAsync(step, progress, cancellationToken);
                _stepResults[step.Name] = stepResult;

                _completedSteps++;

                // 触发步骤完成事件
                StepCompleted?.Invoke(this, new StartupStepCompletedEventArgs(
                    step.Name,
                    step.Order,
                    stepResult,
                    _completedSteps,
                    sortedSteps.Count));

                // 如果必需步骤失败，终止管道
                if (!stepResult.Success && step.IsRequired)
                {
                    _logger.LogError("必需步骤 {StepName} 执行失败，终止启动管道: {ErrorMessage}",
                        step.Name, stepResult.ErrorMessage);

                    TransitionTo(StartupPipelineState.Failed);
                    _totalStopwatch.Stop();

                    return StartupPipelineResult.Failed(
                        step.Name,
                        stepResult.ErrorMessage ?? "未知错误",
                        _totalStopwatch.Elapsed,
                        new Dictionary<string, StartupStepResult>(_stepResults));
                }

                // 非必需步骤失败只记录警告
                if (!stepResult.Success && !step.IsRequired)
                {
                    _logger.LogWarning("可选步骤 {StepName} 执行失败，继续执行: {ErrorMessage}",
                        step.Name, stepResult.ErrorMessage);
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

        _logger.LogDebug("开始执行步骤: {StepName}", step.Name);

        try
        {
            var result = await step.ExecuteAsync(progress, cancellationToken);
            stepStopwatch.Stop();

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
            _logger.LogWarning("步骤 {StepName} 被取消", step.Name);
            return StartupStepResult.Failed("步骤被取消", duration: stepStopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stepStopwatch.Stop();
            _logger.LogError(ex, "步骤 {StepName} 执行异常", step.Name);
            return StartupStepResult.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("执行步骤", ex), ex, stepStopwatch.Elapsed);
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
