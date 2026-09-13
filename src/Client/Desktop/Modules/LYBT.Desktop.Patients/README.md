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
├── Mappers/
│   └── PatientMapper.cs                           # Mapperly 编译时映射器（DTO↔Model↔InputDto）
├── Models/
│   ├── Items/
│   │   └── PatientEditContext.cs                  # 编辑上下文模型（ValidatableModelBase）
│   ├── ImportWizardStep.cs                        # 导入向导枚举 + ImportProgressInfo
│   └── PatientDetailModel.cs                      # Detail 编辑模型（ValidatableModelBase）
├── Repositories/
│   └── PatientRepository.cs                       # 仓储实现（Repository 抽象层）
├── Services/
│   ├── PatientCardReaderIntegration.cs            # 读卡器集成服务（PRD-15 去重链）
│   ├── PatientExcelService.cs                     # Excel 导入导出转换（B-12：模板/导入/导出 .xlsx，后端 JSON 契约不变）
│   └── PatientService.cs                     # 业务服务（CrudServiceBase 泛型，统一错误处理）
├── ViewModels/
│   ├── Handlers/
│   │   ├── IPatientStatusHandler.cs               # 状态处理接口
│   │   └── PatientStatusHandler.cs                # 状态处理实现（仅 Restore）
│   ├── PatientCardReaderViewModel.cs              # 读卡器 ViewModel
│   ├── PatientEditorViewModel.cs                  # 编辑器 ViewModel（EditorViewModelBase）
│   └── PatientMasterDetailViewModel.cs            # 核心 ViewModel（组合模式）
└── PatientsModule.cs                              # Prism 模块注册
```

> 本模块**无 `Views/` 与 `Dialogs/`**：不注册导航视图与对话框，仅向角色台 / 临床工作台提供可嵌入的 `Controls/`。

## 视图 / ViewModel 清单

**计数口径**：View = 页面/导航级 XAML（`*/Views/*.xaml`）；Control = 内嵌组件（`*/Controls/*.xaml`）；Dialog = `*/Dialogs/**/*.xaml`；ViewModel 按「每文件 1 个 VM 类型」计。

| 类别 | 数量 | 明细 |
|------|------|------|
| View | 0 | 本模块不注册导航视图 |
| Control | 4 | `PatientMasterDetailControl`、`PatientEditControl`、`PatientSelectionControl`、`PatientViewControl` |
| Dialog | 0 | — |
| ViewModel | 3 | `PatientMasterDetailViewModel`、`PatientEditorViewModel`、`PatientCardReaderViewModel`（`ViewModels/Handlers/` 下 2 个文件为 Status Handler，非 VM） |

> 全桌面口径：View 30 / Control 33 / Dialog 7 / ViewModel 55（代码实际：`src/Client/Desktop`）。

## 核心组件

| 类 | 设计依据 | 职责 |
|---|---|---|
| **PatientsModule** : IModule | Prism 模块注册，依赖 Auth+Users | RegisterTypes: PatientMasterDetailVM + CardReaderVM + EditorVM + IPatientService + IPatientCardReaderIntegration + PatientMapper + MasterDetailServices |
| **PatientMasterDetailViewModel** : MasterDetailViewModelBase | 组合模式 ViewModel，集成 CardReader | 扩展属性: IsAdmin/GenderOptions/StatusOptions/DetailTitle/IsCardReaderConnected/IsReadingCard。基类实现: LoadListAsync/LoadDetailAsync/CreateNewDetail/SaveDetailAsync/DeleteItemAsync。命令: `RestoreCommand`/`ReadCardCommand`/`NewConsultationCommand`/`ViewMedicalRecordsCommand`/`ImportPatientsCommand`/`ExportPatientsCommand`/`DownloadImportTemplateCommand` |
| **PatientEditorViewModel** : `EditorViewModelBase<PatientEditContext>` | 编辑子 VM，映射经 Mapperly `PatientMapper` | Validate / GetPatientData 方法，编辑表单逻辑分离 |
| **PatientCardReaderViewModel** | ICardReaderService 集成 | ReadCardAsync / FindPatientByIdNumberAsync / MaskIdNumber，身份证读卡交互 |
| **PatientStatusHandler** : BaseStatusHandler | Handler 组件拆分，SRP | 仅实现 Restore（恢复软删除），不含 ToggleStatus |
| **PatientService** : IPatientService | 统一 CommandResult 错误处理，[SVC] 日志前缀 | 8 个方法: CreatePatientAsync / UpdatePatientAsync / DeletePatientAsync / SearchPatientsAsync / GetPatientsPagedAsync / GetByIdAsync 等 |
| **PatientCardReaderIntegration** | PRD-15 去重链设计 | 去重链: exact → fuzzy → multiple → no match。加密照片处理。方法: FindPatientByIdNumberAsync / QuickCreatePatientAsync / FindOrCreatePatientAsync / GetPatientDetailByIdAsync |
| **PatientRepository** : IPatientRepository | Repository 抽象层，Local/Remote 透明切换 | CRUD 委托 Repository，批量导入/导出/模板下载通过 IPatientApi（Remote 专有） |
| **PatientExcelService** | B-12 Excel 化（用户操作 .xlsx，后端 JSON 契约不变） | GenerateTemplate（服务端 JSON 字段说明 → .xlsx 模板：患者数据表表头 + 填写说明表）/ ParseImportFile（.xlsx → `PatientBatchImportInputDto`，行级校验：日期/性别非法抛 `InvalidDataException` 含行号）/ GenerateExportFile（服务端导出 JSON 数组 → .xlsx，性别/状态转中文标签，脱敏值原样） |

## 依赖关系

### 依赖（编译时 ProjectReference）

| 项目 | 用途 |
|------|------|
| LYBT.Desktop.Infrastructure | `MasterDetailViewModelBase`、`ValidatableModelBase`、`DependencyInjection`、`CardReader`（`ICardReaderService`、读卡器集成/模型）、`Services`、`Helpers` |
| LYBT.Desktop.Contracts | `Repositories.IPatientRepository`、`Services.IPatientService`、`ApiClient` |
| LYBT.Shared.Models | `PatientListDto` / `PatientDetailDto` / `PatientInputDto`、`Enums`、`Validators.Patients`、`Utilities.Text.PinYinHelper` |

传递依赖（经上述项目）：`LYBT.Desktop.Foundation`（`Repositories` 基类、`Security`、`ExceptionHandling`）、`LYBT.Shared.ExceptionHandling`、`FluentValidation`（`PatientInputDtoValidator`）。
NuGet 直接引用：`Prism.Core` / `Prism.DryIoc` / `Prism.Wpf`、`Riok.Mapperly`、`ClosedXML`（B-12 Excel 化）。

### 被依赖

| 消费方 | 接口/控件 | 说明 |
|--------|-----------|------|
| `Roles/LYBT.Desktop.Clinical` `PatientManagementView` | `PatientMasterDetailControl` | 薄包装：View 在角色台，Control 在业务模块 |
| `Roles/LYBT.Desktop.Clinical` `PatientSelectionView`、`ClinicalWorkspaceView` | `PatientSelectionControl` | 医案工作台 / 一体化工作台左侧患者列表 |
| `Registrations` `RegistrationCreateDialogViewModel` | `Contracts.Services.IPatientService` | 患者搜索自动补全（经接口，无模块引用） |
| `MedicalCaseModule` | `[ModuleDependency("PatientsModule")]` + `Contracts.Services.IPatientService` | 医案必须关联患者；运行时加载顺序声明 |
| `Roles/LYBT.Desktop.Clinical` `ReceptionistHomeViewModel` | `Contracts.Services.IPatientService` | 前台搜索患者、读卡建档 |
| `Clinical` / `Receptionist` 工作台 | `Infrastructure.CardReader` 集成 | 读卡建档链路 |

> 跨模块仅经 `LYBT.Desktop.Contracts` 接口或共享 Control 交互，模块间无直接 ProjectReference。

## 设计决策

| 决策 | 原因 |
|------|------|
| Handler 组件拆分（PatientStatusHandler 仅 Restore） | ViewModel 职责过重，SRP 拆分；Patients 的 StatusHandler 只需恢复功能 |
| PatientCardReaderIntegration PRD-15 去重链 | 身份证读卡→精确匹配→模糊匹配→多结果→无匹配，加密照片处理 |
| Repository 抽象层经 `IApiClient` 统一路由 | `PatientRepository : EntityApiClientRepositoryBase<...>` 全部经 `IApiClientPatients` 访问服务端，不再有 Local/Remote 双分支；异常由 `ExecuteAsync`/`catch` 收敛为 null + 日志 |
| Mapperly 编译时映射替代 AutoMapper | 零运行时开销，编译期类型安全 |
| Excel 由 Desktop 前端处理（ClosedXML，B-12） | 后端保持 JSON 通用契约（2026-08-13 IMPORTEXPORT-JSON 决策）；Excel 属表现层——字段说明以服务端模板 JSON 为 SSOT，前端只做格式转换 |

## 已知陷阱

1. **PatientDetailModel.Name 自动生成 PinYinCode**: setter 自动触发 PinYinHelper，Clone() 方法直接赋值 _name/_pinYinCode 私有字段绕过此行为
2. **批量操作无本地回退**: `PatientRepository` 的 `BatchImportAsync` / `ExportPatientsAsync` / `ExportTemplateAsync` 经 `IApiClientPatients` 访问服务端；API 失败或异常时返回 `null`（模板/导出）或失败结果（导入），ViewModel 需容忍 null 并给出提示
3. **PatientEditControl.ErrorsSource 类型**: 是 `ValidationErrorsAccessor`（来自 `LYBT.Desktop.Infrastructure.ViewModels.Base`，定义于 `Core/LYBT.Desktop.Infrastructure/ViewModels/ValidationAccessors.cs`），非标准类型
4. **Excel 文件格式错误定位**: PatientExcelService 抛的 `InvalidDataException` 消息含行号与列名（如「第 3 行「出生日期」格式无效」）——ViewModel 直接展示为「文件格式错误：...」，无需再查日志

---

2026-09-13 docs 复盘：与代码对齐（View/VM 清单、目录树、依赖）
