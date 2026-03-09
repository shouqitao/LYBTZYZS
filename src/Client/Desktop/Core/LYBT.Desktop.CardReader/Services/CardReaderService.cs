using LYBT.Desktop.CardReader.Abstractions;
using LYBT.Desktop.CardReader.Models;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.CardReader.Services;

/// <summary>
/// 读卡器服务实现
/// 提供高层业务逻辑封装，管理读卡器生命周期和自动读卡模式
/// </summary>
public class CardReaderService : ICardReaderService
{
    private readonly ICardReaderFactory _factory;
    private readonly CardReaderOptions _options;
    private readonly ILogger<CardReaderService>? _logger;
    private readonly object _lockObj = new();

    private ICardReader? _currentReader;
    private Timer? _autoReadTimer;
    private bool _isAutoReading;
    private bool _disposed;
    private string? _lastReadIdNumber;

    /// <summary>当前读卡器实例</summary>
    public ICardReader? CurrentReader => _currentReader;

    /// <summary>是否已连接</summary>
    public bool IsConnected => _currentReader?.IsConnected ?? false;

    /// <summary>是否启用自动读卡模式</summary>
    public bool IsAutoReadEnabled => _isAutoReading;

    /// <summary>连接状态变化事件</summary>
    public event EventHandler<CardReaderConnectionEventArgs>? ConnectionStateChanged;

    /// <summary>自动读卡成功事件</summary>
    public event EventHandler<CardReadResult>? CardReadCompleted;

    /// <summary>读卡错误事件</summary>
    public event EventHandler<CardReadErrorEventArgs>? CardReadError;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="factory">读卡器工厂</param>
    /// <param name="options">读卡器配置选项 (PRD-13: 从 appsettings.json 读取)</param>
    /// <param name="logger">日志记录器（可选）</param>
    public CardReaderService(ICardReaderFactory factory, CardReaderOptions? options = null, ILogger<CardReaderService>? logger = null)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _options = options ?? new CardReaderOptions();
        _logger = logger;
    }

    /// <summary>
    /// 初始化读卡器
    /// </summary>
    /// <param name="readerType">读卡器类型（Auto=自动检测）</param>
    /// <returns>是否成功</returns>
    public async Task<bool> InitializeAsync(CardReaderType readerType = CardReaderType.Auto)
    {
        lock (_lockObj)
        {
            if (_currentReader != null)
            {
                _logger?.LogWarning("读卡器已初始化，先断开现有连接");
            }
        }

        // 先断开现有连接
        await DisconnectAsync();

        try
        {
            ICardReader? reader;

            if (readerType == CardReaderType.Auto)
            {
                _logger?.LogInformation("开始自动检测读卡器...");
                reader = await _factory.AutoDetectReaderAsync(_options);

                if (reader == null)
                {
                    _logger?.LogWarning("未检测到可用的读卡器");
                    OnConnectionStateChanged(false, "未检测到可用的读卡器");
                    return false;
                }

                _logger?.LogInformation("自动检测到读卡器: {Name} ({Vendor})", reader.Name, reader.Vendor);
            }
            else
            {
                reader = _factory.CreateReader(readerType, _options);
                if (!await reader.ConnectAsync())
                {
                    _logger?.LogError("连接读卡器失败: {Type}", readerType);
                    reader.Dispose();
                    OnConnectionStateChanged(false, $"连接{readerType}读卡器失败");
                    return false;
                }
            }

            lock (_lockObj)
            {
                _currentReader = reader;
                _currentReader.ConnectionStateChanged += OnReaderConnectionStateChanged;
            }

            _logger?.LogInformation("读卡器初始化成功: {Name}", reader.Name);
            OnConnectionStateChanged(true, null);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "初始化读卡器时发生异常");
            OnConnectionStateChanged(false, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 断开读卡器连接
    /// </summary>
    public async Task DisconnectAsync()
    {
        StopAutoRead();

        ICardReader? reader;
        lock (_lockObj)
        {
            reader = _currentReader;
            _currentReader = null;
        }

        if (reader != null)
        {
            reader.ConnectionStateChanged -= OnReaderConnectionStateChanged;

            try
            {
                await reader.DisconnectAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "断开读卡器连接时发生异常");
            }
            finally
            {
                reader.Dispose();
            }

            _logger?.LogInformation("读卡器已断开连接");
            OnConnectionStateChanged(false, null);
        }
    }

    /// <summary>
    /// 读取身份证
    /// </summary>
    /// <param name="savePhoto">是否保存照片</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>读取结果</returns>
    public async Task<CardReadResult> ReadCardAsync(bool savePhoto = false, CancellationToken cancellationToken = default)
    {
        ICardReader? reader;
        lock (_lockObj)
        {
            reader = _currentReader;
        }

        if (reader == null || !reader.IsConnected)
        {
            return CardReadResult.Failure(-1, "读卡器未连接");
        }

        try
        {
            var result = await reader.ReadCardAsync(savePhoto, null, cancellationToken);

            if (result.IsSuccess)
            {
                _logger?.LogInformation("读卡成功: {Name}", result.Name);
                _lastReadIdNumber = result.IdNumber;
            }
            else
            {
                _logger?.LogWarning("读卡失败: {ErrorCode} - {ErrorMessage}",
                    result.ErrorCode, result.ErrorMessage);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("读卡操作被取消");
            return CardReadResult.Failure(-3, "操作被取消");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "读卡时发生异常");
            OnCardReadError(-99, ex.Message, ex);
            return CardReadResult.Failure(-99, ex.Message);
        }
    }

    /// <summary>
    /// 启动自动读卡模式
    /// </summary>
    /// <param name="intervalMs">检测间隔（毫秒）</param>
    public void StartAutoRead(int intervalMs = 500)
    {
        if (intervalMs < 100)
            intervalMs = 100;

        lock (_lockObj)
        {
            if (_isAutoReading)
            {
                _logger?.LogWarning("自动读卡模式已启用");
                return;
            }

            if (_currentReader == null || !_currentReader.IsConnected)
            {
                _logger?.LogWarning("读卡器未连接，无法启动自动读卡");
                return;
            }

            _isAutoReading = true;
            _lastReadIdNumber = null;

            _autoReadTimer = new Timer(
                AutoReadCallback,
                null,
                intervalMs,
                intervalMs);

            _logger?.LogInformation("自动读卡模式已启动，间隔: {Interval}ms", intervalMs);
        }
    }

    /// <summary>
    /// 停止自动读卡模式
    /// </summary>
    public void StopAutoRead()
    {
        lock (_lockObj)
        {
            if (!_isAutoReading) return;

            _isAutoReading = false;
            _autoReadTimer?.Dispose();
            _autoReadTimer = null;
            _lastReadIdNumber = null;

            _logger?.LogInformation("自动读卡模式已停止");
        }
    }

    /// <summary>
    /// 自动读卡回调
    /// </summary>
    private async void AutoReadCallback(object? state)
    {
        if (!_isAutoReading) return;

        ICardReader? reader;
        lock (_lockObj)
        {
            reader = _currentReader;
            if (reader == null || !reader.IsConnected || !_isAutoReading)
                return;
        }

        try
        {
            // 检测是否有卡片
            if (!await reader.DetectCardAsync())
                return;

            // 读取卡片
            var result = await reader.ReadCardAsync(false, null, CancellationToken.None);

            if (!result.IsSuccess) return;

            // 检查是否是同一张卡（防止重复触发）
            if (_lastReadIdNumber == result.IdNumber) return;

            _lastReadIdNumber = result.IdNumber;
            _logger?.LogInformation("自动读卡成功: {Name}", result.Name);

            // 触发事件（在UI线程）
            CardReadCompleted?.Invoke(this, result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "自动读卡时发生异常");
            OnCardReadError(-99, ex.Message, ex);
        }
    }

    /// <summary>
    /// 处理底层读卡器连接状态变化
    /// </summary>
    private void OnReaderConnectionStateChanged(object? sender, CardReaderConnectionEventArgs e)
    {
        if (!e.IsConnected)
        {
            StopAutoRead();
        }

        ConnectionStateChanged?.Invoke(this, e);
    }

    /// <summary>
    /// 触发连接状态变化事件
    /// </summary>
    private void OnConnectionStateChanged(bool isConnected, string? errorMessage)
    {
        ConnectionStateChanged?.Invoke(this, new CardReaderConnectionEventArgs
        {
            IsConnected = isConnected,
            ErrorMessage = errorMessage
        });
    }

    /// <summary>
    /// 触发读卡错误事件
    /// </summary>
    private void OnCardReadError(int errorCode, string message, Exception? exception = null)
    {
        CardReadError?.Invoke(this, new CardReadErrorEventArgs
        {
            ErrorCode = errorCode,
            Message = message,
            Exception = exception
        });
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        StopAutoRead();

        lock (_lockObj)
        {
            _currentReader?.Dispose();
            _currentReader = null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
