using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services.Diagnostics;

/// <summary>
/// 启动诊断实现
/// 记录各启动阶段的耗时，便于性能分析和问题定位
/// </summary>
public class StartupDiagnostics : IStartupDiagnostics
{
    private readonly ILogger<StartupDiagnostics> _logger;
    private readonly object _lock = new();
    private readonly List<StartupStepRecord> _steps = new();
    private readonly List<StartupMarker> _markers = new();
    private readonly Stopwatch _totalStopwatch = new();

    private DateTime _startupStartTime;
    private DateTime? _startupEndTime;
    private string? _currentStepName;
    private DateTime _currentStepStartTime;
    private readonly Stopwatch _currentStepStopwatch = new();

    /// <summary>
    /// 慢步骤阈值（秒）
    /// </summary>
    private const double SlowStepThresholdSeconds = 3.0;

    public StartupDiagnostics(ILogger<StartupDiagnostics> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void BeginStartup()
    {
        lock (_lock)
        {
            _startupStartTime = DateTime.Now;
            _startupEndTime = null;
            _steps.Clear();
            _markers.Clear();
            _totalStopwatch.Restart();
        }

        _logger.LogInformation("========== 应用启动开始 ==========");
    }

    /// <inheritdoc />
    public void EndStartup()
    {
        lock (_lock)
        {
            _startupEndTime = DateTime.Now;
            _totalStopwatch.Stop();
        }

        var report = GetReport();
        LogStartupReport(report);
    }

    /// <inheritdoc />
    public void BeginStep(string stepName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stepName);

        lock (_lock)
        {
            // 如果有未结束的步骤，先结束它
            if (_currentStepName != null)
            {
                EndStepInternal(true, "自动结束（新步骤开始）");
            }

            _currentStepName = stepName;
            _currentStepStartTime = DateTime.Now;
            _currentStepStopwatch.Restart();
        }

        _logger.LogDebug("启动步骤开始: {StepName}", stepName);
    }

    /// <inheritdoc />
    public void EndStep(bool success = true, string? errorMessage = null)
    {
        lock (_lock)
        {
            if (_currentStepName == null)
            {
                _logger.LogWarning("EndStep被调用但没有活跃的步骤");
                return;
            }

            EndStepInternal(success, errorMessage);
        }
    }

    /// <inheritdoc />
    public void RecordMarker(string markerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(markerName);

        lock (_lock)
        {
            var marker = new StartupMarker(
                Name: markerName,
                Timestamp: DateTime.Now,
                ElapsedSinceStart: _totalStopwatch.Elapsed
            );
            _markers.Add(marker);
        }

        _logger.LogDebug("启动标记: {MarkerName} @ {Elapsed:F2}s", markerName, _totalStopwatch.Elapsed.TotalSeconds);
    }

    /// <inheritdoc />
    public StartupReport GetReport()
    {
        lock (_lock)
        {
            return new StartupReport
            {
                StartTime = _startupStartTime,
                EndTime = _startupEndTime,
                Steps = _steps.ToList().AsReadOnly(),
                Markers = _markers.ToList().AsReadOnly()
            };
        }
    }

    /// <summary>
    /// 内部结束步骤方法（必须在锁内调用）
    /// </summary>
    private void EndStepInternal(bool success, string? errorMessage)
    {
        _currentStepStopwatch.Stop();
        var duration = _currentStepStopwatch.Elapsed;

        var record = new StartupStepRecord(
            StepName: _currentStepName!,
            StartTime: _currentStepStartTime,
            EndTime: DateTime.Now,
            Duration: duration,
            Success: success,
            ErrorMessage: errorMessage
        );

        _steps.Add(record);

        // 记录日志
        if (!success)
        {
            _logger.LogError("启动步骤失败: {StepName}, 耗时: {Duration:F2}s, 错误: {Error}",
                _currentStepName, duration.TotalSeconds, errorMessage);
        }
        else if (record.IsSlow)
        {
            _logger.LogWarning("启动步骤完成(慢): {StepName}, 耗时: {Duration:F2}s (超过{Threshold}s阈值)",
                _currentStepName, duration.TotalSeconds, SlowStepThresholdSeconds);
        }
        else
        {
            _logger.LogDebug("启动步骤完成: {StepName}, 耗时: {Duration:F2}s",
                _currentStepName, duration.TotalSeconds);
        }

        _currentStepName = null;
    }

    /// <summary>
    /// 记录启动报告到日志
    /// </summary>
    private void LogStartupReport(StartupReport report)
    {
        _logger.LogInformation("========== 应用启动完成 ==========");
        _logger.LogInformation("总启动时间: {Duration:F2}s", report.TotalDuration?.TotalSeconds ?? 0);
        _logger.LogInformation("步骤数量: {Count}", report.Steps.Count);

        if (report.SlowSteps.Any())
        {
            _logger.LogWarning("慢步骤数量: {Count}", report.SlowSteps.Count);
            foreach (var step in report.SlowSteps)
            {
                _logger.LogWarning("  - {StepName}: {Duration:F2}s", step.StepName, step.Duration.TotalSeconds);
            }
        }

        if (report.FailedSteps.Any())
        {
            _logger.LogError("失败步骤数量: {Count}", report.FailedSteps.Count);
            foreach (var step in report.FailedSteps)
            {
                _logger.LogError("  - {StepName}: {Error}", step.StepName, step.ErrorMessage);
            }
        }

        // 输出详细步骤列表
        _logger.LogDebug("启动步骤详情:");
        foreach (var step in report.Steps)
        {
            var status = step.Success ? (step.IsSlow ? "SLOW" : "OK") : "FAIL";
            _logger.LogDebug("  [{Status}] {StepName}: {Duration:F2}s",
                status, step.StepName, step.Duration.TotalSeconds);
        }

        _logger.LogInformation("==================================");
    }
}
