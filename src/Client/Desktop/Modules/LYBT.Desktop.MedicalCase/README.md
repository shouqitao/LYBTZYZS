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
