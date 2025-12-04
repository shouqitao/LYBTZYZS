# LYBT.Desktop.Prescriptions

> 处方管理模块 | 开方/药材选择/验方加载/打印

## 项目定位

- **层级**: Client Modules层
- **职责**: 提供中医处方开具界面，支持药材选择、验方模板加载、剂量计算、价格预览、处方打印。作为MedicalCase流程Step2组件

## 目录结构

```
LYBT.Desktop.Prescriptions/
├── Components/                       # 通用组件
│   ├── BasicValidator.cs            # 基础验证器
│   └── PriceCalculator.cs           # 价格计算器
├── Constants/
│   └── PrescriptionConstants.cs     # 处方常量
├── Interfaces/
│   └── IPrescriptionRepository.cs   # 处方仓储接口
├── Models/
│   ├── PrescriptionItem.cs          # 处方条目模型
│   └── PrescriptionPrintDto.cs      # 打印DTO
├── Services/
│   ├── IPrescriptionPrintService.cs # 打印服务接口
│   ├── PrescriptionEditorService.cs # 编辑服务
│   ├── PrescriptionFlowDocumentBuilder.cs # FlowDocument构建
│   └── PrescriptionPrintService.cs  # 打印服务实现
├── ViewModels/
│   ├── Components/                   # ViewModel组件化
│   │   ├── PrescriptionCalculator.cs       # 价格计算
│   │   ├── PrescriptionCommandHandler.cs   # 命令处理
│   │   ├── PrescriptionDataManager.cs      # 数据管理
│   │   ├── PrescriptionEventCoordinator.cs # 事件协调
│   │   └── PrescriptionValidator.cs        # 验证组件
│   ├── PrescriptionEditorDialogViewModel.cs # 编辑对话框(核心)
│   ├── PrescriptionManagementViewModel.cs   # 管理ViewModel
│   ├── HerbSelectionDialogViewModel.cs      # 药材选择对话框
│   ├── FormulaTemplateDialogViewModel.cs    # 验方模板对话框
│   └── SelectFormulaDialogViewModel.cs      # 选择验方对话框
├── Views/
│   ├── PrescriptionEditorDialog.xaml        # 编辑对话框
│   ├── PrescriptionManagementView.xaml      # 管理视图
│   ├── HerbSelectionDialog.xaml             # 药材选择
│   ├── FormulaTemplateDialog.xaml           # 验方模板
│   └── SelectFormulaDialog.xaml             # 选择验方
└── PrescriptionsModule.cs            # Prism模块注册
```

## PrescriptionEditorDialogViewModel(核心)

### 属性(30个)

| 属性类别 | 属性 | 说明 |
|----------|------|------|
| 基本信息 | PrescriptionId | 处方ID |
| 基本信息 | PrescriptionNo | 处方编号 |
| 基本信息 | DosageCount | 剂数(默认7) |
| 基本信息 | Usage | 用法 |
| 基本信息 | MedicalAdvice | 医嘱 |
| 基本信息 | Remark | 备注 |
| 价格 | Discount | 折扣 |
| 价格 | TotalAmount | 总金额 |
| 药材 | PrescriptionItems | 药材条目列表 |
| 状态 | HasChanges | 变更标记 |
| 状态 | IsReadOnly | 只读模式 |
| 状态 | CanEdit | 可编辑 |

### 命令(15个)

| 命令 | 说明 |
|------|------|
| SaveCommand | 保存处方 |
| CancelCommand | 取消编辑 |
| ResetCommand | 重置表单 |
| AddHerbCommand | 添加药材(打开HerbSelectionDialog) |
| EditHerbCommand | 编辑药材条目 |
| RemoveHerbCommand | 删除药材条目 |
| LoadFormulaTemplateCommand | 加载验方模板 |
| PreviewCommand | 预览打印 |
| ValidateCommand | 验证处方 |

### ISaveable接口实现

| 成员 | 说明 |
|------|------|
| SaveAsync() | 异步保存处方到服务器 |
| ValidateAll() | 验证必填(剂数>0、药材>0) |
| HasChanges | 数据变更状态 |
| IsReadOnly | 只读状态 |

## Dialog架构

| 对话框 | 用途 |
|--------|------|
| PrescriptionEditorDialog | 主编辑对话框 |
| HerbSelectionDialog | 从Herbs模块选择药材 |
| FormulaTemplateDialog | 从Formula模块加载验方 |
| SelectFormulaDialog | 验方列表选择 |

## IPrescriptionRepository

| 方法 | 说明 |
|------|------|
| GetByIdAsync | 按ID获取处方 |
| GetByMedicalCaseIdAsync | 按医案ID获取 |
| CreateAsync | 创建处方 |
| UpdateAsync | 更新处方 |
| ConfirmAsync | 确认处方 |
| DeleteAsync | 删除处方 |

## 与其他模块集成

| 模块 | 集成点 |
|------|--------|
| MedicalCase | Step2组件，ISaveable接口 |
| Herbs | HerbSelectionDialog药材选择 |
| Formula | FormulaTemplateDialog验方加载 |

## 依赖关系

### 依赖
- LYBT.Desktop.Models (ViewModelBase)
- LYBT.Desktop.Foundation (BaseApiRepository)
- LYBT.Desktop.Contracts (IPrescriptionApi/ISaveable)
- LYBT.Desktop.Herbs (药材选择)
- LYBT.Desktop.Formula (验方模板)
- LYBT.Shared.Models (PrescriptionDto)
- Prism.Core/Prism.DryIoc (8.x)
- MaterialDesignThemes (5.1.x)

### 被依赖
- LYBT.Desktop.Shell (模块加载)
- LYBT.Desktop.MedicalCase (Step2组件)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-29 | 初始版本 |
