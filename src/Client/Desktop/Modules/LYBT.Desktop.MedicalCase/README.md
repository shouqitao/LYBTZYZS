# LYBT.Desktop.MedicalCase

> 医案流程编排模块 | 看诊工作流容器 | 三步诊疗流程

## 项目定位

- **层级**: Client Modules层
- **职责**: 作为看诊流程的核心编排容器，协调Consultation(四诊)→Prescriptions(处方)→Summary(总结)三步流程

## 目录结构

```
LYBT.Desktop.MedicalCase/
├── Interfaces/
│   └── IMedicalCaseRepository.cs    # 医案仓储接口
├── Repositories/
│   └── MedicalCaseRepository.cs     # 医案仓储实现
├── Services/                         # 服务层(Epic #2175)
│   ├── MedicalCaseFlowService.cs    # 流程控制服务
│   ├── MedicalCaseStateService.cs   # 状态管理服务
│   ├── MedicalCaseValidationService.cs # 验证服务
│   ├── MedicalCaseSaveService.cs    # 保存服务
│   └── Interfaces/                   # 服务接口
├── ViewModels/
│   ├── MedicalCaseFlowViewModel.cs  # 流程控制ViewModel(核心)
│   ├── MedicalCaseListViewModel.cs  # 医案列表ViewModel
│   └── MedicalCaseSummaryViewModel.cs # 总结ViewModel
├── Views/
│   ├── MedicalCaseFlowView.xaml     # 流程容器视图
│   ├── MedicalCaseListView.xaml     # 列表视图
│   └── MedicalCaseSummaryView.xaml  # 总结视图
└── MedicalCaseModule.cs              # Prism模块注册
```

## 三步诊疗流程

| 步骤 | 组件 | 模块 | 说明 |
|------|------|------|------|
| Step1 | ConsultationFormView | Consultation | 中医四诊数据采集 |
| Step2 | PrescriptionEditorDialog | Prescriptions | 处方开具 |
| Step3 | MedicalCaseSummaryView | MedicalCase | 医案总结与确认 |

## MedicalCaseFlowViewModel

### 核心功能

- 属性(13个): 步骤控制(CurrentStep/CanGoNext/CanGoPrev)、医案标识、加载/保存状态
- 命令: 步骤导航(Next/Prev)、保存、完成、取消、验证
- ISaveable协调: 通过接口调用当前步骤组件的Save/Validate/HasChanges

## IMedicalCaseRepository

| 方法 | 说明 |
|------|------|
| GetByIdAsync | 按ID获取医案 |
| GetByPatientIdAsync | 按患者ID获取医案列表 |
| GetPagedAsync | 分页查询 |
| CreateAsync | 创建医案 |
| UpdateAsync | 更新医案 |
| UpdateStatusAsync | 更新医案状态 |
| CompleteAsync | 完成医案 |
| DeleteAsync | 删除医案 |

## 医案状态流转

| 状态 | 说明 | 可操作 |
|------|------|--------|
| Created | 新建 | 编辑/删除 |
| InProgress | 进行中 | 编辑/保存 |
| PrescriptionConfirmed | 处方已确认 | 查看/完成 |
| Completed | 已完成 | 只读 |

## 设计依据

- MedicalCase作为DDD聚合根，统一编排Consultation和Prescription子实体的生命周期
- 三步流程(四诊->处方->总结)映射中医真实诊疗流程，每步独立保存，支持中断恢复
- 通过ISaveable/IValidatable接口与子步骤组件交互，FlowViewModel不依赖具体子模块实现
- 状态机(Created->InProgress->PrescriptionConfirmed->Completed)确保医案流转合规

## 依赖关系

### 依赖
- LYBT.Desktop.Models (ViewModelBase)
- LYBT.Desktop.Foundation (BaseApiRepository)
- LYBT.Desktop.Contracts (IMedicalCaseApi/ISaveable/IValidatable)
- LYBT.Desktop.Consultation (Step1组件)
- LYBT.Desktop.Prescriptions (Step2组件)
- LYBT.Shared.Models (MedicalCaseDto)
- Prism.Core/Prism.DryIoc (8.x)

### 被依赖
- LYBT.Desktop.Shell (模块加载)
- LYBT.Desktop.Patients (启动医案流程)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-11-20 | Epic #2175服务层重构 |
| 2025-10-29 | 初始版本 |

## 开发笔记

# MedicalCase Module

## 模块定位

MedicalCase是医案管理的**聚合根模块**，整合了处方、诊断等核心业务功能。

## 代码文件结构

### MedicalCaseModule.cs

Prism模块入口，注册DI服务和Dialog。

- 依赖模块: PatientsModule, HerbsModule, FormulaModule
- 注册: `IMedicalCaseRepository`, `IMedicalCaseService`, `IMedicalCaseQueryService`, `IMedicalCaseCommandService`, `IMedicalCaseLifecycleService`
- ~~注册Component: `MedicalCaseWorkspaceCoordinator` (Scoped), `MedicalCaseEditModeStateMachine` (Scoped)~~ 已移除。Coordinator 合并到 MedicalCaseService；StateMachine 已替换为 WorkspaceState immutable record
- 注册Dialog: `FormulaImportDialog`, `HistoryCopyDialog`, `UnsavedChangesDialog`
- 注册ViewModel: `MedicalCaseMasterDetailViewModel`
- 注册MasterDetailServices: `MedicalCaseListDto` / `MedicalCaseDetailModel`

### Interfaces/

#### IDataProvider.cs

数据提供者接口，替代ISaveable模式。Panel仅负责数据收集，由Coordinator统一调用聚合保存API。

| 方法 | 说明 |
|------|------|
| `GetConsultationData()` | 获取诊断数据，返回 `ConsultationInputDto?` |
| `GetPrescriptionData()` | 获取处方数据，返回 `PrescriptionInputDto?` |

#### IMedicalCaseRepository.cs

医案数据仓储接口，RESTful设计。List返回轻量DTO，Detail返回完整DTO。

| 方法 | 说明 |
|------|------|
| `GetPagedAsync(page, pageSize, keyword)` | 分页查询医案列表 |
| `SearchAsync(patientName, diagnosisKeyword, startDate, endDate, page, pageSize)` | 搜索医案（跨医生查询） |
| `GetByIdAsync(id)` | 获取医案详情 |
| `QueryAsync(query)` | 统一查询医案（多种QueryType） |
| `CreateAsync(dto)` | 创建医案 |
| `UpdateAsync(dto)` | 更新医案 |
| `DeleteAsync(id)` | 删除医案 |
| `CloseCaseAsync(medicalCaseId)` | 关闭医案（直接标记Completed） |
| `GetPermissionsAsync(medicalCaseId)` | 获取当前用户对医案的权限 |
| `SaveAsync(medicalCaseId, dto)` | 聚合保存（诊断+处方一次性） |
| `GetBatchDetailsAsync(ids)` | 批量获取详情（最多50个） |
| `SetPrescriptionFlagAsync(id, request)` | 设置处方标志 |
| `UpdateStatusAsync(id, request)` | 更新医案状态 |
| `CancelMedicalCaseAsync(id, request)` | 取消医案 |
| `SuspendAsync(id, request)` | 挂起医案 |
| `RecordPrintCompletedAsync(medicalCaseId, request)` | 打印回写（IsPrinted/PrintCount/LastPrintedAt） |

#### IMedicalCaseService.cs

聚合根门面接口，继承 `IMedicalCaseQueryService` + `IMedicalCaseCommandService` + `IMedicalCaseLifecycleService`，实现SRP职责分离。

#### IValidatable.cs

数据验证接口，由Item类直接实现。

| 成员 | 说明 |
|------|------|
| `Validate()` | 验证数据，返回bool |
| `ValidationMessage` | 验证错误消息 |

### Models/

#### EditState.cs

编辑状态枚举: `Editing` (编辑中), `ReadOnly` (只读)

#### EditType.cs

编辑类型枚举: `Create`, `EditSuspended`, `EditCompleted`, `ViewOnly`

#### WorkspaceMode.cs

工作区模式枚举: `Clinical` (临床看诊), `Management` (管理编辑), `Reception` (前台挂号)

#### WorkspaceState.cs (immutable record)

Replaces old ObservableObject-based WorkspaceState. Uses C# record with `with` expressions for state transitions. Single `OnPropertyChanged(nameof(State))` replaces 10+ individual notifications.

#### MedicalCaseNavigationParameters.cs

导航参数封装类，继承 `NavigationParameters`。

| 工厂方法 | 说明 |
|----------|------|
| `ForClinical(patientId, medicalCaseId?)` | 临床看诊模式参数 |
| `ForManagementView(medicalCaseId, patientId)` | 管理查看模式参数 |
| `ForManagementEdit(medicalCaseId, patientId)` | 管理编辑模式参数 |

引用方: Clinical模块的 `MedicalCaseWorkspaceViewModel`, `PatientSelectionViewModel`, `PendingQueueHandler`

#### MedicalCaseDetailModel.cs

医案详情模型，继承 `ValidatableModelBase`，用于Master-Detail模式。

| 属性分组 | 属性 |
|----------|------|
| 基础信息 | `Id`, `PatientId`, `PatientName`, `Status`, `Remark` |
| 诊断摘要 | `PresentIllness`, `TongueDiagnosis`, `PulseDiagnosis`, `TcmDiagnosis`, `DiagnosisSummary` |
| 处方摘要 | `HerbCount`, `DoseCount`, `ReferencedFormulas`, `PrescriptionItems`, `PrescriptionSummary` |
| 审计信息 | `CreatedAt`, `UpdatedAt`, `DoctorName` |
| 计算属性 | `ConsultationDate` (=CreatedAt), `StatusText`, `FormulaSource`, `HasPrescriptionItems` |
| 方法 | `Clone()` |

#### Models/Items/ConsultationItem.cs

诊断数据Item，继承 `BindableBase`，实现 `IDataProvider` + `IValidatable`。用于XAML绑定。

| 属性分组 | 属性 |
|----------|------|
| 标识字段 | `Id`, `MedicalCaseId`, `PatientId`, `UserId` |
| 展示字段 | `PatientName`, `DoctorName` |
| 诊断核心 | `PresentIllness`, `TongueDiagnosis`, `PulseDiagnosis`, `TcmDiagnosis` |
| 审计字段 | `CreatedAt`, `UpdatedAt` |
| UI状态 | `IsSelected`, `IsExpanded` |
| 计算属性 | `IsDiagnosisComplete`, `DisplayText` |
| 方法 | `Reset()`, `Validate()` |

#### Models/Items/PrescriptionItem.cs

处方数据Item，继承 `BindableBase`，实现 `IDataProvider` + `IValidatable`。用于XAML绑定。

| 属性分组 | 属性 |
|----------|------|
| 标识字段 | `Id`, `PrescriptionNumber`, `MedicalCaseId` |
| 处方核心 | `DosageCount`, `Usage`, `Advice`, `ReferencedFormulas`, `Remark`, `Discount` |
| 价格字段 | `SingleDosePrice`, `TotalWeight` |
| 药材列表 | `Items` (ObservableCollection\<PrescriptionItemDto\>) |
| 警告字段 | `DuplicateWarning`, `MissingDrugWarning` |
| UI状态 | `IsSelected`, `IsExpanded`, `IsReadOnly`, `ValidationEnabled` |
| 计算属性 | `ItemCount`, `HasItems`, `IsValid`, `TotalPrice`, `DisplayText` |
| 方法 | `Clear()`, `Reset()`, `NotifyItemsChanged()`, `Validate()` |

### Mappers/

#### ConsultationMapper.cs

Mapperly源生成映射器。`ConsultationDetailDto` <-> `ConsultationItem` <-> `ConsultationInputDto`。

| 方法 | 说明 |
|------|------|
| `ToItem(dto)` | DTO -> Item（API加载） |
| `ToDto(item)` | Item -> DTO（展示用） |
| `ToInputDto(item)` | Item -> InputDto（保存到API） |

#### PrescriptionMapper.cs

Mapperly源生成映射器。`PrescriptionDetailDto` <-> `PrescriptionItem` <-> `PrescriptionInputDto`。

| 方法 | 说明 |
|------|------|
| `ToItem(dto)` | DTO -> Item（含Items集合手动映射） |
| `ToDto(item)` | Item -> DTO（含Items集合） |
| `ToInputDto(item)` | Item -> InputDto（含药材项转换） |

#### MedicalCaseDetailModelMapper.cs

Mapperly源生成映射器。`MedicalCaseDetailDto` -> `MedicalCaseDetailModel`。需手动映射嵌套的Consultation和Prescription DTO中的字段。

| 方法 | 说明 |
|------|------|
| `ToItem(dto)` | DTO -> Model（含嵌套字段展开） |
| `ToInputDto(model)` | Model -> InputDto |

引用方: `MedicalCaseMasterDetailViewModel`

#### MedicalCaseCloneMapper.cs

Mapperly源生成深拷贝映射器，用于变更检测和回滚。

| 方法 | 说明 |
|------|------|
| `Clone(MedicalCaseDetailDto)` | 深拷贝医案详情DTO |
| `Clone(ConsultationDetailDto)` | 深拷贝诊断DTO |
| `Clone(PrescriptionDetailDto)` | 深拷贝处方DTO |

引用方: 仅 `MedicalCaseService`

### Repositories/

#### MedicalCaseRepository.cs

`IMedicalCaseRepository` 实现，基于 `IMedicalCaseDataSource` 抽象层，支持Local/Remote模式切换。

- 标准CRUD通过 `_dataSource` 实现（Local和Remote通用）
- 高级查询（Search, Query, GetPermissions, SetPrescriptionFlag, UpdateStatus, Suspend, RecordPrintCompleted）优先使用 `_api`（Remote模式），本地模式降级处理
- 批量获取 `GetBatchDetailsAsync`: Remote用API批量请求，Local逐个获取

### Services/

#### MedicalCaseService.cs

聚合根门面模式实现，实现 `IMedicalCaseService`（Query + Command + Lifecycle三职责）。

| 属性 | 说明 |
|------|------|
| `MedicalCaseId` | 聚合根ID |
| `Current` | 当前医案详情DTO |
| `CurrentConsultation` | 当前诊断DTO |
| `CurrentPrescription` | 当前处方DTO |
| `HasChanges` | 是否有未保存的变更（检测MedicalCase/Consultation/Prescription三层变化） |

| 方法分组 | 方法 |
|----------|------|
| 初始化 | `InitializeAsync(entityId)`, `ReloadAsync()` |
| CRUD | `SaveAsync()`, `DeleteAsync()`, `GetByIdSimpleAsync(id)`, `GetPagedAsync(page, pageSize, searchText)`, `QueryAsync(query)` |
| 业务命令 | `SetPrescriptionFlagAsync(id, request)`, `CloseCaseAsync(id)`, `GetUnfinishedCaseByPatientIdAsync(patientId, doctorId, checkAllDoctors)`, `DeleteMedicalCaseAsync(id)`, `UpdateStatusAsync(id, request)`, `SuspendViaApiAsync(id, consultationData)`, `CancelMedicalCaseViaApiAsync(id, reason)` |
| 生命周期 | `CreateMedicalCaseAsync(patientId)`, `SuspendAsync(id)`, `CancelMedicalCaseAsync(id, reason)`, `CompleteMedicalCaseAsync(id)`, `ResumeSuspendedAsync(id)` |

### ViewModels/

#### MedicalCaseMasterDetailViewModel.cs

继承 `MasterDetailViewModelBase<MedicalCaseListDto, MedicalCaseDetailModel>`，医案Master-Detail视图模型。

| 属性 | 类型 | 说明 |
|------|------|------|
| `DetailTitle` | string | 详情标题（编辑/查看 + 患者名） |
| `SelectedPatientName` | string | 选中项的患者姓名 |
| `Consultation` | ConsultationItem? | 诊断数据模型 |
| `Prescription` | PrescriptionItem? | 处方数据模型 |
| `AllHerbs` | ObservableCollection\<HerbListDto\> | 药材列表（拼音补全用） |

| 重写方法 | 说明 |
|----------|------|
| `LoadListAsync()` | 分页加载医案列表 |
| `LoadDetailAsync(item)` | 加载详情并初始化编辑模型 |
| `CreateNewDetail()` | 不支持新建（抛出NotSupportedException） |
| `SaveDetailAsync(detail)` | 构建聚合DTO保存 |
| `DeleteItemAsync(item)` | 删除医案 |
| `OnNavigatedTo(context)` | 预加载药材列表 |

### ViewModels/Components/

#### MedicalCaseEditModeStateMachine.cs -- 已删除

已删除 - 替换为 `Models/WorkspaceState.cs` immutable record。状态机的可变状态和10+个 PropertyChanged 通知被替换为 C# record 的 `with` 表达式和单次 `OnPropertyChanged(nameof(State))` 通知。

#### MedicalCaseWorkspaceCoordinator.cs -- 已删除

已合并到 MedicalCaseService (commit ece8c5d0a)。Coordinator 的数据加载、聚合保存、生命周期操作职责全部迁移到 `IMedicalCaseService` 接口。

#### PrescriptionPrintHandler.cs

处方打印处理器，负责打印预览和打印数据模型构建。

| 方法 | 说明 |
|------|------|
| `PrintPreviewAsync(medicalCaseId, prescriptionProvider, currentPatient, consultationData)` | 执行处方打印预览 |
| `BuildPrescriptionDetailDto(medicalCaseId, prescriptionProvider)` | 构建处方详情DTO（缓存优先） |

内部方法:
- `BuildPrintModel()` - 组装 `PrescriptionPrintModel`，自动绑定DoctorName，含Discount折扣计算
- `RecordPrintCompletedAsync()` - 打印回写状态到服务端
- `CalculateAge()` - 计算患者年龄

附带结果类型: `PrintResult`

引用方: Clinical模块的 `MedicalCaseWorkspaceViewModel`

#### WorkspaceState.cs (旧 ObservableObject 版本) -- 已删除

已删除 - 替换为 `Models/WorkspaceState.cs` immutable record。见 Models 章节。

### ViewModels/Workspace/

Composite ViewModel pattern child VMs. Created by parent (MedicalCaseWorkspaceViewModel), not container-resolved.

#### ConsultationEditorViewModel.cs

Child VM wrapping ConsultationItem. Handles initialization from DTO via ConsultationMapper.

#### PrescriptionEditorViewModel.cs

Child VM wrapping PrescriptionItem. Handles initialization, collection change notification to parent.

#### MedicalCaseCommandsViewModel.cs (455 lines)

Child VM for aggregate root commands (save/suspend/complete/print/import/clear).
Constructor deps: IMedicalCaseWorkspaceContext, IWorkspaceHost, IMedicalCaseService, PrescriptionPrintHandler, IDialogService?
Import operations (formula/history/clear) migrated from PrescriptionImportHandler.

### Dialogs/

#### FormulaImportDialog.xaml.cs

验方导入弹窗视图，继承UserControl。

#### FormulaImportDialogViewModel.cs

继承 `DialogViewModelBase`，验方导入弹窗ViewModel。从经验方库搜索选择验方，批量导入药材到处方。

| [ObservableProperty] 属性 | 说明 |
|---------------------------|------|
| `SearchText` | 搜索文本 |
| `Categories` | 分类列表 |
| `SelectedCategory` | 选中的分类（默认"全部"） |
| `FilteredFormulas` | 筛选后的验方列表 |
| `SelectedFormula` | 选中的验方（触发ConfirmCommand刷新） |
| `SelectedFormulaDetail` | 选中验方的详情（FormulaViewControl预览） |
| `SelectedFormulaHerbs` | 选中验方的药材列表 |
| `StatusMessage` | 状态消息 |
| `LoadingMessage` | 加载提示消息 |

| 重写方法 | 说明 |
|----------|------|
| `OnDialogOpenedCore(parameters)` | 打开时加载验方列表 |
| `CanConfirm()` | 选中验方且有药材时可确认 |
| `Confirm()` | 返回SelectedFormula和SelectedHerbs |

过滤逻辑: 仅显示 `Validated` 且 `Enabled` 的验方，支持名称/功效/适应症搜索

#### HistoryCopyDialog.xaml.cs

历史处方复制弹窗视图，继承UserControl。

#### HistoryCopyDialogViewModel.cs

继承 `DialogViewModelBase`，历史医案复制弹窗ViewModel。支持左右双栏布局。

| [ObservableProperty] 属性 | 说明 |
|---------------------------|------|
| `PatientName` | 患者姓名 |
| `SearchText` | 搜索文本（患者姓名/中医诊断） |
| `StartDate`, `EndDate` | 时间区间筛选 |
| `FilteredCases` | 筛选后的医案列表 |
| `SelectedCase` | 选中的医案 |
| `SelectedCaseDetail` | 选中医案的详情（MedicalCaseViewControl预览） |
| `StatusMessage` | 状态消息 |
| `IsShowingAllPatients` | 是否显示全部患者模式 |
| `IsShowingAllCurrentPatient` | 是否显示当前患者全部记录 |

| 命令 | 说明 |
|------|------|
| `ShowMoreCurrentPatientCommand` | 显示更多当前患者记录 |
| `ToggleAllPatientsCommand` | 切换全部患者模式 |

| 重写方法 | 说明 |
|----------|------|
| `OnDialogOpenedCore(parameters)` | 接收PatientId/PatientName，加载历史医案 |
| `CanConfirm()` | 选中医案且有处方药材时可确认 |
| `Confirm()` | 返回SelectedCase和SelectedItems |

UX设计: 默认显示当前患者最近5条已完成记录，支持展开全部和切换全局查询

#### UnsavedChangesDialog.xaml.cs

未保存修改确认弹窗视图，继承UserControl。

#### UnsavedChangesDialogViewModel.cs

继承 `ObservableObject`，实现 `IDialogAware`。提供三选项: 保存(Yes)/放弃(No)/取消(Cancel)。

| 命令 | 说明 |
|------|------|
| `SaveCommand` | 保存修改后返回 (ButtonResult.Yes) |
| `DiscardCommand` | 放弃修改直接返回 (ButtonResult.No) |
| `CancelCommand` | 留在编辑界面 (ButtonResult.Cancel) |

### Controls/

#### MedicalCaseMasterDetailControl.xaml.cs

继承 `MasterDetailControlBase`，可复用业务控件。供Admin和Clinical角色台MedicalCaseManagementView使用。

引用方: Admin模块和Clinical模块的 `MedicalCaseManagementView`

#### MedicalCaseEditControl.xaml.cs

医案编辑控件，继承UserControl。支持Full/Compact两种显示模式。

| DependencyProperty | 类型 | 说明 |
|--------------------|------|------|
| `IsCompactMode` | bool | 紧凑模式（Workspace场景） |
| `PatientName` | string | 患者姓名（Full模式） |
| `ConsultationDate` | DateTime | 就诊日期 |
| `DoctorName` | string | 医生姓名 |
| `Status` | MedicalCaseStatus | 状态 |
| `Consultation` | object | 诊断数据对象（duck typing绑定） |
| `Prescription` | object | 处方数据对象（duck typing绑定） |
| `FormulaSource` | string | 方源 |
| `AllHerbs` | IEnumerable | 药材列表 |
| `IsPrescriptionEnabled` | bool | 是否启用处方区 |
| `NeedsPrescription` | bool | 是否需要处方 |
| `ImportFormulaCommand` | ICommand | 导入经验方命令 |
| `ImportHistoryCommand` | ICommand | 导入历史处方命令 |
| `ClearAllCommand` | ICommand | 清空药材命令 |
| `CreatedAt` | DateTime | 创建时间 |
| `UpdatedAt` | DateTime? | 更新时间 |
| `ErrorsSource` | ValidationErrorsAccessor | 验证错误源 |

引用方: Clinical模块的 `MedicalCaseWorkspaceView`

#### MedicalCaseViewControl.xaml.cs

医案预览控件，继承UserControl。支持Full/Compact两种显示模式。

| DependencyProperty | 类型 | 说明 |
|--------------------|------|------|
| `IsCompactMode` | bool | 紧凑模式 |
| `Detail` | MedicalCaseDetailModel | 医案详情（Full模式） |
| `Consultation` | object | 诊断数据对象（Compact模式） |
| `Prescription` | object | 处方数据对象（Compact模式） |
| `AllHerbs` | IEnumerable | 药材列表 |
| `ShowPrintButton` | bool | 是否显示打印按钮 |
| `PrintCommand` | ICommand | 打印命令 |
| `ShowAuditInfo` | bool | 是否显示审计信息（Full模式） |

引用方: Clinical模块的 `MedicalCaseWorkspaceView`, HistoryCopyDialog

### Extensions/

#### PrescriptionImportExtensions.cs

处方导入扩展方法，替代PrescriptionImportHandler。

| 方法 | 说明 |
|------|------|
| `ToPrescriptionItemDtos(FormulaDetailDto, List<FormulaHerbItemDto>)` | 验方药材转PrescriptionItemDto列表 |
| `ToPrescriptionItemDtos(List<PrescriptionItemDto>)` | 历史处方药材直接返回只读列表 |

引用方: Clinical模块的 `MedicalCaseWorkspaceViewModel`

## 架构演进记录

### 2025-01 迁入的功能

| 来源模块 | 迁入组件 | 位置 | 状态 |
|----------|----------|------|------|
| Prescriptions | `PrescriptionHerbItem` | `Models/Items/` | 已删除 - 由PrescriptionItemDto替代 |

### OpenSpec: create-printing-module (2025-01) 迁出的功能

| 迁出组件 | 目标模块 | 新位置 |
|----------|----------|--------|
| `IPrescriptionPrintService` | LYBT.Desktop.Printing | 已替换为 `IPrintService<T>` |
| `PrescriptionPrintService` | LYBT.Desktop.Printing | `Services/` |
| `PrescriptionPrintModel` | LYBT.Desktop.Printing | `Models/` |
| `PrescriptionPrintTemplate.xaml` | LYBT.Desktop.Printing | `Templates/` |

### 模块依赖

```csharp
[ModuleDependency("PatientsModule")] // 病历依赖患者
// [已移除] PrescriptionsModule - 所有功能已迁移到本模块
// [已移除] ConsultationModule - MedicalCase是聚合根，不应依赖子实体模块
```

## 关键类型

### Entity→DTO→Item模式

MedicalCase作为聚合根，持有诊断和处方的Item类：

| 层级 | 诊断(Consultation) | 处方(Prescription) |
|------|-------------------|-------------------|
| Entity | 服务端Consultation | 服务端Prescription |
| DTO | ConsultationDetailDto | PrescriptionDetailDto |
| Item | ConsultationItem | PrescriptionItem |

### ConsultationItem

位置: `Models/Items/ConsultationItem.cs`

诊断数据Item，用于XAML绑定。OpenSpec: consolidate-panel-viewmodels

核心属性:
- `PresentIllness`, `TongueDiagnosis`, `PulseDiagnosis`, `TcmDiagnosis`
- `IsDiagnosisComplete` - 验证必填字段

方法:
- ~~`FromDto()`, `ToDto()`~~ - 已废弃，请使用 `ConsultationMapper`
- `ToInputDto()`

### PrescriptionItem

位置: `Models/Items/PrescriptionItem.cs`

处方数据Item，用于XAML绑定。OpenSpec: consolidate-panel-viewmodels

核心属性:
- `DosageCount`, `Usage`, `Advice`, `ReferencedFormulas`, `Remark`
- `Items` (ObservableCollection<PrescriptionItemDto>) - 药材列表 (OpenSpec: unify-control-data-binding)
- `ItemCount`, `SingleDosePrice`, `TotalPrice`, `HasItems`, `IsValid`

方法:
- ~~`FromDto()`, `ToDto()`~~ - 已废弃，请使用 `PrescriptionMapper`
- `ToInputDto()`, `Clear()`

### 处方药材项类型演进 (OpenSpec: unify-control-data-binding)

**当前架构**: 统一使用 `PrescriptionItemDto` (LYBT.Shared.Models.Contracts.Prescriptions)

**已删除的类型**:
- `PrescriptionHerbItem` - 旧版处方药材项ViewModel (2026-01已从代码库移除)
- `HerbItemDto` (LYBT.Desktop.Herbs.Models.Items) - 桌面端中间类型，已由PrescriptionItemDto替代

**类型流向**:
```
API Response → PrescriptionItemDto → PrescriptionItem.Items → HerbListControl → UI
```

**注意**: PrescriptionPanelViewModel已删除，改用PrescriptionItem

### 打印服务

OpenSpec: create-printing-module - 打印功能已迁移到独立的 `LYBT.Desktop.Printing` 模块

- 通过 `IPrintService<PrescriptionPrintModel>` 接口使用打印功能
- `PrescriptionPrintHandler` 负责组装打印数据模型

## Mapperly与CommunityToolkit.Mvvm源生成器兼容性

**重要**: Item类使用`[ObservableProperty]`源生成器时，Mapperly的`[MapProperty]`属性无法正常工作。

### 问题原因

Mapperly源生成器在编译时验证属性是否存在，但`[ObservableProperty]`生成的属性（如`CaseStatus`、`CompletedAt`）在Mapperly运行时尚未生成，导致RMG005/RMG006错误。

### 解决方案

对于源生成的属性，使用`[MapperIgnoreSource]`/`[MapperIgnoreTarget]`忽略，在包装方法中手动映射：

```csharp
// 错误模式（会导致编译错误）
[MapProperty(nameof(Dto.CaseStatus), "CaseStatus")]
public partial Item ToItemCore(Dto dto);

// 正确模式
[MapperIgnoreTarget("CaseStatus")]  // 字符串字面量
[MapperIgnoreSource(nameof(Dto.CaseStatus))]
public partial Item ToItemCore(Dto dto);

public Item ToItem(Dto dto)
{
    var item = ToItemCore(dto);
    item.CaseStatus = dto.CaseStatus;  // 手动映射
    return item;
}
```

### 受影响的Mapper

- `MedicalCaseItemMapper.cs` - CaseStatus, CompletedAt
- `ConsultationMapper.cs` - IsSelected, IsExpanded, 审计字段
- `PrescriptionMapper.cs` - IsSelected, IsExpanded, IsReadOnly, Items

## 注意事项

1. **命名空间**: 从Prescriptions迁入的类使用`LYBT.Desktop.MedicalCase.*`命名空间
2. **依赖方向**: MedicalCase作为聚合根，不应依赖Consultation等子实体模块
3. **打印依赖**: 通过项目引用 `LYBT.Desktop.Printing` 使用打印服务
4. **Mapper属性**: 对`[ObservableProperty]`生成的属性，必须使用字符串字面量而非`nameof()`
