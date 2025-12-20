using System.Windows.Threading;
using LYBT.Desktop.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services;

/// <summary>
/// 应用级别的统一定时任务调度服务实现
/// OpenSpec: refactor-token-sliding-expiration (AUTH-000)
/// 使用单一DispatcherTimer,每秒触发Tick事件
/// </summary>
public class ApplicationTickService : IApplicationTickService, IDisposable
{
    private readonly ILogger<ApplicationTickService> _logger;
    private readonly DispatcherTimer _timer;
    private readonly object _lock = new();

    private long _tickCount;
    private bool _isRunning;
    private bool _disposed;

    /// <inheritdoc />
    public event EventHandler<ApplicationTickEventArgs>? Tick;

    /// <inheritdoc />
    public long TickCount
    {
        get
        {
            lock (_lock)
            {
                return _tickCount;
            }
        }
    }

    /// <inheritdoc />
    public bool IsRunning
    {
        get
        {
            lock (_lock)
            {
                return _isRunning;
            }
        }
    }

    public ApplicationTickService(ILogger<ApplicationTickService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;

        _logger.LogDebug("ApplicationTickService已创建");
    }

    /// <inheritdoc />
    public void Start()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                _logger.LogWarning("尝试启动已释放的ApplicationTickService");
                return;
            }

            if (_isRunning)
            {
                _logger.LogDebug("ApplicationTickService已在运行中");
                return;
            }

            _timer.Start();
            _isRunning = true;
            _logger.LogInformation("ApplicationTickService已启动");
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        lock (_lock)
        {
            if (!_isRunning)
            {
                return;
            }

            _timer.Stop();
            _isRunning = false;
            _logger.LogInformation("ApplicationTickService已停止");
        }
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        long currentTick;
        lock (_lock)
        {
            _tickCount++;
            currentTick = _tickCount;
        }

        var args = new ApplicationTickEventArgs
        {
            TickCount = currentTick,
            Timestamp = DateTime.Now
        };

        try
        {
            Tick?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            // 不让单个订阅者的异常影响其他订阅者
            _logger.LogError(ex, "Tick事件处理器执行出错 (TickCount={TickCount})", currentTick);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _isRunning = false;
        }

        _logger.LogDebug("ApplicationTickService已释放");
    }
}
