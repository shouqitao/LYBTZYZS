# LYBTZYZS 架构与设计分析报告

**日期**: 2026-07-19
**Commit**: `13a974c`
**构建状态**: 0 errors, clean build

---

## 一、架构全景

### 1.1 项目结构

| 层级 | 技术栈 | 位置 |
|------|--------|------|
| **Server WebAPI** | ASP.NET Core 8 + MediatR CQRS | `src/Server/Services/LYBT.WebAPI/` |
| **Server Modules** | 8 个独立业务模块 | `src/Server/Modules/LYBT.Module.*/` |
| **Server Core** | Infrastructure + Entities + SharedKernel | `src/Server/Core/` |
| **Desktop Shell** | WPF + Prism.DryIoc + CommunityToolkit.Mvvm | `src/Client/Desktop/Shell/` |
| **Desktop Modules** | 8 个业务模块 + 4 个角色模块 | `src/Client/Desktop/{Modules,Roles}/` |
| **Desktop Core** | Foundation + Infrastructure + Contracts | `src/Client/Desktop/Core/` |
| **Shared** | DTOs + Contracts + Validators | `src/Shared/` |

### 1.2 设计模式清单

| 模式 | 位置 | 评价 |
|------|------|------|
| **模块化单体 (Modular Monolith)** | Server 8模块 + Desktop 14模块 | 优秀 — 模块间零直接引用 |
| **CQRS (MediatR)** | 所有 Server 模块操作 | 清晰 — Command/Query 分离 |
| **Repository 模式** | `BaseRepository<T>` + 模块具体实现 | 良好 — 模板方法扩展 |
| **策略模式 (Strategy)** | `SwitchingApiClient` 双模式路由 | 精妙 — 无锁快路径 |
| **组合优于继承** | `MasterDetailViewModelBase` 使用 `IMasterDetailServices` | 良好 |
| **Result 模式** | `Result<T>` / `ApiResponse<T>` | 存在多个变体（见问题清单） |
| **领域事件** | `IDomainEvent` + `InMemoryDomainEventDispatcher` | 基础设施就绪 |
| **Pipeline 步骤** | `StartupPipeline` 支持并行组 | 设计精良 |

---

## 二、Server 架构深度分析

### 2.1 WebAPI 层

**Program.cs** (323行) — 设计精良：
- 两阶段 Serilog（Bootstrap + Final），测试环境绕过避免 "logger frozen" 错误
- 热更新机制（`.update-pending` 标志文件 + Zip 解压）
- 配置验证管道：生产环境验证失败直接 `Environment.Exit(1)`
- Windows Service 支持（`UseWindowsService`）
- Identity 注册在 JWT 之前（已修复默认方案覆盖问题）

**12 个 Controller** — 统一 MediatR 派发模式：
```
GetOperator() → ValidateInput() → _sender.Send() → HandleResult()
```

**BaseApiController** (437行) — 功能丰富但值得关注：
- `GetOperator()` 多 Claims 源回退（NameIdentifier/Sub/sub, Role/role/roles）
- `HandleResult<T>()` 映射 `ModuleErrorCode` → HTTP 状态码
- `ValidateOwnership()` Admin 跳过所有权检查
- `SensitiveDataMasker` 日志脱敏

### 2.2 模块系统

8 个 Server 模块，每个通过静态扩展方法注册（如 `AddPatientsModule`）：

| 模块 | 关键服务 | 特殊点 |
|------|---------|--------|
| Auth | `JwtService` (Singleton), `SecurityAuditService` | JWT 密钥强度验证 |
| Users | `UserService`, `IdentitySeedData` | 智能重置（仅首次登录） |
| MedicalCase | CQRS 拆分 (Command/Query/State) | DDD 聚合根 |
| Patients | `PatientService`, `ImportExport` | 跨模块查询服务 |
| Herbs | `HerbService`, `ImportExport` | 引用检查 |
| Formula | `FormulaService` | 药材验证 |
| Registration | `RegistrationCrossModuleService` | 挂号管理 |
| Reports | 报表查询 | — |

**跨模块通信**：5 个 `ICrossModuleService` 接口 + `IDomainEvent` 异步事件 — 模块间零直接引用。

### 2.3 基础设施层

**AppDbContext** (197行)：
- 继承 `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`
- 13 个 DbSet
- `SaveChangesAsync` 重写实现审计字段自动填充
- `ApplyOptimizations()` + `ApplyConfigurationsFromAssembly()` 标准配置发现

**BaseRepository<T>** (714行)：
- 模板方法：`ApplyKeywordFilter()` / `ApplyDefaultOrdering()` 虚方法
- 全局软删除过滤器
- RowVersion 同步机制

---

## 三、Desktop 架构深度分析

### 3.1 Shell 启动

**App.xaml.cs** (203行)：
- PrismApplication + DryIoc 容器
- 全局 Mutex 单实例保护
- 两阶段启动：`OnStartup()` → `OnInitialized()` + 后台 `AppStartupOrchestrator`

**StartupPipeline** (412行)：
- 有序步骤执行 + 并行组
- Required/Optional 步骤区分
- 流水线状态机

### 3.2 模块加载策略

**双层模块体系**：
```
Shell → Roles/ (4个角色模块, WhenAvailable) → Modules/ (8个业务模块, OnDemand)
```

**角色驱动加载**：登录 → `IRoleRegistry.GetModulesForRole()` → `IModuleManager.LoadModule()` → 侧边栏构建

### 3.3 MVVM 架构

**ViewModel 继承链**：
```
CoreViewModelBase (IsBusy, 错误处理, 事件订阅)
  └→ NavigableViewModelBase (导航感知, 未保存追踪, IEditable)
      └→ MasterDetailViewModelBase<TList, TDetail> (分页, 搜索, 选择)
```

**CommunityToolkit.Mvvm 全面使用**：`[ObservableProperty]`、`[RelayCommand]`、`[NotifyPropertyChangedFor]`

### 3.4 双模式通信

**SwitchingApiClient** — 核心架构亮点：
- `IsLocal == true` → `HttpClientApiClient`（内嵌 LocalWebAPI, LocalDB）
- `IsLocal == false` → `RefitApiClient`（远程 SQL Server）
- 双重检查锁定 + volatile 字段实现无锁快路径
- Repository 层完全无感知 — 相同的 `IApiClient` 接口

---

## 四、发现的问题与优化建议

### 🔴 高优先级（架构级）

#### 问题 1：RowVersion 全局同步破坏乐观并发

**文件**: `BaseRepository.cs:678-694`

```csharp
// 全局RowVersion同步：遍历所有tracked实体
// 将OriginalValue同步为CurrentValue，跳过乐观并发检查
rowVersionProperty.OriginalValue = rowVersionProperty.CurrentValue;
```

**影响**：在单个请求内消除了并发冲突检测。虽然注释说"同一请求内多次操作是安全的"，但这实际上**完全绕过了 EF Core 的乐观并发机制**。真正的跨请求并发冲突永远不会被检测到。

**建议**：
- 仅对需要的实体（而非所有实体）进行 RowVersion 同步
- 或者移除此机制，依赖 EF Core 的原生并发检查
- 如果确实需要单请求内多操作，考虑使用 `AcceptAllChangesAfterSave()` 替代

#### 问题 2：三种 Result 模式并存

| 类型 | 位置 | 用途 |
|------|------|------|
| `Result<T>` (SharedKernel) | `src/Server/Core/LYBT.SharedKernel/Common/Result.cs` | Server MediatR 命令/查询 |
| `Result<T>` (Shared.Models) | `src/Shared/LYBT.Shared.Models/Common/Result.cs` | Shared 层（288行，功能更丰富） |
| `ServiceResult<T>` | `src/Shared/LYBT.Shared.Models/Contracts/Common/ServiceResult.cs` | 服务层响应 |

**影响**：维护成本高，新开发者困惑，潜在的类型不兼容。

**建议**：
- 统一为一个 `Result<T>` 实现，放在 `LYBT.SharedKernel` 中
- 删除 `ServiceResult<T>`（功能已被 `Result<T>` 覆盖）
- `Shared.Models/Common/Result.cs` 是 `SharedKernel` 的过时副本，应删除

#### 问题 3：AppDbContext 缺少软删除全局过滤器

**对比**：
- `LocalDbContext`：明确调用 `ApplySoftDeleteFilter(modelBuilder)` 遍历所有 `ISoftDeletable` 实体
- `AppDbContext`：依赖 `ApplyOptimizations()` 中的配置（隐式）

**风险**：如果 `ApplyOptimizations()` 的实现变更，Server 端可能意外暴露已删除数据。

**建议**：在 `AppDbContext.OnModelCreating` 中显式添加与 `LocalDbContext` 相同的 `ApplySoftDeleteFilter` 调用，作为防御性编程。

### 🟡 中优先级（设计级）

#### 问题 4：JwtService 作为 Singleton

**文件**: `AuthModule.cs:36` — `JwtService` 注册为 Singleton

**风险**：JWT 密钥在构造时捕获，如果需要密钥轮换（hot-reload），Singleton 生命周期无法响应变化。

**建议**：改为 Scoped + `IOptionsMonitor<JwtOptions>`，或确认密钥轮换不是需求。

#### 问题 5：AddServerRepositories 空实现

**文件**: `RepositoryServiceCollectionExtensions.cs:48-60`

```csharp
public static IServiceCollection AddServerRepositories(this IServiceCollection services)
{
    // 当前为空 — 所有具体Repository由各模块自行注册
    return services;
}
```

**建议**：删除此空方法，或添加注释说明其设计意图（占位/未来扩展）。

#### 问题 6：MedicalCase 双 Controller 共享路由

**文件**: `MedicalCasesController` + `MedicalCaseProcessingController` — 同一路由 `api/v1/medicalcases`

**风险**：依赖 HTTP 方法 + 子路径区分，容易混淆，需要文档说明。

**建议**：要么合并为单个 Controller，要么将 Processing 路由改为 `api/v1/medicalcases/{id}/workflow` 以明确语义。

#### 问题 7：ModuleLazyLoader 同步阻塞

**文件**: `ModuleLazyLoader.cs:49`

```csharp
public void EnsureModuleLoaded(string viewName)
{
    _moduleLoadingService.LoadModuleAsync().GetAwaiter().GetResult(); // 同步阻塞
}
```

**风险**：在 UI 线程上同步阻塞，可能导致界面卡顿。

**建议**：改为异步加载 + 加载指示器，或确保模块加载时间 < 100ms。

#### 问题 8：开发环境硬编码 JWT 密钥

**文件**: `AuthenticationServiceCollectionExtensions.cs:44`

```csharp
const string DefaultDevelopmentSecretKeyForJWTAuthentication_ShouldBeReplacedInProduction = "...";
```

**建议**：从 `appsettings.Development.json` 读取，而非硬编码在代码中。

### 🟢 低优先级（代码级）

#### 问题 9：BaseApiController 职责过重

437 行，包含：操作者提取、日志脱敏、所有权验证、分页验证、模型验证、多种响应辅助方法、Result 映射。

**建议**：考虑拆分为 `AuthorizationFilter`、`ValidationFilter`、`ApiResponseFilter` 等 ASP.NET Core Filters，减少 Controller 基类体积。

#### 问题 10：Desktop HttpClientApiClient 777 行

实现所有 8 个 API 子接口，手动包装 `ApiResponse<T>`。

**建议**：考虑用 Source Generator 自动生成适配代码，或统一 Local/Remote 的响应格式。

---

## 五、架构亮点（值得保持）

1. **SwitchingApiClient 无锁快路径** — 双重检查锁 + volatile，99.9% 缓存命中率
2. **模块化单体的严格隔离** — 模块间零直接引用，通过 ICrossModuleService + DomainEvent 通信
3. **StartupPipeline 并行组** — 支持有序执行 + 并行步骤，优雅降级
4. **两阶段 Serilog** — Bootstrap logger 捕获启动错误，Final logger 完整配置
5. **ClaimsNormalizationMiddleware** — 防御性多格式 Claims 处理
6. **响应式缓存策略** — `OutputCache` 命名策略（Herbs 30min, Formulas 2h, Patients 30min）
7. **RoleRegistry 驱动一切** — 登录后角色 → 模块加载 → 导航构建 → 首页视图，单一真相源
8. **CommunityToolkit.Mvvm 源生成器** — 零手动 INotifyPropertyChanged 样板代码

---

## 六、量化指标

| 指标 | 数值 |
|------|------|
| Server Controller | 12 个 |
| Server 模块 | 8 个 |
| Desktop 模块 | 8 业务 + 4 角色 + 6 核心 |
| Entity/DTO 总数 | ~50+ |
| Middleware | 5 个自定义 |
| Authorization Policy | 5 个 |
| Output Cache Policy | 4 个命名策略 |
| Shared Validators | ~15 个 |
| BaseRepository 行数 | 714 行 |
| BaseApiController 行数 | 437 行 |
| AppDbContext DbSet | 13 个 |

---

## 七、优化路线图建议

### Phase 1（快速收益，1-2天）
- [ ] 删除空的 `AddServerRepositories` 方法
- [ ] 显式添加 AppDbContext 软删除过滤器
- [ ] 将开发环境 JWT 密钥移至配置文件

### Phase 2（架构治理，3-5天）
- [ ] 统一三种 Result 模式为一种
- [ ] 评估 RowVersion 全局同步的必要性
- [ ] 拆分 BaseApiController 为 Filters

### Phase 3（长期演进）
- [ ] JwtService 改为 Scoped + IOptionsMonitor
- [ ] MedicalCase Controller 路由重构
- [ ] ModuleLazyLoader 异步化
- [ ] HttpClientApiClient 代码生成

---

**分析工具**: codegraph (167+ symbols), serena (symbol-level analysis), 3 parallel explore subagents
**分析耗时**: ~5 minutes
