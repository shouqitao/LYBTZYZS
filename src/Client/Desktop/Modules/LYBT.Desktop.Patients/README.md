# LYBT.Desktop.Patients

> 患者管理模块 | 档案 CRUD / 读卡建档 / 搜索缓存 / 医案启动协调 / 待诊队列

## 项目定位

- **层级**: Client → Desktop → Modules
- **职责**: 患者档案全生命周期管理，提供看诊工作流入口（读卡建档、医案启动协调、待诊队列），支持身份证读卡去重链、LRU 搜索缓存、Excel 批量导入导出

## 目录结构

```
LYBT.Desktop.Patients/
├── Controls/
│   ├── PatientEditControl.xaml/.xaml.cs           # 患者编辑控件（双向绑定+验证）
│   ├── PatientMasterDetailControl.xaml/.xaml.cs   # Master-Detail 可复用控件
│   ├── PatientSelectionControl.xaml/.xaml.cs      # 患者选择控件（左右分栏）
│   └── PatientViewControl.xaml/.xaml.cs           # 患者只读预览控件
├── Interfaces/
│   ├── IPatientRepository.cs                      # 患者仓储接口（CRUD+搜索+批量）
│   ├── IPatientSearchCache.cs                     # 搜索缓存接口（LRU）
│   ├── IPatientService.cs                         # 患者业务服务接口
│   ├── IPatientValidator.cs                       # 验证器接口
│   ├── IPatientCardReaderIntegration.cs           # 读卡器集成接口
│   └── IMedicalCaseStartCoordinator.cs            # 医案启动协调器接口
├── Mappers/
│   └── PatientMapper.cs                           # Mapperly 编译时映射器
├── Models/
│   ├── Display/
│   │   └── PatientDetailDisplayModel.cs           # 只读展示模型
│   ├── Items/
│   │   ├── PatientEditContext.cs                  # 编辑上下文模型
│   │   └── PatientItem.cs                         # 列表项 UI 模型（BindableBase）
│   ├── ImportWizardStep.cs                        # 导入向导枚举 + ImportProgressInfo
│   └── PatientDetailModel.cs                      # Detail 编辑模型（ValidatableModelBase）
├── Repositories/
│   └── PatientRepository.cs                       # 仓储实现（Repository 抽象层）
├── Services/
│   ├── PatientCardReaderIntegration.cs            # 读卡器集成服务（PRD-15 去重链）
│   ├── PatientImportDataMapper.cs                 # Excel 数据映射器
│   ├── PatientImportExecutor.cs                   # BackgroundWorker 导入执行器
│   ├── PatientSearchCache.cs                      # LRU 搜索缓存（线程安全）
│   ├── PatientSearchManager.cs                    # 搜索分页管理器（298 行提取）
│   ├── PatientService.cs                          # 业务服务（统一错误处理）
│   ├── PendingQueueManager.cs                     # 待诊队列管理器
│   └── UnfinishedCaseHandler.cs                   # 未完成医案处理器
├── ViewModels/
│   ├── Components/
│   │   ├── MedicalCaseStartCoordinator.cs         # 医案启动协调器
│   │   └── PatientValidator.cs                    # FluentValidation 验证器
│   ├── Handlers/
│   │   ├── IPatientStatusHandler.cs               # 状态处理接口
│   │   └── PatientStatusHandler.cs                # 状态处理实现（仅 Restore）
│   ├── PatientCardReaderViewModel.cs              # 读卡器 ViewModel
│   ├── PatientEditorViewModel.cs                  # 编辑器 ViewModel
│   └── PatientMasterDetailViewModel.cs            # 核心 ViewModel（组合模式）
└── PatientsModule.cs                              # Prism 模块注册
```

## 核心组件

| 类 | 设计依据 | 职责 |
|---|---|---|
| **PatientsModule** : IModule | Prism 模块注册，依赖 Auth+Users | RegisterTypes: PatientMasterDetailVM + CardReaderVM + EditorVM + IPatientService + IPatientValidator + IPatientCardReaderIntegration + PatientSearchManager + PatientSearchCache + MedicalCaseStartCoordinator + MasterDetailServices |
| **PatientMasterDetailViewModel** : MasterDetailViewModelBase | 组合模式 ViewModel，集成 CardReader | 扩展属性: IsAdmin/GenderOptions/StatusOptions/DetailTitle/IsCardReaderConnected/IsReadingCard。基类实现: LoadListAsync/LoadDetailAsync/CreateNewDetail/SaveDetailAsync/DeleteItemAsync。命令: RestoreCommand/ReadCardCommand/NewConsultationCommand/ImportCommand/ExportCommand/DownloadTemplateCommand |
| **PatientEditorViewModel** | PatientEditContext 编辑上下文 | Validate / GetPatientData 方法，编辑表单逻辑分离 |
| **PatientCardReaderViewModel** | ICardReaderService 集成 | ReadCardAsync / FindPatientByIdNumberAsync / MaskIdNumber，身份证读卡交互 |
| **PatientStatusHandler** : BaseStatusHandler | Handler 组件拆分，SRP | 仅实现 Restore（恢复软删除），不含 ToggleStatus |
| **PatientService** : IPatientService | 统一 CommandResult 错误处理，[SVC] 日志前缀 | 9 个方法: CreatePatientAsync / UpdatePatientAsync / DeletePatientAsync / BatchDeletePatientsAsync / SearchPatientsAsync / GetPatientsPagedAsync / GetByIdAsync 等 |
| **PatientSearchManager** | 从 ViewModel 提取（298 行），搜索+分页+缓存集成 | ExecuteSearchAsync / LoadInitialPatientsAsync / LoadCurrentPageAsync / PreviousPageAsync / NextPageAsync / InvalidateCache。事件: SearchCompleted |
| **PatientSearchCache** : IPatientSearchCache | LRU 策略，用户隔离 | 最多 10 条，5 分钟 TTL，线程安全（lock），用户隔离（SessionManager.CurrentUserId），事件驱动失效（PatientEvents.Created/Updated + CacheEvents.Invalidated + SessionChanged） |
| **PatientCardReaderIntegration** | PRD-15 去重链设计 | 去重链: exact → fuzzy → multiple → no match。加密照片处理。方法: FindPatientByIdNumberAsync / QuickCreatePatientAsync / FindOrCreatePatientAsync / GetPatientDetailByIdAsync |
| **MedicalCaseStartCoordinator** | 多医生场景处理 | StartResult 枚举: ContinueExisting / CreateNew / CloseOnly / Cancelled / BlockedByOtherDoctor / Error。方法: CheckUnfinishedCaseAsync / IsOtherDoctorCase / GetOtherDoctorName / HandleUserChoiceAsync |
| **PatientValidator** | FluentValidation 集成 | ValidatePatientInputAsync（异步 DTO 验证）/ ValidateBasicInfo / ValidateIdNumber / ValidateAge / ValidateEmergencyContact / IsValid / ConvertToInputDto |
| **PatientRepository** : IPatientRepository | Repository 抽象层，Local/Remote 透明切换 | CRUD 委托 Repository，批量导入/导出/模板下载通过 IPatientApi（Remote 专有） |

## 依赖关系

### 依赖
- LYBT.Desktop.Infrastructure（MasterDetailControlBase / ViewModelBase / Services）
- LYBT.Desktop.Foundation（BaseApiRepository / Security）
- LYBT.Desktop.Contracts（IPatientApi / IPatientRepository）
- LYBT.Desktop.Models（ValidatableModelBase / ValidationErrorsAccessor）
- LYBT.Shared.Models（PatientListDto / PatientDetailDto / PatientInputDto）
- LYBT.Desktop.CardReader（ICardReaderService）
- Prism.DryIoc（8.x）

### 被依赖
- LYBT.Desktop.Admin（PatientManagementView 嵌入 PatientMasterDetailControl）
- LYBT.Desktop.Clinical（PatientSelectionControl 嵌入临床工作台）
- LYBT.Desktop.Registration（通过 IPatientService 搜索患者）

## 设计决策

| 决策 | 原因 |
|------|------|
| Handler 组件拆分（PatientStatusHandler 仅 Restore） | ViewModel 职责过重，SRP 拆分；Patients 的 StatusHandler 只需恢复功能 |
| PatientSearchManager 从 ViewModel 提取（298 行） | 搜索+分页+缓存逻辑独立，支持事件驱动（SearchCompleted），ViewModel 变薄 |
| PatientSearchCache LRU + 用户隔离 | 避免重复 API 调用，GenerateKey 包含 userId 隔离不同用户缓存 |
| PatientCardReaderIntegration PRD-15 去重链 | 身份证读卡→精确匹配→模糊匹配→多结果→无匹配，加密照片处理 |
| MedicalCaseStartCoordinator 多医生场景 | 检测未完成医案归属医生，非本人医案需确认后才能关闭/续接 |
| Repository 抽象层支持 Local/Remote | 批量导入/导出/模板下载仅 Remote 模式可用（_api != null），Local 返回 null |
| Mapperly 编译时映射替代 AutoMapper | 零运行时开销，编译期类型安全 |
| PatientItem.Age 从 BirthDate 实时计算 | 不存储在数据库，Mapper 必须 IgnoreSource Age 字段 |

## 已知陷阱

1. **PatientItem.Age 只读计算属性**: 从 BirthDate 实时计算（Issue #2240），不存储在数据库，Mapper 必须 IgnoreSource Age 字段
2. **PatientDetailModel.Name 自动生成 PinYinCode**: setter 自动触发 PinYinHelper，Clone() 方法直接赋值 _name/_pinYinCode 私有字段绕过此行为
3. **批量操作仅 Remote 模式**: PatientRepository 的 BatchImport/Export/Template 方法仅 Remote 模式可用（_api != null），Local 模式返回 null
4. **PatientSearchCache 线程安全**: 使用 lock 保证线程安全，GenerateKey 包含 userId 实现用户隔离
5. **PatientImportDataMapper 兼容旧模板**: 优先读取"出生日期"列，仅在无此列时才从"年龄"反算
6. **PatientEditControl.ErrorsSource 类型**: 是 ValidationErrorsAccessor（来自 LYBT.Desktop.Models.ViewModels.Base），非标准类型
7. **IPatientCommandHandler 疑似死代码**: 未在 DI 容器注册，实际业务通过 PatientService 处理
8. **PatientViewState 疑似死代码**: 仅被文档引用，无运行时消费者
