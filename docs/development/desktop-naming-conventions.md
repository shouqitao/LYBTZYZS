# Desktop Layer Naming Conventions

**OpenSpec**: standardize-service-naming
**Created**: 2025-12-30
**Status**: Active

---

## 1. Service Class Naming (服务类命名规范)

### 1.1 Service

**适用场景**: 业务逻辑服务

| 特征 | 说明 |
|------|------|
| 无状态 | 不维护实例状态，每次调用独立 |
| 业务操作 | 提供业务级别的操作方法 |
| 返回类型 | 统一使用 `Result<T>` 或 `Task<Result<T>>` |
| 依赖 | 注入 Repository 进行数据访问 |

**示例**:
```csharp
public class HerbService : IHerbService
{
    public async Task<Result<HerbDetailDto>> GetByIdAsync(Guid id);
    public async Task<Result<Guid>> CreateAsync(HerbCreateDto dto);
}
```

### 1.2 Handler

**适用场景**: HTTP处理器 / 事件处理器

| 特征 | 说明 |
|------|------|
| HTTP拦截 | 继承 `DelegatingHandler` |
| 事件订阅 | 实现 `IEventHandler<TEvent>` |
| 管道处理 | 请求/响应管道中的中间件 |

**示例**:
```csharp
// HTTP处理器
public class TokenRefreshHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(...);
}

// 事件处理器
public class PatientSelectedHandler : IEventHandler<PatientSelectedEvent>
{
    public void Handle(PatientSelectedEvent @event);
}
```

### 1.3 Manager

**适用场景**: 状态管理类

| 特征 | 说明 |
|------|------|
| 有状态 | 维护运行时状态 |
| 生命周期 | 管理资源或对象的生命周期 |
| 单例 | 通常注册为单例 |

**示例**:
```csharp
public class TokenManager : ITokenManager
{
    private string? _accessToken;
    public string? AccessToken => _accessToken;
    public void SetToken(string token);
    public void ClearToken();
}

public class LoadingStateManager : ILoadingStateManager
{
    public bool IsLoading { get; private set; }
    public void StartLoading();
    public void StopLoading();
}
```

### 1.4 Coordinator

**适用场景**: 复杂业务流程编排

| 特征 | 说明 |
|------|------|
| 多服务协调 | 组合多个 Service/Manager |
| 工作流 | 维护业务流程状态 |
| 复杂逻辑 | 处理跨领域的业务规则 |

**示例**:
```csharp
public class MedicalCaseWorkspaceCoordinator
{
    private readonly IMedicalCaseService _caseService;
    private readonly IPrescriptionService _prescriptionService;

    public async Task<Result<Guid>> SaveAsync(/* 多个参数 */);
}
```

### 1.5 Repository

**适用场景**: 数据访问抽象

| 特征 | 说明 |
|------|------|
| CRUD | 基础的增删改查操作 |
| API封装 | 封装HTTP API调用 |
| 返回类型 | 返回 DTO 或 Entity |

**示例**:
```csharp
public class HerbRepository : IHerbRepository
{
    public async Task<HerbDetailDto?> GetByIdAsync(Guid id);
    public async Task<PagedResult<HerbListDto>> GetPagedAsync(HerbQueryParams query);
}
```

---

## 2. Quick Reference (快速参考)

| 后缀 | 关键词 | 状态 | 典型方法 |
|------|--------|------|----------|
| **Service** | 业务、逻辑、操作 | 无状态 | `CreateAsync`, `UpdateAsync`, `ValidateAsync` |
| **Handler** | HTTP、事件、拦截 | 无状态 | `SendAsync`, `Handle`, `OnEvent` |
| **Manager** | 状态、生命周期、缓存 | **有状态** | `Get`, `Set`, `Start`, `Stop`, `Clear` |
| **Coordinator** | 编排、协调、工作流 | 有状态 | `ExecuteAsync`, `SaveAsync`, `ProcessAsync` |
| **Repository** | 数据、存储、查询 | 无状态 | `GetAsync`, `FindAsync`, `SaveAsync` |

---

## 3. Decision Flowchart (决策流程图)

```
需要创建新类？
    │
    ├─ 是否进行HTTP/事件处理？
    │   └─ 是 → Handler
    │
    ├─ 是否需要维护状态？
    │   └─ 是 → Manager (单例) 或 Coordinator (复杂编排)
    │
    ├─ 是否进行数据访问？
    │   └─ 是 → Repository
    │
    └─ 是否提供业务操作？
        └─ 是 → Service
```

---

## 4. Anti-Patterns (反模式)

### 4.1 避免混用后缀

```csharp
// BAD: Service 不应该维护状态
public class CacheService
{
    private readonly Dictionary<string, object> _cache;  // 状态！
}

// GOOD: 有状态 -> Manager
public class CacheManager
{
    private readonly Dictionary<string, object> _cache;
}
```

### 4.2 避免过度使用 Helper/Utility

```csharp
// BAD: 业务逻辑不应该放在 Helper
public static class PatientHelper
{
    public static bool ValidatePatient(Patient patient);
}

// GOOD: 业务验证 -> Service
public class PatientService
{
    public Result Validate(Patient patient);
}
```

---

## 5. Related Documents

- [Custom Control Guidelines](./custom-control-guidelines.md)
- [Exception Throwing Guidelines](./ExceptionThrowingGuidelines.md)
