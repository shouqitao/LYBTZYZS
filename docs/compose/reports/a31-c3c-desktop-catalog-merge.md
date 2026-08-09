# A-31-C3c：Desktop Herbs+Formula → Catalog 同步合并报告

> **任务**：`docs/compose/specs/task-a31-c3c-desktop-catalog-merge-2026-08-09.md`（T2 方案调整——命名空间改写+引用更新）
> **执行**：Mimo Code | **日期**：2026-08-09 | **分支**：master
> **依据**：Server 端 C-3b 已完成（`5b94893f5`），Desktop 需同步对齐；`docs/03-architecture/15-solution-integration-plan.md` §2.2 合并 3

---

## 1. 执行摘要

将 Desktop 侧 `LYBT.Desktop.Herbs`（13 cs）+ `LYBT.Desktop.Formula`（14 cs）合并为 `LYBT.Desktop.Catalog`，与 Server 端 Catalog 对齐。命名空间改写 `LYBT.Desktop.Herbs`/`LYBT.Desktop.Formula` → `LYBT.Desktop.Catalog`（29 文件 git mv + 45 处改写），模块注册合一（HerbsModule + FormulaModule → CatalogModule），外部引用全量更新（5 csproj + 3 Shell 代码文件 + 2 Clinical XAML + MedicalCase ModuleDependency + Infrastructure 模块名常量 + 2 角色定义 + 架构/Desktop 测试）。删除旧项目整目录。验证：build 0 错误 0 警告、架构测试 86/86、受影响 Desktop VM 单测 15/15。

| 项 | 内容 | Commit |
|----|------|--------|
| 代码 | 合并迁移 + 外部引用更新 + 删旧项目 + sln/测试同步 | 本次代码 commit |
| 文档 | 本报告 + 总账/整合方案状态同步 | 本次文档 commit |

## 2. 合并执行

**新建 `LYBT.Desktop.Catalog/`**（csproj 引用 Foundation/Infrastructure/Contracts/Shared.Models + Prism/Mapperly/Logging 包，AssemblyName/RootNamespace=LYBT.Desktop.Catalog）：

```
LYBT.Desktop.Catalog/
├── CatalogModule.cs          # HerbsModule + FormulaModule 注册合一（[Module(ModuleName = nameof(CatalogModule))]）
├── Controls/                 # HerbMasterDetailControl + HerbViewControl + HerbEditControl + FormulaMasterDetailControl + FormulaEditControl（+ .xaml.cs）
├── Mappers/                  # FormulaDetailModelMapper
├── Models/                   # HerbDetailModel + FormulaDetailModel + Items/{HerbEditContext, FormulaEditContext}
├── Repositories/             # HerbRepository + FormulaRepository
├── Services/                 # RemoteHerbService + HerbSearchProvider + FormulaService + FormulaSearchProvider
└── ViewModels/               # Herb/Formula MasterDetail + Editor + FormulaHerbItem + Handlers/{IHerb,Herb,IFormula,Formula}StatusHandler
```

- **文件迁移**：`git mv` 29 文件（27 cs + 5 xaml，两个 Module 类除外）→ 命名空间批量改写 45 处（serena replace_in_files，dry-run 核对后应用）
- **Editor VM**：`HerbEditorViewModel`/`FormulaEditorViewModel` 基类 `EditorViewModelBase<TContext>` 已由 C5-4 提取至 `LYBT.Desktop.Infrastructure.ViewModels.Base`（共享），**保持不变**（任务书第 4 条）
- **Contracts 层**：`IFormulaApi`/`IHerbApi`（`Contracts/Api/`，internal Refit 接口）**保持原名**（路由不变，任务书第 5 条用户定案）
- **Shared 实体层**：`LYBT.Shared.Models.Contracts.Herbs/Formula`（HerbListDto/FormulaListDto 等）不动

## 3. DI 注册合并（CatalogModule）

HerbsModule（6 项注册）+ FormulaModule（6 项注册）合一，`[ModuleDependency("AuthenticationModule")]`（原 FormulaModule 依赖 HerbsModule 的内部依赖随合并消除）：

- ViewModelLocationProvider：HerbMasterDetailControl + FormulaMasterDetailControl → 各自 VM
- `IHerbService→RemoteHerbService` / `IFormulaService→FormulaService`
- 跨模块：`IHerbSearchProvider→HerbSearchProvider` / `IFormulaSearchProvider→FormulaSearchProvider`（供 MedicalCase）
- Handlers：IHerbStatusHandler + IFormulaStatusHandler
- `FormulaDetailModelMapper`（Singleton）+ `AddMasterDetailServices<HerbListDto,HerbDetailModel>` + `<FormulaListDto,FormulaDetailModel>`
- 4 个 VM（Herb/Formula MasterDetail + Editor）注册

## 4. 外部引用更新（实际 13 文件，任务书"4 个外部模块"为估算口径）

| 引用方 | 改动 |
|--------|------|
| `Shell/App.xaml.cs` | using ×2→1；`AddModule<HerbsModule/FormulaModule>` → `AddModule<CatalogModule>`（OnDemand） |
| `Shell/LYBT.Desktop.Shell.csproj` | ProjectReference ×2 → ×1 |
| `Shell/Extensions/DataSourceRegistrationExtensions.cs` | using `LYBT.Desktop.{Herbs,Formula}.Repositories` → `LYBT.Desktop.Catalog.Repositories` |
| `Shell/Services/NavigationManager.cs` | 药材/验方导航项统一由 `modules.Contains("CatalogModule")` 门控 |
| `Roles/LYBT.Desktop.Clinical.csproj` | ProjectReference ×2 → ×1 |
| `Roles/LYBT.Desktop.Clinical/Views/HerbManagementView.xaml` + `FormulaManagementView.xaml` | xmlns `clr-namespace:LYBT.Desktop.Catalog.Controls;assembly=LYBT.Desktop.Catalog` |
| `Roles/LYBT.Desktop.Admin.csproj` | ProjectReference ×2 → ×1 |
| `Modules/LYBT.Desktop.MedicalCase/MedicalCaseModule.cs` | `[ModuleDependency("HerbsModule")]+[ModuleDependency("FormulaModule")]` → `[ModuleDependency("CatalogModule")]` |
| `Core/LYBT.Desktop.Infrastructure/Navigation/ModuleLazyLoader.cs` | 视图→模块映射 ×2 → "CatalogModule"；Doctor 预加载数组收敛 |
| `Core/LYBT.Desktop.Infrastructure/Roles/Definitions/DoctorRoleDefinition.cs` + `AdminRoleDefinition.cs` | RequiredModules 中 HerbsModule/FormulaModule → CatalogModule |
| `Core/LYBT.Desktop.Contracts/.../IHerbSearchProvider.cs` + `IFormulaSearchProvider.cs` | 注释中旧模块名 → Catalog（2 处） |
| `tests/LYBT.Tests.Desktop.csproj` + `LYBT.Tests.Architecture.csproj` | ProjectReference ×2 → ×1 |

## 5. 删除旧项目 + sln

- `LYBT.Desktop.Herbs/` + `LYBT.Desktop.Formula/` 整目录删除（8 个跟踪文件：2 Module + 2 csproj + 2 README + 2 AGENTS；bin/obj 物理清理）
- sln：2 项目声明 → 1（`LYBT.Desktop.Catalog`，复用原 Herbs GUID `{4C4FBB8B-...}`，ProjectConfigurationPlatforms 保留该 GUID 块，删除 Formula GUID `{B9D29E3D-...}` 12 行 + NestedProjects 1 行），嵌套组 Desktop.BusinessModules 不变

## 6. 测试同步（架构 + Desktop）

- `TestAssemblies.cs`：`Assembly.Load("LYBT.Desktop.Herbs"/"LYBT.Desktop.Formula")` ×2 → `Assembly.Load("LYBT.Desktop.Catalog")` ×1
- `DesktopLayerArchTests.cs` 5 段去重收敛：DM01b 模块名单 / DM01 / DM05（Repository 接口名单）/ DM06 / DP07（模块隔离字典）——Herbs+Formula 双条目合并为 Catalog 单条目
- `Unit/Herbs/HerbMasterDetailViewModelTests.cs` + `Unit/Formula/FormulaMasterDetailViewModelTests.cs`：using 改写

## 7. 验证结果（真实输出）

| 验证项 | 结果 |
|--------|------|
| `dotnet build LYBTZYZS.sln --no-incremental` | **0 错误 0 警告**（31 项目，LYBT.Desktop.Catalog 正常产出） |
| `dotnet test tests/LYBT.Tests.Architecture/` | **86/86 全绿** |
| `dotnet test tests/LYBT.Tests.Desktop/ --filter FullyQualifiedName~HerbMasterDetailViewModelTests\|FullyQualifiedName~FormulaMasterDetailViewModelTests` | **15/15 通过** |

## 8. 关键约束达成

| 约束 | 达成 |
|------|------|
| 路由 `/api/v1/herbs/*` + `/api/v1/formulas/*` 不变（Desktop Refit 接口不动） | ✅ Contracts `IHerbApi`/`IFormulaApi` 未触碰 |
| 命名空间改写 Herbs/Formula → Catalog | ✅ 45 处（含 XAML x:Class/clr-namespace/assembly） |
| Shared 实体层不动 | ✅ `LYBT.Shared.Models.Contracts.Herbs/Formula` 零改动 |
| Editor VM 基类共享（C5-4） | ✅ `EditorViewModelBase<TContext>`（Infrastructure）保持不变 |
| 0 错误 0 警告 | ✅ |
| 架构测试全绿 | ✅ 86/86 |

## 9. 文档更新

- `docs/03-architecture/13-project-master-plan.md`：A-31 行追加 C-3c 完成记录
- `docs/03-architecture/15-solution-integration-plan.md`：§2.2 合并 3 → 已完成；批次表 C-3c → ✅（C-4 冗余行标注已覆盖）

## 10. 遗留/后续

- 本批仅 Desktop 模块层合并；Server 侧已于 C-3b 完成（双端现已对齐 Catalog 命名）
- Desktop LocalWebAPI 控制器测试的 `/api/herbs` 路径不匹配（HEAD 既有缺陷，C-3b 报告 §5 已记录）不在本任务范围

---

*报告完成。全部验证基于真实 build/test 输出。*
