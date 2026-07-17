# LYBT.Desktop.Formula

> 验方管理模块 | 经验方CRUD / 药材组方 / 验方复制 / 跨模块搜索

## 项目定位

- **层级**: `src/Client/Desktop/Modules/` — Prism 模块层
- **职责**: 验方(经验方/经典方剂模板)的增删改查、药材组方编辑、验方复制、按分类搜索，为处方开具提供模板支持
- **ModuleDependency**: `HerbsModule`（运行时需要 `IHerbSearchProvider` 已注册）

## 目录结构

```
LYBT.Desktop.Formula/
├── FormulaModule.cs                          # Prism 模块注册入口
├── Controls/
│   ├── FormulaMasterDetailControl.xaml/.cs    # Master-Detail 可复用控件（Admin/Clinical 角色台嵌入）
│   └── FormulaEditControl.xaml/.cs            # 验方编辑控件（DependencyProperty 绑定）
├── Interfaces/
│   └── IFormulaService.cs                     # 验方 Service 接口（9 方法，CommandResult<T>）
├── Mappers/
│   ├── FormulaDetailModelMapper.cs            # Mapperly: FormulaDetailDto ↔ FormulaDetailModel（Singleton）
│   ├── FormulaHerbItemMapper.cs               # Mapperly: FormulaHerbItemDto ↔ FormulaHerbItem
│   └── FormulaMapper.cs                       # Mapperly: FormulaDetailDto ↔ FormulaItem（IsShared↔IsPersonal 反转）
├── Models/
│   ├── FormulaDetailModel.cs                  # Detail 编辑模型（ValidatableModelBase，DataAnnotations 验证）
│   └── Items/
│       ├── FormulaEditContext.cs               # 编辑上下文（替代 FormulaDetailModel 的编辑角色，XAML 绑定目标）
│       ├── FormulaHerbItem.cs                  # 验方药材项 UI 模型（BindableBase）
│       └── FormulaItem.cs                      # 验方列表项 UI 模型（BindableBase）
├── Repositories/
│   └── FormulaRepository.cs                   # 仓储实现（Repository 抽象，Local/Remote 双模式）
├── Services/
│   ├── FormulaService.cs                      # 业务服务（9 方法：CRUD + 复制 + 状态 + 批量 + 导入导出）
│   └── FormulaSearchProvider.cs               # 跨模块搜索提供者（IFormulaSearchProvider，供 MedicalCase 调用）
└── ViewModels/
    ├── FormulaMasterDetailViewModel.cs         # 核心 VM（MasterDetailViewModelBase 组合模式）
    ├── FormulaEditorViewModel.cs               # 子 VM（验方编辑，封装 FormulaEditContext + EditHerbItems）
    ├── FormulaHerbItemViewModel.cs             # 药材项 VM（HerbItemViewModelBase）
    └── Handlers/
        ├── IFormulaStatusHandler.cs            # 状态操作接口
        └── FormulaStatusHandler.cs             # 状态操作实现（ToggleStatus/Restore）
```

## 核心组件

| 类 | 设计依据 | 职责 |
|---|---|---|
| **FormulaModule** | `[Module(ModuleName)]` + `[ModuleDependency("HerbsModule")]`；RegisterTypes 注册所有 DI | Prism 模块入口，注册 IFormulaSearchProvider、IFormulaService、FormulaDetailModelMapper(Singleton)、MasterDetailServices、FormulaMasterDetailViewModel、FormulaEditorViewModel |
| **FormulaMasterDetailViewModel** | 继承 `MasterDetailViewModelBase<FormulaListDto, FormulaDetailModel>`；组合模式，聚合 Loading/Pagination/Dialog/ErrorHandler 服务 | 分页列表加载、详情加载(委托 FormulaEditor)、保存(委托 FormulaEditor.GetHerbInputDtos)、删除、6 个扩展命令 |
| **FormulaEditorViewModel** | `ObservableObject` 子 VM；封装 `FormulaEditContext` + `ObservableCollection<FormulaHerbItemViewModel>` | InitializeFromDto / InitializeForNewCase / SetAllHerbs / GetHerbInputDtos / AddHerb / DeleteHerb / Validate / Reset |
| **FormulaService** | `IFormulaService` 实现；`CommandResult<T>` 统一返回；委托 `IFormulaRepository` | GetByIdAsync、GetPagedAsync、CreateFormulaAsync、UpdateFormulaAsync、CopyFormulaAsync、DeleteFormulaAsync、ToggleStatusAsync、BatchDeleteAsync、BatchImportAsync |
| **FormulaSearchProvider** | `IFormulaSearchProvider` 实现；跨模块接口解耦 | GetFormulasPagedAsync、GetFormulaByIdAsync（供 MedicalCase 模块使用） |
| **FormulaDetailModelMapper** | Mapperly `[Mapper]` 编译时生成；Singleton 注册；Herbs 集合手动映射(→ObservableCollection) | ToItem(Dto→Model)、ToDto(Model→Dto)、ToInputDto(Model→InputDto，Id 空 Guid→null) |
| **FormulaMapper** | Mapperly `[Mapper]`；IsShared↔IsPersonal 手动反转；Herbs 集合手动映射 | ToItem(FormulaDetailDto/FormulaListDto→FormulaItem)、ToDto、ToInputDto |
| **FormulaDetailModel** | 继承 `ValidatableModelBase`；DataAnnotations 验证([Required]/[StringLength]) | XAML 绑定目标，CreateNew() 静态工厂，Clone() 深拷贝 |
| **FormulaEditContext** | 继承 `ValidatableModelBase`；替代 FormulaDetailModel 的编辑角色 | EditControl 的 Object DP 绑定目标，所有编辑字段集中于此 |
| **FormulaStatusHandler** | `IFormulaStatusHandler` 实现 | ToggleStatusAsync（启用/禁用切换）、RestoreAsync（软删除恢复） |

## 依赖关系

### 编译时依赖（ProjectReference）

| 项目 | 用途 |
|---|---|
| LYBT.Desktop.Foundation | BaseApiRepository / Security 基础设施 |
| LYBT.Desktop.Infrastructure | MasterDetailControlBase / ViewModelBase / DI 扩展 / Services |
| LYBT.Desktop.Contracts | IFormulaRepository / IFormulaSearchProvider / IHerbSearchProvider 跨模块接口 |
| LYBT.Shared.Models | FormulaListDto / FormulaDetailDto / FormulaInputDto / HerbListDto DTO |
| LYBT.Shared.Primitives | ValidationConstants / CommonStatus 枚举 |
| LYBT.Shared.Components | 共享 UI 组件 |

### 运行时依赖（ModuleDependency）

| 模块 | 接口 | 说明 |
|---|---|---|
| HerbsModule | `IHerbSearchProvider` | 拼音码快速匹配、AllHerbs 列表加载 |

### 被依赖

| 消费方 | 接口/控件 | 说明 |
|---|---|---|
| MedicalCaseModule | `IFormulaSearchProvider` | 验方导入弹窗搜索验方 |
| Admin / Clinical 角色台 | `FormulaMasterDetailControl` | 嵌入 FormulaManagementView |

## 设计决策

1. **组合模式 ViewModel**: FormulaMasterDetailViewModel 继承 MasterDetailViewModelBase，通过 IMasterDetailServices 聚合 Loading/Pagination/Dialog/ErrorHandler/DetailEditor 服务，避免重复基础设施代码
2. **FormulaEditor 子 VM 分离**: 编辑逻辑(InitializeFromDto/GetHerbInputDtos/AddHerb/DeleteHerb/Validate)封装到独立子 VM，MasterDetailVM 仅协调列表/保存/删除流程
3. **FormulaEditContext 统一编辑真源**: 替代 FormulaDetailModel 的编辑角色，作为 EditControl 的 Object DP 绑定目标，所有编辑字段集中于此
4. **Mapperly 编译时映射**: 替代 AutoMapper，零运行时开销；FormulaDetailModelMapper 注册为 Singleton
5. **Herbs 集合手动映射**: ObservableCollection 无法由 Mapperly 自动生成，需在 ToItem/ToDto 包装方法中手动逐项转换
6. **IFormulaSearchProvider 跨模块解耦**: 通过 Contracts 层接口实现 Formula→MedicalCase 解耦，无需 ProjectReference
7. **FormulaMapper 的 IsShared↔IsPersonal 反转**: DTO 用 IsShared(共享=真)，UI 用 IsPersonal(个人=真)，映射时需手动 `!` 取反

## 已知陷阱

1. **FormulaEditControl 的 Effect 属性命名冲突**: 使用 `FormulaEffect` 而非 `Effect`，是为避免与 `UIElement.Effect` 冲突
2. **IsShared/IsPersonal 反转容易遗漏**: FormulaMapper 和 FormulaMasterDetailViewModel 中多处需手动映射 `IsShared = !IsPersonal`，新增字段时务必同步
3. **Herbs 集合手动映射**: FormulaDetailModelMapper 无法自动映射 ObservableCollection，新增 Herbs 子字段时必须同步更新 ToItem/ToDto/ToInputDto 的手动映射段
4. **BatchEnable/BatchDisable 本地模式不支持**: 返回 null 而非异常，调用方需检查返回值
5. **FormulaCommandHandler 未注册 DI**: 实现完整但从未在容器注册，属于废弃代码
6. **FormulaHerbItemViewModel.AllHerbs 设置时机**: 必须在 InitializeFromDto / AddHerb 之后调用 SetAllHerbs，否则拼音自动补全列表为空
