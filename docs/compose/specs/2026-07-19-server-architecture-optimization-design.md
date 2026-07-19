# Server 层架构优化设计

## [S1] 问题

Server 层经过多轮迭代，核心架构已较好（CQRS、模块化单体、StartupPipeline），但与最佳实践对比仍有以下问题：

### 1.1 三种 Result 模式并存

| 类型 | 位置 | 行数 | 用途 |
|------|------|------|------|
| `Result<T>` (SharedKernel) | `src/Server/Core/LYBT.SharedKernel/Common/Result.cs` | 70 | Server MediatR 命令/查询 |
| `Result<T>` (Shared.Models) | `src/Shared/LYBT.Shared.Models/Common/Result.cs` | 288 | Shared 层 |
| `ServiceResult<T>` | `src/Shared/LYBT.Shared.Models/Contracts/Common/ServiceResult.cs` | 136 | 服务层响应 |

**问题**：维护成本高，新开发者困惑，潜在的类型不兼容。

### 1.2 JwtService 作为 Singleton

`JwtService` 注册为 Singleton，构造时捕获 `IOptions<JwtOptions>`。如果需要密钥轮换（hot-reload），Singleton 生命周期无法响应变化。

### 1.3 AddServerRepositories 空实现

`RepositoryServiceCollectionExtensions.AddServerRepositories()` 方法体为空，但被 `DatabaseServiceCollectionExtensions` 调用。

### 1.4 MedicalCase 双 Controller 共享路由

`MedicalCasesController` + `MedicalCaseProcessingController` 共享路由 `api/v1/medicalcases`，依赖 HTTP 方法 + 子路径区分。

---

## [S2] 目标

| # | 目标 | 度量 |
|---|------|------|
| G1 | 统一三种 Result 模式为一种 | 文件数 3→1 |
| G2 | JwtService 改为 Scoped + IOptionsMonitor | 生命周期变更 |
| G3 | 删除 AddServerRepositories 空方法 | 方法数 -1 |
| G4 | MedicalCase Controller 路由明确化 | 路由不再共享 |
| G5 | 编译通过，现有测试全绿 | `dotnet build` + `dotnet test` |

---

## [S3] 非目标

- 不改变任何功能行为、API 契约
- 不引入新框架或新 DI 容器
- 不改变 Prism 模块化架构
- 不重构业务模块

---

## [S4] 设计：统一 Result 模式

### 4.1 现状分析

**SharedKernel.Result** (70行)：
- `Result<T>`: `IsSuccess`, `Value`, `Error`, `ErrorCode`
- `Result`: `IsSuccess`, `Error`, `ErrorCode`
- 使用 `ErrorCode` 枚举
- 不可变（私有构造函数）
- 被 Server MediatR 命令/查询使用

**Shared.Models.Result** (288行)：
- `Result<T>`: `IsSuccess`, `Data`, `ErrorMessage`, `Errors`, `ModuleErrorCode`
- `Result`: `IsSuccess`, `ErrorMessage`, `Errors`, `ModuleErrorCode`
- 使用 `GenericErrorCode` 枚举
- 可变（公共 setter）
- 有 `FromException` 辅助方法
- 被 Shared 层使用

**ServiceResult** (136行)：
- `ServiceResult<T>`: `IsSuccess`, `Data`, `ErrorMessage`, `Exception`
- `ServiceResult`: `IsSuccess`, `ErrorMessage`, `Exception`
- 有 `Exception` 属性（独特功能）
- 被服务层使用

### 4.2 方案：统一到 SharedKernel.Result

**理由**：
- SharedKernel.Result 最简洁（70行），不可变，符合函数式编程最佳实践
- Server MediatR 已经在使用它
- `ErrorCode` 枚举比 `GenericErrorCode` 更结构化

**迁移步骤**：

**Step 1**: 将 `Shared.Models.Result` 的有用功能合并到 `SharedKernel.Result`：
- 添加 `Errors` 属性（支持多个错误）
- 添加 `FromException` 静态工厂方法
- 添加 `ModuleErrorCode` 属性（可选）

**Step 2**: 将 `ServiceResult` 的 `Exception` 属性添加到 `SharedKernel.Result`

**Step 3**: 更新所有 `Shared.Models.Result` 的引用到 `SharedKernel.Result`

**Step 4**: 更新所有 `ServiceResult` 的引用到 `SharedKernel.Result`

**Step 5**: 删除 `Shared.Models.Result` 和 `ServiceResult`

### 4.3 影响范围

- `SharedKernel.Result.cs`：扩展功能
- `Shared.Models.Result.cs`：删除
- `ServiceResult.cs`：删除
- 所有使用 `Shared.Models.Result` 的文件：更新 using
- 所有使用 `ServiceResult` 的文件：更新 using

---

## [S5] 设计：JwtService 改为 Scoped

### 5.1 现状

```csharp
// AuthModule.cs
containerRegistry.RegisterSingleton<IJwtService, JwtService>();
```

### 5.2 方案

```csharp
// AuthModule.cs
containerRegistry.RegisterScoped<IJwtService, JwtService>();
```

同时将 `IOptions<JwtOptions>` 改为 `IOptionsMonitor<JwtOptions>` 以支持热重载：

```csharp
public JwtService(IOptionsMonitor<JwtOptions> jwtOptions, IWebHostEnvironment environment)
{
    _jwtOptions = jwtOptions.CurrentValue;
    _environment = environment;
    _tokenHandler = new JwtSecurityTokenHandler();
    ValidateSecretKeyStrength();
}
```

### 5.3 影响范围

- `JwtService.cs`：构造函数参数类型变更
- `AuthModule.cs`：注册生命周期变更

---

## [S6] 设计：删除 AddServerRepositories 空方法

### 6.1 现状

```csharp
// RepositoryServiceCollectionExtensions.cs
public static IServiceCollection AddServerRepositories(this IServiceCollection services)
{
    // 当前为空 — 所有具体Repository由各模块自行注册
    return services;
}
```

### 6.2 方案

删除此空方法，并更新所有调用方。

### 6.3 影响范围

- `RepositoryServiceCollectionExtensions.cs`：删除方法
- `DatabaseServiceCollectionExtensions.cs`：移除调用

---

## [S7] 设计：MedicalCase Controller 路由明确化

### 7.1 现状

两个 Controller 共享路由 `api/v1/medicalcases`：
- `MedicalCasesController`：GET/POST/PUT/DELETE 操作
- `MedicalCaseProcessingController`：状态转换操作

### 7.2 方案

将 `MedicalCaseProcessingController` 的路由改为 `api/v1/medicalcases/{id}/workflow`：

```csharp
[Route("api/v1/medicalcases/{id:guid}/workflow")]
public class MedicalCaseProcessingController : BaseApiController
{
    [HttpPut("close")]
    public async Task<IActionResult> CloseCase(Guid id, ...)
    
    [HttpPut("suspend")]
    public async Task<IActionResult> SuspendCase(Guid id, ...)
    
    [HttpPut("cancel")]
    public async Task<IActionResult> CancelCase(Guid id, ...)
}
```

### 7.3 影响范围

- `MedicalCaseProcessingController.cs`：路由变更
- Desktop `HttpClientApiClient`：更新 API 调用路径

---

## [S8] 实施顺序

| 顺序 | 任务 | 风险 | 预估工时 |
|------|------|------|----------|
| 1 | 删除 AddServerRepositories 空方法 | 低 | 0.5h |
| 2 | JwtService 改为 Scoped + IOptionsMonitor | 低 | 1h |
| 3 | MedicalCase Controller 路由明确化 | 中 | 2h |
| 4 | 统一 Result 模式（SharedKernel.Result 扩展） | 高 | 4h |
| 5 | 迁移 Shared.Models.Result 引用 | 高 | 3h |
| 6 | 迁移 ServiceResult 引用 | 中 | 2h |
| 7 | 删除旧 Result 文件 | 低 | 0.5h |
| 8 | 最终验证 | 低 | 1h |
| **合计** | | | **14h** |

---

## [S9] 验证标准

1. `dotnet build LYBTZYZS.sln` 编译通过（0 errors）
2. `dotnet test tests/LYBT.Tests.Server/` 全部通过
3. `Shared.Models.Result.cs` 已删除
4. `ServiceResult.cs` 已删除
5. `AddServerRepositories` 方法已删除
6. `MedicalCaseProcessingController` 路由为 `api/v1/medicalcases/{id}/workflow`
7. `JwtService` 注册为 Scoped
