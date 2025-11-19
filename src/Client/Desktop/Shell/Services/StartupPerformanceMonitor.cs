using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services;

/// <summary>
/// 启动性能监控服务
/// 用于追踪和记录应用程序启动各阶段的性能指标
/// </summary>
public class StartupPerformanceMonitor
{
    private readonly ILogger<StartupPerformanceMonitor> _logger;
    private readonly Stopwatch _overallStopwatch = new();
    private readonly Dictionary<string, long> _stageTimes = new();
    private Stopwatch? _currentStageStopwatch;
    private string? _currentStageName;

    public StartupPerformanceMonitor(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<StartupPerformanceMonitor>();
    }

    /// <summary>
    /// 开始监控整体启动性能
    /// </summary>
    public void StartMonitoring()
    {
        _overallStopwatch.Restart();
        _logger.LogInformation("========== 启动性能监控开始 ==========");
    }

    /// <summary>
    /// 开始监控特定阶段
    /// </summary>
    /// <param name="stageName">阶段名称</param>
    public void StartStage(string stageName)
    {
        // 结束上一个阶段
        if (_currentStageStopwatch != null && _currentStageName != null)
        {
            EndStage();
        }

        _currentStageName = stageName;
        _currentStageStopwatch = Stopwatch.StartNew();
        _logger.LogInformation(" 阶段开始: {StageName}", stageName);
    }

    /// <summary>
    /// 结束当前阶段
    /// </summary>
    public void EndStage()
    {
        if (_currentStageStopwatch == null || _currentStageName == null)
        {
            return;
        }

        _currentStageStopwatch.Stop();
        var elapsedMs = _currentStageStopwatch.ElapsedMilliseconds;
        _stageTimes[_currentStageName] = elapsedMs;

        _logger.LogInformation(" 阶段完成: {StageName} - 耗时: {ElapsedMs}ms", _currentStageName, elapsedMs);

        _currentStageStopwatch = null;
        _currentStageName = null;
    }

    /// <summary>
    /// 完成监控并生成报告
    /// </summary>
    public void Finish()
    {
        // 结束最后一个阶段
        if (_currentStageStopwatch != null)
        {
            EndStage();
        }

        _overallStopwatch.Stop();
        var totalMs = _overallStopwatch.ElapsedMilliseconds;

        _logger.LogInformation("========== 启动性能报告 ==========");
        _logger.LogInformation("总启动时间: {TotalMs}ms ({TotalSeconds}秒)", totalMs, totalMs / 1000.0);
        _logger.LogInformation("");
        _logger.LogInformation("各阶段耗时：");

        foreach (var (stage, time) in _stageTimes.OrderByDescending(x => x.Value))
        {
            var percentage = (time * 100.0) / totalMs;
            _logger.LogInformation("  {Stage}: {Time}ms ({Percentage:F1}%)", stage, time, percentage);
        }

        _logger.LogInformation("=====================================");

        // 性能评估
        if (totalMs < 2000)
        {
            _logger.LogInformation(" 启动性能: 优秀 (< 2秒)");
        }
        else if (totalMs < 5000)
        {
            _logger.LogWarning(" 启动性能: 一般 (2-5秒)");
        }
        else
        {
            _logger.LogWarning(" 启动性能: 较慢 (> 5秒)");
        }
    }

    /// <summary>
    /// 获取当前总耗时（毫秒）
    /// </summary>
    public long GetElapsedMilliseconds() => _overallStopwatch.ElapsedMilliseconds;

    /// <summary>
    /// 获取指定阶段的耗时（毫秒）
    /// </summary>
    public long GetStageTime(string stageName)
    {
        return _stageTimes.TryGetValue(stageName, out var time) ? time : 0;
    }
}
