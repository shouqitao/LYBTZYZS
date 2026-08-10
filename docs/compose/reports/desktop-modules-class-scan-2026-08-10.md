# Desktop 端业务模块类设计扫描报告

> **日期**: 2026-08-10
> **范围**: `src/Client/Desktop/` 全部 15 个项目（6 业务模块 + 2 Roles + 5 Core + LocalWebAPI + Shell）
> **方法**: 8 组并行只读扫描（scout）+ Python 行数脚本复核 + spec §3.3 对标
> **结论性质**: 事实清单 + 可统一项建议，未修改任何代码

---

## 〇、总览

| 项目 | 文件数(.cs) | 行数 | 角色 | VM 基类体系 | Mapperly | 超大类型(>500) |
|---|---|---|---|---|---|---|
| LYBT.Desktop.Auth | 11 | 1,280 | 登录/首次运行/服务器配置 | Navigable/Dialog | 无 | 无 |
| LYBT.Desktop.Users | 14 | 1,764 | 用户管理 MasterDetail | MasterDetail/裸 ObservableObject | 引而不用 | 无 |
| LYBT.Desktop.Patients | 17 | 1,977 | 患者管理 MasterDetail | MasterDetail/Editor/Navigable | 引而不用 | 无 |
| LYBT.Desktop.Registrations | 11 | 1,435 | 挂号队列 | Navigable/Dialog | 无 | 无 |
| LYBT.Desktop.Catalog | 27 | 3,386 | 药材+验方（合并产物） | MasterDetail/Editor/HerbItem | 死代码 1 个 | 无 |
| LYBT.Desktop.MedicalCase | 49 | 7,168 | 医案（B1-B3 重构后） | MasterDetail/Navigable/Dialog/Child | 3 个在用 | **2 个**（560/514） |
| LYBT.Desktop.Admin (Role) | 18 | ~1,400 | 管理员组合根 | Navigable/Child | 无 | 无 |
| LYBT.Desktop.Clinical (Role) | 21 | ~4,200 | 临床组合根 | Navigable/Child | 无 | **1 个**（596） |
| LYBT.Desktop.Contracts | 78 | 5,101 | 纯契约层 | — | 无 | 无 |
| LYBT.Desktop.Controls | 42 | 4,238 | 共享控件库 | ObservableObject 直系 | 1 个（HerbItemMapper） | 无 |
| LYBT.Desktop.Foundation | 64 | 7,411 | Http/安全/Token/仓储基类 | — | 无 | **1 个**（536） |
| LYBT.Desktop.Infrastructure | 90 | 9,743 | VM 基类/组合模式/服务 | Navigable 根 + Dialog/MasterDetail + ObservableObject 轻量 | 引而不用 | 1 个类级（642 partial） |
| LYBT.Desktop.Printing | 12 | 1,480 | WPF 打印 | — | 无 | 无 |
| LocalWebAPI | 22 | 2,329 | 内嵌 Kestrel | — | 无 | **1 个**（549） |
| Shell | 48 | 5,794 | 主窗口/登出/启动 | Navigable/Dialog | 无 | 无（1 个生成代码） |
| **合计** | **522** | **~58,000** | | | | **6 个** |

---

## 一、业务模块类清单与分层职责

### 1.1 LYBT.Desktop.Users（14 文件 / 1,764 行）— 用户管理 MasterDetail

| 层 | 文件 | 行数 | 关键类型 |
|---|---|---|---|
| Models | 2 | 362 | `UserDetailModel`(181) + `UserEditContext`(181) — **字段几乎全同** |
| ViewModels | 5 | 681 | `UserMasterDetailViewModel`(407, MasterDetailViewModelBase) + `UserEditorViewModel`(103, **裸 ObservableObject**) + 2 Handler |
| Services | 1 | 168 | `RemoteUserService`（CrudServiceBase 派生） |
| Repositories | 1 | 233 | `UserRepository`（EntityApiClientRepositoryBase） |
| Controls | 6 | 937 | MasterDetail/View/Edit 三控件 |

**要点**：DetailModel+EditContext 双实例齐备、无 DTO 直编辑 ✓；但 LoadDetailAsync **双份手写 DTO→模型映射**（DetailModel 一份 + EditContext 一份）+ SaveDetailAsync 第三次手写回填，三处维护面；Mapperly 引而不用。

### 1.2 LYBT.Desktop.Patients（17 文件 / 1,977 行）— 患者管理 MasterDetail

| 层 | 文件 | 行数 | 关键类型 |
|---|---|---|---|
| Models | 4 | 412 | `PatientDetailModel`(167) + `PatientEditContext`(134) + **`PatientDetailDisplayModel`(46, 零引用死代码)** + ImportWizardStep |
| ViewModels | 4 | 575 | `PatientMasterDetailViewModel`(343) + `PatientEditorViewModel`(76, EditorViewModelBase✓) + CardReader(104) |
| Services | 2 | 308 | `PatientService`（**无 Remote 前缀**，与 Users/Registrations 不一致）+ PatientCardReaderIntegration |
| Repositories | 1 | 163 | `PatientRepository` |
| Controls | 8 | 1,173 | MasterDetail/View/Edit/Selection 四控件（ViewControl code-behind 271 行最重） |

**要点**：三份重叠患者模型（Detail/EditContext/DisplayModel）；README 宣称的 5 个 Manager/Handler 类**代码中不存在**（文档描述一次未落地重构）。

### 1.3 LYBT.Desktop.Registrations（11 文件 / 1,435 行）— 挂号队列

| 层 | 文件 | 行数 | 关键类型 |
|---|---|---|---|
| Models | 2 | 261 | `RegistrationDetailModel`(159) + `RegistrationEditContext`(102, **ObservableObject 无验证**) |
| ViewModels | 1 | 375 | `RegistrationListViewModel`(NavigableViewModelBase, 未用 MasterDetail 合理) |
| Dialogs | 3 | 464 | `RegistrationCreateDialogViewModel`(254, DialogViewModelBase) |
| Services | 2 | 334 | `RemoteRegistrationService`(168) + `SignalRClient`(166) |
| Repositories | 1 | 131 | `RegistrationRepository` |

**要点**：EditContext 基类与 User/Patient 不一致（无验证）；VM 内联手写 DTO→模型映射**绕过模型自带 FromDto**（漏 Remark/UpdatedAt）；**`LYBT.Desktop.Registrations_s1utir2s_wpftmp.csproj`(486行) MSBuild 临时文件误提交**；单数/复数命名规则总体自洽（复数=程序集/端点，单数=类型名）。

### 1.4 LYBT.Desktop.Catalog（27 文件 / 3,386 行）— Herbs+Formula 合并产物 ⚠️ 双轨残留最重

| 层 | 文件 | 行数 | 关键类型 |
|---|---|---|---|
| Models | 5 | 760 | HerbDetailModel(212)/FormulaDetailModel(194) + HerbEditContext(170)/FormulaEditContext(115) + FormulaHerbItemModel(69) |
| ViewModels | 9 | 1,017 | Herb/FormulaMasterDetailViewModel(279/362) + Herb/FormulaEditorViewModel(83/148) + FormulaHerbItemViewModel + 2 StatusHandler |
| Services | 4 | 536 | **`RemoteHerbService`(CrudServiceBase 风格) vs `FormulaService`(340, 手写 CommandResult 风格)——双轨模式分裂** + 2 个 SearchProvider |
| Repositories | 2 | 361 | HerbRepository(171)/FormulaRepository(190) 同构孪生 |
| Mappers | 1 | 174 | `FormulaDetailModelMapper`（Mapperly，**注册+注入但全模块零调用 = 死代码**）；Herb 轨**无 Mapper** 双重手工映射 |
| Controls | 5 | 449 | 两套 MasterDetail/Edit/View 控件 |

**要点**：VM 基类抽取良好（MasterDetail/Editor/HerbItem/BaseStatusHandler 全复用）；**分歧集中在 Service 层（两轨模式）与 Mapper 层（一轨死代码、一轨缺失）**；DTO 跨模块直出（FormulaSearchProvider 返回 FormulaDetailDto）；Model/EditContext 字段+验证重复。

### 1.5 LYBT.Desktop.MedicalCase（49 文件 / 7,168 行）— B1/B2/B3 重构后 ⚠️ 双体系并存

| 层 | 文件 | 行数 | 关键类型 |
|---|---|---|---|
| Models | 8 | 1,215 | MedicalCaseDetailModel(271) + **B2 EditContext**(202) + **B1 PrescriptionItemModel**(237) + ConsultationItem(308, 旧 BindableBase) |
| ViewModels | 9 | 2,135 | MasterDetail(308) + 3 ChildViewModelBase（Commands 514/Consultation 63/Prescription 93）+ PrescriptionItemViewModel(471, 遗留) |
| Dialogs | 9 | 1,641 | HistoryCopyDialogViewModel(560, **B3 已 Model 化**) + FormulaImportDialogViewModel(341, DP-M1 豁免) |
| Services | 6 | 881 | MedicalCaseService 聚合代理(331) + Query/Command/Lifecycle 分解(internal) + AuditLog + Report |
| Repositories | 1 | 371 | MedicalCaseRepository（唯一） |
| Mappers | 3 | 600 | **全部 Mapperly**（DetailModel/Prescription/Consultation） |

**要点**：B1/B2/B3 核心落地（PrescriptionItemModel/EditContext 单例会话/对话框 Model 化）✓；**双详情加载路径并存**——新路径 LifecycleService→EditContext，旧路径 MasterDetail VM 走 DTO 门面缓存（CachedConsultation/CachedPrescription）→子 VM InitializeFromDto，新老体系未打通；`EditorViewModelBase`/`HerbItemViewModelBase` 模块内零使用；README/AGENTS 过时（引用不存在的 IMedicalCaseEditContext/IMedicalCaseDataManager）。

### 1.6 Roles（组合根层，38 文件 / ~5,600 行）

| 角色 | 文件 | 行数 | 关键类型 |
|---|---|---|---|
| Admin | 18 | ~1,400 | AdminHome/SystemSettings VM + Sysadmin 子模块（SysadminHome/Deployment/LogLevelControl） |
| Clinical | 21 | ~4,200 | ClinicalHome/ClinicalWorkspace/PatientSelection + **MedicalCaseWorkspaceViewModel(596, 核心工作区 FSM)** + 4 个 Workspace 子 VM |

**要点**：Roles 是组合根（XAML 全走 ViewModelLocator，View 复用业务模块 Control）；VM 统一 Navigable/ChildViewModelBase ✓；**DP10 违规：3 个 VM 直注 IApiClient**（SystemSettings/Deployment/LogLevelControl，仅 SysadminHome 经 AuthHealthService 封装——同模块内风格不一致）；多 VM 直持/直绑合同 DTO（PatientDetailDto/MedicalCaseListDto 等，导航参数直接传 DTO）；无 DetailModel/EditContext（可编辑实例在 MedicalCase 模块）。

---

## 二、Core 基础设施层

### 2.1 LYBT.Desktop.Infrastructure（90 文件 / 9,743 行）— VM 基类 + 组合模式核心

**ViewModel 基类家族**（对标 spec §3.1 命名规范）：

| 基类 | 行数 | 继承 | 职责 |
|---|---|---|---|
| `NavigableViewModelBase` | 642（partial×3） | ObservableObject+INavigationAware+IConfirmNavigationRequest+IEditable | VM 根基类：IViewModelServices 9 服务聚合注入 |
| `DialogViewModelBase` | 217 | NavigableViewModelBase+IDialogAware | 对话框基类 |
| `MasterDetailViewModelBase<TList,TDetail>` | 306 | NavigableViewModelBase | 列表页基类 V2：组合模式（委托 CommandGroup/ServiceEventBridge） |
| `EditorViewModelBase<TContext>` | 58 | **ObservableObject 直系**（有意不继承 Navigable） | 编辑器子 VM 基类（A-31-C5-4 收敛） |
| `HerbItemViewModelBase` | 265 | ObservableObject+IHerbItemEditable | 药材行项基类（拼音过滤） |
| `ChildViewModelBase` | 31 | ObservableObject | 复合子 VM 基类 |
| `ValidatableModelBase` | 165 | BindableBase+INotifyDataErrorInfo | DetailModel 验证基类 |

**组合模式**：MasterDetailViewModelBase 委托 `MasterDetailServices`（组合根）+ `MasterDetailCommandGroup`（16 命令）+ `ServiceEventBridge`（8 子服务事件桥）——VM 仅剩委托属性+抽象钩子。

**要点**：三轻量基类（Editor/HerbItem/Child）直接 ObservableObject 是有意设计分歧（避免拖入 9 服务聚合）；**无 Repository/Mapper 类**（Mapperly 引而不用）；服务命名 XxxService 为主 + Manager/Handler/Tracker/Coordinator 混用（DialogManager/SessionManager/LoadingStateManager/EventSubscriptionManager）；obj 残留 UnfinishedCaseDialogViewModel 生成文件（源已删）。

### 2.2 LYBT.Desktop.Controls（42 文件 / 4,238 行）— 共享控件库

**关键契约**：
- `HerbListControl.HerbItems` DP = **IEnumerable 宽松** + BindsTwoWayByDefault（B1 后）——接受 PrescriptionItemDto 或 IHerbItemEditable（PrescriptionItemModel/FormulaHerbItemViewModel）
- **风险点**：回写 `SyncToHerbItemsProperty` 产出 `List<PrescriptionItemDto>` 赋给 HerbItems，而绑定源是 `ObservableCollection<PrescriptionItemModel>`（不可赋值），TwoWay 回写可能触发绑定转换错误——增删行回写路径待实测 [INFERENCE]
- `AllHerbs` DP（HerbItem+HerbList）= 强类型 `ObservableCollection<HerbListDto>`——**DTO 直依赖**，宽松度不足
- 交换形状 LoadFromDto/ToDto/HerbList/AddHerbs 全用 `PrescriptionItemDto`——**控件层对 DTO 直编辑**，与模块层 PrescriptionItemModel 的「DTO 不暴露 UI 层」原则存在张力
- 良好范例：PatientInfoCardControl.Patient 绑本地 PatientDisplayModel（无 DTO）、StatusBadge.Status 为 object

**要点**：控件内 VM 无共享基类（直接 ObservableObject + `new` 挂 LayoutRoot.DataContext，刻意避开 UserControl DataContext）；两个同名 `BreadcrumbItem`（Contracts record vs Controls class）分裂；README 宣称 MasterDetailControlBase : ObservableObject 实为 UserControl（文档漂移）；超大：HerbListControlViewModel 493/HerbItemControl 482（逼近阈值）。

### 2.3 LYBT.Desktop.Contracts（78 文件 / 5,101 行）— 纯契约层

- Services 36 接口（含 CrossModule 2 个 SearchProvider）+ ApiClient 12 段（IEntityApiSegment 泛型 CRUD 基）+ 11 个 **internal Refit I{域}Api** + 6 Repository 接口
- **双层 API 面**：internal Refit `I{域}Api` 与 public `IApiClient{域}` 段并存（Refit 层经 InternalsVisibleTo 隐藏，属设计而非残留）
- **不一致**：跨模块 `I{Formula|Herb}SearchProvider`（Provider 后缀）与 `I{Formula|Herb}Service` 搜索面重叠（GetFormulasPagedAsync vs GetPagedAsync）；`IEditable`（编辑状态）vs Shared.Models `IHerbItemEditable`（药材编辑）语义相近易混；无超大类型（最大 IStartupPipeline 206）

### 2.4 LYBT.Desktop.Foundation（64 文件 / 7,411 行）— Http/安全/Token

- 双模式 API 客户端（Refit 远程 + HttpApiClientBase 本地 adapter，SwitchingApiClient 切换）+ 完整认证/Token 安全子系统 + 仓储基类（ApiClientRepositoryBase/EntityApiClientRepositoryBase）+ 错误映射
- **超大类型**：`TokenRefreshHandler` 536 行
- **死桩**：TokenManager 仅 getter 无写入路径；README 引不存在的 PrintLogEntry.cs

### 2.5 LYBT.Desktop.Printing（12 文件 / 1,480 行）

- IPrintService<TModel> 泛型 + PrescriptionPrintService 编排 + DocumentBuilder/PrintExecutor/PreviewWindowBuilder + QuestPDF 导出
- **完全不含 DTO**（DTO→Model 映射在调用方 MedicalCase 模块 PrescriptionPrintHandler.BuildPrintModel）✓
- 死选项：PrintOptions.Orientation/DuplexPrinting 未被读取

---

## 三、LocalWebAPI + Shell

### 3.1 LocalWebAPI（22 文件 / 2,329 行）— 内嵌 Kestrel

- 11 控制器（复用 Server 模块基类：BaseCrudController×2 + 模块基类×3 + BaseApiController×6）；6 个 Server 模块注册
- **超大类型**：`CatalogController` 549 行
- **DTO 变异点**：MedicalCasesController.Query 非 Admin 时直接改写入参 `query.DoctorId`（越权约束靠 DTO 变异而非独立参数）；Create 直接置 `input.Id = null`

### 3.2 Shell（48 文件 / 5,794 行）— 主窗口/登出/启动

- 核心服务：StartupPipeline(383)/SessionLifecycleManager(321)/LoginCoordinator(298)/ApiHealthMonitor(249)/ShellEventCoordinator(223)/MenuManager(207)
- **无 IRegionManager 直注**（经 INavigationCoordinator 封装）✓；MainWindowViewModel 委托模式（四大管理器）
- **三条登出链**：LoginCoordinator 状态机 + LoginStateManager.PerformLogoutAsync + SessionLifecycleManager（已收敛，无重复）
- 超大：StringResources.Designer.cs 514 为生成代码（不计）

---

## 四、类设计模式一致性

### 4.1 ViewModel 基类使用

| 模式 | 采用处 | 不一致点 |
|---|---|---|
| MasterDetailViewModelBase | Users/Patients/Catalog/MedicalCase 主 VM ✓ | — |
| NavigableViewModelBase | Auth/Roles/Shell/RegistrationList ✓ | — |
| EditorViewModelBase\<TContext\> | Patients(PatientEditorViewModel) / Catalog(Herb/FormulaEditor) ✓ | **Users.UserEditorViewModel 用裸 ObservableObject**（同职责不同基类）；MedicalCase 模块内零使用 |
| DialogViewModelBase | Auth 对话框/Registrations 创建框/MedicalCase 3 对话框/Shell 3 对话框 ✓ | — |
| ChildViewModelBase | MedicalCase 3 子 VM/Clinical Workspace 子 VM ✓ | — |
| 裸 BindableBase/ObservableObject | MedicalCase 的 ConsultationItem/PrescriptionItemViewModel/PrescriptionItemModel（旧条目） | 新旧条目基类三风格并存 |

### 4.2 Model / EditContext 存在性（对标 spec §4 各域目标态）

| 域 | DetailModel | EditContext | DTO 直编辑 | spec 目标态 |
|---|---|---|---|---|
| Herb | ✅ HerbDetailModel | ✅ HerbEditContext | 无（UI 绑 EditContext）| ✅ 保持 |
| Formula | ✅ FormulaDetailModel | ✅ FormulaEditContext | ⚠️ FormulaSearchProvider 直出 DTO、CopyFormulaAsync 消费 DTO | ⚠️ Herbs 集合已 Model 化但映射死代码 |
| Patient | ✅ PatientDetailModel | ✅ PatientEditContext | 无 ✓（+ 1 个零引用 DisplayModel） | ✅ 保持 |
| User | ✅ UserDetailModel | ✅ UserEditContext | 无 ✓（但双份手写映射） | ✅ 保持 |
| Registration | ✅ RegistrationDetailModel | ✅ RegistrationEditContext | 无 ✓（VM 内联映射绕过 FromDto） | ✅（spec 曾标 🔴 已补） |
| MedicalCase | ✅ MedicalCaseDetailModel | ✅ B2 EditContext（单例会话） | ⚠️ DTO 门面缓存仍暴露（Cached*）+ PrintHandler 手建 DTO | ✅ B1-B3 已落地，双体系待打通 |

**总体**：6 域 DetailModel+EditContext 双实例全部齐备、编辑均经 EditContext→InputDto、无 UI 直接编辑 DTO ✓——spec §4.6「前后端各自实例原则」基本闭合。

### 4.3 Service 命名（XxxService/XxxManager/XxxRepository 混用）

| 观察 | 详情 |
|---|---|
| ⚠️ Service 前缀 | `RemoteUserService`/`RemoteRegistrationService`/`RemoteHerbService`（Remote 前缀）vs `PatientService`/`FormulaService`（无前缀）——**混用** |
| ⚠️ Service 模式分裂（Catalog 最重） | `RemoteHerbService` 走 CrudServiceBase 泛型（窄方法+CommandResult 包装）；`FormulaService` 手写 8 位置参数 CommandResult——合并时未统一 |
| ⚠️ Manager 后缀 | Infrastructure 有 DialogManager/SessionManager/LoadingStateManager/EventSubscriptionManager；Shell 有 MenuManager/NavigationManager/StatusBarManager/LoginStateManager——**与 XxxService 并存无统一策略**（契约层统一 IXxxService） |
| ⚠️ 跨模块搜索 | `I{Herb|Formula}SearchProvider`（Provider 后缀）与 `I{Herb|Formula}Service` 搜索面重叠 |
| ✅ Repository | `XxxRepository` 完全一致，均继承 Entity/ApiClientRepositoryBase 统一经 IApiClient（Refit）路由；无 XxxRepositoryManager |

### 4.4 Mapper 用法（Mapperly vs 手写）

| 模块 | Mapper | 状态 |
|---|---|---|
| MedicalCase | 3 个 Mapperly（DetailModel/Prescription/Consultation）| ✅ 在用（PrescriptionItemModel↔Dto 手写静态） |
| Catalog | 1 个 Mapperly（FormulaDetailModelMapper）| 🔴 **死代码**（注册+注入零调用）；Herb 轨无 Mapper 双重手工映射 |
| Users/Patients/Infrastructure | csproj 引 Riok.Mapperly | 🔴 **引而不用**，映射全手写且多处重复（Users LoadDetailAsync 双份） |
| Controls | 1 个 Mapperly（HerbItemMapper：VM→PrescriptionItemDto）| ✅ 在用 |
| Roles/Shell/Auth/Registrations/Foundation | 无 | 手写内联 |

**结论**：Mapperly 采纳不彻底——只有 MedicalCase/Controls 真正使用；Users/Patients/Infrastructure 三个项目「引而不用」，Catalog 一半死代码一半缺失。手写映射重复最高点：Catalog Herb（VM 内联重建 DTO + Editor VM 再映射）、Users（3 处同字段映射）。

### 4.5 DTO 直编辑残留（DP-M1 合规）

| 位置 | 残留 | 判定 |
|---|---|---|
| Controls HerbList/HerbItem | 交换形状 PrescriptionItemDto + AllHerbs DP 强绑 ObservableCollection\<HerbListDto\> | ⚠️ 控件层对 DTO 直编辑（spec §4.6 张力） |
| Roles Clinical | 多 VM 直持/直绑 PatientDetailDto/MedicalCaseListDto/HerbListDto/PendingMedicalCaseDto；导航参数直接传 DTO | ⚠️ 组合根轻量越界（展示/导航型，非编辑） |
| MedicalCaseService | `CachedMedicalCase/CachedConsultation/CachedPrescription` DTO 门面缓存暴露给 VM 层 | ⚠️ B2 有意保留的接口兼容门面（行为等价决策） |
| LocalWebAPI Controllers | MedicalCasesController 变异入参 DTO（query.DoctorId / input.Id） | ⚠️ 服务端变异（与 Desktop DP-M1 无关，独立关注点） |
| Catalog | FormulaSearchProvider 跨模块直出 FormulaDetailDto；CopyFormulaAsync 消费 DTO | ⚠️ 跨模块契约 DTO 直出 |
| 6 业务模块 VM | 编辑路径均经 EditContext→InputDto | ✅ 合规 |

---

## 五、行数统计与超大类型（>500 行）

### 5.1 项目行数（.cs，排除 obj/bin）

| 项目 | 文件 | 行数 | 最大单文件 | 行数 |
|---|---|---|---|---|
| LYBT.Desktop.Infrastructure | 90 | 9,743 | CardReaderService.cs | 369 |
| LYBT.Desktop.MedicalCase | 49 | 7,168 | HistoryCopyDialogViewModel.cs | 559 |
| LYBT.Desktop.Foundation | 64 | 7,411 | TokenRefreshHandler.cs | 535 |
| LYBT.Desktop.Shell | 48 | 5,794 | StringResources.Designer.cs（生成） | 514 |
| LYBT.Desktop.Contracts | 78 | 5,101 | IStartupPipeline.cs | 206 |
| LYBT.Desktop.Controls | 42 | 4,238 | HerbListControlViewModel.cs | 493 |
| LYBT.Desktop.Clinical (Role) | 21 | ~4,200 | MedicalCaseWorkspaceViewModel.cs | 595 |
| LYBT.Desktop.Catalog | 27 | 3,386 | FormulaMasterDetailViewModel.cs | 362 |
| LocalWebAPI | 22 | 2,329 | CatalogController.cs | **548** |
| LYBT.Desktop.Patients | 17 | 1,977 | PatientMasterDetailViewModel.cs | 343 |
| LYBT.Desktop.Users | 14 | 1,764 | UserMasterDetailViewModel.cs | 407 |
| LYBT.Desktop.Printing | 12 | 1,480 | PrescriptionPrintService.cs | — |
| LYBT.Desktop.Admin (Role) | 18 | ~1,400 | SystemSettingsViewModel.cs | — |
| LYBT.Desktop.Auth | 11 | 1,280 | LoginViewModel.cs | 326 |
| LYBT.Desktop.Registrations | 11 | 1,435 | RegistrationListViewModel.cs | 375 |

### 5.2 超大类型清单（>500 行）

| 类型 | 行数 | 项目 | 备注 |
|---|---|---|---|
| MedicalCaseWorkspaceViewModel | 595 | Clinical (Role) | 自带 TODO 标注（工作区 FSM 组合） |
| HistoryCopyDialogViewModel | 559 | MedicalCase | B3 已 Model 化后仍偏大 |
| CatalogController | 548 | LocalWebAPI | 药材+验方合并端点 |
| TokenRefreshHandler | 535 | Foundation | 认证刷新管线 |
| MedicalCaseCommandsViewModel | 513 | MedicalCase | 9 命令聚合 |
| MedicalCaseEditControl.xaml | 648 | MedicalCase | XAML（DP 密集，非代码） |
| NavigableViewModelBase（类级） | 642 | Infrastructure | 已拆 3 个 partial（单文件 ≤278） |
| StringResources.Designer.cs | 514 | Shell | 生成代码（不计） |

**另：逼近阈值（400-500）**：PrescriptionItemViewModel 471、HerbListControlViewModel 493、HerbItemControl.xaml.cs 482、UserMasterDetailViewModel 407。

---

## 六、目录结构对标 spec §3.3 矩阵

| 子目录 | Auth | Catalog | Patients | Users | Registration | MedicalCase | spec 3.3 记录 |
|--------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Models/ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 全部达标（spec 曾标 Registration ❌ 已补） |
| Models/Items/ | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ | 仅 Auth 无（合理，无子项模型） |
| ViewModels/ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 达标 |
| Views/ | ✅ | ❌ 用 Controls/ | ❌ 用 Controls/ | ❌ 用 Controls/ | ✅ | ❌ 用 Controls/ | 与 spec 3.3 一致（Controls/ 为合理 Prism 用法） |
| Services/ | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ | 与 spec 一致（Auth 无 Service 合理） |
| Mappers/ | ❌ | ✅ | ❌ | ❌ | ❌ | ✅ | **spec 已记录 Catalog/MedicalCase 有、其余无——但 Users/Patients 的 Mapperly 引而不用** |
| Repositories/ | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ | 与 spec 一致 |

**目录结构结论**：6 模块目录与 spec §3.3 矩阵**完全一致**（Auth 缺失 Services/Mappers/Repositories 为 spec 已接受的合理简化）；Controls/ vs Views/ 的分歧与 spec 结论一致（Prism Region 导航，不需要强制改）。

---

## 七、可统一项汇总（按优先级）

### P0 — 结构性不一致（影响模块间一致性理解）

1. **Catalog Service 双轨模式分裂**：`RemoteHerbService`（CrudServiceBase 泛型）vs `FormulaService`（手写 CommandResult）——合并未统一，建议 Formula 对齐 Herb 或显式 ADR 记录差异
2. **Catalog Mapper 死代码 + Herb 缺 Mapper**：`FormulaDetailModelMapper` 注册+注入但零调用；Herb 轨无 Mapper 靠 2 处手工映射（VM 内联重建 DTO + Editor VM 再映射）——两轨统一到 Mapperly 或删除死代码
3. **Mapperly 引而不用**：Users/Patients/Infrastructure csproj 引用 Riok.Mapperly 但零使用，映射全手写且重复（Users 三处同字段映射、Catalog Herb 双重映射）——启用或移除
4. **MedicalCase 双详情加载体系并存**：DTO 门面缓存路径（MasterDetail VM→InitializeFromDto）vs EditContext 路径（LifecycleService→BeginEdit）——新老未打通，需决策收敛方向
5. **Controls 控件层 DTO 直编辑**：HerbList/HerbItem 交换形状 PrescriptionItemDto + AllHerbs DP 强绑 `ObservableCollection<HerbListDto>`——与模块层 Model 封装原则张力；HerbItems 回写 List\<PrescriptionItemDto\> 替换外部集合引用风险 [INFERENCE 待实测]

### P1 — 命名/基类不一致

6. **UserEditorViewModel 基类**：裸 ObservableObject vs Patients/Catalog 的 EditorViewModelBase\<TContext\>——同职责不同基类
7. **RegistrationEditContext 无验证**：ObservableObject vs User/Patient 的 ValidatableModelBase
8. **Service 前缀混用**：RemoteUserService/RemoteRegistrationService/RemoteHerbService vs PatientService/FormulaService——统一 Remote 前缀或无前缀
9. **Manager 后缀混用**：Infrastructure/Shell 的 XxxManager 与 XxxService 并存无统一策略（契约层已统一 IXxxService）
10. **跨模块搜索 Provider**：I{Herb|Formula}SearchProvider 与 I{Herb|Formula}Service 搜索面重叠——收敛或明确分工
11. **Roles DP10 违规**：SystemSettings/Deployment/LogLevelControl 直注 IApiClient（仅 SysadminHome 经 AuthHealthService 封装）——补服务门面或豁免记录
12. **Model/EditContext 字段+验证重复**：Users（181/181 全同）、Catalog（Herb/Formula 两对）、Patients（三份模型其一死代码）——消除 DetailModel 编辑角色或抽共享基类
13. **子 VM 装配不一致**：Auth 手动 `new` 不入 DI vs Patients/Users DI 注入

### P2 — 卫生清理

14. **超大类型**（6 个 >500）：MedicalCaseWorkspaceViewModel(595)/HistoryCopyDialogViewModel(559)/CatalogController(548)/TokenRefreshHandler(535)/MedicalCaseCommandsViewModel(513)——拆分或文档化
15. **死代码/死选项**：PatientDetailDisplayModel（零引用）、TokenManager（无写入路径）、PrintOptions.Orientation/DuplexPrinting（未读）、FormulaDetailModelMapper（零调用）
16. **误提交文件**：`LYBT.Desktop.Registrations_s1utir2s_wpftmp.csproj`（486 行 MSBuild 临时文件）应删除
17. **obj 残留**：UnfinishedCaseDialogViewModel 生成文件（源已删，构建产物陈旧）

### P3 — 文档债务（与代码脱节）

18. **README 过期**：Patients（宣称 5 个 Manager 不存在）、Catalog（结构描述）、MedicalCase（引用不存在的 IMedicalCaseEditContext/MedicalCaseCloneMapper/IMedicalCaseDataManager）、Controls（MasterDetailControlBase 基类描述漂移）、Printing（PrintLogEntry.cs 不存在）、Registrations（旧三层结构）
19. **两个同名 BreadcrumbItem**（Contracts record vs Controls class）——命名消歧

---

## 附：依赖方向核对

- 业务模块 → Foundation/Infrastructure/Contracts/Shared.Models（无跨模块编译依赖，MedicalCase 注释 Epic #2175 已验证）✓
- Controls → Contracts/Foundation/Shared.Models（经 Infrastructure 传递引用可达模块）✓
- LocalWebAPI → Server 6 模块（ADR-0010 唯一跨层例外）✓
- Shell → LocalWebAPI + 全部模块/角色 ✓
- Infrastructure 引用 Controls（HerbItemViewModelBase 依赖？[INFERENCE] 实际是 Controls 依赖 Shared.Models 契约）——引用方向总体单向无环

---

*本报告为只读扫描产物，未修改任何代码/文档。行数经 Python 脚本复核（utf-8 逐行计数）。*
