# ADR-0017: Modular Monolith with MediatR CQRS

**状态**: Accepted
**日期**: 2026-06-29
**来源**: 从三层架构迁移到模块化单体架构

## 背景

系统最初采用经典的三层架构（Controller → Service → Repository → DbContext），所有业务逻辑集中在共享 Service 层，模块间通过直接方法调用通信。随着模块数量增长（Auth、Users、Patients、Herbs、Formula、MedicalCase、Registration、Reports），出现以下问题：

1. **模块边界模糊**：Service 层可直接访问任意实体的 DbContext，缺乏强制隔离
2. **跨模块耦合**：模块间直接引用，形成隐式依赖图
3. **CQRS 缺失**：读写混合在同一 Service 中，MedicalCase 是唯一使用 CQRS 的模块
4. **数据隔离不足**：共享单一 `AppDbContext`，所有模块共用同一数据库上下文

MedicalCase 模块率先采用 CQRS（CommandHandler）模式，验证了命令/查询分离的可行性。其他模块逐步跟进，形成模块化单体架构。

## 决策

采用 **MediatR CQRS 模块化单体架构**，每个业务模块包含完整的 Domain / Application / Infrastructure 三层。

### 架构分层

```
Services(WebAPI)
  ├── Controllers (dispatch to MediatR)
  └── Modules/
        ├── Patients/
        │   ├── Domain/          # Patient (IAggregateRoot), Domain Events
        │   ├── Application/     # Commands/, Queries/, Validators/, Mappers/
        │   ├── Infrastructure/  # PatientsDbContext, PatientRepository
        │   ├── Interfaces/      # ICrossModuleService contracts
        │   └── PatientsModule.cs # DI registration
        ├── Herbs/
        │   ├── Domain/          # Herb (IAggregateRoot), Domain Events
        │   ├── Application/     # Commands/, Queries/, Validators/, Mappers/
        │   ├── Infrastructure/  # HerbsDbContext, HerbRepository
        │   └── HerbsModule.cs
        └── ... (other modules)
```

### 核心规则

| 规则 | 约束 | 说明 |
|------|------|------|
| 模块隔离 | 禁止模块间直接引用 | 跨模块通信通过 SharedKernel 的 `ICrossModuleService` 或领域事件 |
| 数据隔离 | 每个模块独立 DbContext | 每个模块注册自己的 `<Module>DbContext`，不共享 `AppDbContext` |
| CQRS | Command/Query 分离 | 写操作用 `IRequestHandler<TCommand, TResponse>`，读操作用 `IRequestHandler<TQuery, TResponse>` |
| 领域事件 | `IDomainEvent : INotification` | 状态变更通过 MediatR 领域事件传播，Outbox 保证可靠投递 |
| Repository 模式 | 每个模块独立 Repository | 模块内部 Repository 继承 `BaseRepository<T>`，不注入 `AppDbContext` |

### MediatR 注册

每个模块在 `<Name>Module.cs` 中注册 MediatR handlers：
```csharp
services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreatePatientCommand).Assembly));
```

### 领域事件

- 实体方法或 CommandHandler 中 raise domain event
- 跨模块通过 `INotificationHandler<TEvent>` 订阅
- Outbox 模式：同一事务写入 `OutboxMessage`，后台 worker 异步处理

## 理由

- **模块隔离**：编译时强制模块间无直接引用，架构测试验证
- **数据隔离**：每个模块独立 DbContext，避免跨模块数据泄露
- **CQRS 清晰**：读写分离使业务逻辑更清晰，MedicalCase 已验证此模式
- **领域事件**：松耦合的跨模块通信，支持未来异步处理
- **单体部署**：保持单体部署的简单性，同时具备模块化的代码组织
- **渐进迁移**：可逐模块迁移，无需一次性重写

## 后果

### 优势
- 模块边界清晰，编译时强制隔离
- 每个模块可独立开发、测试、维护
- CQRS 模式使读写逻辑分离，代码更易理解
- 领域事件支持松耦合的跨模块通信
- Outbox 模式保证事件可靠投递

### 权衡
- 每个模块需维护独立的 DbContext 和 Repository
- MediatR 增加了间接层，调试时需跟踪 handler 链
- 跨模块查询需通过 `ICrossModuleService` 接口，不能直接 JOIN
- 模块间数据一致性依赖领域事件，最终一致性模型

## 关联

- [ADR-0001: MedicalCase 聚合根](0001-medicalcase-aggregate-root.md) — MedicalCase 是首个采用 CQRS 的模块
- [SharedKernel Events](../../src/Server/Core/LYBT.SharedKernel/Events/) — `IDomainEvent`, `IDomainEventDispatcher`
- [SharedKernel Outbox](../../src/Server/Core/LYBT.SharedKernel/Outbox/) — `IOutboxService`, `OutboxMessage`

## 关联 US

- 全部 141 个 US 的后端实现均受此架构决策约束
