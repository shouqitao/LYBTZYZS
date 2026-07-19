using LYBT.Desktop.Infrastructure.CardReader.Models;

namespace LYBT.Desktop.Infrastructure.CardReader.Abstractions;

/// <summary>
/// 身份证读卡器抽象接口
/// 采用策略模式支持多厂商读卡器
/// </summary>
public interface ICardReader : IDisposable
{
    /// <summary>
    /// 读卡器名称（用于显示和日志）
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 读卡器厂商
    /// </summary>
    string Vendor { get; }

    /// <summary>
    /// 读卡器型号
    /// </summary>
    string Model { get; }

    /// <summary>
    /// 是否已连接
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 初始化读卡器连接
    /// </summary>
    /// <param name="connectionString">连接参数（如端口号、USB等）</param>
    /// <returns>是否成功</returns>
    Task<bool> ConnectAsync(string? connectionString = null);

    /// <summary>
    /// 断开读卡器连接
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// 读取身份证信息
    /// </summary>
    /// <param name="savePhoto">是否保存照片到文件</param>
    /// <param name="photoPath">照片保存路径（可选，默认临时目录）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>读取结果</returns>
    Task<CardReadResult> ReadCardAsync(
        bool savePhoto = false,
        string? photoPath = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 检测是否有卡片在感应区
    /// </summary>
    /// <returns>是否有卡</returns>
    Task<bool> DetectCardAsync();

    /// <summary>
    /// 读卡器连接状态变化事件
    /// </summary>
    event EventHandler<CardReaderConnectionEventArgs>? ConnectionStateChanged;

    /// <summary>
    /// 检测到卡片事件（用于自动读卡模式）
    /// </summary>
    event EventHandler<CardDetectedEventArgs>? CardDetected;
}

/// <summary>
/// 读卡器连接状态变化事件参数
/// </summary>
public class CardReaderConnectionEventArgs : EventArgs
{
    /// <summary>是否已连接</summary>
    public bool IsConnected { get; init; }

    /// <summary>错误信息（断开连接时）</summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// 检测到卡片事件参数
/// </summary>
public class CardDetectedEventArgs : EventArgs
{
    /// <summary>检测到卡片的时间</summary>
    public DateTime DetectedTime { get; init; } = DateTime.UtcNow;
}
