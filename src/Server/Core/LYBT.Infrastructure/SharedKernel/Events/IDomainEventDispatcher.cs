namespace LYBT.Infrastructure.SharedKernel.Events;

/// <summary>
/// 领域事件分发器接口。负责将领域事件分发给所有注册的处理器。
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// 分发一组领域事件。
    /// </summary>
    /// <param name="events">要分发的事件集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);
}


