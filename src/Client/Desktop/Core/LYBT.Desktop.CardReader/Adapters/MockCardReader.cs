using LYBT.Desktop.CardReader.Abstractions;
using LYBT.Desktop.CardReader.Models;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.CardReader.Adapters;

/// <summary>
/// 模拟读卡器（用于开发测试）
/// 不需要真实硬件即可测试读卡流程
/// </summary>
public sealed class MockCardReader : ICardReader
{
    private bool _isConnected;
    private readonly CardReaderOptions _options;

    /// <summary>读卡器名称</summary>
    public string Name => "模拟读卡器";

    /// <summary>厂商</summary>
    public string Vendor => "LYBT";

    /// <summary>型号</summary>
    public string Model => "Mock-v1";

    /// <summary>是否已连接</summary>
    public bool IsConnected => _isConnected;

    /// <summary>连接状态变化事件</summary>
    public event EventHandler<CardReaderConnectionEventArgs>? ConnectionStateChanged;

    /// <summary>检测到卡片事件</summary>
    public event EventHandler<CardDetectedEventArgs>? CardDetected;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MockCardReader(CardReaderOptions? options = null)
    {
        _options = options ?? new CardReaderOptions();
    }

    /// <summary>
    /// 模拟连接
    /// </summary>
    public Task<bool> ConnectAsync(string? connectionString = null)
    {
        return Task.Run(async () =>
        {
            // 模拟连接延迟
            await Task.Delay(500);
            _isConnected = true;
            ConnectionStateChanged?.Invoke(this, new CardReaderConnectionEventArgs { IsConnected = true });
            return true;
        });
    }

    /// <summary>
    /// 模拟断开连接
    /// </summary>
    public Task DisconnectAsync()
    {
        _isConnected = false;
        ConnectionStateChanged?.Invoke(this, new CardReaderConnectionEventArgs { IsConnected = false });
        return Task.CompletedTask;
    }

    /// <summary>
    /// 模拟读卡（返回测试数据）
    /// </summary>
    public Task<CardReadResult> ReadCardAsync(
        bool savePhoto = false,
        string? photoPath = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(async () =>
        {
            if (!_isConnected)
            {
                return CardReadResult.Failure(-2, "读卡器未连接");
            }

            // 模拟读卡延迟
            await Task.Delay(800, cancellationToken);

            // 返回模拟数据
            return new CardReadResult
            {
                IsSuccess = true,
                Name = "张三",
                IdNumber = "110101199001011234",
                Gender = Gender.Male,
                Nation = "汉",
                BirthDate = new DateTime(1990, 1, 1),
                Address = "北京市东城区测试路1号",
                IssuingAuthority = "北京市公安局东城分局",
                ValidFrom = new DateTime(2020, 1, 1),
                ValidTo = new DateTime(2040, 1, 1),
                CardType = CardType.IdCard,
                ReadTime = DateTime.Now
            };
        }, cancellationToken);
    }

    /// <summary>
    /// 模拟检测卡片（始终返回true）
    /// </summary>
    public Task<bool> DetectCardAsync()
    {
        if (_isConnected)
        {
            CardDetected?.Invoke(this, new CardDetectedEventArgs());
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _isConnected = false;
    }
}
