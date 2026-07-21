# 继承冗余与过度设计优化方案

> [!NOTE]
> This document may not reflect the current implementation.
> See the final report for up-to-date state:
> [Final Report](../reports/inheritance-redundancy-optimization.md)

> 日期: 2026-07-21
> 状态: 待审核
> 范围: 全解决方案 — 继承层次、过度抽象、冗余设计
> 前置依赖: 2026-07-20-design-pattern-unification-design.md（Phase 3 已完成）

## [S1] Problem

系统性审计发现 12 处设计问题，分三类：

### 继承过深（4处）

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| D1 | Desktop ViewModel 4层继承链 | CoreViewModelBase → NavigableViewModelBase → MasterDetailViewModelBase → 具体VM | 12个可重写钩子，子类必须理解全部4层生命周期 |
| D2 | DTO 4层继承链 | BaseDto → TimestampDto → StatusDto + 4个接口 | StatusDto 几乎无使用，ICreatorTrackable仅1字段 |
| D3 | Server BaseService 职责越界 | Infrastructure层BaseService被MedicalCaseStateService继承 | 权限验证逻辑泄漏到基础设施层 |
| D4 | Server BaseRepository 712行 | BaseRepository.cs | CRUD+软删+分页+模板方法+排序+投影+异常，职责过多 |

### 过度抽象（5处）

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| A1 | IViewModelServices 上帝接口 | 9个服务聚合，所有VM注入 | 掩盖真实依赖，测试需mock全部9属性 |
| A2 | ApiClientRepositoryBase 4泛型参数 | TCreateDto=TUpdateDto的场景占多数 | 声明冗长，4个参数仅2个真正不同 |
| A3 | BaseApiController 放在 Infrastructure | LYBT.Infrastructure/Web/BaseApiController.cs | Controller基类不应在基础设施层 |
| A4 | CrossModule接口碎片化 | 4个域接口+旧ICrossModuleService共存 | 接口多但每个仅1-3方法 |
| A5 | MasterDetail 3层间接 | VM → CommandGroup → Services + VM → EventBridge → Services | 命令和事件回调需跨3层追踪 |

### 冗余设计（3处）

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| R1 | Server/Desktop 两套独立Repository体系 | 同名接口，API完全不同 | 认知负担，两端同名不同义 |
| R2 | Controller 重复 — WebAPI+LocalWebAPI各10个 | 10对Controller几乎相同 | 已在 controller-deduplication-spec 识别 |
| R3 | 每个模块重复三层结构 | Interface→Service→Repository 每模块都重复 | 样板代码多 |

## [S2] Solution Overview

分 4 个批次执行，每批次独立可验证：

| 批次 | 范围 | 风险 | 工作量 | 依赖 |
|------|------|------|--------|------|
| P1 | A1 拆分 IViewModelServices | 中 | 高 | 无 |
| P2 | D1 扁平化 ViewModel 继承链 | 中 | 高 | P1 |
| P3 | D4+A2 瘦身 BaseRepository + ApiClientRepositoryBase | 低 | 中 | 无 |
| P4 | D2+A4 精简 DTO 继承 + CrossModule 接口 | 低 | 低 | 无 |

**不在本方案范围**（已有独立 spec）：
- R2 Controller 去重 → `2026-06-29-controller-deduplication-spec.md`
- A3 BaseApiController 移动 → `2026-06-29-server-core-fix-spec.md`（方案3）
- D3 BaseService 职责分离 → `2026-06-29-server-core-fix-spec.md`（方案2）

---

## [S3] P1: 拆分 IViewModelServices 上帝接口

### 问题

`IViewModelServices` 聚合 9 个服务，每个 ViewModel 只用其中 2-3 个却必须接受全部。测试时需 mock 整个接口。

### 当前状态

```csharp
// 9 个属性
public interface IViewModelServices
{
    ILoggerFactory LoggerFactory { get; }
    IEventAggregator EventAggregator { get; }
    IRegionManager RegionManager { get; }
    ISessionManager SessionManager { get; }
    IUserNotificationService UserNotificationService { get; }
    ICommonDialogService CommonDialogService { get; }
    IToastService ToastService { get; }
    IRoleRegistry RoleRegistry { get; }
    IUiThreadDispatcher UiThreadDispatcher { get; }
}
```

### 方案: 保留接口但不再注入到具体 VM

**策略**: 保留 `IViewModelServices` 作为 **Base 层内部实现细节**，具体 ViewModel 不再直接注入它。基类从 `IViewModelServices` 中提取自己需要的服务为 protected 字段，子类通过基类属性访问。

**Phase 1: 基类内部解构（不改子类签名）**

```csharp
// CoreViewModelBase — 只保留它真正需要的
public abstract partial class CoreViewModelBase : ObservableObject
{
    protected ILogger Logger { get; }
    protected IEventAggregator EventAggregator { get; }
    protected IUiThreadDispatcher UiThreadDispatcher { get; }

    protected CoreViewModelBase(IViewModelServices services)
    {
        Logger = services.LoggerFactory.CreateLogger(GetType());
        EventAggregator = services.EventAggregator;
        UiThreadDispatcher = services.UiThreadDispatcher;
    }
}

// NavigableViewModelBase — 额外需要的服务
public abstract partial class NavigableViewModelBase : CoreViewModelBase
{
    protected IRegionManager RegionManager { get; }
    protected ISessionManager SessionManager { get; }
    protected IUserNotificationService UserNotificationService { get; }
    protected ICommonDialogService CommonDialogService { get; }
    protected IToastService ToastService { get; }
    protected IRoleRegistry RoleRegistry { get; }

    protected NavigableViewModelBase(IViewModelServices services) : base(services)
    {
        RegionManager = services.RegionManager;
        SessionManager = services.SessionManager;
        UserNotificationService = services.UserNotificationService;
        CommonDialogService = services.CommonDialogService;
        ToastService = services.ToastService;
        RoleRegistry = services.RoleRegistry;
    }
}
```

**Phase 2: 消除子类对 IViewModelServices 的直接引用**

当前 51 个 ViewModel 注入了 `IViewModelServices`。改为：
- 基类 ViewModel（CoreViewModelBase/NavigableViewModelBase/MasterDetailViewModelBase）保留 `IViewModelServices` 构造参数
- 具体 ViewModel 不再接收 `IViewModelServices`，改为接收实际需要的独立服务

```csharp
// Before: PatientMasterDetailViewModel
public PatientMasterDetailViewModel(
    IViewModelServices viewModelServices,           // 9个服务
    IMasterDetailServices<...> masterDetailServices,
    IPatientService patientService, ...)
    : base(viewModelServices, masterDetailServices)

// After: PatientMasterDetailViewModel
public PatientMasterDetailViewModel(
    IViewModelServices viewModelServices,           // 仅传给基类
    IMasterDetailServices<...> masterDetailServices,
    IPatientService patientService,
    IDesktopCacheManager cacheManager, ...)         // 直接注入具体依赖
    : base(viewModelServices, masterDetailServices)
```

**注意**: 这一步**不改变子类构造函数签名**——子类仍然接收 `IViewModelServices` 并传给基类。改变的是：子类不再从 `viewModelServices` 中提取服务，而是通过基类的 protected 属性访问。

**Phase 3（未来）: 彻底消除 IViewModelServices**

当所有具体 ViewModel 不再直接访问 `IViewModelServices` 属性后，基类构造函数可以改为接收独立服务：

```csharp
protected CoreViewModelBase(
    ILoggerFactory loggerFactory,
    IEventAggregator eventAggregator,
    IUiThreadDispatcher uiThreadDispatcher)
```

这是最终目标，但不在本批次执行。

### 影响范围

- 修改: `IViewModelServices.cs`（保持不变）
- 修改: `ViewModelServices.cs`（保持不变）
- 修改: `CoreViewModelBase.cs` — 构造函数内部提取
- 修改: `NavigableViewModelBase.cs` — 构造函数内部提取
- 修改: 所有具体 ViewModel — 移除对 `services.XXX` 的直接访问，改用基类属性

### 验证标准

1. `dotnet build LYBTZYZS.sln` 通过
2. 所有 Desktop 测试通过
3. 具体 ViewModel 不再直接引用 `IViewModelServices` 的任何属性（仅传给基类）

---

## [S4] P2: 扁平化 ViewModel 继承链

### 问题

4层继承链（CoreViewModelBase → NavigableViewModelBase → MasterDetailViewModelBase → 具体VM），每层仅增加少量职责但引入12个可重写钩子。

### 方案: 合并 CoreViewModelBase 和 NavigableViewModelBase

**理由**: `CoreViewModelBase` 仅提供 `IsBusy`/`StatusMessage`/`ErrorMessage` 三个属性 + `SetBusy`/`SetError`/`ClearError` 三个方法。这些功能完全可以在 `NavigableViewModelBase` 中直接实现。

**合并后的层次**:

```
NavigableViewModelBase (合并 CoreViewModelBase)
  └─ MasterDetailViewModelBase<TListItem, TDetail>
       └─ 具体 ViewModel
```

3层 → 3层，但每层职责更清晰：
- `NavigableViewModelBase`: 状态管理 + 导航 + 编辑 + 对话框
- `MasterDetailViewModelBase`: 列表/详情/CRUD 命令
- 具体 ViewModel: 业务逻辑

### 合并步骤

1. 将 `CoreViewModelBase` 的所有成员（~170行）合并到 `NavigableViewModelBase`
2. 删除 `CoreViewModelBase.cs`
3. 更新所有引用 `CoreViewModelBase` 的文件（~30个）
4. `MasterDetailViewModelBase` 的基类从 `NavigableViewModelBase` 改为直接继承合并后的 `NavigableViewModelBase`

### 特殊情况

`LoginViewModel` 和 `SystemSettingsViewModel` 继承 `NavigableViewModelBase` 但不使用导航功能。这些可以：
- **选项A**: 保持继承 `NavigableViewModelBase`，忽略导航钩子（当前状态）
- **选项B**: 创建轻量 `BaseViewModel`（仅状态管理），这些简单 VM 继承它

**推荐选项A**：为了2个VM创建新基类不值得。

### 验证标准

1. `dotnet build LYBTZYZS.sln` 通过
2. 所有 Desktop 测试通过
3. 继承链从4层减到3层

---

## [S5] P3: 瘦身 BaseRepository + ApiClientRepositoryBase

### 问题

- Server `BaseRepository` 712行，承担 CRUD+软删+分页+模板方法+排序+投影+异常
- Desktop `ApiClientRepositoryBase` 4个泛型参数，多数场景 TCreateDto=TUpdateDto

### 方案 A: Server BaseRepository 拆分

**当前**: 一个 712 行的基类包含所有功能

**拆分为 3 个关注点**:

```
BaseRepository<TEntity>                 (~200行) — 核心CRUD + 软删除
  + 分页扩展方法 GetPagedResultAsync    (~50行)  — 扩展方法或静态工具
  + 模板方法 ApplyKeywordFilter/Ordering         — 保持在基类
```

具体操作：
1. 将 `GetPagedResultAsync` 移为 `IQueryable<T>` 的扩展方法（`QueryablePagingExtensions`）
2. 将 `SelectAsync` 投影查询移为扩展方法
3. 将 `FindAsync` 的 5 参数重载简化（当前有2个 FindAsync 重载）
4. 保留核心 CRUD + 模板方法在基类

**目标**: BaseRepository 从 712 行降到 ~350 行

### 方案 B: Desktop ApiClientRepositoryBase 简化泛型

**当前**:
```csharp
ApiClientRepositoryBase<TListDto, TDetailDto, TCreateDto, TUpdateDto>
```

**简化为 2 泛型参数**:
```csharp
ApiClientRepositoryBase<TListDto, TDetailDto>
{
    // 标准 CRUD 方法使用 TDetailDto 作为输入
    // 子类重写 CreateAsync/UpdateAsync 处理 InputDto 转换
}
```

**理由**: 6 个 Repository 中，4 个的 TCreateDto=TUpdateDto。剩余 2 个（MedicalCase、Registration）的 Create/Update 也可以在子类方法中处理。

**具体步骤**:
1. 将 `ApiClientRepositoryBase` 从 4 泛型改为 2 泛型
2. `CallApiCreateAsync`/`CallApiUpdateAsync` 的参数改为 `object` 或使用泛型方法
3. 更新 6 个子类的声明

### 验证标准

1. `dotnet build LYBTZYZS.sln` 通过
2. 所有 Server + Desktop 测试通过
3. BaseRepository 行数 < 400

---

## [S6] P4: 精简 DTO 继承 + CrossModule 接口

### 问题

- DTO 4层继承链（BaseDto → TimestampDto → StatusDto），StatusDto 几乎无使用
- CrossModule 4个域接口+旧接口共存，每个接口仅1-3方法

### 方案 A: DTO 继承精简

**当前继承链**:
```
BaseDto (Id)
  └─ TimestampDto (CreatedAt, UpdatedAt, CreatedBy) + IAuditable + ICreatorTrackable
       └─ StatusDto (Status, IsEnabled) + IStatusManageable
```

**评估**:
- `BaseDto` — 仅 `Id` 字段，但所有 DTO 都需要它，保留合理
- `TimestampDto` — 所有业务 DTO 继承它，保留合理
- `StatusDto` — 仅少数 DTO 使用，可以移除继承层，改为在具体 DTO 中直接添加 `Status` 属性

**操作**:
1. 统计哪些 DTO 继承 `StatusDto`（预期 < 5 个）
2. 这些 DTO 改为继承 `TimestampDto`，直接添加 `Status` 属性
3. 保留 `IStatusManageable` 接口（如果需要多态）
4. 删除 `StatusDto` 类

**接口精简**:
- `ICreatorTrackable` 仅 `CreatedBy` 1 个字段 → 合并到 `IAuditable`
- `IAuditable` 从 2 字段（CreatedAt, UpdatedAt）扩展为 3 字段（+CreatedBy）
- 删除 `ICreatorTrackable` 接口

**合并后的层次**:
```
BaseDto (Id)
  └─ TimestampDto (CreatedAt, UpdatedAt, CreatedBy) + IAuditable
```

3层 → 2层，接口从 4 个减到 2 个（IIdentifiable + IAuditable）。

### 方案 B: CrossModule 接口合并

**当前**:
```
ICrossModuleService [Obsolete]
IPatientCrossModuleService (2 methods)
IHerbCrossModuleService (2 methods)
IUserCrossModuleService (3 methods)
ICrossModuleAuthService (1 method)
```

**方案**: 合并为 1 个接口 + 按需拆分

```csharp
// 方案 B1: 单一接口（推荐，方法总数 < 10）
public interface ICrossModuleService
{
    // Patient
    Task<PatientBasicInfo?> GetPatientBasicInfoAsync(Guid patientId, CancellationToken ct = default);
    Task<bool> HasPatientReferencesAsync(Guid patientId, CancellationToken ct = default);

    // Herb
    Task<HerbBasicInfo?> GetHerbBasicInfoAsync(Guid herbId, CancellationToken ct = default);
    Task<bool> HasHerbReferencesAsync(Guid herbId, CancellationToken ct = default);

    // User
    Task<UserBasicInfo?> GetUserBasicInfoAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ValidateUserCredentialsAsync(string username, string password, CancellationToken ct = default);

    // Auth
    Task RevokeTokenFamilyAsync(string familyId, CancellationToken ct = default);
}
```

**理由**: 总共 7 个方法，放在 1 个接口中比 5 个接口更清晰。模块按需注入，不影响隔离。

### 验证标准

1. `dotnet build LYBTZYZS.sln` 通过
2. DTO 继承链从 4 层减到 2 层
3. 接口从 4+1 个减到 1 个

---

## [S7] 不在本方案范围的已知问题

以下问题已有独立 spec 或属于 v2.0 规划：

| 问题 | 已有 Spec | 状态 |
|------|----------|------|
| R2 Controller 去重 | `2026-06-29-controller-deduplication-spec.md` | 待实施 |
| A3 BaseApiController 移动 | `2026-06-29-server-core-fix-spec.md` | 待实施 |
| D3 BaseService 职责分离 | `2026-06-29-server-core-fix-spec.md` | 待实施 |
| R1 Server/Desktop 双 Repository | 架构固有特征（两端协议不同） | 保持现状 |
| R3 模块三层结构重复 | 模块化架构固有特征 | 保持现状 |

## [S8] 实施优先级与依赖

```
P1 (IViewModelServices 拆分)
 └─→ P2 (ViewModel 继承链扁平化)     // P2 依赖 P1

P3 (BaseRepository 瘦身)              // 独立
P4 (DTO + CrossModule 精简)           // 独立
```

**推荐顺序**: P3 → P4 → P1 → P2

**理由**:
- P3/P4 风险最低，可独立验证
- P1 影响面最广（51个VM），需要充分测试
- P2 依赖 P1 完成

## [S9] 成功标准

1. ViewModel 继承链 ≤ 3 层
2. 具体 ViewModel 不直接引用 `IViewModelServices` 属性
3. Server BaseRepository < 400 行
4. Desktop ApiClientRepositoryBase ≤ 2 泛型参数
5. DTO 继承链 ≤ 2 层
6. CrossModule 接口 ≤ 2 个（含旧接口完全移除）
7. `dotnet build` + 全部测试通过
