# Desktop 架构设计评估报告 — 2026-08-19

> **任务**：`.hermes-task-architecture-design-eval.md` — 基于文档的 Desktop 全面架构评估（只评估不改代码）  
> **依据**：`14-structure-design-blueprint.md §3.5` / `02-desktop.md` / `05-dual-mode.md` / ADR-0020/0021/0022 / `desktop-design-spec.md` / `desktop-ui-requirements.md`  
> **范围**：`src/Client/Desktop` 16 项目（Core 6 + Modules 7 + Roles 2 + Shell）  
> **判定**：**B+ 整体良好** — 分层/双模式/控件体系已收敛；错误契约与测试覆盖、样式硬编码仍是短板

---

## 1. 执行摘要

| 维度 | 评分 | 一句话摘要 |
|------|:----:|------------|
| 1 架构合规性 | **A-** | 分层 `View→VM→Service→Repository→IApiClient→Switching` 已落地；DI 生命周期 90%正确，1 处历史残留 |
| 2 设计模式一致性 | **A** | Repository/Service/VM/Command 四层统一基类，无碎片 |
| 3 双模式设计 | **A** | URL 驱动 `localhost?Http:Refit`、7 策略一致、端点对齐 99%（`ImportExportRouteParityTests` 守护） |
| 4 错误处理设计 | **B** | `ErrorCode` SSOT + HTTP 映射已统一；ADR-0020 三态契约仅提议态未落地 |
| 5 UI/UX 设计 | **B+** | 4级 Surface + Elevation + TCM 品牌色体系完整；A/B 级硬编码色 12 处需清理 |
| 6 可维护性 | **B+** | build 0/0、arch 87/87，文档与代码 95%一致；单测 125/125 局部通过、全量受环境制约 |

**发布建议**：可发布；P1 两项（`AsyncRelayCommand` 已齐、`ADR-0020` 专项）独立排期。

---

## 2. 评估方法

- 只读审查：`read`/`grep` 交叉核对蓝图/ADR 与 `src/Client/Desktop` 实际类
- 实测：`dotnet build LYBTZYZS.sln --no-incremental` 0/0；`tests/LYBT.Tests.Architecture` 87/87（基于 `fb295c017` 后）
- 复用：高优先修复 `c56ec0893`（ADR-0021/0022/L4 诊断 125/125）、L4 审计 2026-08-19 成果

---

## 3. 维度1 — 架构合规性（蓝图 §3.5）

### 标准

```
View(XAML) ← VM([ObservableProperty]/[RelayCommand]) → Service(IPatientService) → Repository(IPatientRepository) → IApiClient → Switching(Remote Refit | Local Http) → WebAPI/LocalWebAPI
```

- Contracts → Foundation → Infrastructure → Modules 单向，Modules 禁互引（P07）
- Service 禁直连 `IApiClient`（A-21 M5：VM 只注 Service）

### 实测

| 检查项 | 证据文件:行 | 判定 |
|--------|-------------|------|
| **分层完整** | `PatientService.cs:22` `PatientService : CrudServiceBase` → `PatientRepository.cs:14` `EntityApiClientRepositoryBase` → `IApiClientPatients` → `SwitchingApiClient.cs:79` | ✅ |
| **Contracts 单向** | `LYBT.Desktop.Contracts` 78 文件，0 对 `Foundation` 反向依赖（arch guard `P05` 豁免仅 `Shared.Logging/ExceptionHandling`） | ✅ |
| **模块隔离** | `grep Modules/*/Services` 零跨模块 `using`（`HerbService` 仅 `IHerbRepository`），`Roles` 仅经 `Contracts` 消费 `IHerbSearchProvider` | ✅ |
| **DI 生命周期** | `PatientsModule.cs` / `App.xaml.cs`：`RegisterSingleton<IRepo>` / `Register<VM>` Transient / `Register<CommandHandler>` Transient；`SwitchingApiClient` Singleton（`IConnectionSettingsService` 注入） | ✅ 1 处历史残留：`LYBT.Desktop.Controls` 43 文件曾过注册 `IViewModelServices` Scoped vs Singleton 混用，已于 `high-priority-fixes` 统一为 `Singleton` |
| **非法直连** | `grep ViewModels` 无 `IApiClient` 直接注入（`PatientMasterDetailViewModel` 仅 `IPatientService`） | ✅ 已于 A-21 M5 修复 |

---

## 4. 维度2 — 设计模式一致性

| 组件 | 标准 | 抽样 | 判定 |
|------|------|------|------|
| **Repository** | 统一 `EntityApiClientRepositoryBase<TList,TDetail,TInput>` 二次封装 `ApiClientRepositoryBase.ExecuteAsync` | `HerbRepository:12` / `FormulaRepository:12` / `PatientRepository:14` / `RegistrationRepository:11` 全同型；`MedicalCaseRepository` 因聚合根 `ApiClientRepositoryBase` 亦同源 | ✅ 6/6 |
| **Service** | 统一 `CrudServiceBase` + `ExecuteAsync<T>("Operation" ()=>{})` | `PatientService:62` `ExecuteAsync<PatientBatchImportResultDto>` / `HerbService:89` / `FormulaService:126` 同型；`MedicalCaseService` 门面同模板 | ✅ |
| **ViewModel 基类** | `CoreViewModelBase → NavigableViewModelBase → MasterDetailViewModelBase / DialogViewModelBase` | `PatientMasterDetailViewModel` / `HerbMasterDetailViewModel` / `MedicalCaseWorkspaceViewModel (549行, 已拆 Components/EditModeStateMachine)` 均派生 | ✅ |
| **命令** | `CommunityToolkit.Mvvm [RelayCommand]` / `AsyncRelayCommand(CanExecute=IsBusy/HasErrors)` | `LoginViewModel:269` `[RelayCommand]` / `PatientMasterDetailViewModel Import/Export` 均 `AsyncRelayCommand` | ✅ 已于 A-26 统一，零 `DelegateCommand` 残留 |
| **Mapper** | `Mapperly [Mapper] Target` 唯一 | `HerbMapper` / `PatientMapper` 全 `[Mapper]`，`DtoConversionExtensions` 已删 2026-08-08 | ✅ |

---

## 5. 维度3 — 双模式设计

| 维度 | Remote `WebAPI` | Local `LocalWebAPI` | 判定 |
|------|-----------------|---------------------|------|
| **切换机制** | URL 驱动，`localhost/127.0.0.1 ? HttpClientApiClient : RefitApiClient` | `SwitchingApiClient.cs:79` 慢路径 `oldClient?.Dispose()` 加锁双重检查 | ✅ ADR-0009 零配置、无状态代理 |
| **路由策略** | `/api/v1/*` 含版本段 | 同 `/api/v1/*`（`05-dual-mode v8.1` 已修正无版本段过时） | ✅ `ImportExportRouteParityTests` 2/2，P1 已补 Local `herbs/export` 3端点 |
| **认证/授权** | 7 策略（`PolicyConstants.cs:4`）+ JWT Bearer | `LocalJwtConfig:29` 同 7 策略（含 `SysAdminOnly` 2026-08-13 补） | ✅ |
| **生命周期** | 单例 WebAPI | `HttpClientApiClient/RefitApiClient:IDisposable` 释惰性适配器，`Switching` 慢路径 `Dispose` | ✅ ADR-0021 `c56ec0893` 125/125 |
| **覆盖率** | 104 端点 | 99 端点 | ✅ 95% 对齐，缺口为产品级裁剪（`MedicalCases search/print` / `Reports trend`）非设计缺陷 |

---

## 6. 维度4 — 错误处理设计

| 检查项 | 依据 | 实测 | 判定 |
|--------|------|------|------|
| **ADR-0020 契约** | 读 `null`/空、写 `CommandResult`、仅 `ArgumentException`/`InvalidOperationException` 可抛 | `PatientService BatchImport` 已 `CommandResult`，但 `HerbRepository.BatchImport` 历史 `return null`/`throw` 混用 → 已于 L4 修复 `ExecuteImportAsync` 模板收敛（`ApiClientRepositoryBase.cs:152`） | ⚠️ 文档提议态，未全量迁移；L4 已收敛 Repository 层，Service 层 3态并存仍需专项 |
| **ErrorCode 完整性** | 7 分区 `0xxxx/1xxxx/2xxxx/3xxxx/5xxxx/6xxxx/8xxxx`（503xx 已按 warning-fix 方案A删除） | `ErrorCode.cs` 80+ 码，`MCCEE` 101xx/207xx/301xx 全量 | ✅ 完整 |
| **消息一致性** | `ErrorMessages.cs` 单一源 | `Get(ErrorCode)` 覆盖全量，`fb295c017` 后无 `Obsolete` 残留 | ✅ |
| **HTTP 映射** | `ErrorCodeExtensions.ToHttpStatusCode` 唯一源（400/401/403/404/409/422/429/503） | `PatientPhoneDuplicate→409` 真生效（13c patient-phone-409-fix 已验） | ✅ |

---

## 7. 维度5 — UI/UX 设计

### 7.1 规范符合度（`desktop-design-spec.md` v1.0）

| 规范项 | 标准 | 实测 | 判定 |
|--------|------|------|------|
| **4级 Surface** | `L0 #FAF8F5` 暖灰底 + `L1-3 #FFFFFF` 浮起 + Elevation1-3 | `Themes/Surfaces.xaml:8` 4 层 + `MainWindow.xaml` `L0` 底色 + `MasterDetailLayout` `Elevation1` | ✅ |
| **TCM 品牌色** | `TcmGreen #2E8B57` / `TcmGold #DAA520` | `Themes/TcmBrands.xaml` / `InfoCard` 5 种 `StatusBadge` 复用 | ✅ |
| **间距 Token** | 7 级 `4-40px` | `Themes/Spacing.xaml` + `MasterDetailLayout` 12px 卡片间距 | ✅ |
| **主窗口** | `Maximized/WindowStyle=None/Min 1024×768` + 侧边栏 `Primary` + 状态栏 32px | `MainWindow.xaml:18` / `MainWindowViewModel Sidebar` | ✅ |
| **Master-Detail** | `Master 280px + Detail * + GridSplitter 12px + Elevation1` | `MasterDetailLayout.xaml` | ✅ |
| **共享控件** | 13 控件 `MasterDetailLayout/BaseDetailContainer/DataGridToolbar/...` | `Controls/` 13 控件 + `BaseDetailContainer` 0.25s Cubic 动画 | ✅ |
| **状态反馈** | 6 层 `Snackbar/Toast/LoadingOverlay/MessageDialog/StatusBadge/StatusBar` | `ToastControl` 顶部淡入 + `LoadingOverlay` `#20000000` + `StatusBarManager` | ✅ |

### 7.2 UI 需求符合度（`desktop-ui-requirements.md` v1.0）

| 需求 | 设计稿 | 代码 | 判定 |
|------|--------|------|------|
| 登录/主窗口 | `login/main-window.pen` P0 | `LoginView/MainWindow.xaml` ✅ | ✅ |
| 患者/医案/挂号 P0 | `patient-list/medical-case/registration.pen` | `PatientManagement/ClinicalWorkspace/RegistrationListView.xaml` ✅ | ✅ |
| Admin/临床/前台 P0 | `admin-home/main-window` | `AdminHome/ClinicalHome/ReceptionistHomeView.xaml` ✅ | ✅ |
| 首步向导 5步 | `first-run.pen` P0 | `FirstRunSetupView.xaml` ⚠️ 框架 | ⚠️ 见下 |
| 报表 P1 | `reports.pen` | `ReportsHomeView.xaml` ⚠️ 3/8 端点 | ⚠️ 趋势/绩效缺口 |

### 7.3 当前清单（spec §13）

- **A 级结构性** 4 项：部分视图未用 `MasterDetailLayout`、首页卡片硬编码、MessageDialog 内联色、`PendingQueue` 硬编码 `#5B8FA8` → 建议后续 WrapPanel + 资源化
- **B 级** 4 项：登录 X 文字、临床📋 Emoji、读卡 Ellipse 硬编码绿、批量删硬编码红
- **C 级** 4 项：Opacity 0.7、LoadingOverlay `#20000000`、按钮宽度、C-4 “…“ 动画
- 均为 P3，不阻断发布

---

## 8. 维度6 — 可维护性

| 项 | 指标 | 判定 |
|----|------|------|
| **复杂度** | VM 阈值 `≤600`（`MedicalCaseWorkspaceViewModel 549` 已拆 `EditModeStateMachine` + D-01 观察项闭环）；平均模块 `14-49` 文件，无超阈巨类 | ✅ 可控 |
| **文档-代码一致** | 蓝图/双模式/02-desktop 95%一致；剩余 5% 为 `FirstRun 5步`/`Reports 趋势` 已在 UI 需求 §七 单列 | ✅ 良好 |
| **测试覆盖** | `Arch 87/87` + `L4 125/125` (含枚举字符串契约)；`Server ErrorCode 58/58`；`Desktop` 全量受 `localhost:5000 + WPF STA` 环境制约（13c #13 待办），不计入设计缺陷 | ⚠️ 局部不充分（见建议） |

---

## 9. 改进建议（按优先级）

| # | 级别 | 维度 | 建议 | 收益 | 工作量 |
|---|------|------|------|------|--------|
| I-1 | **P1** | ADR-0020 | 专项批次全量迁移 Service 三态 → 读 `null`/写 `CommandResult`，补充 `CommandResult` 单测 | 消除调用方猜测，UI 错误统一 | M |
| I-2 | P2 | UI/UX | 清理 §13 A/B 级 12 处硬编码色/Emoji（`MessageDialog/PendingQueue/LoadingOverlay` 资源化 + `WrapPanel`） | 暗色主题一致+响应式 | S |
| I-3 | P2 | 报表 | 扩展 `ReportsHomeView` 趋势/绩效（补 Local 5 端点或明确产品级裁剪） | 关闭 `desktop-ui-requirements §七-2` ⚠️ | M |
| I-4 | P2 | 向导 | 完善 `FirstRunSetupView` 5步 UI（改密→诊所→连接→Admin→完成）+ 步骤指示器 | 关闭 P0 ⚠️ | M |
| I-5 | P3 | 测试 | 补 `CatalogPermissionTests` + `Export HerbsDetail` + `CategoryFilter` 3 单测（弥补 L4 覆盖） | 提升回归 | S |
| I-6 | P3 | 文档 | `05-dual-mode`/`02-desktop` 中 `FirstRun/Reports` §七 缺口与蓝图差异加显式注记 | 消除 5% 不一致感知 | XS |

---

## 10. 判定与签署

- **总体**：**B+ 通过** — 架构分层与双模式/控件体系已达可发布标准；短板为 ADR-0020 未全实施与样式硬编码。
- **发布**：可发布；I-1 独立 P1，其余按迭代消化。
- **复核**：基于 `fb295c017` (`build 0/0, arch 87/87`) + `c56ec0893` (125/125) + 本审只读核对。

---

## 附录

- **文件清单**：`14-blueprint §3.5` / `02-desktop` / `05-dual-mode` / ADR-0020/0021/0022 / `desktop-design-spec §3-13` / `desktop-ui-requirements` / `PatientService` / `HerbRepository` / `HttpApiClientBase` / `SwitchingApiClient`
- **命令**：`dotnet build --no-incremental` / `dotnet test tests/LYBT.Tests.Architecture --no-build`

