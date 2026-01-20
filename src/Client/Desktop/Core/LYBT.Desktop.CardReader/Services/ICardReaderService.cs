using LYBT.Desktop.CardReader.Abstractions;
using LYBT.Desktop.CardReader.Models;

namespace LYBT.Desktop.CardReader.Services;

/// <summary>
/// 读卡器服务接口
/// 提供高层业务逻辑封装，管理读卡器生命周期
/// </summary>
public interface ICardReaderService : IDisposable
{
    /// <summary>
    /// 当前读卡器
    /// </summary>
    ICardReader? CurrentReader { get; }

    /// <summary>
    /// 是否已连接读卡器
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 是否启用自动读卡模式
    /// </summary>
    bool IsAutoReadEnabled { get; }

    /// <summary>
    /// 初始化读卡器（自动检测或指定类型）
    /// </summary>
    /// <param name="readerType">读卡器类型（Auto=自动检测）</param>
    /// <returns>是否成功</returns>
    Task<bool> InitializeAsync(CardReaderType readerType = CardReaderType.Auto);

    /// <summary>
    /// 断开读卡器连接
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// 读取身份证
    /// </summary>
    /// <param name="savePhoto">是否保存照片</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>读取结果</returns>
    Task<CardReadResult> ReadCardAsync(bool savePhoto = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启动自动读卡模式
    /// </summary>
    /// <param name="intervalMs">检测间隔（毫秒）</param>
    void StartAutoRead(int intervalMs = 500);

    /// <summary>
    /// 停止自动读卡模式
    /// </summary>
    void StopAutoRead();

    /// <summary>
    /// 读卡器连接状态变化事件
    /// </summary>
    event EventHandler<CardReaderConnectionEventArgs>? ConnectionStateChanged;

    /// <summary>
    /// 自动读卡成功事件
    /// </summary>
    event EventHandler<CardReadResult>? CardReadCompleted;

    /// <summary>
    /// 读卡错误事件
    /// </summary>
    event EventHandler<CardReadErrorEventArgs>? CardReadError;
}

/// <summary>
/// 读卡错误事件参数
/// </summary>
public class CardReadErrorEventArgs : EventArgs
{
    /// <summary>错误码</summary>
    public int ErrorCode { get; init; }

    /// <summary>错误信息</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>异常（如有）</summary>
    public Exception? Exception { get; init; }
}
