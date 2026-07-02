namespace LYBT.SharedKernel.Outbox;

/// <summary>
/// Outbox服务接口。负责存储和处理outbox消息。
/// </summary>
public interface IOutboxService
{
    /// <summary>
    /// 保存outbox消息（在同一事务中）。
    /// </summary>
    /// <param name="message">outbox消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取待处理的outbox消息。
    /// </summary>
    /// <param name="batchSize">批次大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>待处理消息列表</returns>
    Task<IReadOnlyList<OutboxMessage>> GetPendingMessagesAsync(int batchSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记消息为已处理。
    /// </summary>
    /// <param name="messageId">消息ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记消息处理失败。
    /// </summary>
    /// <param name="messageId">消息ID</param>
    /// <param name="error">错误信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
}


