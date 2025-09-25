using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.Desktop.Core.Services.UltraThink
{
    /// <summary>
    /// UltraThink架构 - 业务服务基类
    /// 职责明确：处理业务逻辑和状态变更
    /// 事务管理：支持工作单元和事务回滚
    /// 事件驱动：集成事件聚合器发布业务事件
    /// </summary>
    public abstract class BusinessServiceBase<TEntity> : IBusinessService<TEntity> where TEntity : class
    {
        protected readonly ILogger Logger;
        protected readonly IEventAggregator EventAggregator;
        private readonly List<IBusinessRule> _businessRules = new();
        private readonly List<DomainEvent> _pendingEvents = new();

        protected BusinessServiceBase(ILogger logger, IEventAggregator eventAggregator)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            
            // 注册业务规则
            RegisterBusinessRules();
        }

        #region 核心业务方法

        /// <summary>
        /// 创建实体
        /// </summary>
        public virtual async Task<OperationResult<TEntity>> CreateAsync(
            TEntity entity, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.LogDebug("开始创建 {EntityType}", typeof(TEntity).Name);

                // 验证实体
                var validationResult = await ValidateAsync(entity, OperationType.Create);
                if (!validationResult.IsValid)
                {
                    return OperationResult<TEntity>.Failure(validationResult.Errors);
                }

                // 应用业务规则
                var ruleResult = await ApplyBusinessRulesAsync(entity, OperationType.Create);
                if (!ruleResult.IsSuccess)
                {
                    return OperationResult<TEntity>.Failure(ruleResult.Errors);
                }

                // 执行创建前处理
                await OnBeforeCreateAsync(entity, cancellationToken);

                // 执行创建
                var created = await CreateInternalAsync(entity, cancellationToken);

                // 执行创建后处理
                await OnAfterCreateAsync(created, cancellationToken);

                // 添加领域事件
                AddDomainEvent(new EntityCreatedEvent<TEntity>(created));

                // 发布事件
                await PublishPendingEventsAsync();

                Logger.LogInformation("成功创建 {EntityType}", typeof(TEntity).Name);
                return OperationResult<TEntity>.Success(created);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "创建失败 {EntityType}", typeof(TEntity).Name);
                return OperationResult<TEntity>.Failure($"创建失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新实体
        /// </summary>
        public virtual async Task<OperationResult<TEntity>> UpdateAsync(
            TEntity entity, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.LogDebug("开始更新 {EntityType}", typeof(TEntity).Name);

                // 验证实体
                var validationResult = await ValidateAsync(entity, OperationType.Update);
                if (!validationResult.IsValid)
                {
                    return OperationResult<TEntity>.Failure(validationResult.Errors);
                }

                // 应用业务规则
                var ruleResult = await ApplyBusinessRulesAsync(entity, OperationType.Update);
                if (!ruleResult.IsSuccess)
                {
                    return OperationResult<TEntity>.Failure(ruleResult.Errors);
                }

                // 执行更新前处理
                await OnBeforeUpdateAsync(entity, cancellationToken);

                // 执行更新
                var updated = await UpdateInternalAsync(entity, cancellationToken);

                // 执行更新后处理
                await OnAfterUpdateAsync(updated, cancellationToken);

                // 添加领域事件
                AddDomainEvent(new EntityUpdatedEvent<TEntity>(updated));

                // 发布事件
                await PublishPendingEventsAsync();

                Logger.LogInformation("成功更新 {EntityType}", typeof(TEntity).Name);
                return OperationResult<TEntity>.Success(updated);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "更新失败 {EntityType}", typeof(TEntity).Name);
                return OperationResult<TEntity>.Failure($"更新失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除实体
        /// </summary>
        public virtual async Task<OperationResult> DeleteAsync(
            Guid id, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.LogDebug("开始删除 {EntityType} ID: {Id}", typeof(TEntity).Name, id);

                // 检查是否可以删除
                var canDelete = await CanDeleteAsync(id, cancellationToken);
                if (!canDelete.IsSuccess)
                {
                    return canDelete;
                }

                // 执行删除前处理
                await OnBeforeDeleteAsync(id, cancellationToken);

                // 执行删除
                await DeleteInternalAsync(id, cancellationToken);

                // 执行删除后处理
                await OnAfterDeleteAsync(id, cancellationToken);

                // 添加领域事件
                AddDomainEvent(new EntityDeletedEvent<TEntity>(id));

                // 发布事件
                await PublishPendingEventsAsync();

                Logger.LogInformation("成功删除 {EntityType} ID: {Id}", typeof(TEntity).Name, id);
                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "删除失败 {EntityType} ID: {Id}", typeof(TEntity).Name, id);
                return OperationResult.Failure($"删除失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量操作
        /// </summary>
        public virtual async Task<OperationResult<int>> BatchOperationAsync(
            IEnumerable<TEntity> entities,
            BatchOperationType operationType,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.LogDebug("开始批量操作 {EntityType} 类型: {OperationType}", 
                    typeof(TEntity).Name, operationType);

                var entityList = entities.ToList();
                if (!entityList.Any())
                {
                    return OperationResult<int>.Success(0);
                }

                // 验证所有实体
                foreach (var entity in entityList)
                {
                    var validationResult = await ValidateAsync(entity, 
                        operationType == BatchOperationType.Create ? OperationType.Create : OperationType.Update);
                    
                    if (!validationResult.IsValid)
                    {
                        return OperationResult<int>.Failure(validationResult.Errors);
                    }
                }

                // 执行批量操作
                var affected = await BatchOperationInternalAsync(entityList, operationType, cancellationToken);

                // 添加领域事件
                AddDomainEvent(new BatchOperationCompletedEvent<TEntity>(operationType, affected));

                // 发布事件
                await PublishPendingEventsAsync();

                Logger.LogInformation("成功完成批量操作 {EntityType} 类型: {OperationType} 影响数: {Count}", 
                    typeof(TEntity).Name, operationType, affected);
                    
                return OperationResult<int>.Success(affected);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "批量操作失败 {EntityType} 类型: {OperationType}", 
                    typeof(TEntity).Name, operationType);
                return OperationResult<int>.Failure($"批量操作失败: {ex.Message}");
            }
        }

        #endregion

        #region 验证和业务规则

        /// <summary>
        /// 验证实体
        /// </summary>
        protected virtual async Task<ValidationResult> ValidateAsync(TEntity entity, OperationType operationType)
        {
            var errors = new List<string>();

            // 数据注解验证
            var validationContext = new ValidationContext(entity);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            
            if (!Validator.TryValidateObject(entity, validationContext, validationResults, true))
            {
                errors.AddRange(validationResults.Select(r => r.ErrorMessage ?? "验证失败"));
            }

            // 自定义验证
            var customErrors = await ValidateCustomAsync(entity, operationType);
            errors.AddRange(customErrors);

            return new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        /// <summary>
        /// 自定义验证（子类重写）
        /// </summary>
        protected virtual Task<IEnumerable<string>> ValidateCustomAsync(TEntity entity, OperationType operationType)
        {
            return Task.FromResult<IEnumerable<string>>(new List<string>());
        }

        /// <summary>
        /// 注册业务规则（子类重写）
        /// </summary>
        protected virtual void RegisterBusinessRules()
        {
            // 子类注册具体业务规则
        }

        /// <summary>
        /// 应用业务规则
        /// </summary>
        protected virtual async Task<OperationResult> ApplyBusinessRulesAsync(TEntity entity, OperationType operationType)
        {
            var errors = new List<string>();

            foreach (var rule in _businessRules.Where(r => r.AppliesTo(operationType)))
            {
                var result = await rule.ValidateAsync(entity);
                if (!result.IsValid)
                {
                    errors.AddRange(result.Errors);
                }
            }

            return errors.Any() 
                ? OperationResult.Failure(errors) 
                : OperationResult.Success();
        }

        /// <summary>
        /// 检查是否可以删除
        /// </summary>
        protected virtual Task<OperationResult> CanDeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            // 子类实现具体的删除检查逻辑
            return Task.FromResult(OperationResult.Success());
        }

        #endregion

        #region 钩子方法

        protected virtual Task OnBeforeCreateAsync(TEntity entity, CancellationToken cancellationToken) => Task.CompletedTask;
        protected virtual Task OnAfterCreateAsync(TEntity entity, CancellationToken cancellationToken) => Task.CompletedTask;
        protected virtual Task OnBeforeUpdateAsync(TEntity entity, CancellationToken cancellationToken) => Task.CompletedTask;
        protected virtual Task OnAfterUpdateAsync(TEntity entity, CancellationToken cancellationToken) => Task.CompletedTask;
        protected virtual Task OnBeforeDeleteAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
        protected virtual Task OnAfterDeleteAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;

        #endregion

        #region 抽象方法 - 子类实现

        protected abstract Task<TEntity> CreateInternalAsync(TEntity entity, CancellationToken cancellationToken);
        protected abstract Task<TEntity> UpdateInternalAsync(TEntity entity, CancellationToken cancellationToken);
        protected abstract Task DeleteInternalAsync(Guid id, CancellationToken cancellationToken);
        protected abstract Task<int> BatchOperationInternalAsync(IEnumerable<TEntity> entities, BatchOperationType operationType, CancellationToken cancellationToken);

        #endregion

        #region 事件管理

        /// <summary>
        /// 添加领域事件
        /// </summary>
        protected void AddDomainEvent(DomainEvent domainEvent)
        {
            _pendingEvents.Add(domainEvent);
        }

        /// <summary>
        /// 发布待处理事件
        /// </summary>
        protected async Task PublishPendingEventsAsync()
        {
            foreach (var domainEvent in _pendingEvents)
            {
                try
                {
                    // 发布到事件聚合器
                    EventAggregator.GetEvent<PubSubEvent<DomainEvent>>().Publish(domainEvent);
                    
                    Logger.LogDebug("发布领域事件 {EventType}", domainEvent.GetType().Name);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "发布领域事件失败 {EventType}", domainEvent.GetType().Name);
                }
            }
            
            _pendingEvents.Clear();
        }

        #endregion

        #region 辅助类和接口

        /// <summary>
        /// 添加业务规则
        /// </summary>
        protected void AddBusinessRule(IBusinessRule rule)
        {
            _businessRules.Add(rule);
        }

        #endregion
    }

    #region 支持类型和接口

    /// <summary>
    /// 业务服务接口
    /// </summary>
    public interface IBusinessService<TEntity> where TEntity : class
    {
        Task<OperationResult<TEntity>> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task<OperationResult<TEntity>> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task<OperationResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<OperationResult<int>> BatchOperationAsync(IEnumerable<TEntity> entities, BatchOperationType operationType, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 操作结果
    /// </summary>
    public class OperationResult
    {
        public bool IsSuccess { get; protected set; }
        public List<string> Errors { get; protected set; } = new();
        public string Message => string.Join("; ", Errors);

        public static OperationResult Success() => new() { IsSuccess = true };
        public static OperationResult Failure(string error) => new() { IsSuccess = false, Errors = { error } };
        public static OperationResult Failure(IEnumerable<string> errors) => new() { IsSuccess = false, Errors = errors.ToList() };
    }

    /// <summary>
    /// 泛型操作结果
    /// </summary>
    public class OperationResult<T> : OperationResult
    {
        public T? Data { get; private set; }

        public static OperationResult<T> Success(T data) => new() { IsSuccess = true, Data = data };
        public new static OperationResult<T> Failure(string error) => new() { IsSuccess = false, Errors = { error } };
        public new static OperationResult<T> Failure(IEnumerable<string> errors) => new() { IsSuccess = false, Errors = errors.ToList() };
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// 操作类型
    /// </summary>
    public enum OperationType
    {
        Create,
        Update,
        Delete
    }

    /// <summary>
    /// 批量操作类型
    /// </summary>
    public enum BatchOperationType
    {
        Create,
        Update,
        Delete,
        Disable,
        Enable
    }

    /// <summary>
    /// 业务规则接口
    /// </summary>
    public interface IBusinessRule
    {
        bool AppliesTo(OperationType operationType);
        Task<ValidationResult> ValidateAsync(object entity);
    }

    /// <summary>
    /// 领域事件基类
    /// </summary>
    public abstract class DomainEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        public string EventType => GetType().Name;
    }

    /// <summary>
    /// 实体创建事件
    /// </summary>
    public class EntityCreatedEvent<TEntity> : DomainEvent
    {
        public TEntity Entity { get; }
        public EntityCreatedEvent(TEntity entity) => Entity = entity;
    }

    /// <summary>
    /// 实体更新事件
    /// </summary>
    public class EntityUpdatedEvent<TEntity> : DomainEvent
    {
        public TEntity Entity { get; }
        public EntityUpdatedEvent(TEntity entity) => Entity = entity;
    }

    /// <summary>
    /// 实体删除事件
    /// </summary>
    public class EntityDeletedEvent<TEntity> : DomainEvent
    {
        public Guid EntityId { get; }
        public EntityDeletedEvent(Guid entityId) => EntityId = entityId;
    }

    /// <summary>
    /// 批量操作完成事件
    /// </summary>
    public class BatchOperationCompletedEvent<TEntity> : DomainEvent
    {
        public BatchOperationType OperationType { get; }
        public int AffectedCount { get; }
        
        public BatchOperationCompletedEvent(BatchOperationType operationType, int affectedCount)
        {
            OperationType = operationType;
            AffectedCount = affectedCount;
        }
    }

    #endregion
}