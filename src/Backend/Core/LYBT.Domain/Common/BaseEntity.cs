using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Domain.Common
{
    /// <summary>
    /// 实体基类 - UltraThink重构DDD架构
    /// 提供领域实体的基础设施和领域事件支持
    /// </summary>
    public abstract class BaseEntity<TId>
    {
        /// <summary>
        /// 实体标识符
        /// </summary>
        public TId Id { get; protected set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; protected set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; protected set; }

        /// <summary>
        /// 创建用户ID
        /// </summary>
        public Guid? CreatedBy { get; protected set; }

        /// <summary>
        /// 更新用户ID
        /// </summary>
        public Guid? UpdatedBy { get; protected set; }

        /// <summary>
        /// 领域事件集合
        /// </summary>
        [NotMapped]
        private readonly List<IDomainEvent> _domainEvents = new();

        protected BaseEntity()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        protected BaseEntity(TId id) : this()
        {
            Id = id;
        }

        /// <summary>
        /// 获取领域事件（只读）
        /// </summary>
        [NotMapped]
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        /// <summary>
        /// 添加领域事件
        /// </summary>
        /// <param name="domainEvent">领域事件</param>
        protected void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        /// <summary>
        /// 移除领域事件
        /// </summary>
        /// <param name="domainEvent">领域事件</param>
        protected void RemoveDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Remove(domainEvent);
        }

        /// <summary>
        /// 清空领域事件
        /// </summary>
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

        /// <summary>
        /// 更新实体的修改时间和修改人
        /// </summary>
        /// <param name="updatedBy">修改人ID</param>
        public virtual void MarkAsUpdated(Guid? updatedBy = null)
        {
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }

        /// <summary>
        /// 设置创建信息（仅在创建时调用一次）
        /// </summary>
        /// <param name="createdBy">创建人ID</param>
        public virtual void SetCreationInfo(Guid? createdBy)
        {
            CreatedBy = createdBy;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Guid类型ID的实体基类
    /// </summary>
    public abstract class BaseEntity : BaseEntity<Guid>
    {
        protected BaseEntity() : base(Guid.NewGuid())
        {
        }

        protected BaseEntity(Guid id) : base(id)
        {
        }
    }

    /// <summary>
    /// 聚合根标记接口
    /// </summary>
    public interface IAggregateRoot
    {
        IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
        void ClearDomainEvents();
    }

    /// <summary>
    /// 聚合根基类
    /// </summary>
    /// <typeparam name="TId">标识符类型</typeparam>
    public abstract class AggregateRoot<TId> : BaseEntity<TId>, IAggregateRoot
    {
        protected AggregateRoot() : base()
        {
        }

        protected AggregateRoot(TId id) : base(id)
        {
        }
    }

    /// <summary>
    /// Guid类型的聚合根基类
    /// </summary>
    public abstract class AggregateRoot : AggregateRoot<Guid>
    {
        protected AggregateRoot() : base()
        {
        }

        protected AggregateRoot(Guid id) : base(id)
        {
        }
    }
}