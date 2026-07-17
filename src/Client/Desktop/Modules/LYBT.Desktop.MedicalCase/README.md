# LYBT.Desktop.MedicalCase

> 医案管理模块 | 聚合根 CRUD / 诊到处方编辑 / 状态机 / 打印导出 / 历史复制

## 项目定位

- **层级**: `src/Client/Desktop/Modules/` — Prism 模块层
- **职责**: 医案(MedicalCase)作为 DDD 聚合根，统一管理 Consultation(四诊) + Prescription(处方) 的生命周期，提供 Master-Detail 管理视图和工作区编辑能力
- **ModuleDependency**: `PatientsModule`（运行时需要患者数据）、`HerbsModule`（IHerbSearchProvider）、`FormulaModule`（IFormulaSearchProvider）

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
│   ├── IMedicalCaseService.cs                         # 聚合根门面接口（Query+Command+Lifecycle）
│   ├── IMedicalCaseEditContext.cs                     # 编辑上下文接口
│   ├── IMedicalCaseWorkspaceContext.cs                # 工作区上下文接口
│   ├── IEditModeStateMachine.cs                       # 编辑状态机接口
│   ├── IDataProvider.cs                               # 数据提供者接口
│   └── IValidatable.cs                                # 验证接口
├── Mappers/
│   ├── MedicalCaseDetailModelMapper.cs                # Mapperly: MedicalCaseDetailDto → MedicalCaseDetailModel
│   ├── ConsultationMapper.cs                          # Mapperly: ConsultationDetailDto ↔ ConsultationItem ↔ ConsultationInputDto
│   ├── PrescriptionMapper.cs                          # Mapperly: PrescriptionDetailDto ↔ PrescriptionItem ↔ PrescriptionInputDto
│   └── MedicalCaseCloneMapper.cs                      # Mapperly: 深拷贝映射器（变更检测/回滚）
├── Models/
│   ├── MedicalCaseDetailModel.cs                      # Detail 编辑模型（ValidatableModelBase）
│   ├── MedicalCaseNavigationParameters.cs             # 导航参数封装（ForClinical/ForManagementView/ForManagementEdit）
│   ├── WorkspaceState.cs                              # 工作区状态 immutable record
│   ├── WorkspaceEditState.cs                          # 编辑状态枚举（6 状态）
│   ├── WorkspaceEditEvent.cs                          # 编辑事件枚举（10 事件）
│   ├── EditState.cs / EditType.cs / WorkspaceMode.cs  # 状态/类型/模式枚举
│   └── Items/
│       ├── ConsultationItem.cs                        # 诊断数据 Item（BindableBase, IDataProvider+IValidatable）
│       └── PrescriptionItem.cs                        # 处方数据 Item（BindableBase, IDataProvider+IValidatable）
├── Repositories/
│   └── MedicalCaseRepository.cs                       # 仓储实现（Repository 抽象，Local/Remote 双模式）
├── Services/
│   ├── MedicalCaseService.cs                          # 聚合代理（委托 Query/Command/Lifecycle，Coordinator 职责）
│   ├── MedicalCaseQueryService.cs                     # 查询服务
│   ├── MedicalCaseCommandService.cs                   # 命令服务
│   ├── MedicalCaseLifecycleService.cs                 # 生命周期服务
│   ├── MedicalCaseEditContext.cs                      # 共享编辑上下文（缓存 MedicalCase/Consultation/Prescription DTO）
│   └── MedicalCaseChangeTracker.cs                    # 变更追踪
├── ViewModels/
│   ├── MedicalCaseMasterDetailViewModel.cs            # 核心 VM（MasterDetailViewModelBase 组合模式）
│   ├── AuditLogViewModel.cs                           # 审计日志 VM
│   ├── Components/
│   │   ├── EditModeStateMachine.cs                    # 编辑状态机（6 状态 / 10 事件 / 转换表驱动 / 线程安全）
│   │   └── PrescriptionPrintHandler.cs                # 处方打印处理器（IPrintService + 诊所配置热更新 + 草稿水印）
│   └── Workspace/
│       ├── ConsultationEditorViewModel.cs             # 诊断编辑子 VM（ChildViewModelBase）
│       ├── PrescriptionEditorViewModel.cs             # 处方编辑子 VM（ChildViewModelBase）
│       └── MedicalCaseCommandsViewModel.cs            # 命令子 VM（ChildViewModelBase, 9 命令, ~555 行）
├── Views/
│   ├── MedicalCaseMasterDetailView.xaml/.cs           # Master-Detail 视图（导航注册）
│   └── AuditLogView.xaml/.cs                          # 审计日志视图
└── LYBT.Desktop.MedicalCase.csproj                    # 项目文件
```

## 核心组件

| 类 | 设计依据 | 职责 |
|---|---|---|
| **MedicalCaseModule** | `[ModuleDependency("PatientsModule"/"HerbsModule"/"FormulaModule")]`；RegisterTypes 注册聚合服务 + 3 Dialog + MasterDetailServices | 模块入口：MedicalCaseEditContext、Query/Command/Lifecycle 三服务、MedicalCaseService 聚合代理、MedicalCaseDetailModelMapper(Singleton)、3 Dialog、AuditLog |
| **MedicalCaseMasterDetailViewModel** | 继承 `MasterDetailViewModelBase<MedicalCaseListDto, MedicalCaseDetailModel>`；组合模式含 ConsultationEditor + PrescriptionEditor 子 VM | 分页列表、详情加载(缓存→子 VM)、聚合保存(AggregateSaveAsync)、删除(CancelMedicalCase)、CreateNewDetail 抛 NotSupportedException |
| **ConsultationEditorViewModel** | `ChildViewModelBase` 子 VM；ConsultationMapper 编译时映射 | InitializeFromDto(ConsultationDetailDto→ConsultationItem)、GetConsultationData(→ConsultationInputDto)、Validate |
| **PrescriptionEditorViewModel** | `ChildViewModelBase` 子 VM；PrescriptionMapper 编译时映射；CollectionChanged 通知父 VM 状态重算 | InitializeFromDto(PrescriptionDetailDto→PrescriptionItem)、GetPrescriptionData(→PrescriptionInputDto)、Validate、HasItems |
| **MedicalCaseCommandsViewModel** | `ChildViewModelBase`；~555 行；9 个 DelegateCommand；delegate 属性由父 VM 赋值（跨子 VM 边界不能用 ObservesProperty） | Save / Suspend / Complete / Print / ExportPdf / EnterEditMode / ImportFormula / CopyHistory / ClearHerbs |
| **EditModeStateMachine** | Dictionary 转换表驱动；`lock` 线程安全；事件在锁外触发防死锁；参考 AuthenticationStateMachine 模式 | 6 状态(ReadOnly/Editing/DirtyEditing/Saving/TransitionBlocked/LeavingConfirming) × 10 事件(EnterEdit/ExitEdit/MakeChange/Save/SaveCompleted/SaveFailed/RequestLeave/LeaveConfirmed/LeaveCancelled) |
| **PrescriptionPrintHandler** | `IPrintService<PrescriptionPrintModel>` 委托；诊所配置 `clinic-settings.json` 热更新(IClinicSettingsService)；草稿水印(IsDraft=非 Completed) | PrintPreviewAsync、ExportPdfAsync(SaveFileDialog)、BuildPrintModel(自动绑定 DoctorName + Discount 折扣计算) |
| **FormulaImportDialogViewModel** | `DialogViewModelBase`；跨模块 `IFormulaSearchProvider`；自动筛选 Validated + Enabled 验方 | 搜索/分类筛选/详情预览/确认导入；过滤逻辑：ValidationStatus==Validated && Status==Enabled |
| **HistoryCopyDialogViewModel** | `DialogViewModelBase`；~549 行；左右双栏；当前患者最近 5 条 → 展开全部 → 全局查询三模式 | ShowMoreCurrentPatient / ToggleAllPatients 命令；搜索(患者名+诊断) + 时间区间筛选；复制时刷新为当前药材价格 |
| **MedicalCaseService** | `IMedicalCaseService` 聚合代理；委托 Query/Command/Lifecycle 三独立服务；自身保留 Coordinator 职责 | LoadDetailsAsync(缓存到 EditContext)、AggregateSaveAsync(诊断+处方聚合保存)、SaveAndCompleteAsync(验证+保存+完成)、SaveAndSuspendAsync、SaveAndCancelAsync |
| **MedicalCaseEditContext** | 共享状态缓存；Scoped 生命周期 | CachedMedicalCase / CachedConsultation / CachedPrescription / ClearCache |

## 依赖关系

### 编译时依赖（ProjectReference）

| 项目 | 用途 |
|---|---|
| LYBT.Desktop.Foundation | BaseApiRepository / Security 基础设施 |
| LYBT.Desktop.Infrastructure | MasterDetailControlBase / ViewModelBase / ChildViewModelBase / DI 扩展 / ToastService |
| LYBT.Desktop.Contracts | IMedicalCaseRepository / IHerbSearchProvider / IFormulaSearchProvider 跨模块接口 |
| LYBT.Desktop.Printing | IPrintService\<T\> / PrescriptionPrintModel / ExportFormat |
| LYBT.Shared.Models | MedicalCaseListDto / MedicalCaseDetailDto / ConsultationDetailDto / PrescriptionDetailDto 等 DTO |
| LYBT.Shared.Primitives | ValidationConstants / CommonStatus / MedicalCaseStatus 枚举 |

### 运行时依赖（ModuleDependency / 跨模块接口）

| 模块 | 接口 | 说明 |
|---|---|---|
| PatientsModule | 患者数据 | 医案必须关联患者 |
| HerbsModule | `IHerbSearchProvider` | 拼音自动补全、AllHerbs 列表 |
| FormulaModule | `IFormulaSearchProvider` | 验方导入弹窗搜索验方 |

### 被依赖

| 消费方 | 接口/控件 | 说明 |
|---|---|---|
| Admin / Clinical 角色台 | `MedicalCaseMasterDetailControl` | 嵌入 MedicalCaseManagementView |
| Clinical 工作区 | `MedicalCaseEditControl` / `MedicalCaseViewControl` | 看诊场景编辑/预览 |

## 设计决策

1. **聚合根模式**: MedicalCase 是唯一聚合根，统一管理 Consultation + Prescription 的生命周期；Consultation/Prescription 不作为独立模块存在（Issue #1463 移除 ConsultationModule 依赖）
2. **Service 四拆分**: MedicalCaseService 聚合代理委托 Query/Command/Lifecycle 三独立服务 + MedicalCaseEditContext 共享缓存，SRP 职责分离
3. **子 VM + delegate 属性模式**: ConsultationEditor / PrescriptionEditor / Commands 三个 ChildViewModelBase 子 VM；CommandsVM 的数据提供者(GetConsultationData/GetPrescriptionData 等)由父 VM 通过 delegate 属性注入，因 DelegateCommand.ObservesProperty 无法跨子 VM 边界工作
4. **转换表驱动状态机**: EditModeStateMachine 用 `Dictionary<(State,Event), State>` 静态转换表 + `lock` 线程安全 + 事件锁外触发防死锁，替代嵌套 if/switch
5. **处方打印热更新**: PrescriptionPrintHandler 通过 IClinicSettingsService 读取 `clinic-settings.json`，支持诊所信息(名称/地址/电话)运行时更新
6. **历史复制 UX**: HistoryCopyDialog 默认显示当前患者最近 5 条已完成记录 → "显示更多"展开本患者全部 → "查看全部患者"切换全局模式
7. **禁用药材过滤**: ImportFormula / CopyHistory 时自动跳过 Status!=Enabled 的药材（T5-P2-21），复制历史处方时刷新为当前药材价格（CODE-08）
8. **Mapperly + ObservableProperty 兼容**: Item 类使用 `[ObservableProperty]` 源生成器时，Mapperly 的 `[MapProperty]` 无法正常工作（RMG005/RMG006），解决方案是 `[MapperIgnoreSource/Target]` + 包装方法手动映射

## 已知陷阱

1. **CreateNewDetail 抛异常**: MedicalCaseMasterDetailViewModel.CreateNewDetail() 抛 NotSupportedException，医案不支持从管理视图新建，仅通过看诊入口创建
2. **子 VM delegate 属性必须在构造后立即赋值**: CommandsVM 的 GetConsultationData / GetPrescriptionData 等 delegate 由父 VM 赋值，若赋值前调用命令会 NullReferenceException
3. **RefreshCanExecute 手动调用**: 子 VM 间 ObservesProperty 不工作，父 VM 状态变更后必须手动调用 Commands.RefreshCanExecute() 更新 CanExecute
4. **HistoryCopyDialogViewModel 字段赋值绕过回调**: 初始化时直接赋值 `_isShowingAllPatients` 而非属性，避免触发 OnIsShowingAllPatientsChanged 导致重复加载
5. **Mapperly 与 CommunityToolkit.Mvvm 源生成器冲突**: `[ObservableProperty]` 生成的属性在 Mapperly 运行时尚未生成，必须用 `[MapperIgnoreTarget("PropertyName")]` 字符串字面量 + 手动映射
6. **FormulaImportDialog 过滤条件**: 仅显示 ValidationStatus==Validated && Status==Enabled 的验方，开发者新增验方筛选逻辑时需同步更新 FilterFormulas()
7. **PrescriptionPrintHandler 草稿水印**: IsDraft 判断条件是 `_medicalCaseService.Current?.CaseStatus != Completed`，非 Completed 状态打印均带草稿水印
8. **AggregateSaveAsync 缓存更新**: 保存成功后自动更新 EditContext 缓存(CachedMedicalCase/CachedConsultation/CachedPrescription)，若绕过此方法直接调用 Repository 会导致缓存不一致
