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
│   └── PatientRepository.cs                # 仓储实现 (DataSource 抽象层)
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

- Repository 通过 IPatientDataSource 抽象支持 Local/Remote 模式无缝切换
- 组件化架构: ViewModel 功能拆分为 Components 和 Services，避免单一 ViewModel 膨胀
- 搜索缓存使用 LRU 策略，支持用户隔离和事件驱动失效
- Mapperly 编译时映射替代运行时 AutoMapper，零运行时开销

## 依赖关系

### 依赖
- LYBT.Desktop.Foundation (BaseApiRepository/Security)
- LYBT.Desktop.Infrastructure (MasterDetailControlBase/ViewModelBase/Services)
- LYBT.Desktop.Models (ValidatableModelBase)
- LYBT.Desktop.Contracts (IPatientApi/IPatientDataSource)
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
