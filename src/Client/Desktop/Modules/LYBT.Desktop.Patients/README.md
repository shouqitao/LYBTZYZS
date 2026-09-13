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

## 核心组件

| 类 | 设计依据 | 职责 |
|---|---|---|
| **PatientsModule** : IModule | Prism 模块注册，依赖 Auth+Users | RegisterTypes: PatientMasterDetailVM + CardReaderVM + EditorVM + IPatientService + IPatientCardReaderIntegration + PatientMapper + MasterDetailServices |
| **PatientMasterDetailViewModel** : MasterDetailViewModelBase | 组合模式 ViewModel，集成 CardReader | 扩展属性: IsAdmin/GenderOptions/StatusOptions/DetailTitle/IsCardReaderConnected/IsReadingCard。基类实现: LoadListAsync/LoadDetailAsync/CreateNewDetail/SaveDetailAsync/DeleteItemAsync。命令: RestoreCommand/ReadCardCommand/NewConsultationCommand/ImportCommand/ExportCommand/DownloadTemplateCommand |
| **PatientEditorViewModel** | PatientEditContext 编辑上下文 | Validate / GetPatientData 方法，编辑表单逻辑分离 |
| **PatientCardReaderViewModel** | ICardReaderService 集成 | ReadCardAsync / FindPatientByIdNumberAsync / MaskIdNumber，身份证读卡交互 |
| **PatientStatusHandler** : BaseStatusHandler | Handler 组件拆分，SRP | 仅实现 Restore（恢复软删除），不含 ToggleStatus |
| **PatientService** : IPatientService | 统一 CommandResult 错误处理，[SVC] 日志前缀 | 8 个方法: CreatePatientAsync / UpdatePatientAsync / DeletePatientAsync / SearchPatientsAsync / GetPatientsPagedAsync / GetByIdAsync 等 |
| **PatientCardReaderIntegration** | PRD-15 去重链设计 | 去重链: exact → fuzzy → multiple → no match。加密照片处理。方法: FindPatientByIdNumberAsync / QuickCreatePatientAsync / FindOrCreatePatientAsync / GetPatientDetailByIdAsync |
| **PatientRepository** : IPatientRepository | Repository 抽象层，Local/Remote 透明切换 | CRUD 委托 Repository，批量导入/导出/模板下载通过 IPatientApi（Remote 专有） |
| **PatientExcelService** | B-12 Excel 化（用户操作 .xlsx，后端 JSON 契约不变） | GenerateTemplate（服务端 JSON 字段说明 → .xlsx 模板：患者数据表表头 + 填写说明表）/ ParseImportFile（.xlsx → `PatientBatchImportInputDto`，行级校验：日期/性别非法抛 `InvalidDataException` 含行号）/ GenerateExportFile（服务端导出 JSON 数组 → .xlsx，性别/状态转中文标签，脱敏值原样） |

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
- LYBT.Desktop.Registrations（通过 IPatientService 搜索患者）

## 设计决策

| 决策 | 原因 |
|------|------|
| Handler 组件拆分（PatientStatusHandler 仅 Restore） | ViewModel 职责过重，SRP 拆分；Patients 的 StatusHandler 只需恢复功能 |
| PatientCardReaderIntegration PRD-15 去重链 | 身份证读卡→精确匹配→模糊匹配→多结果→无匹配，加密照片处理 |
| Repository 抽象层支持 Local/Remote | 批量导入/导出/模板下载仅 Remote 模式可用（_api != null），Local 返回 null |
| Mapperly 编译时映射替代 AutoMapper | 零运行时开销，编译期类型安全 |
| Excel 由 Desktop 前端处理（ClosedXML，B-12） | 后端保持 JSON 通用契约（2026-08-13 IMPORTEXPORT-JSON 决策）；Excel 属表现层——字段说明以服务端模板 JSON 为 SSOT，前端只做格式转换 |

## 已知陷阱

1. **PatientDetailModel.Name 自动生成 PinYinCode**: setter 自动触发 PinYinHelper，Clone() 方法直接赋值 _name/_pinYinCode 私有字段绕过此行为
2. **批量操作仅 Remote 模式**: PatientRepository 的 BatchImport/Export/Template 方法仅 Remote 模式可用（_api != null），Local 模式返回 null
3. **PatientEditControl.ErrorsSource 类型**: 是 ValidationErrorsAccessor（来自 LYBT.Desktop.Models.ViewModels.Base），非标准类型
4. **Excel 文件格式错误定位**: PatientExcelService 抛的 `InvalidDataException` 消息含行号与列名（如「第 3 行「出生日期」格式无效」）——ViewModel 直接展示为「文件格式错误：...」，无需再查日志
