# LYBTZYZS Desktop 架构审计报告

> **日期**: 2026-08-14  
> **范围**: `src/Client/Desktop/` 全量 Desktop 项目（15 个 .csproj，~12,275 .cs 文件，~269K 行代码）  
> **基线**: 构建状态 0 错误 0 警告 | 架构测试 87 条  
> **审计方法**: 代码静态分析 + 依赖图扫描 + 模式匹配 + 结构对比

---

## 总览评分

| 维度 | 评级 | 关键发现数 |
|------|------|-----------|
| 1. 项目结构 | ✅ 健康 | 0 P0 / 1 P2 |
| 2. 依赖方向 | ✅ 健康 | 0 P0 / 0 P1 |
| 3. MVVM 规范 | ⚠️ 需改进 | 0 P0 / 2 P1 / 1 P2 |
| 4. Dual-mode | ✅ 健康 | 0 P0 / 1 P2 |
| 5. 数据访问 | ⚠️ 需改进 | 0 P0 / 1 P1 / 1 P2 |
| 6. 导航 | ✅ 健康 | 0 P0 / 0 P1 |
| 7. 模块隔离 | ✅ 健康 | 0 P0 / 0 P1 |
| 8. 配置管理 | ⚠️ 需改进 | 0 P0 / 1 P1 / 1 P2 |
| 9. 代码质量 | ⚠️ 需改进 | 0 P0 / 2 P1 / 2 P2 |
| 10. 测试覆盖 | ⚠️ 需改进 | 0 P0 / 2 P1 / 1 P2 |
| **合计** | **基本健康** | **0 P0 / 10 P1 / 9 P2** |

---

## 1. 项目结构

### 现状

Desktop 采用标准 Prism 四层结构，职责清晰：

```
Shell (1 项目)          → 入口、DI、模块加载
├── Roles (2 项目)      → Admin / Clinical 角色工作空间
│   └── Modules (6 项目) → Auth / Catalog / MedicalCase / Patients / Registrations / Users
├── Core (5 项目)       → Contracts → Foundation → Infrastructure → Controls → Printing
└── LocalWebAPI (1 项目)→ 本地模式嵌入式 WebAPI
```

**文件分布**：
| 层 | .cs 文件数 | 占比 |
|----|-----------|------|
| Core | 299 | 59.1% |
| Modules | 132 | 26.1% |
| Shell | 49 | 9.7% |
| Roles | 49 | 9.7% |
| LocalWebAPI | 23 | 4.5% |

**关键文件**：
- 入口：`Shell/App.xaml.cs`
- DI 扫描：`Shell/Extensions/DataSourceRegistrationExtensions.cs`
- 导航协调：`Core/Infrastructure/Navigation/NavigationCoordinator.cs`
- API 代理：`Core/Foundation/Http/SwitchingApiClient.cs`

### 评估

✅ **层级职责清晰**：Shell → Roles → Modules → Core(Infrastructure → Foundation → Contracts) 依赖方向严格  
✅ **Core 层 5 项目细分合理**：Contracts(接口定义) / Foundation(基础服务实现) / Infrastructure(WPF基类) / Controls(自定义控件) / Printing(打印)

### 发现

| # | 级别 | 问题 | 建议 |
|---|------|------|------|
| D-01 | P2 | Core 层 299 文件占比 59%，Foundation(406 行 CredentialVault) 和 Infrastructure(132 文件) 是最大层 | 持续观察，当前无具体问题 |

---

## 2. 依赖方向

### 依赖图（已验证）

```
Shell ──→ Auth, Users, Patients, MedicalCase, Catalog, Registrations, Admin, Clinical, LocalWebAPI, Foundation, Infrastructure
Admin ──→ Catalog, Patients, MedicalCase, Users, Foundation, Infrastructure, Contracts
Clinical ──→ Catalog, Patients, MedicalCase, Registrations, Foundation, Infrastructure, Contracts
Auth ──→ Foundation, Infrastructure, Contracts
Catalog ──→ Foundation, Infrastructure, Contracts
MedicalCase ──→ Foundation, Infrastructure, Printing, Contracts
Patients ──→ Infrastructure, Contracts
Registrations ──→ Infrastructure, Contracts
Users ──→ Foundation, Infrastructure, Contracts
```

### 评估

✅ **严格单向依赖**：Shell → Roles → Modules → Core，无逆向引用  
✅ **无循环引用**：所有模块引用 Core 层和 Shared.Models，模块间零直接引用  
✅ **P07/P08 守卫有效**：架构测试强制模块间禁止直接引用  
✅ **LocalWebAPI 跨层引用合理**：引用 7 个 Server Module（`LYBT.Module.Identity/Patients/Catalog/MedicalCases/Registration/Reports` + `LYBT.Infrastructure`），这是 ADR-0010 已批准的 Local 模式设计（嵌入式 WebAPI 需复用 Server 业务逻辑）

### 发现

| # | 级别 | 问题 | 建议 |
|---|------|------|------|
| — | — | 无问题 | — |

---

## 3. MVVM 规范

### ViewModel 基类分布

| 基类 | VM 数 | 说明 |
|------|-------|------|
| NavigableViewModelBase | 23 | 标准单实体导航 |
| DialogViewModelBase | 7 | 对话框 |
| MasterDetailViewModelBase | 5 | 列表/详情 |
| ChildViewModelBase | 5 | 子 VM |
| EditorViewModelBase | 4 | 编辑器 |
| ConnectionTestViewModelBase | 2 | 连接测试 |
| ObservableObject | 2 | 简单 VM |
| HerbItemViewModelBase | 1 | 药材项目 |
| BindableBase | 1 | Prism 基类（非推荐） |

### View/ViewModel 匹配

- **Views**: 25 个 .xaml
- **ViewModels**: 50 个（含 Editor/Dialog/Control 等子组件，非全部需要独立 View）
- **ViewModelLocator.AutoWireViewModel**: 已正确配置

### IApiClient 越层检查

✅ **零子接口注入**：所有 ViewModel 均未注入 `IApiClient*` 子接口（DP10 规则通过）  
✅ **注入 Service 接口**：VM 注入的是业务 Service 接口（`IUserService`, `IPatientService`, `IMedicalCaseService` 等），符合分层纪律

### 发现

| # | 级别 | 问题 | 建议 |
|---|------|------|------|
| M-01 | P1 | `HistoryCopyDialogViewModel`(559 行) 直接注入 `IMedicalCaseRepository`，跳过 Service 层（Repository 是底层，VM 不应直接调用） | 改为注入 `IMedicalCaseQueryService` 或对应 Service 接口 |
| M-02 | P1 | 1 个 VM 使用 `BindableBase`（Prism 原生基类）而非 `CommunityToolkit.Mvvm` 的 `ObservableObject` | 统一使用 `ObservableObject` 或项目推荐基类 |
| M-03 | P2 | `PatientSelectionViewModel`(481 行)、`MedicalCaseWorkspaceViewModel`(592 行) 文件较大，职责可能过重 | 关注是否可拆分为更小的子 VM（当前已有 ChildViewModelBase 模式） |

---

## 4. Dual-mode（远程/本地切换）

### 架构

```
用户操作 → IConnectionModeService.SetModeAsync(mode)
         → SwitchingApiClient 切换内部实现
         → Remote: Refit IApiXxx → HTTP → WebAPI
         → Local: IApiXxx → LocalWebAPI → LocalDB
```

**关键组件**：
- `ConnectionModeService`：模式管理，Remote/Local 二选一（Auto 已移除，A-19 决策）
- `SwitchingApiClient`：运行时可切换的 IApiClient 代理
- `EmbeddedLocalWebApiService`：启动/停止本地嵌入式 WebAPI

### 评估

✅ **Auto 模式已移除**：`ConnectionMode` 枚举只有 `Remote`/`Local`，无自动降级（A-19 已落地）  
✅ **用户自主切换**：`ConnectionStatusViewModel` 提供手动切换按钮  
✅ **配置统一**：`appsettings.json` / `appsettings.Production.json` 定义 URL 和密钥  
✅ **凭证安全**：`CredentialVault` 用 DPAPI + HMAC 保护本地密码存储

### 发现

| # | 级别 | 问题 | 建议 |
|---|------|------|------|
| DM-01 | P2 | `FirstRunSetupViewModel` 中 `SetModeAsync` 在 `ConnectRemoteAsync` 后用 `GetAwaiter().GetResult()` 同步阻塞（L109），可能在 UI 线程造成卡顿 | 改为异步等待 |

---

## 5. 数据访问

### Desktop 数据访问模式

Desktop 作为客户端，数据访问通过两种路径：

1. **远程模式**：Repository → `IApiClient` → Refit HTTP → WebAPI → Server Repository/DB
2. **本地模式**：Repository → LocalWebAPI（嵌入 ASP.NET Core） → EF Core → LocalDB

**Repository 层**：
- 所有 Repository 实现（如 `UserRepository`, `PatientRepository`）通过 `IApiClient` 调用 API
- 本地模式下 `IApiClient` 内部路由到 `LocalWebAPI` 控制器

### 评估

✅ **Repository → Service → ViewModel** 分层纪律良好  
✅ **无直接 DbContext 注入到 Service 层**（Desktop Service 通过 API Client 获取数据）  
✅ **LocalWebAPI 使用 AppDbContext**（嵌入式宿主，直接操作 DB，设计合理）

### 发现

| # | 级别 | 问题 | 建议 |
|---|------|------|------|
| DA-01 | P1 | `HistoryCopyDialogViewModel` 直接注入 `IMedicalCaseRepository`，绕过 Service 层 | 移入 Service 层（与 M-01 同一问题） |
| DA-02 | P2 | `LocalWebApiSeedData` 直接操作 `DbContext` 做种子数据 | 可接受（种子数据初始化场景），记录为设计例外 |

---

## 6. 导航

### 导航架构

```
RegionNames (常量)
├── ContentRegion → 主内容区
├── LoginRegion → 登录区
NavigationCoordinator → 前进/后退栈管理
INavigationCoordinator → ViewModelServices.RegionManager
```

**导航模式**：
- **Region-based**：使用 Prism `IRegionManager.RequestNavigate("ContentRegion", nameof(SomeView))`
- **View 名称引用**：通过 `nameof(Views.XxxView)` 引用，**无硬编码字符串路由**
- **导航栈**：`NavigationCoordinator` 管理前进/后退栈（`NavigateBack`/`NavigateForward`）
- **面包屑**：`BreadcrumbBar` 自定义控件支持 `NavigateCommand`

### 评估

✅ **无硬编码路由字符串**：所有 View 导引用 `nameof()` 引用，编译安全  
✅ **RegionNames 常量集中定义**：`ContentRegion` / `LoginRegion` 统一管理  
✅ **导航服务封装**：`INavigationCoordinator` 接口封装导航操作，VM 不直接操作 `IRegionManager`  
✅ **前进/后退栈完整**：支持用户导航历史

### 发现

| # | 级别 | 问题 | 建议 |
|---|------|------|------|
| — | — | 无问题 | — |

---

## 7. 模块隔离

### 模块间引用检查

| 模块 | 引用的其他模块 | 状态 |
|------|--------------|------|
| Auth | 无 | ✅ |
| Catalog | 无 | ✅ |
| MedicalCase | 无 | ✅ |
| Patients | 无 | ✅ |
| Registrations | 无 | ✅ |
| Users | 无 | ✅ |
| Admin | Catalog, Patients, MedicalCase, Users | ✅ (Roles 可引用 Modules) |
| Clinical | Catalog, Patients, MedicalCase, Registrations | ✅ (Roles 可引用 Modules) |

### 跨模块通信

- **Event Aggregator**：24 个文件使用 `IEventAggregator` / `PubSubEvent` 进行跨模块通信
- **Service 接口**：跨模块服务通过 `IUserService`, `IPatientService` 等接口定义在 `Contracts` 层
- **Contracts 层**：所有共享接口下沉到 `LYBT.Desktop.Contracts`，模块通过接口解耦

### 评估

✅ **P07 模块间零直接引用**（架构测试 87/87 通过）  
✅ **P08 跨模块用接口**：Service 接口在 Contracts，实现在各 Module  
✅ **Roles → Modules 引用合理**：Admin/Clinical 作为角色工作空间引用业务模块是设计意图  
✅ **Event Aggregator 使用规范**：24 文件使用事件总线进行松耦合通信

### 发现

| # | 级别 | 问题 | 建议 |
|---|------|------|------|
| — | — | 无问题 | — |

---

## 8. 配置管理

### 配置文件

| 文件 | 用途 |
|------|------|
| `Shell/appsettings.json` | 主配置（URL、JWT、默认密码） |
| `Shell/appsettings.Production.json` | 生产覆盖（安全配置） |
| `LocalWebAPI/appsettings.json` | 本地 WebAPI 配置 |

### 敏感配置处理

- **CredentialVault**：用 `System.Security.Cryptography.ProtectedData` (DPAPI) 加密密码存储
- **JWT SecretKey**：从 `appsettings.json` 读取，`LocalTokenValidator` 验证长度 ≥32 字符
- **DefaultPasswords**：通过 `DefaultPasswordOptions` 配置绑定，运行时注入到 LocalWebAPI
- **ConfigurationWritePolicy**：服务端限制敏感键不可通过 API 修改（ConnectionStrings/JwtSecretKey/DefaultPasswords）
- **SensitiveData 属性**：JSON 序列化时自动脱敏

### 评估

✅ **配置分层**：`appsettings.json` → `appsettings.Production.json` 覆盖模式  
✅ **密码加密存储**：DPAPI + HMAC 校验，非明文  
✅ **JWT 校验**：`LocalTokenValidator` 检查 SecretKey 长度和有效性  
✅ **发布红线**：`appsettings*.json` 发布时不覆盖（`src/Client/Desktop/Shell/appsettings.Production.json` 在 git 脱敏版）

### 发现

| # | 级别 | 问题 | 建议 |
|---|------|------|------|
| CF-01 | P1 | `LocalWebAPI/appsettings.json` 包含明文连接串 `"DefaultConnection": "..."`，虽然是本地开发配置，但文件在仓库中 | 确认此文件在 `.gitignore` 中，或改为从环境变量读取 |
| CF-02 | P2 | `EmbeddedLocalWebApiService` 中 `LocalConnectionString` 硬编码在代码中（L18-19），非从配置读取 | 改为从 `appsettings.json` 读取 |

---

## 9. 代码质量

### 大文件 Top 10（排除生成代码）

| 文件 | 行数 | 关注点 |
|------|------|--------|
| `LocalWebAPI/Controllers/CatalogController.cs` | 743 | 最大非生成文件，含 29 方法 + 6 override |
| `Clinical/MedicalCaseWorkspaceViewModel.cs` | 592 | 复杂工作空间 VM |
| `MedicalCase/HistoryCopyDialogViewModel.cs` | 559 | 对话框 VM 过大 |
| `Foundation/Http/TokenRefreshHandler.cs` | 535 | Token 刷新逻辑复杂 |
| `MedicalCase/Workspace/MedicalCaseCommandsViewModel.cs` | 519 | 医案命令 VM |
| `Controls/HerbList/HerbListControlViewModel.cs` | 492 | 药材列表控制 |
| `Clinical/PatientSelectionViewModel.cs` | 481 | 患者选择 VM |
| `MedicalCase/Items/PrescriptionItemViewModel.cs` | 456 | 处方项 VM |
| `Clinical/CardReaderViewModel.cs` | 449 | 读卡器 VM |
| `Admin/SystemSettingsViewModel.cs` | 414 | 系统设置 VM |

### 死代码检查

- **零 IApiClient 子接口注入**：已清理干净
- **无孤儿 View/ViewModel**：所有 View 有对应 ViewModel（ViewModel 可独立存在如 Dialog）
- **LocalWebAPI**：`UsersController.cs` 为 0 方法空控制器（可能待实现或已废弃）

### Mapperly 使用

- Desktop 使用 `Riok.Mapperly` 编译期映射（4 个模块：Catalog, MedicalCase, Patients, Users）
- `PrescriptionMapper` 有大量 `[MapperIgnoreTarget]` / `[MapperIgnoreSource]` 注解（20+），Mapperly 价值降低

### 发现

| # | 级别 | 问题 | 建议 |
|---|------|------|------|
| CQ-01 | P1 | `CatalogController.cs` 743 行 29 方法，LocalWebAPI 最大 Controller | 考虑拆分或审查是否承载过多职责 |
| CQ-02 | P1 | `UsersController.cs`（LocalWebAPI）为 0 方法空控制器 | 确认是待实现还是废弃，废弃则删除 |
| CQ-03 | P2 | `PrescriptionMapper` 有 20+ Ignore 注解，映射覆盖不完整 | 评估是否应补充映射或使用手动映射替代 |
| CQ-04 | P2 | `HistoryCopyDialogViewModel` 559 行 + `MedicalCaseWorkspaceViewModel` 592 行，超 500 行大 VM | 关注职责单一性，考虑拆分 |

---

## 10. 测试覆盖

### 测试项目

| 项目 | 测试类数 | 说明 |
|------|---------|------|
| `LYBT.Tests.Desktop` | 90 | Desktop 单元测试 + 集成测试 |
| `LYBT.Tests.Architecture` | 7 | 架构守卫（87 条规则） |
| `LYBT.Tests.Server` | 83 | Server 单元测试 |

### Desktop 测试分布

| 目录 | 说明 |
|------|------|
| `Unit/` | 56 测试类（纯 VM 单测，NSubstitute mock） |
| `Integration/` | 34 测试类（LocalWebAPI 控制器，真实 Kestrel + LocalDB） |

### 测试特征

- **440 个 `[Trait]`** 标注（按模块/类型分类）
- **12 个 `[Fact]`** 独立测试
- **架构测试 87 条**：P07 模块边界 / P08 接口 / P10 DbContext / DP10 VM 注入 / AM01-AM03 反 Mock

### 评估

✅ **架构测试完善**：87 条守卫规则覆盖分层/接口/注入/命名  
✅ **VM 单测充足**：56 纯逻辑测试类，无 DB 依赖（T2-2 去耦合后）  
✅ **集成测试覆盖核心模块**：LocalWebAPI 控制器真实 Kestrel 测试

### 发现

| # | 级别 | 问题 | 建议 |
|---|------|------|------|
| T-01 | P1 | E2E 测试依赖 `localhost:5000` 运行中服务，无服务即全挂（已知设计限制） | 考虑用 `WebApplicationFactory` 替代（Server 已有此基建），或明确标注 E2E 需手动启动服务 |
| T-02 | P1 | `LocalWebApiControllerTestBase` 只注册 DbContext/Identity/JWT，缺 `AddMediatR` + 模块服务注册（已知缺口） | 补充完整 DI 注册，或在测试基类中标注已知限制 |
| T-03 | P2 | 无 Desktop 模块级单元测试覆盖导航/事件聚合器交互 | 核心导航逻辑可补充集成测试 |

---

## 发现汇总

### P0（严重）— 0 项

无 P0 级问题。Desktop 架构整体健康，分层纪律严格。

### P1（中等）— 10 项

| # | 维度 | 问题 | 文件 | 建议 |
|---|------|------|------|------|
| M-01 | MVVM | VM 直接注入 Repository（跳过 Service） | `HistoryCopyDialogViewModel.cs` | 改注入 Service 接口 |
| M-02 | MVVM | 1 个 VM 使用非推荐基类 | 1 处 | 统一基类 |
| DA-01 | 数据访问 | VM 直接操作 Repository | 同 M-01 | 同 M-01 |
| CF-01 | 配置 | `appsettings.json` 明文连接串在仓库中 | `LocalWebAPI/appsettings.json` | 确认 `.gitignore` 或环境变量 |
| CQ-01 | 代码质量 | LocalWebAPI 最大 Controller 743 行 | `CatalogController.cs` | 拆分或审查职责 |
| CQ-02 | 代码质量 | 空 Controller（0 方法） | `UsersController.cs` | 确认废弃则删除 |
| T-01 | 测试 | E2E 测试强依赖运行中服务 | `EndToEnd/` | 考虑 WebApplicationFactory |
| T-02 | 测试 | 集成测试基类 DI 不完整 | `LocalWebApiControllerTestBase` | 补充注册 |

### P2（低等）— 9 项

| # | 维度 | 问题 | 建议 |
|---|------|------|------|
| D-01 | 项目结构 | Core 层 299 文件占比大 | 持续观察 |
| DM-01 | Dual-mode | `FirstRunSetupViewModel` 同步阻塞调用 | 改异步 |
| DA-02 | 数据访问 | 种子数据直接操作 DbContext | 记录为设计例外 |
| CF-02 | 配置 | 连接串硬编码在代码中 | 改配置读取 |
| CQ-03 | 代码质量 | Mapperly 大量 Ignore 注解 | 评估映射完整性 |
| CQ-04 | 代码质量 | 2 个 VM 超 500 行 | 关注职责单一 |
| T-03 | 测试 | 导航/事件聚合器交互无测试 | 补充核心测试 |

---

## 优化建议（按优先级排序）

### 高优先级（建议尽快修复）

1. **统一 ViewModel 基类**（M-02）：将 `BindableBase` 改为 `ObservableObject`，保持一致
2. **修复 Repository 越层**（M-01/DA-01）：`HistoryCopyDialogViewModel` 改注入 Service 接口
3. **空 Controller 清理**（CQ-02）：确认 `UsersController.cs`（LocalWebAPI）状态

### 中优先级（建议安排修复）

4. **大文件拆分**（CQ-01/CQ-04）：`CatalogController`(743 行)、`MedicalCaseWorkspaceViewModel`(592 行)
5. **配置安全性**（CF-01/CF-02）：确认 `appsettings.json` 在 `.gitignore` 中
6. **测试基建完善**（T-01/T-02）：补充 `LocalWebApiControllerTestBase` DI 注册

### 低优先级（持续改进）

7. **Mapperly 映射完整性**（CQ-03）：评估 `PrescriptionMapper` Ignore 列表
8. **E2E 测试独立性**（T-01）：考虑 `WebApplicationFactory` 替代方案
9. **Core 层结构观察**（D-01）：299 文件占比大，关注是否需要进一步细分

---

## 架构亮点（值得保持的设计）

1. **模块间零直接引用**：P07/P08 守卫严格，架构测试 87/87 通过
2. **IApiClient 统一代理**：`SwitchingApiClient` 实现运行时模式切换，VM 无感知
3. **导航无硬编码**：所有 View 引用使用 `nameof()` 编译安全
4. **凭证安全**：DPAPI + HMAC 双重保护本地密码存储
5. **Event Aggregator 松耦合**：24 处跨模块通信走事件总线
6. **VM 分层纪律**：50 个 ViewModel 零子接口注入，全部通过 Service 接口获取数据
7. **架构测试守卫**：87 条规则覆盖分层/接口/注入/命名/反 Mock，防止回归

---

> **审计结论**：LYBTZYZS Desktop 架构整体健康，分层纪律严格，无 P0 级问题。10 个 P1 级问题集中在 MVVM 规范细节（1 处 Repository 越层）、测试基建（E2E 依赖）和代码质量（大文件）。建议优先处理 Repository 越层和空 Controller，其余可安排在后续迭代中。
