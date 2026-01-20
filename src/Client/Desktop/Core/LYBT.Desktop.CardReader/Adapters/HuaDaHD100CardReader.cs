using System.IO;
using System.Text;
using LYBT.Desktop.CardReader.Abstractions;
using LYBT.Desktop.CardReader.Models;
using LYBT.Desktop.CardReader.Native;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.CardReader.Adapters;

/// <summary>
/// 华大HD100身份证读卡器适配器
/// 实现ICardReader接口，封装HDstdapi.dll原生调用
/// </summary>
public sealed class HuaDaHD100CardReader : ICardReader
{
    private readonly ILogger<HuaDaHD100CardReader>? _logger;
    private readonly CardReaderOptions _options;
    private readonly object _lockObj = new();
    private bool _isConnected;
    private bool _disposed;

    /// <summary>读卡器名称</summary>
    public string Name => "华大HD100";

    /// <summary>厂商</summary>
    public string Vendor => "华大电子";

    /// <summary>型号</summary>
    public string Model => "HD100";

    /// <summary>是否已连接</summary>
    public bool IsConnected => _isConnected;

    /// <summary>连接状态变化事件</summary>
    public event EventHandler<CardReaderConnectionEventArgs>? ConnectionStateChanged;

    /// <summary>检测到卡片事件</summary>
    public event EventHandler<CardDetectedEventArgs>? CardDetected;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options">配置选项</param>
    /// <param name="logger">日志记录器（可选）</param>
    public HuaDaHD100CardReader(CardReaderOptions? options = null, ILogger<HuaDaHD100CardReader>? logger = null)
    {
        _options = options ?? new CardReaderOptions();
        _logger = logger;
    }

    /// <summary>
    /// 初始化读卡器连接
    /// </summary>
    public Task<bool> ConnectAsync(string? connectionString = null)
    {
        return Task.Run(() =>
        {
            lock (_lockObj)
            {
                if (_isConnected)
                {
                    _logger?.LogWarning("读卡器已连接，跳过重复连接");
                    return true;
                }

                try
                {
                    // 确定端口号
                    int port = _options.UsbPort;
                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        if (int.TryParse(connectionString, out var customPort))
                            port = customPort;
                    }

                    _logger?.LogInformation("正在连接华大HD100读卡器，端口: {Port}", port);

                    // 初始化设备
                    var result = HuaDaNativeMethods.HD_InitComm(port);
                    if (result < 0)
                    {
                        _logger?.LogError("读卡器连接失败，错误码: {ErrorCode}", result);
                        OnConnectionStateChanged(false, $"连接失败，错误码: {result}");
                        return false;
                    }

                    _isConnected = true;
                    _logger?.LogInformation("华大HD100读卡器连接成功");
                    OnConnectionStateChanged(true, null);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "连接读卡器时发生异常");
                    OnConnectionStateChanged(false, ex.Message);
                    return false;
                }
            }
        });
    }

    /// <summary>
    /// 断开读卡器连接
    /// </summary>
    public Task DisconnectAsync()
    {
        return Task.Run(() =>
        {
            lock (_lockObj)
            {
                if (!_isConnected) return;

                try
                {
                    HuaDaNativeMethods.HD_CloseComm();
                    _isConnected = false;
                    _logger?.LogInformation("华大HD100读卡器已断开连接");
                    OnConnectionStateChanged(false, null);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "断开读卡器连接时发生异常");
                }
            }
        });
    }

    /// <summary>
    /// 读取身份证信息
    /// </summary>
    public Task<CardReadResult> ReadCardAsync(
        bool savePhoto = false,
        string? photoPath = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            lock (_lockObj)
            {
                if (!_isConnected)
                {
                    return CardReadResult.Failure(-2, "读卡器未连接");
                }

                try
                {
                    // 准备StringBuilder缓冲区
                    var bmpData = new StringBuilder(256);
                    var name = new StringBuilder(100);
                    var sex = new StringBuilder(10);
                    var nation = new StringBuilder(50);
                    var birth = new StringBuilder(20);
                    var address = new StringBuilder(200);
                    var certNo = new StringBuilder(20);
                    var department = new StringBuilder(100);
                    var effectDate = new StringBuilder(20);
                    var expire = new StringBuilder(20);

                    // 设置照片保存路径
                    if (savePhoto)
                    {
                        var photoDir = photoPath ?? _options.PhotoSaveDirectory;
                        if (!Directory.Exists(photoDir))
                            Directory.CreateDirectory(photoDir);

                        var photoFile = Path.Combine(photoDir, $"idcard_{DateTime.Now:yyyyMMddHHmmss}.bmp");
                        bmpData.Append(photoFile);
                    }

                    // 卡认证
                    var authResult = HuaDaNativeMethods.HD_Authenticate(1);
                    if (authResult != 0)
                    {
                        _logger?.LogWarning("卡认证失败，错误码: {ErrorCode}", authResult);
                        return CardReadResult.Failure(authResult, "卡认证失败，请确认身份证已放置正确");
                    }

                    // 读取基本信息
                    var readResult = HuaDaNativeMethods.HD_Read_BaseMsg(
                        bmpData, name, sex, nation, birth,
                        address, certNo, department, effectDate, expire);

                    if (readResult != 0)
                    {
                        _logger?.LogWarning("读卡失败，错误码: {ErrorCode}", readResult);
                        return CardReadResult.Failure(readResult);
                    }

                    _logger?.LogInformation("读卡成功: {Name}, {CertNo}",
                        name.ToString().Trim(),
                        MaskIdNumber(certNo.ToString().Trim()));

                    var result = CardReadResult.Success(
                        name.ToString(),
                        certNo.ToString(),
                        sex.ToString(),
                        nation.ToString(),
                        birth.ToString(),
                        address.ToString(),
                        department.ToString(),
                        effectDate.ToString(),
                        expire.ToString());

                    // 设置证件类型
                    var cardType = HuaDaNativeMethods.GetCardType();
                    result.CardType = (CardType)cardType;

                    // 设置照片路径
                    if (savePhoto && bmpData.Length > 0)
                    {
                        result.PhotoFilePath = bmpData.ToString();
                    }

                    return result;
                }
                catch (AccessViolationException ex)
                {
                    _logger?.LogError(ex, "读卡时发生内存访问异常");
                    return CardReadResult.Failure(-100, "读卡器访问异常，请重新连接设备");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "读卡时发生异常");
                    return CardReadResult.Failure(-99, ex.Message);
                }
            }
        }, cancellationToken);
    }

    /// <summary>
    /// 检测是否有卡片
    /// </summary>
    public Task<bool> DetectCardAsync()
    {
        return Task.Run(() =>
        {
            lock (_lockObj)
            {
                if (!_isConnected) return false;

                try
                {
                    var result = HuaDaNativeMethods.HD_Authenticate(1);
                    if (result == 0)
                    {
                        CardDetected?.Invoke(this, new CardDetectedEventArgs());
                        return true;
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            }
        });
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        DisconnectAsync().Wait(1000);
        _disposed = true;
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
    /// 脱敏身份证号（用于日志）
    /// </summary>
    private static string MaskIdNumber(string idNumber)
    {
        if (string.IsNullOrEmpty(idNumber) || idNumber.Length < 10)
            return "***";

        return $"{idNumber[..4]}**********{idNumber[^4..]}";
    }
}
