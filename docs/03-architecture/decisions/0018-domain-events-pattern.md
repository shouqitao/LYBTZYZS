# ADR-0018: Domain Events Pattern

**状态**: Accepted
**日期**: 2026-06-29
**来源**: 模块化单体架构的跨模块通信需求

## 背景

在模块化单体架构中，模块间不能直接引用。当一个模块的状态变更需要通知其他模块时（如患者创建后触发统计更新、药材价格变更后通知验方重算），需要一种松耦合的通信机制。

传统做法是通过同步方法调用（`ICrossModuleService`），但这要求：
1. 消费方模块引用提供方模块的接口
2. 同步执行，一个模块的失败会阻塞另一个
3. 隐式依赖关系，难以追踪

领域事件提供了一种异步、松耦合的替代方案。

## 决策

采用 **MediatR 领域事件 + Outbox 模式**实现跨模块通信。

### 领域事件定义

所有领域事件实现 `IDomainEvent : INotification`（MediatR）：

```csharp
// SharedKernel/Events/IDomainEvent.cs
public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
```

每个模块在 `Domain/Events/` 下定义自己的事件：

```csharp
// Module.Patients/Domain/Events/PatientCreatedEvent.cs
public sealed record PatientCreatedEvent(
    Guid PatientId,
    string Name,
    Gender Gender,
    Guid? CreatedBy
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
```

### 事件发布

事件在以下时机发布：
1. **实体方法内**：领域实体的状态变更方法 raise event（如 `Patient.SoftDelete()` → `PatientDeletedEvent`）
2. **CommandHandler 内**：业务操作完成后 raise event（如 `CreatePatientCommandHandler` → `PatientCreatedEvent`）
3. **事务提交后**：通过 Outbox 模式，同一事务写入 `OutboxMessage`，后台 worker 异步处理

### Outbox 模式

```csharp
// SharedKernel/Outbox/IOutboxService.cs
public interface IOutboxService
{
    Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboxMessage>> GetPendingMessagesAsync(int batchSize = 20, ...);
    Task MarkAsProcessedAsync(Guid messageId, ...);
    Task MarkAsFailedAsync(Guid messageId, string error, ...);
}
```

流程：
1. CommandHandler 在同一事务中写入实体变更 + `OutboxMessage`
2. 事务提交后，后台 worker 读取待处理消息
3. Worker 通过 `IDomainEventDispatcher` 分发事件
4. 各模块的 `INotificationHandler<TEvent>` 异步处理

### 跨模块事件处理

```csharp
// 另一个模块订阅 PatientCreatedEvent
public class PatientCreatedEventHandler : INotificationHandler<PatientCreatedEvent>
{
    public async Task Handle(PatientCreatedEvent notification, CancellationToken ct)
    {
        // 异步处理：如更新统计、初始化默认数据等
    }
}
```

### 事件分发器

```csharp
// SharedKernel/Events/IDomainEventDispatcher.cs
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);
}
```

## 理由

- **松耦合**：事件发布方不需要知道谁在监听，模块间无直接引用
- **可靠性**：Outbox 模式保证事件在事务提交后不丢失
- **可追溯**：`OutboxMessage` 记录事件类型、payload、处理状态、重试次数
- **可测试**：事件处理器可独立测试，Mock 事件即可验证下游逻辑
- **渐进迁移**：可逐步将同步 `ICrossModuleService` 调用替换为领域事件

## 后果

### 优势
- 模块间完全解耦，编译时无直接依赖
- 事件可被多个模块订阅，一对多通信
- Outbox 保证事件可靠投递，支持重试
- 事件历史可用于审计和调试

### 权衡
- 最终一致性：事件处理是异步的，下游模块可能有短暂延迟
- 调试复杂度：事件链跨越多个模块，需通过 EventId 追踪
- Outbox 增加了数据库写入量（每条事件一条 OutboxMessage）
- 事件版本管理：事件 schema 变更需考虑向后兼容

## 关联

- [ADR-0017: Modular Monolith with CQRS](0017-modular-monolith-cqrs.md) — 模块化单体架构的基础
- [SharedKernel Events](../../src/Server/Core/LYBT.SharedKernel/Events/) — `IDomainEvent`, `IDomainEventDispatcher`
- [SharedKernel Outbox](../../src/Server/Core/LYBT.SharedKernel/Outbox/) — `IOutboxService`, `OutboxMessage`
- [MedicalCase 聚合根](0001-medicalcase-aggregate-root.md) — 首个使用领域事件的模块

## 变更记录

| 日期 | 变更 | 原因 |
|------|------|------|
| 2026-06-29 | 新建 ADR-0018，记录领域事件模式决策 | 模块化单体架构迁移 |

## 关联 US

- 全部 141 个 US 中涉及跨模块通信的场景均受此模式约束
