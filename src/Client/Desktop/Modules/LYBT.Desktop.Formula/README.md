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
│   └── FormulaRepository.cs                # 仓储实现 (Repository 抽象层)
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
- Repository 模式支持 Local(SQL Server LocalDB) / Remote(API) 模式切换
- 跨模块通过 IFormulaSearchProvider 和 IHerbSearchProvider 接口解耦
- Mapperly 编译时映射替代 AutoMapper，零运行时开销

## 依赖关系

### 依赖
- LYBT.Desktop.Foundation (BaseApiRepository/Security)
- LYBT.Desktop.Infrastructure (MasterDetailControlBase/ViewModelBase/Services)
- LYBT.Desktop.Models (ValidatableModelBase/HerbItemViewModelBase)
- LYBT.Desktop.Contracts (IFormulaApi/IFormulaRepository)
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

## 开发笔记

# LYBT.Desktop.Formula CLAUDE.md

## 代码文件结构

### FormulaModule.cs
- `FormulaModule : IModule` -- Prism模块入口，依赖HerbsModule
  - `OnInitialized()` -- 空实现
  - `RegisterTypes()` -- 注册IFormulaRepository(Singleton)、IFormulaSearchProvider、IFormulaService、FormulaValidator、MasterDetail服务、FormulaMasterDetailViewModel

### CommandHandlers/

- `IFormulaCommandHandler.cs` -- `IFormulaCommandHandler : ICommandHandlerBase<FormulaListDto, FormulaDetailDto, FormulaInputDto>` 接口，扩展SearchByNameAsync/SearchByPinyinAsync/GetHerbItemsAsync/CopyAsync
- `FormulaCommandHandler.cs` -- `FormulaCommandHandler : IFormulaCommandHandler` 实现，封装IFormulaRepository，提供CRUD+搜索+复制操作

### Controls/

- `FormulaEditControl.xaml.cs` -- 验方编辑UserControl，DependencyProperty: FormulaName/Category/Property/FormulaEffect/Usage/Remark/CreatedAt/UpdatedAt/HerbCount/AllHerbs/HerbItems/ErrorsSource
- `FormulaMasterDetailControl.xaml.cs` -- `FormulaMasterDetailControl : MasterDetailControlBase`，InitializeViewModel<FormulaMasterDetailViewModel>()，供Admin和Clinical角色台复用

### Interfaces/

- `IFormulaRepository.cs` -- 验方仓储接口(RESTful设计)，方法: GetPagedAsync/GetByIdAsync/CreateAsync/UpdateAsync/DeleteAsync/SearchAsync/CloneFormulaAsync/ToggleStatusAsync/RestoreAsync/BatchDeleteAsync/BatchEnableAsync/BatchDisableAsync
- `IFormulaService.cs` -- 验方Service接口，方法: SaveFormulaAsync/CopyFormulaAsync/DeleteFormulaAsync

### Mappers/

- `FormulaDetailModelMapper.cs` -- Mapperly编译时映射器 [Mapper]，映射FormulaDetailDto<->FormulaDetailModel，FormulaDetailModel->FormulaInputDto，手动映射Herbs集合(ObservableCollection)
  - `ToItemCore()` / `ToItem()` -- DTO->Model
  - `ToDtoCore()` / `ToDto()` -- Model->DTO
  - `ToInputDtoCore()` / `ToInputDto()` -- Model->InputDto(Id空Guid转null)
- `FormulaHerbItemMapper.cs` -- Mapperly编译时映射器 [Mapper]，映射FormulaHerbItemDto<->FormulaHerbItem，FormulaHerbItem->FormulaHerbItemInputDto
  - `ToItem()` / `ToDto()` / `ToInputDto()`
- `FormulaMapper.cs` -- Mapperly编译时映射器 [Mapper]，映射FormulaDetailDto<->FormulaItem、FormulaListDto->FormulaItem、FormulaItem->FormulaInputDto，手动处理IsShared<->IsPersonal反转和Herbs集合
  - `ToItem(FormulaDetailDto)` / `ToItem(FormulaListDto)` / `ToDto()` / `ToInputDto()` -- 含私有Core方法

### Models/

- `FormulaDetailModel.cs` -- `FormulaDetailModel : ValidatableModelBase`，Master-Detail Detail区域编辑模型
  - 属性: Id/IsNew(计算)/Name([Required])/Effect/Usage/Property/Remark/IsShared/Category/Status/CreatedAt/UpdatedAt/CreatedBy/Source/Herbs(ObservableCollection<FormulaHerbItemDto>)/HerbCount(计算)
  - 方法: `CreateNew()` (静态工厂) / `Clone()`

### Models/Items/

- `FormulaHerbItem.cs` -- `FormulaHerbItem : BindableBase`，验方药材项UI模型
  - 属性: HerbId(Guid?)/HerbName/Dosage/Unit/Usage/SortOrder/DecocteMethod/DisplayText(计算)
- `FormulaItem.cs` -- `FormulaItem : BindableBase`，验方列表项UI模型(替代直接使用DTO)
  - 属性: Id/Name/Pinyin/Category/Source/Composition/Effect/Indications/Usage/Modification/Contraindications/Remark/CreatedBy/IsClassic/IsPersonal/Status/UsageCount/CreatedAt/UpdatedAt/Herbs/IsSelected/IsExpanded/IsFavorite
  - 计算属性: IsActive/TypeText/TypeColor/StatusText/StatusColor/HerbCount/HerbCompositionText/DisplayText/SearchText/IsAvailable/HasContraindication/HasModification/PopularityLevel/PopularityColor

### Repositories/

- `FormulaRepository.cs` -- `FormulaRepository : IFormulaRepository`，Repository模式，支持Local/Remote模式
  - 依赖: IFormulaRepository + IFormulaApi?(可选，仅Remote批量操作)
  - CRUD: GetPagedAsync/GetByIdAsync/CreateAsync/UpdateAsync/DeleteAsync/SearchAsync
  - 专用: CloneFormulaAsync
  - 状态/批量: ToggleStatusAsync/RestoreAsync/BatchDeleteAsync/BatchEnableAsync/BatchDisableAsync
  - 本地模式BatchDelete逐个执行，BatchEnable/BatchDisable不支持

### Services/

- `FormulaSearchProvider.cs` -- `FormulaSearchProvider : IFormulaSearchProvider`，跨模块搜索提供者(D5-3)，委托IFormulaRepository
  - `GetFormulasPagedAsync()` / `GetFormulaByIdAsync()`
- `FormulaService.cs` -- `FormulaService : IFormulaService`，验方业务服务
  - `SaveFormulaAsync()` -- 保存(创建/更新)，验证至少一味药材
  - `CopyFormulaAsync()` -- 复制(名称加"_副本"，默认不共享)
  - `DeleteFormulaAsync()` -- 删除
- `FormulaValidator.cs` -- `FormulaValidator : HerbValidatorBase<FormulaHerbItemViewModel>`，验方验证器
  - `ValidateFormulaInfo()` -- 基本信息验证(名称/功效/用法长度)
  - `ValidateFormulaHerbs()` -- 药材列表验证(数量/主药检查)
  - `IsUniqueFormulaNameAsync()` -- 名称唯一性(占位实现，始终返回true)
  - `ValidateFormulaCompleteness()` -- 完整性验证(信息+药材)

### ViewModels/

- `FormulaHerbItemViewModel.cs` -- `FormulaHerbItemViewModel : HerbItemViewModelBase`，验方药材项ViewModel
  - 属性: Remark / UnitPrice(override, 固定返回0)
  - 方法: `ToDto()` -- 转换为FormulaHerbItemInputDto
- `FormulaMasterDetailViewModel.cs` -- `FormulaMasterDetailViewModel : MasterDetailViewModelBase<FormulaListDto, FormulaDetailModel>` [partial]
  - 依赖: IFormulaRepository/IFormulaService/IDialogService/IHerbSearchProvider + FormulaDetailModelMapper
  - 属性: IsAdmin/EditHerbItems/HerbCount(计算)/DetailTitle(计算)
  - 基类实现: LoadListAsync()/LoadDetailAsync()/CreateNewDetail()/SaveDetailAsync()/DeleteItemAsync()
  - 命令: ToggleStatusCommand/CopyFormulaCommand/RestoreCommand/AddHerbCommand/DeleteHerbCommand/SearchByCategoryCommand
  - 导航: OnNavigatedTo() -> LoadAllHerbsAsync()

---

## 死代码与废弃标记

| 类型 | 名称 | 状态 | 说明 |
|------|------|------|------|
| 接口+实现 | `IFormulaCommandHandler` / `FormulaCommandHandler` | **死代码** | 未在DI注册，无外部引用(仅CLAUDE.md引用) |
| Mapper | `FormulaMapper` | **疑似死代码** | 未被`new`实例化，仅在LocalData中有类型引用；ViewModel使用的是FormulaDetailModelMapper |
| Model | `FormulaItem` | **疑似死代码** | FormulaModule.AddMasterDetailServices注册了泛型参数，但ViewModel实际使用FormulaDetailModel |
| Model | `FormulaHerbItem` | **活跃** | FormulaHerbItemMapper映射目标，FormulaItem.Herbs引用 |
| 方法 | `FormulaValidator.IsUniqueFormulaNameAsync()` | **占位代码** | 始终返回true，未被任何代码调用 |
| 服务 | `FormulaValidator` | **疑似低活跃** | 在FormulaModule注册但未被ViewModel注入使用 |

### OpenSpec标记

- `standardize-api-architecture` -- MappingService已删除
- `standardize-service-layer` -- 统一Service命名
- `migrate-views-to-role-modules` -- FormulaDetailView/ValidationView/EditDialog已删除
- `refactor-viewmodel-composition` -- V2组合模式ViewModel
- `refactor-admin-workspace` -- Control模式重构
- `adopt-mapperly-unified-mapping` -- Mapperly映射器
- `resolve-mapperly-source-generator-conflict` -- BindableBase确保Mapperly兼容
- `implement-local-mode` -- Repository模式Local/Remote切换
- `unify-herb-controls-to-herbs-module` -- 统一使用HerbListControl编辑处方
- `cross-module-decoupling` -- 使用IHerbSearchProvider替代IHerbRepository
- `cleanup-formula-dead-code` -- 已删除FormulaValidation相关方法
- `simplify-desktop-data-layer` -- 已删除基本CRUD(ViewModel直接用Repository)
- `unify-frontend-backend-types` -- Phase 4/6统一类型
- `enhance-dataflow-logging` -- LOG-018统一[SVC]前缀
- `ui-validation-framework` -- 验证错误显示

---

## 设计分析

### 架构模式
- **Master-Detail组合模式**: FormulaMasterDetailViewModel继承MasterDetailViewModelBase，使用IMasterDetailServices聚合Loading/Pagination/Dialog/ErrorHandler/DetailEditor服务
- **Control复用模式**: FormulaMasterDetailControl继承MasterDetailControlBase，由Admin和Clinical角色台的FormulaManagementView嵌入使用
- **Repository模式**: 支持Local(SQL Server LocalDB)/Remote(API)模式切换
- **跨模块解耦**: 通过IFormulaSearchProvider和IHerbSearchProvider接口实现模块间通信

### 双Mapper体系
- `FormulaDetailModelMapper`: 用于ViewModel(FormulaMasterDetailViewModel)中FormulaDetailDto<->FormulaDetailModel转换
- `FormulaMapper` + `FormulaHerbItemMapper`: 用于FormulaDetailDto<->FormulaItem转换，当前仅被LocalData模块引用

### 数据流
```
FormulaManagementView (角色台)
  -> FormulaMasterDetailControl
    -> FormulaMasterDetailViewModel
      -> IFormulaRepository (FormulaRepository)
        -> IFormulaRepository (Local或Remote)
```

---

## 已知陷阱

1. **FormulaEditControl的FormulaEffect属性**: 使用FormulaEffect而非Effect命名，是为避免与UIElement.Effect冲突
2. **IsShared/IsPersonal反转**: FormulaMapper和FormulaMasterDetailViewModel中多处需要手动映射`IsShared = !IsPersonal`，容易遗漏
3. **Herbs集合手动映射**: FormulaDetailModelMapper无法自动映射ObservableCollection，需要手动逐项转换
4. **BatchEnable/BatchDisable本地模式不支持**: 返回null而非异常，调用方需检查
5. **FormulaValidator未集成**: 已注册到DI但FormulaMasterDetailViewModel未注入使用，SaveDetailAsync中的验证是内联实现
6. **FormulaCommandHandler未注册**: 实现完整但从未在DI容器注册，属于废弃代码

---

最后更新: 2026-03-01
