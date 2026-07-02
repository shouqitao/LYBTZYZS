namespace LYBT.SharedKernel.Outbox;

/// <summary>
/// Outbox消息实体。用于保证领域事件的可靠投递。
/// 在同一事务中保存实体变更和outbox消息，后台worker异步处理。
/// </summary>
public class OutboxMessage
{
    /// <summary>
    /// 消息唯一标识
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 事件类型全名
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// 事件序列化后的JSON payload
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间（UTC）
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 处理时间（UTC），null表示未处理
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// 处理错误信息，null表示成功
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; set; } = 0;
}


