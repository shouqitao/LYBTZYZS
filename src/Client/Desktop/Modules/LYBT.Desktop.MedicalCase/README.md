# LYBT.Desktop.MedicalCase

> 医案管理模块 | 聚合根 CRUD / 诊到处方编辑 / 状态机 / 打印导出 / 历史复制

## 项目定位

- **层级**: `src/Client/Desktop/Modules/` — Prism 模块层
- **职责**: 医案(MedicalCase)作为 DDD 聚合根，统一管理 Consultation(四诊) + Prescription(处方) 的生命周期，提供 Master-Detail 管理视图和工作区编辑能力
- **ModuleDependency**: `PatientsModule`（运行时需要患者数据）、`CatalogModule`（IHerbSearchProvider/IFormulaSearchProvider）

## 目录结构

```
LYBT.Desktop.MedicalCase/
├── MedicalCaseModule.cs                              # Prism 模块注册入口
├── Controls/
│   ├── MedicalCaseMasterDetailControl.xaml/.cs        # Master-Detail 可复用控件
│   ├── MedicalCaseEditControl.xaml/.cs                # 医案编辑控件（Full/Compact 双模式）
│   ├── MedicalCaseViewControl.xaml/.cs                # 医案预览控件
│   └── WorkflowStepIndicator.xaml/.cs                 # 工作流步骤指示器
├── Dialogs/
│   ├── FormulaImportDialog.xaml/.cs                   # 验方导入弹窗视图
│   ├── FormulaImportDialogViewModel.cs                # 验方导入 VM（搜索/筛选/预览/确认）
│   ├── HistoryCopyDialog.xaml/.cs                     # 历史复制弹窗视图
│   ├── HistoryCopyDialogViewModel.cs                  # 历史复制 VM（左右双栏/当前患者5条/全部患者模式）
│   ├── UnsavedChangesDialog.xaml/.cs                  # 未保存修改弹窗视图
│   └── UnsavedChangesDialogViewModel.cs               # 未保存修改 VM（保存/放弃/取消三选）
├── Extensions/
│   └── PrescriptionImportExtensions.cs                # 处方导入扩展方法（验方→PrescriptionItemDto）
├── Interfaces/
│   ├── IMedicalCaseService.cs                         # 聚合根门面接口（继承 Query+Command+Lifecycle）
│   ├── IMedicalCaseDataProvider.cs                    # 工作台数据提供者接口（替代 11 个 Func 委托属性）
│   ├── IMedicalCaseWorkspaceContext.cs                # 工作区上下文接口
│   ├── IEditModeStateMachine.cs                       # 编辑状态机接口
│   ├── IDataProvider.cs                               # 数据提供者接口
│   └── IValidatable.cs                                # 验证接口
├── Mappers/
│   ├── MedicalCaseDetailModelMapper.cs                # Mapperly: MedicalCaseDetailDto → MedicalCaseDetailModel
│   ├── ConsultationMapper.cs                          # Mapperly: ConsultationDetailDto ↔ ConsultationItem ↔ ConsultationInputDto
│   ├── PrescriptionMapper.cs                          # Mapperly: PrescriptionDetailDto ↔ PrescriptionItem ↔ PrescriptionInputDto
│   └── PrescriptionItemMapper.cs                      # 处方药材项共享映射器（DTO ↔ Model，抽取自上述两个 Mapper 的重复实现）
├── Models/
│   ├── MedicalCaseDetailModel.cs                      # Detail 编辑模型（ValidatableModelBase）
│   ├── WorkspaceState.cs                              # 工作区状态 immutable record
│   ├── WorkspaceEditState.cs                          # 编辑状态枚举（6 状态）
│   ├── WorkspaceEditEvent.cs                          # 编辑事件枚举（10 事件）
│   ├── EditType.cs                                    # 编辑类型枚举
│   └── Items/
│       ├── ConsultationItem.cs                        # 诊断数据 Item（BindableBase, IDataProvider+IValidatable）
│       ├── PrescriptionItemModel.cs                   # 处方行 Model（编辑会话承载）
│       └── MedicalCaseEditContext.cs                  # 医案编辑会话（BeginEdit/Commit/Cancel/IsDirty，模块内单例）
├── Repositories/
│   └── MedicalCaseRepository.cs                       # 仓储实现（ApiClientRepositoryBase + IApiClientMedicalCases，恒走远程 API）
├── Reports/                                           # 统计报表子模块（独立 Prism 模块，STUB）
│   ├── ReportsModule.cs                               # [Module] ReportsModule（App 目录 OnDemand 注册）
│   ├── Views/
│   │   └── ReportsHomeView.xaml/.cs                   # 报表主页（日收入/问诊量/药材用量）
│   ├── ViewModels/
│   │   └── ReportsHomeViewModel.cs                    # 报表 VM（日期切换 + 加载版本号防竞态）
│   ├── Repositories/
│   │   └── ReportRepository.cs                        # 只读聚合仓储（封装 IApiClient.Reports）
│   └── Services/
│       └── ReportService.cs                           # IReportService 实现（CommandResult 包装）
├── Services/
│   ├── MedicalCaseService.cs                          # 聚合代理（委托 Query/Command/Lifecycle，Coordinator 职责）
│   ├── MedicalCaseQueryService.cs                     # 查询服务
│   ├── MedicalCaseCommandService.cs                   # 命令服务（基于 EditContext 会话，HasChanges/SaveAsync）
│   ├── MedicalCaseLifecycleService.cs                 # 生命周期服务（InitializeAsync → DTO → EditContext.BeginEdit，唯一 DTO 快照）
│   └── AuditLogService.cs                             # 审计日志服务
├── ViewModels/
│   ├── MedicalCaseMasterDetailViewModel.cs            # 核心 VM（MasterDetailViewModelBase 组合模式）
│   ├── AuditLogViewModel.cs                           # 审计日志 VM
│   ├── Items/
│   │   └── PrescriptionItemViewModel.cs               # 处方数据 Item（BindableBase + IDataProvider + IValidatable + INotifyDataErrorInfo）
│   ├── Components/
│   │   ├── EditModeStateMachine.cs                    # 编辑状态机（6 状态 / 10 事件 / 转换表驱动 / 线程安全）
│   │   └── PrescriptionPrintHandler.cs                # 处方打印处理器（IPrintService + 诊所配置热更新 + 草稿水印）
│   └── Workspace/
│       ├── ConsultationEditorViewModel.cs             # 诊断编辑子 VM（ChildViewModelBase）
│       ├── PrescriptionEditorViewModel.cs             # 处方编辑子 VM（ChildViewModelBase）
│       └── MedicalCaseCommandsViewModel.cs            # 命令子 VM（ChildViewModelBase, 9 命令）
├── Views/
│   ├── MedicalCaseMasterDetailView.xaml/.cs           # Master-Detail 视图（导航注册）
│   └── AuditLogView.xaml/.cs                          # 审计日志视图
└── LYBT.Desktop.MedicalCase.csproj                    # 项目文件
```

## 视图 / ViewModel 清单

**计数口径**：View = 页面/导航级 XAML（`*/Views/*.xaml`，含 `Reports/Views/`）；Control = 内嵌组件（`*/Controls/*.xaml`）；Dialog = `*/Dialogs/**/*.xaml`；ViewModel 按「每文件 1 个 VM 类型」计。

| 类别 | 数量 | 明细 |
|------|------|------|
| View | 3 | `MedicalCaseMasterDetailView`、`AuditLogView`、`Reports/Views/ReportsHomeView` |
| Control | 4 | `MedicalCaseMasterDetailControl`、`MedicalCaseEditControl`、`MedicalCaseViewControl`、`WorkflowStepIndicator` |
| Dialog | 3 | `FormulaImportDialog`、`HistoryCopyDialog`、`UnsavedChangesDialog`（均 `RegisterDialog`） |
| ViewModel | 10 | `MedicalCaseMasterDetailViewModel`、`AuditLogViewModel`、`Items/PrescriptionItemViewModel`、`Workspace/` 三个子 VM、`Components/` 无 VM（状态机/打印处理器为普通类）、`Dialogs/` 三个对话框 VM、`Reports/ViewModels/ReportsHomeViewModel` |

> 全桌面口径：View 30 / Control 33 / Dialog 7 / ViewModel 55（代码实际：`src/Client/Desktop`）。

## 核心组件

| 类 | 设计依据 | 职责 |
|---|---|---|
| **MedicalCaseModule** | `[ModuleDependency("PatientsModule"/"CatalogModule")]`；RegisterTypes 注册聚合服务 + 3 Dialog + MasterDetailServices | 模块入口：MedicalCaseEditContext、Query/Command/Lifecycle 三服务、MedicalCaseService 聚合代理、MedicalCaseDetailModelMapper(Singleton)、3 Dialog、AuditLog |
| **MedicalCaseMasterDetailViewModel** | 继承 `MasterDetailViewModelBase<MedicalCaseListDto, MedicalCaseDetailModel>`；组合模式含 ConsultationEditor + PrescriptionEditor 子 VM | 分页列表、详情加载(LifecycleService 快照→子 VM)、聚合保存(AggregateSaveAsync)、删除(CancelMedicalCase)、CreateNewDetail 抛 NotSupportedException |
| **ConsultationEditorViewModel** | `ChildViewModelBase` 子 VM；ConsultationMapper 编译时映射 | InitializeFromDto(ConsultationDetailDto→ConsultationItem)、GetConsultationData(→ConsultationInputDto)、Validate |
| **PrescriptionEditorViewModel** | `ChildViewModelBase` 子 VM；PrescriptionMapper 编译时映射；CollectionChanged 通知父 VM 状态重算 | InitializeFromDto(PrescriptionDetailDto→PrescriptionItem)、GetPrescriptionData(→PrescriptionInputDto)、Validate、HasItems |
| **MedicalCaseCommandsViewModel** | `ChildViewModelBase`；9 个 CommunityToolkit 命令（构造中手动实例化 `IRelayCommand`/`AsyncRelayCommand`）；数据经 `IMedicalCaseDataProvider` 从宿主 VM 获取，CanExecute 跨子 VM 边界需父 VM 手动 `RefreshCanExecute()` | Save / Suspend / Complete / Print / ExportPdf / EnterEditMode / ImportFormula / CopyHistory / ClearHerbs |
| **EditModeStateMachine** | Dictionary 转换表驱动；`lock` 线程安全；事件在锁外触发防死锁；参考 AuthenticationStateMachine 模式 | 6 状态(ReadOnly/Editing/DirtyEditing/Saving/TransitionBlocked/LeavingConfirming) × 10 事件(EnterEdit/ExitEdit/MakeChange/Save/SaveCompleted/SaveFailed/RequestLeave/LeaveConfirmed/LeaveCancelled) |
| **PrescriptionPrintHandler** | `IPrintService<PrescriptionPrintModel>` 委托；诊所配置 `clinic-settings.json` 热更新(IClinicSettingsService)；草稿水印(IsDraft=非 Completed) | PrintPreviewAsync、ExportPdfAsync(SaveFileDialog)、BuildPrintModel(自动绑定 DoctorName + Discount 折扣计算) |
| **FormulaImportDialogViewModel** | `DialogViewModelBase`；跨模块 `IFormulaSearchProvider`；自动筛选 Validated + Enabled 验方 | 搜索/分类筛选/详情预览/确认导入；过滤逻辑：ValidationStatus==Validated && Status==Enabled |
| **HistoryCopyDialogViewModel** | `DialogViewModelBase`；~549 行；左右双栏；当前患者最近 5 条 → 展开全部 → 全局查询三模式 | ShowMoreCurrentPatient / ToggleAllPatients 命令；搜索(患者名+诊断) + 时间区间筛选；复制时刷新为当前药材价格 |
| **MedicalCaseService** | `IMedicalCaseService` 聚合代理；委托 Query/Command/Lifecycle 三独立服务；自身保留 Coordinator 职责 | LoadDetailsAsync(委托 LifecycleService.InitializeAsync)、AggregateSaveAsync(诊断+处方聚合保存，保存后 UpdateSnapshot 前移会话基线)、SaveAndCompleteAsync(验证+保存+完成)、SaveAndSuspendAsync、SaveAndCancelAsync |
| **MedicalCaseEditContext** | 完整编辑会话（B2 重建）；模块内单例注册，Command/Lifecycle 共享 | BeginEdit(装载 Model+基线快照)/Commit(应用+前移基线)/Cancel(恢复)/IsDirty(对比基线)；承载诊断字段+处方行+状态 |
| **PrescriptionItemViewModel** | `BindableBase` + `IDataProvider` + `IValidatable` + `INotifyDataErrorInfo`（Entity-DTO-Item 契约） | 处方行 UI 绑定模型；`PrescriptionItemMapper` 提供 DTO↔Model 双向映射。**P1-3 保留**：暂不迁移到 `[ObservableProperty]`（Mapperly 看不到源生成成员，会产出空映射） |
| **PrescriptionItemMapper** | 静态共享映射器（mapper-chain-audit H1） | 抽取 `MedicalCaseDetailModelMapper` / `PrescriptionMapper` 中逐行一致的 14 字段映射，避免双份重复 |
| **IMedicalCaseDataProvider** | 工作台数据提供者接口 | 由 `Clinical.MedicalCaseWorkspaceViewModel` 实现、`MedicalCaseCommandsViewModel` 消费；替代 11 个 `Func<>` 委托属性，提供编译时类型安全 |
| **ReportsModule / ReportsHomeViewModel / ReportService / ReportRepository** | 独立 `[Module]`（App 目录 `OnDemand`）；VM→Service→Repository→`IApiClient.Reports` 分层（P0-2） | 报表域（日收入 / 问诊量 / 药材用量）。**当前为 STUB**：`ReportsModule` 仅注册基础导航，趋势/绩效待迭代（TODO 2026-08-21） |

## 依赖关系

### 编译时依赖（ProjectReference）

| 项目 | 用途 |
|---|---|
| LYBT.Desktop.Foundation | `ApiClientRepositoryBase`、`ExceptionHandling`（`ClientErrorMessageMapper`）、`Security` |
| LYBT.Desktop.Infrastructure | `MasterDetailViewModelBase`、`ChildViewModelBase`、`DialogViewModelBase`、DI 扩展、`IToastService`、`IViewModelServices` |
| LYBT.Desktop.Contracts | `Repositories.IMedicalCaseRepository` / `IReportRepository`、`Services.IMedicalCase{Query,Command,Lifecycle}Service` / `IAuditLogService` / `IReportService`、`Services.CrossModule.IHerbSearchProvider` / `IFormulaSearchProvider`、`ApiClient` |
| LYBT.Desktop.Printing | `IPrintService<T>` / `PrescriptionPrintModel` / `ExportFormat` |
| LYBT.Shared.Models | `MedicalCaseListDto` / `MedicalCaseDetailDto` / `ConsultationDetailDto` / `PrescriptionDetailDto` 等 DTO、`Enums`（`MedicalCaseStatus`/`CommonStatus`）、`Contracts.Reports` |
| LYBT.Shared.ExceptionHandling | 传递依赖（经 Foundation） |

NuGet 直接引用：`Prism.Core` / `Prism.DryIoc` / `Prism.Wpf`、`Riok.Mapperly`、`Microsoft.Extensions.Logging.Abstractions`。

> `LYBT.Desktop.MedicalCase.csproj` 注释明确：**跨模块 ProjectReference 已全部移除**（Epic #2175 D5-3 / #1676 Phase 4），`[ModuleDependency]` 只保证运行时加载顺序。

### 运行时依赖（ModuleDependency / 跨模块接口）

| 模块 | 接口/数据 | 说明 |
|---|---|---|
| `PatientsModule` | 患者数据 | 医案必须关联患者（`[ModuleDependency("PatientsModule")]`） |
| `CatalogModule` | `IHerbSearchProvider`（拼音自动补全、AllHerbs 列表）、`IFormulaSearchProvider`（验方导入弹窗搜索） | Herbs/Formula 已合并为 Catalog 模块（`[ModuleDependency("CatalogModule")]`） |

### 被依赖

| 消费方 | 接口/控件 | 说明 |
|---|---|---|
| `Roles/LYBT.Desktop.Clinical` `MedicalCaseManagementView` | `MedicalCaseMasterDetailControl` | 薄包装：View 在角色台，Control 在业务模块 |
| `Roles/LYBT.Desktop.Clinical` `MedicalCaseWorkspaceView` | `MedicalCaseEditControl` / `MedicalCaseViewControl` | 看诊场景编辑/预览（`IsCompactMode`） |
| `Roles/LYBT.Desktop.Clinical` `MedicalCaseWorkspaceViewModel` | `IMedicalCaseWorkspaceContext` / `IMedicalCaseDataProvider` / `MedicalCaseCommandsViewModel` | 实现数据提供者接口供 CommandsVM 消费 |
| `Roles/LYBT.Desktop.Clinical` `PendingQueueViewModel`、`Registrations` `RegistrationListViewModel` | `Contracts.Services.IMedicalCaseService` / `ViewNames.MedicalCaseWorkspace` | 挂起医案、接诊后导航到工作区 |
| `Admin` `AdminHomeViewModel` | 导航 `ViewNames.MedicalCaseManagement` | 管理台入口 |
| `Admin` `AdminHomeViewModel` / `Clinical` `ClinicalHomeViewModel` | 导航 `ViewNames.ReportsHome` / `ViewNames.AuditLog` | 报表与审计入口（`ReportsModule` / `MedicalCaseModule` 懒加载） |

## 设计决策

1. **聚合根模式**: MedicalCase 是唯一聚合根，统一管理 Consultation + Prescription 的生命周期；Consultation/Prescription 不作为独立模块存在（Issue #1463 移除 ConsultationModule 依赖）
2. **Service 四拆分**: MedicalCaseService 聚合代理委托 Query/Command/Lifecycle 三独立服务 + MedicalCaseEditContext 编辑会话（单例），SRP 职责分离；DTO 快照单一持有于 LifecycleService（D5 收敛）
3. **子 VM + `IMedicalCaseDataProvider` 模式**: ConsultationEditor / PrescriptionEditor / Commands 三个 `ChildViewModelBase` 子 VM；CommandsVM 不再持有 11 个 `Func<>` 委托属性，改由父 VM（`Clinical.MedicalCaseWorkspaceViewModel`）实现 `IMedicalCaseDataProvider` 注入，提供编译时类型安全；因 CommunityToolkit 的 CanExecute 属性观察无法跨子 VM 边界工作，父 VM 状态变更后仍需手动调用 `Commands.RefreshCanExecute()`
4. **转换表驱动状态机**: EditModeStateMachine 用 `Dictionary<(State,Event), State>` 静态转换表 + `lock` 线程安全 + 事件锁外触发防死锁，替代嵌套 if/switch
5. **处方打印热更新**: PrescriptionPrintHandler 通过 IClinicSettingsService 读取 `clinic-settings.json`，支持诊所信息(名称/地址/电话)运行时更新
6. **历史复制 UX**: HistoryCopyDialog 默认显示当前患者最近 5 条已完成记录 → "显示更多"展开本患者全部 → "查看全部患者"切换全局模式
7. **禁用药材过滤**: ImportFormula / CopyHistory 时自动跳过 Status!=Enabled 的药材（T5-P2-21），复制历史处方时刷新为当前药材价格（CODE-08）
8. **Mapperly + ObservableProperty 兼容**: Item 类使用 `[ObservableProperty]` 源生成器时，Mapperly 的 `[MapProperty]` 无法正常工作（RMG005/RMG006），解决方案是 `[MapperIgnoreSource/Target]` + 包装方法手动映射

## 已知陷阱

1. **CreateNewDetail 抛异常**: MedicalCaseMasterDetailViewModel.CreateNewDetail() 抛 NotSupportedException，医案不支持从管理视图新建，仅通过看诊入口创建
2. **`IMedicalCaseDataProvider` 必须先于命令可用**: CommandsVM 经构造注入 `IMedicalCaseDataProvider`；宿主（`Clinical.MedicalCaseWorkspaceViewModel`）未实现/未注入时所有命令的 CanExecute 会失败或抛 NullReferenceException
3. **RefreshCanExecute 手动调用**: 子 VM 间 ObservesProperty 不工作，父 VM 状态变更后必须手动调用 `Commands.RefreshCanExecute()`（内部对 9 个命令逐个 `NotifyCanExecuteChanged()`）更新 CanExecute
4. **HistoryCopyDialogViewModel 字段赋值绕过回调**: 初始化时直接赋值 `_isShowingAllPatients` 而非属性，避免触发 OnIsShowingAllPatientsChanged 导致重复加载
5. **Mapperly 与 CommunityToolkit.Mvvm 源生成器冲突**: `[ObservableProperty]` 生成的属性 Mapperly 看不到（RMG004/RMG021/RMG066 → 空映射），必须用 `[MapperIgnoreTarget("PropertyName")]` 字符串字面量 + 手动映射；`PrescriptionItemViewModel` 因此保留 `BindableBase`（P1-3）
6. **FormulaImportDialog 过滤条件**: 仅显示 ValidationStatus==Validated && Status==Enabled 的验方，开发者新增验方筛选逻辑时需同步更新 FilterFormulas()
7. **PrescriptionPrintHandler 草稿水印**: IsDraft 判断条件是 `_medicalCaseService.Current?.CaseStatus != Completed`，非 Completed 状态打印均带草稿水印
8. **AggregateSaveAsync 快照同步**: 保存成功后自动调 LifecycleService.UpdateSnapshot（DTO 快照 + EditContext 基线前移），若绕过此方法直接调用 Repository 会导致快照/会话不一致
9. **Reports 子模块是 STUB**: `ReportsModule` 仅注册 `ReportsHomeView` 基础导航与报表查询，趋势/绩效等图表未实现；文档/UI 上不要按「完整报表」描述

---

2026-09-13 docs 复盘：与代码对齐（View/VM 清单、目录树、依赖）
