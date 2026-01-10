# LYBT.Desktop.Patients

> 患者管理模块 | 工作流入口 | 待诊队列/快速建档

## 项目定位

- **层级**: Client Modules层
- **职责**: 提供患者档案管理和看诊工作流入口，管理待诊队列、支持快速建档、启动医案流程

## 目录结构

```
LYBT.Desktop.Patients/
├── Interfaces/
│   └── IPatientRepository.cs        # 患者仓储接口
├── Repositories/
│   └── PatientRepository.cs         # 患者仓储实现
├── ViewModels/
│   ├── PatientSelectionViewModel.cs # 患者选择ViewModel(核心,1065行)
│   ├── PatientDetailViewModel.cs    # 患者详情ViewModel
│   ├── PatientListViewModel.cs      # 患者列表ViewModel
│   └── QuickCreateDialogViewModel.cs # 快速建档对话框ViewModel
├── Views/
│   ├── PatientSelectionView.xaml    # 患者选择视图(工作流入口)
│   ├── PatientDetailView.xaml       # 详情视图
│   ├── PatientListView.xaml         # 列表视图
│   └── QuickCreateDialog.xaml       # 快速建档对话框
└── PatientsModule.cs                 # Prism模块注册
```

## PatientSelectionViewModel(核心)

### 属性(20个)

| 属性类别 | 属性 | 说明 |
|----------|------|------|
| 患者列表 | Patients | 患者列表 |
| 患者列表 | SelectedPatient | 选中的患者 |
| 搜索 | SearchText | 搜索关键词 |
| 搜索 | SearchMode | 搜索模式(姓名/手机/身份证) |
| 待诊队列 | PendingQueue | 待诊患者队列 |
| 待诊队列 | PendingCount | 待诊人数 |
| 未完成 | UnfinishedCases | 未完成医案列表 |
| 未完成 | UnfinishedCount | 未完成数量 |
| 状态 | IsLoading | 加载状态 |
| 状态 | CanStartMedicalCase | 可开始看诊 |
| 分页 | PageIndex | 当前页码 |
| 分页 | TotalCount | 总数量 |

### 命令(25个)

| 命令 | 说明 |
|------|------|
| LoadCommand | 加载患者列表 |
| SearchCommand | 搜索患者 |
| SelectPatientCommand | 选择患者 |
| StartMedicalCaseCommand | 开始看诊(启动MedicalCase流程) |
| QuickCreateCommand | 快速建档 |
| EditPatientCommand | 编辑患者 |
| ViewHistoryCommand | 查看就诊历史 |
| AddToQueueCommand | 加入待诊队列 |
| RemoveFromQueueCommand | 移出待诊队列 |
| ResumeUnfinishedCommand | 继续未完成医案 |
| RefreshCommand | 刷新列表 |
| ExportCommand | 导出患者 |

## 待诊队列功能

| 功能 | 说明 |
|------|------|
| 加入队列 | 选择患者后加入待诊队列 |
| 队列排序 | 按加入时间排序 |
| 叫号提示 | 显示当前叫号状态 |
| 自动移除 | 开始看诊后自动移出队列 |

## 未完成医案处理

| 场景 | 处理 |
|------|------|
| 存在未完成 | 提示"该患者有未完成医案，是否继续？" |
| 继续医案 | 恢复到上次保存的步骤 |
| 新建医案 | 忽略未完成，创建新医案 |

## IPatientRepository

| 方法 | 说明 |
|------|------|
| GetAllAsync | 获取所有患者 |
| GetByIdAsync | 按ID获取 |
| SearchAsync | 搜索患者 |
| CreateAsync | 创建患者 |
| UpdateAsync | 更新患者 |
| DeleteAsync | 删除患者 |
| GetMedicalHistoryAsync | 获取就诊历史 |
| GetUnfinishedCasesAsync | 获取未完成医案 |

## 与MedicalCase集成

| 集成点 | 说明 |
|--------|------|
| StartMedicalCase | 选择患者后启动MedicalCase流程 |
| 传递PatientId | 导航参数传递患者ID |
| 未完成恢复 | 支持恢复中断的医案流程 |

## 依赖关系

### 依赖
- LYBT.Desktop.Models (ViewModelBase)
- LYBT.Desktop.Foundation (BaseApiRepository)
- LYBT.Desktop.Contracts (IPatientApi)
- LYBT.Desktop.Infrastructure (INavigationService)
- LYBT.Shared.Models (PatientDto)
- Prism.Core/Prism.DryIoc (8.x)

### 被依赖
- LYBT.Desktop.Shell (模块加载/主工作区)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-29 | 初始版本 |
