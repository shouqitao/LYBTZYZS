# LYBT.Desktop.Formula

> 验方管理模块 | 方剂模板管理 / 药材组方 / 验方复制

## 项目定位

- **层级**: Client Modules 层
- **职责**: 提供验方(经验方/经典方剂模板)的管理界面，支持创建、编辑、搜索、复制验方，为处方开具提供模板支持

## 目录结构

```
LYBT.Desktop.Formula/
├── CommandHandlers/
│   ├── IFormulaCommandHandler.cs           # CommandHandler 接口
│   └── FormulaCommandHandler.cs            # CommandHandler 实现
├── Controls/
│   ├── FormulaEditControl.xaml/.xaml.cs     # 验方编辑控件 (DependencyProperty 绑定)
│   └── FormulaMasterDetailControl.xaml/.xaml.cs  # Master-Detail 可复用控件
├── Interfaces/
│   ├── IFormulaRepository.cs               # 验方仓储接口 (CRUD+搜索+克隆+批量)
│   └── IFormulaService.cs                  # 验方 Service 接口
├── Mappers/
│   ├── FormulaDetailModelMapper.cs         # Mapperly: FormulaDetailDto <-> FormulaDetailModel
│   ├── FormulaHerbItemMapper.cs            # Mapperly: FormulaHerbItemDto <-> FormulaHerbItem
│   └── FormulaMapper.cs                    # Mapperly: FormulaDetailDto <-> FormulaItem
├── Models/
│   ├── Items/
│   │   ├── FormulaHerbItem.cs              # 验方药材项 UI 模型 (BindableBase)
│   │   └── FormulaItem.cs                  # 验方列表项 UI 模型 (BindableBase)
│   └── FormulaDetailModel.cs               # Detail 编辑模型 (ValidatableModelBase)
├── Repositories/
│   └── FormulaRepository.cs                # 仓储实现 (DataSource 抽象层)
├── Services/
│   ├── FormulaSearchProvider.cs            # 跨模块搜索提供者 (IFormulaSearchProvider)
│   ├── FormulaService.cs                   # 业务服务 (保存/复制/删除)
│   └── FormulaValidator.cs                 # 验方验证器 (信息+药材+完整性)
├── ViewModels/
│   ├── FormulaHerbItemViewModel.cs         # 验方药材项 ViewModel
│   └── FormulaMasterDetailViewModel.cs     # 核心 ViewModel (组合模式)
└── FormulaModule.cs                         # Prism 模块注册
```

## 核心接口

| 接口 | 职责 |
|------|------|
| IFormulaRepository | 验方仓储 (CRUD + 搜索 + 克隆 + 状态切换 + 批量操作) |
| IFormulaService | 业务服务 (保存验证 + 复制 + 删除) |
| IFormulaSearchProvider | 跨模块搜索提供者 (供 MedicalCase 模块调用) |

## 关键功能

| 功能 | 实现 |
|------|------|
| Master-Detail 管理 | FormulaMasterDetailViewModel + MasterDetailControlBase |
| 验方复制 | FormulaService.CopyFormulaAsync (名称加"_副本") |
| 药材组方 | AddHerbCommand / DeleteHerbCommand (HerbItemViewModelBase) |
| 状态管理 | ToggleStatusCommand (启用/禁用) |
| 跨模块搜索 | FormulaSearchProvider 委托 IFormulaRepository |
| 验证 | FormulaValidator (基本信息 + 药材列表 + 完整性) |

## 设计依据

- Master-Detail 组合模式继承 MasterDetailViewModelBase，聚合 Loading/Pagination/Dialog 等服务
- Control 复用模式: FormulaMasterDetailControl 由 Admin 和 Clinical 角色台的 FormulaManagementView 嵌入
- DataSource 抽象层支持 Local(SQLite) / Remote(API) 模式切换
- 跨模块通过 IFormulaSearchProvider 和 IHerbSearchProvider 接口解耦
- Mapperly 编译时映射替代 AutoMapper，零运行时开销

## 依赖关系

### 依赖
- LYBT.Desktop.Foundation (BaseApiRepository/Security)
- LYBT.Desktop.Infrastructure (MasterDetailControlBase/ViewModelBase/Services)
- LYBT.Desktop.Models (ValidatableModelBase/HerbItemViewModelBase)
- LYBT.Desktop.Contracts (IFormulaApi/IFormulaDataSource)
- LYBT.Shared.Models (FormulaListDto/FormulaDetailDto/FormulaInputDto)
- LYBT.Desktop.Herbs (IHerbSearchProvider)
- Prism.DryIoc (8.x)

### 被依赖
- LYBT.Desktop.Admin (FormulaManagementView 嵌入 FormulaMasterDetailControl)
- LYBT.Desktop.Clinical (FormulaManagementView 嵌入 FormulaMasterDetailControl)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 目录结构和接口表更新 |
| 2025-12-04 | 按 README 规范重写文档 |
| 2025-11-15 | Epic #1773 服务层重构 |
| 2025-10-29 | 初始版本 |
