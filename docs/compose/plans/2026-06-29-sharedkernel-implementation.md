# SharedKernel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create `LYBT.SharedKernel` project containing domain event contracts, value object base, outbox pattern, and cross-module query contracts.

**Architecture:** SharedKernel is a pure class library with zero external dependencies (only `MediatR.Abstractions` for `INotification`). It contains only contracts and primitives — no implementations. All modules reference SharedKernel; SharedKernel references nothing.

**Tech Stack:** .NET 8, MediatR.Abstractions, C# 12, records, primary constructors

## Global Constraints

- .NET 8.0, LangVersion=latest, Nullable=enable, ImplicitUsings=enable
- No business logic in SharedKernel — only contracts and primitives
- All public types must have XML documentation
- Follow existing code style: PascalCase public, _camelCase private, I prefix interfaces
- Package versions managed in `Directory.Packages.props`

---

### Task 1: Create SharedKernel Project

**Covers:** [S4] SharedKernel structure

**Files:**
- Create: `src/Server/Core/LYBT.SharedKernel/LYBT.SharedKernel.csproj`
- Create: `src/Server/Core/LYBT.SharedKernel/GlobalUsings.cs`

**Interfaces:**
- Consumes: None (root project)
- Produces: Project reference target for all other projects

- [ ] **Step 1: Create project file**

```xml
<!-- src/Server/Core/LYBT.SharedKernel/LYBT.SharedKernel.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>LYBT.SharedKernel</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MediatR.Abstractions" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create GlobalUsings**

```csharp
// src/Server/Core/LYBT.SharedKernel/GlobalUsings.cs
global using System.ComponentModel;
```

- [ ] **Step 3: Add to solution**

Run: `dotnet sln LYBTZYZS.sln add src/Server/Core/LYBT.SharedKernel/LYBT.SharedKernel.csproj`

- [ ] **Step 4: Verify build**

Run: `dotnet build src/Server/Core/LYBT.SharedKernel/LYBT.SharedKernel.csproj`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add src/Server/Core/LYBT.SharedKernel/
git commit -m "feat(sharedkernel): create SharedKernel project scaffold"
```

---

### Task 2: ValueObject Base Record

**Covers:** [S4] Value objects

**Files:**
- Create: `src/Server/Core/LYBT.SharedKernel/Primitives/ValueObject.cs`
- Create: `src/Server/Core/LYBT.SharedKernel/Primitives/Entity.cs`
- Create: `src/Server/Core/LYBT.SharedKernel/Primitives/AggregateRoot.cs`

**Interfaces:**
- Consumes: None
- Produces: `ValueObject`, `Entity`, `AggregateRoot` base types for all modules

- [ ] **Step 1: Create ValueObject base**

```csharp
// src/Server/Core/LYBT.SharedKernel/Primitives/ValueObject.cs
namespace LYBT.SharedKernel.Primitives;

/// <summary>
/// 值对象基类。不可变，无身份标识，通过属性值相等性比较。
/// </summary>
public abstract record ValueObject
{
    /// <summary>
    /// 获取参与相等性比较的属性值。
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(1, (current, obj) =>
                current * 23 + (obj?.GetHashCode() ?? 0));
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
        => Equals(left, right);

    public static bool operator !=(ValueObject? left, ValueObject? right)
        => !Equals(left, right);
}
```

- [ ] **Step 2: Create Entity base**

```csharp
// src/Server/Core/LYBT.SharedKernel/Primitives/Entity.cs
namespace LYBT.SharedKernel.Primitives;

/// <summary>
/// 实体基类。具有唯一身份标识（Guid）。
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    /// <summary>
    /// 唯一标识
    /// </summary>
    public Guid Id { get; protected init; } = Guid.NewGuid();

    protected Entity() { }

    protected Entity(Guid id) => Id = id;

    public bool Equals(Entity? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id == other.Id;
    }

    public override bool Equals(object? obj) => Equals(obj as Entity);
    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);
    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}
```

- [ ] **Step 3: Create AggregateRoot marker**

```csharp
// src/Server/Core/LYBT.SharedKernel/Primitives/AggregateRoot.cs
namespace LYBT.SharedKernel.Primitives;

/// <summary>
/// 聚合根标记接口。标记该类为DDD聚合根，只能通过聚合根访问内部实体。
/// </summary>
public interface IAggregateRoot { }
```

- [ ] **Step 4: Verify build**

Run: `dotnet build src/Server/Core/LYBT.SharedKernel/LYBT.SharedKernel.csproj`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add src/Server/Core/LYBT.SharedKernel/Primitives/
git commit -m "feat(sharedkernel): add ValueObject, Entity, IAggregateRoot base types"
```

---

### Task 3: Domain Event Contracts

**Covers:** [S4] Domain events

**Files:**
- Create: `src/Server/Core/LYBT.SharedKernel/Events/IDomainEvent.cs`
- Create: `src/Server/Core/LYBT.SharedKernel/Events/IDomainEventDispatcher.cs`

**Interfaces:**
- Consumes: MediatR.Abstractions (`INotification`)
- Produces: `IDomainEvent`, `IDomainEventDispatcher` for all modules

- [ ] **Step 1: Create IDomainEvent**

```csharp
// src/Server/Core/LYBT.SharedKernel/Events/IDomainEvent.cs
using MediatR;

namespace LYBT.SharedKernel.Events;

/// <summary>
/// 领域事件基接口。所有领域事件必须实现此接口。
/// 事件表示领域中发生的重要业务事件。
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// 事件唯一标识
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// 事件发生时间（UTC）
    /// </summary>
    DateTime OccurredOn { get; }
}
```

- [ ] **Step 2: Create IDomainEventDispatcher**

```csharp
// src/Server/Core/LYBT.SharedKernel/Events/IDomainEventDispatcher.cs
namespace LYBT.SharedKernel.Events;

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
```

- [ ] **Step 3: Verify build**

Run: `dotnet build src/Server/Core/LYBT.SharedKernel/LYBT.SharedKernel.csproj`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add src/Server/Core/LYBT.SharedKernel/Events/
git commit -m "feat(sharedkernel): add IDomainEvent and IDomainEventDispatcher contracts"
```

---

### Task 4: Outbox Pattern

**Covers:** [S6] Outbox pattern

**Files:**
- Create: `src/Server/Core/LYBT.SharedKernel/Outbox/OutboxMessage.cs`
- Create: `src/Server/Core/LYBT.SharedKernel/Outbox/IOutboxService.cs`

**Interfaces:**
- Consumes: None
- Produces: `OutboxMessage` entity, `IOutboxService` for reliable event delivery

- [ ] **Step 1: Create OutboxMessage**

```csharp
// src/Server/Core/LYBT.SharedKernel/Outbox/OutboxMessage.cs
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
```

- [ ] **Step 2: Create IOutboxService**

```csharp
// src/Server/Core/LYBT.SharedKernel/Outbox/IOutboxService.cs
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
```

- [ ] **Step 3: Verify build**

Run: `dotnet build src/Server/Core/LYBT.SharedKernel/LYBT.SharedKernel.csproj`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add src/Server/Core/LYBT.SharedKernel/Outbox/
git commit -m "feat(sharedkernel): add OutboxMessage and IOutboxService for reliable event delivery"
```

---

### Task 5: Cross-Module Query Contracts

**Covers:** [S5] Cross-module communication

**Files:**
- Create: `src/Server/Core/LYBT.SharedKernel/Contracts/IUserCrossModuleService.cs`
- Create: `src/Server/Core/LYBT.SharedKernel/Contracts/ICrossModuleAuthService.cs`
- Create: `src/Server/Core/LYBT.SharedKernel/Contracts/IPatientQueryService.cs`
- Create: `src/Server/Core/LYBT.SharedKernel/DTOs/UserBasicDto.cs`

**Interfaces:**
- Consumes: None
- Produces: Cross-module query contracts for inter-module data access

- [ ] **Step 1: Create UserBasicDto**

```csharp
// src/Server/Core/LYBT.SharedKernel/DTOs/UserBasicDto.cs
using LYBT.Shared.Models.Enums;

namespace LYBT.SharedKernel.DTOs;

/// <summary>
/// 跨模块用户基本信息DTO。供其他模块通过IUserCrossModuleService获取用户信息。
/// </summary>
public record UserBasicDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string RealName { get; init; } = string.Empty;

    /// <summary>
    /// 用户角色
    /// </summary>
    public UserRole Role { get; init; }

    /// <summary>
    /// 是否系统管理员
    /// </summary>
    public bool IsSysAdmin { get; init; }
}
```

- [ ] **Step 2: Create IUserCrossModuleService**

```csharp
// src/Server/Core/LYBT.SharedKernel/Contracts/IUserCrossModuleService.cs
using LYBT.SharedKernel.DTOs;

namespace LYBT.SharedKernel.Contracts;

/// <summary>
/// 跨模块用户查询服务接口。供Auth、MedicalCase等模块查询用户信息。
/// 实现位于LYBT.Module.Users。
/// </summary>
public interface IUserCrossModuleService
{
    /// <summary>
    /// 根据用户ID获取用户基本信息。
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户基本信息，不存在返回null</returns>
    Task<UserBasicDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据用户名获取用户基本信息。
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户基本信息，不存在返回null</returns>
    Task<UserBasicDto?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证用户凭证（用户名+密码）。
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="password">密码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证成功返回用户ID，失败返回null</returns>
    Task<Guid?> ValidateCredentialsAsync(string userName, string password, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 3: Create ICrossModuleAuthService**

```csharp
// src/Server/Core/LYBT.SharedKernel/Contracts/ICrossModuleAuthService.cs
namespace LYBT.SharedKernel.Contracts;

/// <summary>
/// 跨模块认证服务接口。供其他模块验证token和获取当前用户信息。
/// 实现位于LYBT.Module.Auth。
/// </summary>
public interface ICrossModuleAuthService
{
    /// <summary>
    /// 验证JWT token有效性。
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>有效返回true</returns>
    bool ValidateToken(string token);

    /// <summary>
    /// 从token中提取用户ID。
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>用户ID，无效token返回null</returns>
    Guid? GetUserIdFromToken(string token);
}
```

- [ ] **Step 4: Create IPatientQueryService**

```csharp
// src/Server/Core/LYBT.SharedKernel/Contracts/IPatientQueryService.cs
namespace LYBT.SharedKernel.Contracts;

/// <summary>
/// 跨模块患者查询服务接口。供MedicalCase、Registration等模块查询患者信息。
/// 实现位于LYBT.Module.Patients。
/// </summary>
public interface IPatientQueryService
{
    /// <summary>
    /// 根据ID获取患者基本信息。
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>患者基本信息，不存在返回null</returns>
    Task<PatientBasicDto?> GetByIdAsync(Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取患者基本信息。
    /// </summary>
    /// <param name="patientIds">患者ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>患者基本信息列表</returns>
    Task<IReadOnlyList<PatientBasicDto>> GetByIdsAsync(IEnumerable<Guid> patientIds, CancellationToken cancellationToken = default);
}

/// <summary>
/// 跨模块患者基本信息DTO。
/// </summary>
public record PatientBasicDto
{
    /// <summary>
    /// 患者ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// 患者姓名
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 手机号码
    /// </summary>
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// 身份证号
    /// </summary>
    public string? IdNumber { get; init; }
}
```

- [ ] **Step 5: Verify build**

Run: `dotnet build src/Server/Core/LYBT.SharedKernel/LYBT.SharedKernel.csproj`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add src/Server/Core/LYBT.SharedKernel/Contracts/ src/Server/Core/LYBT.SharedKernel/DTOs/
git commit -m "feat(sharedkernel): add cross-module query contracts and DTOs"
```

---

### Task 6: Add Project References

**Covers:** [S4] SharedKernel integration

**Files:**
- Modify: `src/Server/Core/LYBT.Infrastructure/LYBT.Infrastructure.csproj`
- Modify: All Server Module .csproj files

**Interfaces:**
- Consumes: All SharedKernel types from Tasks 1-5
- Produces: All modules can reference SharedKernel types

- [ ] **Step 1: Add SharedKernel reference to Infrastructure**

```xml
<!-- src/Server/Core/LYBT.Infrastructure/LYBT.Infrastructure.csproj -->
<ItemGroup>
  <ProjectReference Include="..\LYBT.SharedKernel\LYBT.SharedKernel.csproj" />
</ItemGroup>
```

- [ ] **Step 2: Add SharedKernel reference to each Server Module**

For each of: `LYBT.Module.Auth`, `LYBT.Module.Users`, `LYBT.Module.Patients`, `LYBT.Module.Herbs`, `LYBT.Module.Formula`, `LYBT.Module.MedicalCase`, `LYBT.Module.Registration`, `LYBT.Module.Reports`:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\Core\LYBT.SharedKernel\LYBT.SharedKernel.csproj" />
</ItemGroup>
```

- [ ] **Step 3: Add SharedKernel reference to WebAPI**

```xml
<!-- src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj -->
<ItemGroup>
  <ProjectReference Include="..\..\Core\LYBT.SharedKernel\LYBT.SharedKernel.csproj" />
</ItemGroup>
```

- [ ] **Step 4: Verify full solution build**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add src/Server/
git commit -m "feat(sharedkernel): add project references from all server projects"
```

---

### Task 7: Verify Architecture Tests Pass

**Covers:** [S4] Module isolation

**Files:**
- None (existing architecture tests should still pass)

**Interfaces:**
- Consumes: All SharedKernel types
- Produces: Architecture test validation

- [ ] **Step 1: Run architecture tests**

Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: All tests pass — SharedKernel has no downward dependencies

- [ ] **Step 2: Run server integration tests**

Run: `dotnet test tests/LYBT.Tests.Server/`
Expected: All tests pass — no behavioral changes

- [ ] **Step 3: Final commit (if any fixes needed)**

```bash
git add -A
git commit -m "fix(sharedkernel): address architecture test findings"
```
