using LYBT.EventBus.Abstractions;

namespace LYBT.EventBus.Events;

/// <summary>
/// 集成事件基础类
/// 为所有集成事件提供通用实现
/// </summary>
public abstract class IntegrationEventBase : IIntegrationEvent
{
    /// <summary>
    /// 构造函数
    /// </summary>
    protected IntegrationEventBase()
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
        EventType = GetType().Name;
        Version = 1;
    }

    /// <summary>
    /// 构造函数（指定来源模块）
    /// </summary>
    /// <param name="source">事件来源模块</param>
    protected IntegrationEventBase(string source) : this()
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    /// <inheritdoc />
    public Guid Id { get; private set; }

    /// <inheritdoc />
    public DateTime OccurredOn { get; private set; }

    /// <inheritdoc />
    public string EventType { get; private set; }

    /// <inheritdoc />
    public string Source { get; private set; } = "Unknown";

    /// <inheritdoc />
    public virtual int Version { get; protected set; }

    /// <summary>
    /// 重置事件ID和时间（用于测试）
    /// </summary>
    internal void ResetForTesting()
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
    }

    /// <summary>
    /// 获取事件描述
    /// </summary>
    /// <returns>事件描述字符串</returns>
    public virtual string GetDescription()
    {
        return $"{EventType} from {Source} at {OccurredOn:yyyy-MM-dd HH:mm:ss}";
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return $"[{EventType}] {Id} ({Source}) - {OccurredOn:yyyy-MM-dd HH:mm:ss}";
    }

    /// <summary>
    /// 判断相等性
    /// </summary>
    /// <param name="obj">比较对象</param>
    /// <returns>是否相等</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not IntegrationEventBase other)
            return false;

        return Id == other.Id;
    }

    /// <summary>
    /// 获取哈希码
    /// </summary>
    /// <returns>哈希码</returns>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
