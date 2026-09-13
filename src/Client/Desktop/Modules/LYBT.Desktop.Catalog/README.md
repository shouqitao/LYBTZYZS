# LYBT.Desktop.Catalog

> 药房目录模块（Herbs + Formula 合并，A-26 C3b）——Desktop WPF/Prism 业务模块。

## 项目定位

| 属性 | 值 |
|------|-----|
| 层级 | Client → Desktop → Modules |
| 职责 | 药材（Herb）+ 验方（Formula）主数据：CRUD/搜索/分类筛选/批量操作/引用检查/导入导出/验方待校验，以及供 MedicalCase 复用的跨模块搜索提供者 |
| 状态 | Active |
| 合并背景 | A-26 C3b：Herbs 与 Formula 同域（验方 = 药材模板组合），合并消除 ~45-50 重复方法并收敛 4 个跨模块接口 |

## 目录结构

```
LYBT.Desktop.Catalog/
├── CatalogModule.cs                    # Prism 模块注册（唯一入口）
├── Controls/                           # 内嵌控件（5）
│   ├── HerbMasterDetailControl.xaml(.cs)     # 药材主从（AW，ViewModelLocationProvider 显式映射）
│   ├── HerbViewControl.xaml(.cs)             # 药材只读视图
│   ├── HerbEditControl.xaml(.cs)             # 药材编辑表单
│   ├── FormulaMasterDetailControl.xaml(.cs)  # 验方主从（AW，显式映射）
│   └── FormulaEditControl.xaml(.cs)          # 验方编辑表单
├── ViewModels/                         # ViewModel（6 + 2 Handler）
│   ├── HerbMasterDetailViewModel.cs          # 药材主从（MasterDetailViewModelBase<HerbListDto, HerbDetailModel>）
│   ├── HerbEditorViewModel.cs                # 药材编辑（EditorViewModelBase<HerbEditContext>）
│   ├── FormulaMasterDetailViewModel.cs       # 验方主从（含待校验模式/药材绑定）
│   ├── FormulaEditorViewModel.cs             # 验方编辑（EditorViewModelBase<FormulaEditContext>）
│   ├── FormulaHerbItemViewModel.cs           # 验方内单味药材行（HerbItemViewModelBase，供处方复用）
│   ├── FormulaValidationItemViewModel.cs     # 待校验模式行（HerbItemId + 系统药材选择）
│   └── Handlers/                             # 状态处理（IHerbStatusHandler / IFormulaStatusHandler）
├── Services/
│   ├── HerbService.cs / FormulaService.cs           # 业务服务（CrudServiceBase 派生，统一 CommandResult 错误处理）
│   ├── HerbSearchProvider.cs / FormulaSearchProvider.cs  # 跨模块搜索（IHerbSearchProvider/IFormulaSearchProvider → MedicalCase）
├── Repositories/
│   ├── HerbRepository.cs / FormulaRepository.cs      # IApiClient 路由（Local/Remote 透明）
├── Mappers/
│   ├── HerbDetailModelMapper.cs / FormulaDetailModelMapper.cs  # Mapperly 编译时映射（单例）
└── Models/
    ├── HerbDetailModel.cs / FormulaDetailModel.cs
    └── Items/  HerbEditContext.cs / FormulaEditContext.cs / FormulaHerbItemModel.cs
```

## 视图 / ViewModel 清单（口径：View = 页面级 XAML；Control = 内嵌组件）

| 类型 | 数量 | 条目 |
|------|:---:|------|
| View | 0 | 本模块无独立导航页面（角色台 `Roles/*/Views/HerbManagementView|FormulaManagementView` 为宿主，内嵌本模块 Control） |
| Control | 5 | HerbMasterDetailControl、HerbViewControl、HerbEditControl、FormulaMasterDetailControl、FormulaEditControl |
| Dialog | 0 | 本模块无对话框 |
| ViewModel | 6 | HerbMasterDetailViewModel、HerbEditorViewModel、FormulaMasterDetailViewModel、FormulaEditorViewModel、FormulaHerbItemViewModel、FormulaValidationItemViewModel（+ 2 状态 Handler） |

> 口径与数量校验见 `src/Client/Desktop/DESKTOP_ARCHITECTURE_STANDARD.md` §13.5；全量视图清单见 `docs/compose/specs/desktop-view-inventory.md`。

## 核心组件

| 组件 | 基类 / 接口 | 职责 |
|------|-------------|------|
| **CatalogModule** | `IModule` | 注册 2 处 `ViewModelLocationProvider` 映射、2 个业务服务、2 个跨模块搜索提供者、2 个状态 Handler、2 个 Mapper 单例、两套 `MasterDetailServices`、4 个 ViewModel |
| **HerbMasterDetailViewModel** | `MasterDetailViewModelBase<HerbListDto, HerbDetailModel>` | 药材列表/详情/新建/编辑/删除/启用停用/复制/分类筛选/导入/导出/模板下载（命令由 `[RelayCommand]` 源生成） |
| **FormulaMasterDetailViewModel** | `MasterDetailViewModelBase<FormulaListDto, FormulaDetailModel>` | 验方列表/详情/CRUD/复制/导入导出/模板 + **待校验模式**（`ToggleValidationModeAsync`：`GET /formulas/pending-validation` 分页 + `ValidateHerbAsync` 绑定系统药材，全部绑定自动晋升 Validated，B-13） |
| **HerbEditorViewModel / FormulaEditorViewModel** | `EditorViewModelBase<TEditContext>` | 编辑表单（`HerbEditContext` / `FormulaEditContext`），校验 + 映射到输入 DTO |
| **FormulaHerbItemViewModel** | `HerbItemViewModelBase` | 验方内药材行（名称/用量/单位/炮制/用法），与处方 `HerbItemControl` 同基类，供 `HerbListControl` 复用 |
| **FormulaValidationItemViewModel** | `ObservableObject` | 待校验模式行：`HerbItemId` + 已绑定药材 + 系统药材选择（提交 `POST /formulas/{id}/herbs/{itemId}/validate`） |
| **HerbService / FormulaService** | `CrudServiceBase<...>` + `IHerbService`/`IFormulaService` | 业务服务：统一 `CommandResult` 错误处理与 `[SVC]` 日志；批量/导入导出/引用检查委托 Repository |
| **HerbSearchProvider / FormulaSearchProvider** | `IHerbSearchProvider` / `IFormulaSearchProvider` | 跨模块只读搜索（MedicalCase 处方自动补全/验方导入），避免模块互引 |
| **HerbRepository / FormulaRepository** | `EntityApiClientRepositoryBase<...>` | 经 `IApiClient` 统一路由（Local/Remote 透明），无本地回退分支 |
| **HerbDetailModelMapper / FormulaDetailModelMapper** | Mapperly `[Mapper]`（单例） | DTO ↔ DetailModel 编译时映射（禁止手写映射扩展） |

## CatalogModule 注册表（代码实际）

| 注册 | 目标 |
|------|------|
| `ViewModelLocationProvider.Register` | `HerbMasterDetailControl → HerbMasterDetailViewModel`、`FormulaMasterDetailControl → FormulaMasterDetailViewModel` |
| `Register<IHerbService, HerbService>` / `Register<IFormulaService, FormulaService>` | 业务服务 |
| `Register<IHerbSearchProvider, HerbSearchProvider>` / `Register<IFormulaSearchProvider, FormulaSearchProvider>` | 跨模块搜索（医疗案例处方/验方导入使用） |
| `Register<IHerbStatusHandler, HerbStatusHandler>` / `Register<IFormulaStatusHandler, FormulaStatusHandler>` | 状态处理（启用/停用/恢复） |
| `RegisterSingleton<HerbDetailModelMapper>` / `RegisterSingleton<FormulaDetailModelMapper>` | 映射器单例 |
| `AddMasterDetailServices<HerbListDto, HerbDetailModel>` / `AddMasterDetailServices<FormulaListDto, FormulaDetailModel>` | 主从服务组（列表/详情/对话框/分页/搜索/选择/加载） |
| `Register<HerbMasterDetailViewModel|HerbEditorViewModel|FormulaMasterDetailViewModel|FormulaEditorViewModel>` | 4 个 ViewModel |

`HerbRepository` / `FormulaRepository` 由 **Shell** 组合根统一注册（`Shell/Extensions/DataSourceRegistrationExtensions.RegisterRepositories`）——模块内不重复注册。

## 依赖关系

### 依赖（编译期 ProjectReference）

| 项目 | 用途 |
|------|------|
| LYBT.Desktop.Contracts | `IApiClientHerbs`/`IApiClientFormulas`、`IHerbService`/`IFormulaService`、`IHerbSearchProvider`/`IFormulaSearchProvider`、`ViewNames` |
| LYBT.Desktop.Infrastructure | ViewModel 基类（`MasterDetailViewModelBase`/`EditorViewModelBase`/`HerbItemViewModelBase`）、Dialog/Navigation/Behavior 服务 |
| LYBT.Desktop.Foundation | HTTP 客户端（Refit/Switching）、缓存、安全、配置 |
| LYBT.Shared.Models | 契约 DTO（`HerbListDto`/`HerbDetailDto`/`HerbInputDto`/`FormulaListDto`/`FormulaDetailDto`/`FormulaInputDto` 等）与校验器 |

> 模块**不引用**其他 Desktop 业务模块（P07）：与 MedicalCase 的协作经 `Contracts` 的 `IHerbSearchProvider`/`IFormulaSearchProvider` 与 Shared.Models DTO 完成。

### 被依赖

| 依赖方 | 方式 |
|--------|------|
| Shell | `App.ConfigureModuleCatalog` 按角色加载 `CatalogModule`（`RoleRegistry`：Admin/Doctor/SuperAdmin…） |
| Roles.LYBT.Desktop.Admin / Clinical | 宿主视图 `HerbManagementView` / `FormulaManagementView` 内嵌本模块 `HerbMasterDetailControl` / `FormulaMasterDetailControl`（View 在角色台、Control 在业务模块） |
| Modules.LYBT.Desktop.MedicalCase | 经 `IHerbSearchProvider`（处方药材自动补全单价/单位）与 `IFormulaSearchProvider`（验方导入到处方） |

## 设计决策

| 决策 | 依据 |
|------|------|
| Herbs + Formula 合并为 Catalog | A-26 C3b：同域（验方 = 药材组合），消除重复方法/接口，维护成本减半 |
| 双轨：写走 Handler、读走 Service | A-26 T2（蓝图 §2.2）：写操作经 MediatR 验证管道 + 审计，读操作 Service 直查 |
| 导入导出为 **JSON** 契约 | 2026-08-13 `#112`（后端不涉及 Excel，保持通用性）；Desktop 端保存 `药材导入模板.json`/`验方导入模板.json`，导入复用 `POST batch-import`（2026-08-19 `#129/#130` 补双端路由与参数对齐） |
| 验方延迟绑定 + 待校验模式 | US-FORM-007/008/009（B-13，2026-09-09）：Draft 待校验列表 + 行级系统药材绑定，全部绑定自动 `Validated` |
| 药材行复用 `HerbItemViewModelBase` | 验方组成与处方药材行同形状，复用 `HerbItemControl` 与其 VM 基类，避免两套实现 |

## 已知陷阱

1. **验方导入模板必须含 `Category` 与对象数组 Herbs**：`FormulaImportItemDto.Herbs` 为对象数组（`{HerbName,Dosage,Unit}`），模板示例若写成字符串数组将反序列化失败（`#129` 修复）。
2. **Local 模式路由前缀**：`LocalWebAPI` 的验方 action 路由必须是绝对路径（以 `/` 开头），否则与类级 `Route("api/v1/herbs")` 拼接成 `/api/v1/herbs/api/v1/formulas/*` 导致全部 404（`#129` 修复，守卫测试 `ImportExportRouteParityTests`）。
3. **`FormulaMasterDetailControl`/`HerbMasterDetailControl` 的 VM 依赖显式映射**：控件类型不在 `*.Views` 命名空间，Prism 约定名不存在 → 必须保留 `CatalogModule` 中的 `ViewModelLocationProvider.Register`（漏登记将静默继承宿主 DataContext）。
4. **写操作权限口径**：药材/验方 `batch-import`/`import-template`/`export`/`export-all` 为 `AdminOrSuperAdmin`（`#130` 对齐），Doctor 仅可查看/使用（验方可管理自己创建的）。

## 变更记录

- 2026-09-13 docs 复盘：新建本 README（此前缺失，导致 `FormulaEditorViewModel`/`HerbEditorViewModel`/`FormulaHerbItemViewModel`/`FormulaValidationItemViewModel` 无文档描述），内容按代码实际（目录树、清单、注册表、依赖、决策、陷阱）编写；口径与 `DESKTOP_ARCHITECTURE_STANDARD.md` §13.5 一致。
