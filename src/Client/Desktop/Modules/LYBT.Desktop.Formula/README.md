# LYBT.Desktop.Formula

> 验方管理模块 | 方剂模板/配方管理/快速开方

## 项目定位

- **层级**: Client Modules层
- **职责**: 提供验方(经典方剂模板)的管理界面，支持创建、编辑、搜索、克隆验方，为处方开具提供模板支持

## 目录结构

```
LYBT.Desktop.Formula/
├── Interfaces/
│   └── IFormulaRepository.cs        # 验方仓储接口
├── Repositories/
│   └── FormulaRepository.cs         # 验方仓储实现
├── Services/                         # 服务层(Epic #1773)
│   ├── FormulaCalculationService.cs # 剂量计算服务
│   ├── FormulaCloneService.cs       # 验方克隆服务
│   ├── FormulaExportService.cs      # 导出服务
│   ├── FormulaImportService.cs      # 导入服务
│   ├── FormulaSearchService.cs      # 搜索服务
│   ├── FormulaTemplateService.cs    # 模板服务
│   ├── FormulaValidationService.cs  # 验证服务
│   └── Interfaces/                   # 服务接口
├── ViewModels/
│   ├── FormulaManagementViewModel.cs # 验方管理ViewModel
│   ├── FormulaDetailViewModel.cs     # 验方详情ViewModel
│   └── FormulaItemViewModel.cs       # 验方条目ViewModel
├── Views/
│   ├── FormulaManagementView.xaml   # 管理视图
│   ├── FormulaDetailView.xaml       # 详情视图
│   └── FormulaItemView.xaml         # 条目视图
└── FormulaModule.cs                  # Prism模块注册
```

## FormulaManagementViewModel

### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| Formulas | ObservableCollection | 验方列表 |
| SelectedFormula | FormulaDto | 选中的验方 |
| SearchText | string | 搜索关键词 |
| IsLoading | bool | 加载状态 |
| TotalCount | int | 总数量 |
| PageIndex | int | 当前页码 |
| PageSize | int | 每页数量 |

### 命令(20个)

| 命令 | 说明 |
|------|------|
| LoadCommand | 加载验方列表 |
| SearchCommand | 搜索验方 |
| CreateCommand | 新建验方 |
| EditCommand | 编辑验方 |
| DeleteCommand | 删除验方 |
| CloneCommand | 克隆验方 |
| ExportCommand | 导出验方 |
| ImportCommand | 导入验方 |
| RefreshCommand | 刷新列表 |
| NextPageCommand | 下一页 |
| PrevPageCommand | 上一页 |

## FormulaDetailViewModel

### 属性(25个)

| 属性类别 | 属性 | 说明 |
|----------|------|------|
| 基本信息 | Name | 验方名称 |
| 基本信息 | Category | 分类(如:补益剂/解表剂) |
| 基本信息 | Source | 来源(如:伤寒论) |
| 基本信息 | Description | 方解说明 |
| 组成 | FormulaItems | 药材组成列表 |
| 功效 | Functions | 功效 |
| 功效 | Indications | 主治 |
| 功效 | Contraindications | 禁忌 |
| 状态 | HasChanges | 变更标记 |
| 状态 | IsReadOnly | 只读模式 |

### 命令(11个)

| 命令 | 说明 |
|------|------|
| SaveCommand | 保存验方 |
| CancelCommand | 取消编辑 |
| AddHerbCommand | 添加药材 |
| RemoveHerbCommand | 移除药材 |
| EditHerbCommand | 编辑药材剂量 |
| ValidateCommand | 验证验方 |

## IFormulaRepository

| 方法 | 说明 |
|------|------|
| GetAllAsync | 获取所有验方 |
| GetByIdAsync | 按ID获取 |
| GetPagedAsync | 分页查询 |
| SearchAsync | 搜索验方 |
| CreateAsync | 创建验方 |
| UpdateAsync | 更新验方 |
| DeleteAsync | 删除验方 |
| CloneAsync | 克隆验方 |
| GetByCategoryAsync | 按分类获取 |

## 与Prescriptions集成

| 集成点 | 说明 |
|--------|------|
| FormulaTemplateDialog | 处方模块调用验方选择对话框 |
| ApplyToRecipe | 将验方应用到处方 |
| 剂量换算 | 支持按比例调整剂量 |

## 依赖关系

### 依赖
- LYBT.Desktop.Models (ViewModelBase)
- LYBT.Desktop.Foundation (BaseApiRepository)
- LYBT.Desktop.Contracts (IFormulaApi)
- LYBT.Shared.Models (FormulaDto)
- Prism.Core/Prism.DryIoc (8.x)

### 被依赖
- LYBT.Desktop.Shell (模块加载)
- LYBT.Desktop.Prescriptions (验方模板选择)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-11-15 | Epic #1773服务层重构 |
| 2025-10-29 | 初始版本 |
