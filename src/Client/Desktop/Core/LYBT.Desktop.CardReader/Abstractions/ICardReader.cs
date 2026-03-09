using System.IO;
using LYBT.Desktop.CardReader.Models;

namespace LYBT.Desktop.CardReader.Abstractions;

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
    public DateTime DetectedTime { get; init; } = DateTime.Now;
}

/// <summary>
/// 读卡器配置选项
/// PRD-13: 支持从 appsettings.json 的 "CardReader" 节点读取配置
/// </summary>
public class CardReaderOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "CardReader";

    /// <summary>
    /// 连接超时时间（毫秒）
    /// </summary>
    public int ConnectTimeout { get; set; } = 5000;

    /// <summary>
    /// 读卡超时时间（毫秒）
    /// </summary>
    public int ReadTimeout { get; set; } = 10000;

    /// <summary>
    /// 自动重连间隔（毫秒）
    /// </summary>
    public int ReconnectInterval { get; set; } = 3000;

    /// <summary>
    /// 是否启用自动重连
    /// </summary>
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    /// 照片保存目录
    /// </summary>
    public string PhotoSaveDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "LYBT_CardPhotos");

    /// <summary>
    /// USB端口（华大读卡器默认1001）
    /// </summary>
    public int UsbPort { get; set; } = 1001;

    /// <summary>
    /// 串口端口号（1-16，0表示不使用串口）
    /// </summary>
    public int SerialPort { get; set; } = 0;
}
