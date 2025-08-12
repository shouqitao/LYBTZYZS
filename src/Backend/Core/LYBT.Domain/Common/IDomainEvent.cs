using System;
using MediatR;

namespace LYBT.Domain.Common
{
    /// <summary>
    /// 领域事件接口 - UltraThink重构DDD架构
    /// 定义领域事件的基本契约
    /// </summary>
    public interface IDomainEvent : INotification
    {
        /// <summary>
        /// 事件发生时间
        /// </summary>
        DateTime OccurredOn { get; }

        /// <summary>
        /// 事件ID（用于幂等性和追踪）
        /// </summary>
        Guid EventId { get; }

        /// <summary>
        /// 事件版本（用于事件演化）
        /// </summary>
        int Version { get; }
    }

    /// <summary>
    /// 领域事件基类
    /// </summary>
    public abstract record DomainEvent : IDomainEvent
    {
        /// <summary>
        /// 事件发生时间
        /// </summary>
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// 事件ID
        /// </summary>
        public Guid EventId { get; init; } = Guid.NewGuid();

        /// <summary>
        /// 事件版本
        /// </summary>
        public virtual int Version { get; init; } = 1;
    }

    /// <summary>
    /// 领域事件处理器接口
    /// </summary>
    /// <typeparam name="TDomainEvent">领域事件类型</typeparam>
    public interface IDomainEventHandler<in TDomainEvent> : INotificationHandler<TDomainEvent>
        where TDomainEvent : IDomainEvent
    {
    }
}