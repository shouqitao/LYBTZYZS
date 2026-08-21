# R2 架构与设计模式审查报告

> **审查日期**：2026-08-21
> **审查范围**：src/Server/、src/Client/Desktop/、src/Shared/
> **审查角度**：模块边界/DI/MVVM/Repository/Service分层
> **审查基准**：lybtzys-coder-rules 0.15.0 + 架构测试基线 92 条

---

## 一、审查总览

| 审查项 | 状态 | 说明 |
|--------|------|------|
| P07 模块间禁直接引用 | ✅ 合规 | Server 6 模块 + Desktop 6 模块均通过 CrossModule 通道通信 |
| P08 跨模块必须用接口 | ✅ 合规 | CrossModule 接口位于 Infrastructure.Services.CrossModule |
| P10 Service 禁注入 AppDbContext | ✅ 合规 | CrossModule 服务注入模块自有 DbContext（非 AppDbContext） |
| DI 注册合理性 | ⚠️ 需关注 | 见 §2 |
| Repository 模式 | ✅ 合规 | 统一 BaseRepository<T,TDbContext> 基类 |
| Service 层分层 | ⚠️ 需关注 | MedicalCase 模块较大（1814行/7 文件），但已 CQRS 拆分 |
| MVVM 合规 | ⚠️ 有瑕疵 | MainWindowViewModel 使用 Visibility（WPF 类型泄漏） |
| 跨模块通信 | ✅ 合规 | 通过 ICrossModuleService 接口 |
| 异常处理链 | ✅ 合规 | Server 双层（Business→System）+ Desktop DesktopExceptionHandler |
| 设计模式使用 | ✅ 合规 | CQRS/MediatR/Mapperly/MasterDetail/组合模式 |

---

## 二、详细审查

### 2.1 模块边界与依赖方向

**Server 端（6 模块）**：
- `LYBT.Module.Identity` → 无跨模块引用 ✅
- `LYBT.Module.Patients` → 无跨模块引用 ✅
- `LYBT.Module.MedicalCases` → 无跨模块引用 ✅
- `LYBT.Module.Catalog` → 无跨模块引用 ✅
- `LYBT.Module.Registration` → 无跨模块引用 ✅
- `LYBT.Module.Reports` → 无跨模块引用 ✅

**Desktop 端（6 业务模块）**：
- `LYBT.Desktop.Auth` → 无跨模块引用 ✅
- `LYBT.Desktop.Users` → 无跨模块引用 ✅
- `LYBT.Desktop.Patients` → 无跨模块引用 ✅
- `LYBT.Desktop.MedicalCase` → 无跨模块引用 ✅
- `LYBT.Desktop.Catalog` → 无跨模块引用 ✅
- `LYBT.Desktop.Registrations` → 无跨模块引用 ✅

**CrossModule 通道**（P07/P08 实现）：
- Server：`ICatalogCrossModuleService`、`IUserCrossModuleService`、`IPatientCrossModuleService`、`IRegistrationCrossModuleService`、`IMedicalCaseCrossModuleService`
- 定义在 `LYBT.Infrastructure.Services.CrossModule`（Server Shared 层）
- 实现在各 Module 内部的 Services 目录
- **异常**：`CatalogCrossModuleService` 直接注入 `CatalogDbContext`（非 AppDbContext），`PatientCrossModuleService` 直接注入 `PatientsDbContext`——属于模块自有 DbContext，P10 豁免范围

**依赖方向**：
- Entities → 无依赖（✅ P12 守卫）
- Infrastructure → 不依赖 WebAPI（✅ P12b 守卫）
- Module → 不依赖其他 Module（✅ P07 守卫）
- WebAPI → 引用所有 Module（组合根角色，✅）

### 2.2 DI 注册分析

| 注册方式 | 数量 | 说明 |
|----------|------|------|
| `AddScoped` | ~30+ | 所有 Repository + Service + CrossModule |
| `AddSingleton` | 3 | `MedicalCaseMapper`、`RegistrationMapper`、`RegistrationConnectionManager` |
| `AddTransient` | 0 | 未使用 |

**Singleton 审查**：
| Singleton | 评估 |
|-----------|------|
| `MedicalCaseMapper` | ✅ Mapperly 无状态静态类，线程安全 |
| `RegistrationMapper` | ✅ 同上 |
| `RegistrationConnectionManager` | ⚠️ 管理 SignalR 连接组——需确认是否持有 DbContext 或 Scoped 依赖 |

**DI 设计合理性**：
- Repository → Scoped ✅（匹配 DbContext 生命周期）
- Service → Scoped ✅
- Mapper → Singleton ✅（无状态）
- 基础设施（CacheInvalidationService）→ Singleton ✅（全局缓存操作）

**潜在问题**：`RegistrationConnectionManager` 作为 Singleton 持有 SignalR 连接映射——如果内部引用了 Scoped 服务，可能导致 Captive Dependency 问题。**建议**：审计其构造函数依赖链。

### 2.3 Repository 模式

**统一架构**：
- 基类：`BaseRepository<TEntity, TDbContext>` 提供 4 核心 CRUD + `GetByIdIncludingDeletedAsync`
- 泛型 TDbContext：支持 ADR-0017 模块自有 DbContext 模式
- 所有 Repository 继承 BaseRepository 或有自有 DbContext（P02b 守卫验证）

**Desktop Repository**：
- 接口统一在 `LYBT.Desktop.Contracts/Repositories/`
- 实现在各 Module 的 `Repositories/` 目录
- 继承 `ApiClientRepositoryBase<TDto, TModel>` → 标准 try/catch + 日志模板（A02 守卫验证）

### 2.4 Service 层分层（God Class 审计）

| 服务 | 行数 | 方法数 | 评估 |
|------|------|--------|------|
| `MedicalCaseCommandService` | 449 | 8 async | ⚠️ 较大，但已 CQRS 拆分 |
| `MedicalCaseQueryService` | 441 | — | ⚠️ 较大，但职责单一（查询） |
| `JwtService` | 435 | — | ⚠️ 较大，但职责集中（JWT 生成/验证/刷新） |
| `MedicalCaseStateService` | 367 | — | ⚠️ 状态管理，职责清晰 |
| `CardReaderService` | 368 | — | 硬件交互，复杂度合理 |

**MedicalCase 模块总计**：
- CommandService + QueryService + StateService + PrescriptionService + PrescriptionItemService + CrossModuleService + ServiceHelper = **1814 行 / 7 文件**
- **评估**：已通过 Phase 3 CQRS 拆分为 Command/Query/State 三职责，各文件职责清晰。449 行的 CommandService 包含 `CreateFromInputDto`/`UpdateConsultation`/`UpdatePrescription`/`Delete`/`Save` 等核心写操作，复杂度与业务域匹配，**未达到 God Class 阈值**（单文件 >500 行 + >15 方法）

### 2.5 MVVM 合规

**基类体系**：
- `NavigableViewModelBase`（partial 拆分 3 文件）→ 提供服务聚合/导航/状态/事件
- `MasterDetailViewModelBase<TListItem, TDetail>` → 组合模式（ServiceEventBridge + MasterDetailCommandGroup）
- `DialogViewModelBase` → 对话框场景
- `ChildViewModelBase` → 子视图组合

**DP04 守卫验证**：所有 ViewModel 继承自标准基类 ✅

**问题发现**：

| 问题 | 严重性 | 详情 |
|------|--------|------|
| **MainWindowViewModel 使用 Visibility** | ⚠️ 低 | `NavTextVisibility` 返回 `Visibility.Visible/Collapsed`，WPF 类型泄漏到 ViewModel 层 |
| LoginViewModel 使用 `System.IO` | ℹ️ 可接受 | 文件操作（首次运行标记），不涉及 View 类型 |
| ConnectionStatusViewModel 使用 Visibility | ⚠️ 低 | 同 MainWindowViewModel |

**MVVM 合规性**：
- ✅ ViewModel 不直接引用 IApiClient 子接口（DP10 守卫）
- ✅ ViewModel 不使用 DelegateCommand（DM04 守卫，全部 CommunityToolkit）
- ✅ ViewModel 不直接注入 IRegionManager（DP09 守卫）
- ✅ ViewModel 不直接持有 DTO 做编辑属性（DP-M1 守卫）
- ✅ 每个 MasterDetail 模块有 DetailModel（DP-M2 守卫）

### 2.6 跨模块通信

**Server 端**：
- CrossModule 接口位于 `LYBT.Infrastructure.Services.CrossModule`（Shared 层）
- 接口定义：`ICatalogCrossModuleService`、`IUserCrossModuleService`、`IPatientCrossModuleService`、`IRegistrationCrossModuleService`、`IMedicalCaseCrossModuleService`
- 实现在各 Module 内部
- P08 守卫验证：模块间不得直接引用其他模块的 Service 实现 ✅

**Desktop 端**：
- CrossModule 接口位于 `LYBT.Desktop.Contracts/Services/CrossModule/`
- `IFormulaSearchProvider`、`IHerbSearchProvider` → 跨模块搜索支持
- DP06 守卫：Desktop 层不直接使用 Entity 类（仅 Repository/Mapper 例外）✅

### 2.7 DbContext 注入分析

**Server 端**：
- `AppDbContext`：仅在 `DatabaseServiceCollectionExtensions` 和 `DatabaseInitializationService` 中注册/使用
- 模块自有 DbContext：`CatalogDbContext`、`IdentityDbContext`、`PatientsDbContext`、`MedicalCaseDbContext`
- **P10 守卫**：Service 禁注入 AppDbContext ✅
- **P18 守卫**：模块 Repository 必须注入自有 DbContext（Reports 例外）✅

**CrossModule 服务的 DbContext 注入**：
| 服务 | 注入的 DbContext | 评估 |
|------|------------------|------|
| `CatalogCrossModuleService` | `CatalogDbContext` | ✅ 模块自有 |
| `PatientCrossModuleService` | `PatientsDbContext` | ✅ 模块自有 |
| `UserService` | 无（走 Repository） | ✅ P10-1 合规 |

### 2.8 异常处理链

**Server 端**（ASP.NET Core ExceptionHandler 管道）：
1. `BusinessExceptionHandler` → 处理 `AppException` 及子类（BusinessException/NotFoundException/ValidationException 等）
2. `SystemExceptionHandler` → 兜底处理所有未处理异常

**异常类型体系**：
```
AppException (基类)
├── BusinessException（业务逻辑异常）
├── NotFoundException（资源未找到）
├── ValidationException（验证失败）
├── UnauthorizedException（权限不足）
└── ConflictException（并发冲突）
```

**Desktop 端**：
- `DesktopExceptionHandler`（实现 `IDesktopExceptionHandler`）
- 注册全局异常：`AppDomain.UnhandledException` + `TaskScheduler.UnobservedTaskException`
- `ClientErrorMessageMapper` 提供用户友好的错误消息映射

**注册顺序**：`BusinessExceptionHandler` 先于 `SystemExceptionHandler`（`ExceptionHandlingServiceCollectionExtensions.AddLybtExceptionHandling`）✅

### 2.9 设计模式使用

| 模式 | 使用场景 | 评估 |
|------|----------|------|
| CQRS | MedicalCases/Identity/Patients/Catalog/Registration 模块 | ✅ MediatR Command/Query 分离 |
| Repository | 所有数据访问 | ✅ 统一 BaseRepository 基类 |
| MasterDetail | Desktop CRUD ViewModel | ✅ 泛型基类 + 组合模式 |
| 组合模式 | MedicalCaseMasterDetailViewModel + 子 VM | ✅ ConsultationEditor/PrescriptionEditor |
| 策略模式 | `ICatalogQueryService<TList,TDetail>` 泛型查询 | ✅ 工厂注入 Mapper/ErrorCode |
| 模板方法 | `CatalogEntityCommandHandlerBase<T>` | ✅ 骨架收敛 + 子类差异点 |
| 观察者 | Prism EventAggregator + ServiceEventBridge | ✅ 跨模块事件通信 |
| 适配器 | SwitchingApiClient（Remote/Local 切换） | ✅ 双模式路由 |

---

## 三、发现的问题

### 🔴 P0（阻塞）

无

### 🟡 P1（需关注）

| # | 问题 | 位置 | 严重性 | 建议 |
|---|------|------|--------|------|
| A1 | `RegistrationConnectionManager` 为 Singleton，需审计其依赖链是否持有 Scoped 服务 | `RegistrationModule.cs:49` | 🟡 | 审计构造函数依赖，确认无 Captive Dependency |
| A2 | `MainWindowViewModel` 使用 `System.Windows.Visibility` 类型 | `MainWindowViewModel.cs:67` | 🟡 | 改为 `bool IsNavTextVisible`，XAML 中用 `BooleanToVisibilityConverter` |

### 🟢 P2（建议改进）

| # | 问题 | 位置 | 说明 |
|---|------|------|------|
| B1 | MedicalCaseCommandService 449 行，接近 500 行阈值 | `MedicalCaseCommandService.cs` | 已 CQRS 拆分，当前可接受；若继续增长建议提取 `MedicalCaseValidationHelper` |
| B2 | `CatalogCrossModuleService` 直接注入 DbContext 做 LINQ 查询 | `CatalogCrossModuleService.cs` | 符合 P10 豁免，但如查询逻辑增长建议提取到 Repository |
| B3 | `MedicalCaseServiceHelper` 为 static 类（199 行） | `MedicalCaseServiceHelper.cs` | static 辅助类不利于测试；建议提取为 `IMedicalCaseServiceHelper` 接口 |

---

## 四、架构测试覆盖

当前架构测试 **92/92 通过**（基线），关键守卫：

| 守卫 | 测试 | 状态 |
|------|------|------|
| P07 模块间禁直接引用 | `P07_ServerModules_Should_Not_Reference_Other_ServerModules` + `DP07_DesktopModules_Should_Not_Reference_Other_DesktopModules` | ✅ |
| P08 跨模块必须用接口 | `P08_CrossModule_References_Must_Use_Interfaces` | ✅ |
| P10 Service 禁注入 AppDbContext | `P10_Services_Should_Not_Directly_Inject_AppDbContext` | ✅ |
| P18 模块 Repository 注入自有 DbContext | `P18_Module_Repositories_Must_Inject_Own_DbContext` | ✅ |
| DP10 VM 禁注入 IApiClient 子接口 | `DP10_ViewModels_Must_Not_Inject_IApiClient_SubInterfaces` | ✅ |
| DP-M1 DTO 逃逸检查 | `DP_M1_ViewModels_Must_Not_Hold_Dto_As_Editable_Property` | ✅ |
| CQRS 写方法隔离 | `P19_Cqrs_Services_Must_Not_Expose_Write_Methods` | ✅ |
| IL 级写方法调用检查 | `P19b_Cqrs_Write_Endpoints_Must_Not_Call_Service_Write_Methods` | ✅ |

---

## 五、结论

**整体架构健康度：良好（8.5/10）**

1. **模块边界清晰**：Server 6 模块 + Desktop 6 模块完全隔离，跨模块通信通过 Infrastructure 层定义的 CrossModule 接口
2. **依赖方向正确**：Entities → Shared → Infrastructure → Module → WebAPI，无反向依赖
3. **DI 注册合理**：Scoped 为主（Repository/Service），Singleton 仅用于无状态 Mapper 和全局管理器
4. **Repository 模式统一**：BaseRepository 基类 + 模块自有 DbContext（ADR-0017）
5. **Service 层已 CQRS 拆分**：MedicalCase 模块 Command/Query/State 分离，无 God Class
6. **MVVM 基本合规**：基类体系完善，DP04/DP09/DP10/DP-M1/DP-M2 守卫全部通过
7. **异常处理链完整**：Server 双层 ExceptionHandler + Desktop 全局异常注册
8. **设计模式恰当**：CQRS/MediatR/Mapperly/MasterDetail/组合模式使用得当

**需关注项**：A1（Singleton Captive Dependency 审计）、A2（Visibility 类型泄漏）

---

*报告生成：R2 Architecture Review | 2026-08-21*
