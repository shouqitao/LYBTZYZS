# LYBT.Desktop.Patients

> 患者管理模块 | 工作流入口 | Master-Detail 组合模式

## 项目定位

- **层级**: Client Modules 层
- **职责**: 提供患者档案管理和看诊工作流入口，管理待诊队列、支持快速建档/读卡建档、启动医案流程

## 目录结构

```
LYBT.Desktop.Patients/
├── CommandHandlers/
│   ├── IPatientCommandHandler.cs           # 患者 CommandHandler 接口
│   └── PatientCommandHandler.cs            # CommandHandler 实现
├── Controls/
│   ├── PatientEditControl.xaml/.xaml.cs     # 患者编辑控件 (双向绑定+验证)
│   ├── PatientMasterDetailControl.xaml/.xaml.cs  # Master-Detail 可复用控件
│   ├── PatientSelectionControl.xaml/.xaml.cs     # 患者选择控件 (左右分栏)
│   └── PatientViewControl.xaml/.xaml.cs     # 患者只读预览控件
├── Interfaces/
│   ├── IPatientRepository.cs               # 患者仓储接口 (CRUD+搜索+批量)
│   ├── IPatientSearchCache.cs              # 搜索缓存接口 (LRU)
│   └── IPatientService.cs                  # 患者业务服务接口
├── Mappers/
│   └── PatientMapper.cs                    # Mapperly 编译时映射器
├── Models/
│   ├── Display/
│   │   └── PatientDetailDisplayModel.cs    # 只读展示模型
│   ├── Items/
│   │   └── PatientItem.cs                  # 列表项 UI 模型 (BindableBase)
│   ├── ImportWizardStep.cs                 # 导入向导枚举 + ImportProgressInfo
│   ├── PatientDetailModel.cs               # Detail 编辑模型 (ValidatableModelBase)
│   └── PatientViewState.cs                 # UI 状态模型 (ObservableObject)
├── Repositories/
│   └── PatientRepository.cs                # 仓储实现 (Repository 抽象层)
├── Services/
│   ├── PatientCardReaderIntegration.cs     # 读卡器集成服务
│   ├── PatientImportDataMapper.cs          # Excel 数据映射器
│   ├── PatientImportExecutor.cs            # BackgroundWorker 导入执行器
│   ├── PatientSearchCache.cs               # LRU 搜索缓存 (线程安全)
│   ├── PatientSearchManager.cs             # 搜索分页管理器
│   ├── PatientService.cs                   # 业务服务 (统一错误处理)
│   ├── PendingQueueManager.cs              # 待诊队列管理器
│   └── UnfinishedCaseHandler.cs            # 未完成医案处理器
├── ViewModels/
│   ├── Components/
│   │   ├── MedicalCaseStartCoordinator.cs  # 医案启动协调器
│   │   └── PatientValidator.cs             # FluentValidation 验证器
│   └── PatientMasterDetailViewModel.cs     # 核心 ViewModel (组合模式)
└── PatientsModule.cs                        # Prism 模块注册
```

## 核心接口

| 接口 | 职责 |
|------|------|
| IPatientRepository | 患者仓储 (CRUD + 搜索 + 批量导入导出 + 软删除恢复) |
| IPatientService | 业务服务 (统一 CommandResult 错误处理) |
| IPatientSearchCache | LRU 搜索缓存 (用户隔离 + 事件驱动失效) |

## 关键功能

| 功能 | 实现 |
|------|------|
| Master-Detail 管理 | PatientMasterDetailViewModel + MasterDetailControlBase |
| 待诊队列 | PendingQueueManager + UnfinishedCaseHandler |
| 读卡建档 | PatientCardReaderIntegration (身份证查找/创建) |
| Excel 导入 | PatientImportDataMapper + PatientImportExecutor |
| 搜索缓存 | PatientSearchCache (LRU, 10 条, 5 分钟过期) |
| 医案启动 | MedicalCaseStartCoordinator (多医生场景检测) |

## 设计依据

- Repository 通过 IPatientRepository 抽象支持 Local/Remote 模式无缝切换
- 组件化架构: ViewModel 功能拆分为 Components 和 Services，避免单一 ViewModel 膨胀
- 搜索缓存使用 LRU 策略，支持用户隔离和事件驱动失效
- Mapperly 编译时映射替代运行时 AutoMapper，零运行时开销

## 依赖关系

### 依赖
- LYBT.Desktop.Foundation (BaseApiRepository/Security)
- LYBT.Desktop.Infrastructure (MasterDetailControlBase/ViewModelBase/Services)
- LYBT.Desktop.Models (ValidatableModelBase)
- LYBT.Desktop.Contracts (IPatientApi/IPatientRepository)
- LYBT.Shared.Models (PatientListDto/PatientDetailDto/PatientInputDto)
- Prism.DryIoc (8.x)

### 被依赖
- LYBT.Desktop.Admin (PatientManagementView 嵌入 PatientMasterDetailControl)
- LYBT.Desktop.Clinical (PatientSelectionControl 嵌入临床工作台)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 目录结构和接口表更新 |
| 2025-12-04 | 按 README 规范重写文档 |
| 2025-10-29 | 初始版本 |

## 开发笔记

# LYBT.Desktop.Patients CLAUDE.md

## 架构决策

- Repository 层使用 Repository 抽象 (IPatientRepository)，支持 Local/Remote 模式无缝切换
- CommandHandler 模式 (IPatientCommandHandler) 存在但未被注册到 DI 容器，实际业务通过 PatientService 处理
- 组件化架构: ViewModel 功能拆分为 Components (PatientValidator, MedicalCaseStartCoordinator) 和 Services (PatientSearchManager, PendingQueueManager 等)
- 搜索缓存使用 LRU 策略 (PatientSearchCache)，支持用户隔离和事件驱动失效
- Mapperly 编译时代码生成替代运行时 AutoMapper 映射
- PatientsModule 依赖 AuthenticationModule 和 UsersModule

## 代码文件结构

### 模块注册

| 文件 | 类 | 说明 |
|------|-----|------|
| PatientsModule.cs | `PatientsModule : IModule` | Prism 模块注册，注册 Repository/Service/Validator/SearchCache/Coordinator 等全部服务 |

### CommandHandlers/

| 文件 | 类 | 说明 |
|------|-----|------|
| IPatientCommandHandler.cs | `IPatientCommandHandler : ICommandHandlerBase<PatientListDto, PatientDetailDto, PatientInputDto>` | 患者 CommandHandler 接口，扩展方法: SearchByNameAsync, SearchByPhoneAsync, HasMedicalCasesAsync |
| PatientCommandHandler.cs | `PatientCommandHandler : IPatientCommandHandler` | 实现类，封装 IPatientRepository 提供 CRUD + 搜索 + 医案关联检查 |

### Controls/

| 文件 | 类 | 说明 |
|------|-----|------|
| PatientEditControl.xaml.cs | `PatientEditControl : UserControl` | 患者编辑控件，DependencyProperty: PatientName, PinYinCode, Gender, GenderOptions, BirthDate, Age, IdNumber, PhoneNumber, Address, Status, StatusOptions, ShowStatus, ErrorsSource |
| PatientMasterDetailControl.xaml.cs | `PatientMasterDetailControl : MasterDetailControlBase` | 患者 Master-Detail 控件，构造时初始化 PatientMasterDetailViewModel，供 Admin/Clinical 角色台复用 |
| PatientSelectionControl.xaml.cs | `PatientSelectionControl : UserControl` | 患者选择控件（左右分栏: 列表+详情），PatientDoubleClicked 事件，通过反射从 DataContext 获取 SelectedPatient 和 StartMedicalCaseCommand |
| PatientViewControl.xaml.cs | `PatientViewControl : UserControl` | 患者只读预览控件，DependencyProperty 覆盖完整患者信息: 基本信息(Name/PinYinCode/Gender/BirthDate/Age/IdNumber/IdType/MaritalStatus/BloodType)、联系信息(PhoneNumber/Address)、紧急联系人、病史、就诊统计、系统信息 |

### Interfaces/

| 文件 | 类 | 说明 |
|------|-----|------|
| IPatientRepository.cs | `IPatientRepository` | 患者仓储接口，方法: GetPagedAsync, GetByIdAsync, CreateAsync, UpdateAsync, DeleteAsync, SearchAsync, GetByIdNumberAsync, BatchImportAsync, ExportTemplateAsync, ExportPatientsAsync, RestoreAsync, BatchDeleteAsync |
| IPatientSearchCache.cs | `IPatientSearchCache` | 搜索缓存接口，方法: Get, Set, Invalidate |
| IPatientService.cs | `IPatientService` | 患者业务服务接口，方法: CreatePatientAsync, UpdatePatientAsync, DeletePatientAsync, BatchDeletePatientsAsync, SearchPatientsAsync, GetPatientsPagedAsync, GetByIdAsync |

### Mappers/

| 文件 | 类 | 说明 |
|------|-----|------|
| PatientMapper.cs | `PatientMapper` (Mapperly partial) | 编译时映射器，映射: PatientDetailDto -> PatientItem (ToItem), PatientItem -> PatientDetailDto (ToDto), PatientItem -> PatientInputDto (ToInputDto，带 Id 手动处理) |

### Models/

| 文件 | 类 | 说明 |
|------|-----|------|
| PatientDetailModel.cs | `PatientDetailModel : ValidatableModelBase` | 患者详情编辑模型，DataAnnotation 验证 (Name 必填, PhoneNumber, IdNumber, Address, AllergyHistory, MedicalHistory 长度限制)，计算属性 Age/IsNew，方法: CreateNew(), Clone()。设置 Name 时自动生成 PinYinCode |
| ImportWizardStep.cs | `ImportWizardStep` (enum) + `ImportProgressInfo` (class) | 导入向导步骤枚举 (TemplateDownload/FileSelection/DataPreview/ImportExecution) 和进度信息类 |
| PatientViewState.cs | `PatientViewState : ObservableObject` (CommunityToolkit.Mvvm) | UI 状态模型，[ObservableProperty]: SelectedPatient, Patients, SearchKeyword, GenderFilter, AgeRangeMin/Max, ShowNewPatientsOnly, ShowAllergicPatientsOnly, CurrentPage, PageSize, TotalCount, SortBy, IsDescending, IsLoading, IsSearching, IsEditMode, IsBatchSelectMode, SelectedPatientIds, StatusMessage, ErrorMessage。计算属性: TotalPages, HasPreviousPage, HasNextPage, HasData, IsEmpty。方法: ResetFilters(), ClearSelection(), SelectAll() |

### Models/Display/

| 文件 | 类 | 说明 |
|------|-----|------|
| PatientDetailDisplayModel.cs | `PatientDetailDisplayModel` | 只读展示模型，格式化属性: AgeDisplay, GenderDisplay, Summary, VisitInfo |

### Models/Items/

| 文件 | 类 | 说明 |
|------|-----|------|
| PatientItem.cs | `PatientItem : BindableBase` (Prism) | 列表项 UI 模型，属性: Id, Name, Gender, BirthDate, PhoneNumber, Address, IdNumber, MedicalHistory, AllergyHistory, CreatedAt, LastVisitTime, VisitCount, IsSelected, IsHighlighted。计算属性: GenderDisplay, Age (从 BirthDate 实时计算), DisplayText, IsNewPatient (30天内首次就诊)。方法: UpdateFromDto() |

### Repositories/

| 文件 | 类 | 说明 |
|------|-----|------|
| PatientRepository.cs | `PatientRepository : IPatientRepository` | 仓储实现，依赖 IPatientRepository (Local/Remote 透明切换) + 可选 IPatientApi (仅 Remote 模式用于批量导入/导出)。标准 CRUD 委托 Repository，批量导入/导出/模板下载通过 IPatientApi (Remote 专有) |

### Services/

| 文件 | 类 | 说明 |
|------|-----|------|
| PatientService.cs | `PatientService : IPatientService` | 业务服务，封装 IPatientRepository，统一 CommandResult 错误处理，[SVC] 日志前缀。方法: CreatePatientAsync, UpdatePatientAsync, DeletePatientAsync, BatchDeletePatientsAsync, SearchPatientsAsync, GetPatientsPagedAsync, GetByIdAsync |
| PatientSearchCache.cs | `PatientSearchCache : IPatientSearchCache` | LRU 搜索缓存 (最多 10 条, 5 分钟过期)，线程安全 (lock)，用户隔离 (SessionManager.CurrentUserId)，事件驱动失效 (PatientEvents.Created/Updated + CacheEvents.Invalidated + SessionChanged) |
| PatientSearchManager.cs | `PatientSearchManager` | 搜索分页管理器，集成 PatientService + IPatientSearchCache。方法: ExecuteSearchAsync, LoadInitialPatientsAsync, LoadCurrentPageAsync, PreviousPageAsync, NextPageAsync, InvalidateCache。事件: SearchCompleted |
| PatientImportDataMapper.cs | `PatientImportDataMapper` | Excel 数据映射器，方法: CreatePatientDtoFromRow (支持"出生日期"列优先/"年龄"列兼容), IsImportRowEmpty, ValidateImportRequiredFields |
| PatientImportExecutor.cs | `PatientImportExecutor : IDisposable` | BackgroundWorker 导入执行器，事件: ProgressChanged, ImportCompleted。方法: StartImport, CancelImport。内部类型: ImportRowResult (struct), ImportResult (record), ImportCompletedEventArgs |
| PatientCardReaderIntegration.cs | `PatientCardReaderIntegration : IPatientCardReaderIntegration` | 读卡器集成服务，方法: FindPatientByIdNumberAsync, QuickCreatePatientAsync, FindOrCreatePatientAsync, GetPatientDetailByIdAsync |
| PendingQueueManager.cs | `PendingQueueManager : IPendingQueueManager` | 待诊队列管理器，依赖 IMedicalCaseApi + PatientService + UnfinishedCaseHandler + ISessionManager。方法: LoadPendingCasesAsync, LoadPatientForPendingCaseAsync, RemoveFromQueue, ClearQueue。事件: PendingQueueLoaded, PatientLoaded |
| UnfinishedCaseHandler.cs | `UnfinishedCaseHandler` | 未完成医案处理器，依赖 IMedicalCaseQueryService，内置缓存 (_pendingCaseCache)。方法: CheckUnfinishedMedicalCaseAsync (支持多医生场景), CloseAndCreateNewCaseAsync, CloseOnlyAsync, SetCache, ClearCache, GetCachedMedicalCaseId。事件: CaseCheckCompleted, CaseClosed |

### ViewModels/Components/

| 文件 | 类 | 说明 |
|------|-----|------|
| PatientValidator.cs | `PatientValidator` | FluentValidation 集成验证器，方法: ValidatePatientInputAsync (异步 DTO 验证), ValidateBasicInfo, ValidateIdNumber, ValidateAge, ValidateEmergencyContact, IsValid, ConvertToInputDto |
| MedicalCaseStartCoordinator.cs | `MedicalCaseStartCoordinator` | 医案启动协调器，处理完整看诊启动流程。内部类型: StartResult (enum: ContinueExisting/CreateNew/CloseOnly/Cancelled/BlockedByOtherDoctor/Error), StartResultData。方法: CheckUnfinishedCaseAsync, IsOtherDoctorCase, GetOtherDoctorName, ContinueExistingCaseAsync, CloseAndCreateNewAsync, CloseOnlyAsync, HandleUserChoiceAsync |

### ViewModels/

| 文件 | 类 | 说明 |
|------|-----|------|
| PatientMasterDetailViewModel.cs | `PatientMasterDetailViewModel : MasterDetailViewModelBase<PatientListDto, PatientDetailModel>` | 核心 ViewModel (组合模式)，依赖: PatientService, IPatientRepository, IDialogService, ICardReaderService, IPatientCardReaderIntegration。扩展属性: IsAdmin, GenderOptions, StatusOptions, DetailTitle, IsCardReaderConnected, IsReadingCard。基类实现: LoadListAsync, LoadDetailAsync, CreateNewDetail, SaveDetailAsync, DeleteItemAsync。扩展命令: RestoreCommand (管理员恢复软删除), ImportCommand (Excel 批量导入), ExportCommand (Excel 导出), DownloadTemplateCommand (模板下载), ViewMedicalRecordsCommand (TODO), NewConsultationCommand (TODO), ReadCardCommand (身份证读卡查找/创建患者) |

## 死代码与废弃标记

| 类型 | 位置 | 状态 | 说明 |
|------|------|------|------|
| IPatientCommandHandler + PatientCommandHandler | CommandHandlers/ | 疑似死代码 | 未在 DI 容器注册 (PatientsModule.RegisterTypes 中无注册)，仅 CLAUDE.md 文档引用，无运行时消费者。实际业务通过 PatientService 处理 |
| ImportWizardStep (enum) | Models/ImportWizardStep.cs | 疑似死代码 | 仅定义文件自身引用，无其他消费者。ImportProgressInfo 类仅被 PatientImportExecutor 使用 (有效) |
| PatientViewState | Models/PatientViewState.cs | 疑似死代码 | 仅被文档 (CLAUDE.md, DESKTOP_ARCHITECTURE_STANDARD.md) 引用，无运行时消费者 |
| PatientDetailDisplayModel | Models/Display/ | 低活跃 | 仅自身定义 + 单元测试引用，无运行时 ViewModel/View 消费者 |

## 已知陷阱

- PatientItem.Age 是从 BirthDate 实时计算的只读属性 (Issue #2240)，不存储在数据库。Mapper 必须 IgnoreSource Age 字段
- PatientDetailModel.Name setter 自动触发 PinYinCode 生成。Clone() 方法直接赋值 _name/_pinYinCode 私有字段绕过此行为
- PatientRepository 的 BatchImport/Export/Template 方法仅在 Remote 模式下可用 (_api != null)，Local 模式返回 null
- PatientSearchCache 使用 lock 保证线程安全，GenerateKey 包含 userId 实现用户隔离
- PatientImportDataMapper 优先读取"出生日期"列，仅在无此列时才从"年龄"反算 (兼容旧模板)
- PatientEditControl 的 ErrorsSource 属性类型是 ValidationErrorsAccessor，来自 LYBT.Desktop.Models.ViewModels.Base

## OpenSpec 追踪

| OpenSpec ID | 涉及文件 | 状态 |
|-------------|----------|------|
| standardize-module-structure | PatientsModule.cs | Components 已合并到 Services |
| integrate-cardreader-module | PatientCardReaderIntegration, PatientRepository, PatientMasterDetailViewModel | 已实现 |
| refactor-viewmodel-composition | PatientMasterDetailViewModel | V2 组合模式 |
| refactor-patient-selection | PatientSearchCache, PatientSearchManager | 搜索缓存已集成 |
| cleanup-patient-dead-code | PatientsModule.cs | PatientStateManager 已删除 |
| migrate-views-to-role-modules | PatientsModule.cs | 视图已迁移到角色台模块 |
| multi-doctor-unfinished-case | UnfinishedCaseHandler | 支持多医生场景检测 |

---
最后更新: 2026-03-01
