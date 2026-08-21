# 第5轮审查：架构合规性验证

> **审查日期**: 2026-08-20  
> **审查人**: Hermes Agent (架构师视角, intended vs implemented)  
> **审查范围**: `01-system-overview` 依赖方向/模块自治 + `03-server` 3-Layer + `AGENTS` 0/0门禁 + `A-26 Status/State语义` + `ADR-0010双模式` + `架构守卫87/87` + `Shared单源/Mapperly` + `API契约` + `文档-代码一致性`

## 一、执行摘要

| 维度 | 意图 | 实现 | 结论 |
| :--- | :--- | :--- | :--- |
| **分层/依赖方向** | `WebAPI→Modules→Infrastructure→Entities`，`Server↮Client唯一HTTP`，模块间`IXxxCrossModuleService` | `ServerArchTests 87/87` 全绿，`grep LYBT.Module`仅跨域接口 | ✅ |
| **共享单源/映射** | `Shared.Models`唯一，所有`Mapperly RequiredMappingStrategy=Target` | `Gender`单源`Shared.Models/Enums`，12 `Mapper`全`Target` | ✅ |
| **双模式** | `ADR-0010 Remote/Local同一Service`，`SwitchingApiClient`路由 | `LocalWebApiPatternTests P20/P21` 6模块引用匹配ADR，`P22 ApiController`全标注 | ✅ |
| **工程门禁** | `dotnet build --no-incremental 0错误0警告` + `架构87/87` | `build 6×CS0105` + `架构87/87` | 🔴 **0/0门禁失败** |
| **API契约/文档** | `IApiClient`唯一面对外，`/api/v1`版本化，文档即代码 | `Herb Export`返回`ApiResponse<List>` vs `IApiClientHerbs→HttpResponseMessage`类型分叉 | 🟠 |

**严重度**：🔴 CRITICAL 1 · 🟠 HIGH 2 · 🟡 MEDIUM 3 · ✅ PASS 6

---

## 二、CRITICAL（门禁阻断）

### C1 — `dotnet build --no-incremental` **6×CS0105** 违背 `AGENTS.md` 0/0 硬门禁
- **意图** (`AGENTS.md §编码规范 0/0门禁`)：每个提交必须 `0错误0警告`（`--no-incremental`强制全量）
- **证据**：
  ```
  6 个警告  CS0105 “LYBT.Tests.Desktop.Infrastructure”的 using 指令以前在此命名空间中出现过
   — HistoryCopyDialogViewModelTests.cs:14, FormulaMasterDetailViewModelTests.cs:16, HerbMasterDetailViewModelTests.cs:18, PatientMasterDetailViewModelTests.cs:15, FormulaImportDialogViewModelTests.cs:15, ConnectionStatusViewModelTests.cs:7
  ```
  复测 `dotnet test tests/LYBT.Tests.Architecture: 87/87 pass`（非文档所述`76`/`88`/`81`），`01-system-overview.md:228` 仍写`76 tests`
- **修复**：P0 删除6文件重复`using`（或收敛为`GlobalUsings.cs`），`01-system-overview.md`更新`76→87`，提交信息`fix(build): 移除Desktop tests重复using，恢复0/0门禁`

---

## 三、HIGH（契约/双端一致性）

### H1 — 药材导出API契约分叉
- **意图** (`01-system-overview 契约单一(A-18)` + `06-herbs US-HERB-013`)：`Server`与`Desktop`通过`Shared.Models`单一契约，`IApiClient`为唯一面对外
- **证据**：
  ```csharp
  // Server CatalogController.cs:164-183
  [HttpGet("export-all")] public async Task<IActionResult> HerbExportAll(){ return Success(result.Value!.Items,"药材导出（JSON）"); }
  // Desktop IApiClientHerbs.cs:77
  Task<HttpResponseMessage> ExportHerbsAsync(string? keyword=null); // → HttpResponseMessage (裸文件流)
  ```
  `Server`返回`ApiResponse` wrapper而`Desktop`按`file stream`处理，导致`Desktop`导出的`.json`实为`{"success":true,"data":[...]}`包装体，无法回灌`BatchImport`
- **修复**：统一为`Task<ApiResponse<List<HerbListDto>>> ExportHerbsAsync`，`ParityTests`加返回类型校验

### H2 — `Status` vs `State` 语义边界部分越界
- **意图** (`01-system-overview §设计原则7` A-26)：域持久化用`Status`，客户端会话用`State`
- **证据**：`MedicalCaseStateService`注释`支持Draft/Active/Completed`中的`Draft`已删但注释残留`Draft↔Active`（`BusinessRules.cs`已仅`Active↔Suspended`），属文档-代码滞后
- **修复**：全局清理历史注释，`ArchTest`加`StatusEnumMustEndWithStatus`

---

## 四、MEDIUM（文档-代码一致性）

### M1 — `P01b_UI_Should_Not_Depend_On_Entities` 9控制器豁免
- **证据**：`ArchTests.cs:41-49 excludedControllers = {PatientsController,MedicalCasesController,...9个}`，9个控制器被豁免`NotHaveDependencyOn("LYBT.Entities")`
- **修复**：将`Herb`幻影类型改为`string entityType`，逐步收敛豁免至`0`

### M2 — 双端路由版本占位符分叉：`Server api/v{version:apiVersion}` vs `Local api/v1`
- **证据**：`Server` 31条含`"/api/v{version:apiVersion}/formulas"`，`Local`为`"/api/v1/formulas"`；`P09b`允许`api/v{version`，但`ParityTests`需`NormalizeVersion`后比对
- **修复**：文档`05-dual-mode`加注“Server `v{version}`与Local `v1`逻辑等价”

### M3 — 文档项目数/测试数基线滞后
- **证据**：`01-system-overview` 列`约40+项目`但`LYBT.Tests.Architecture: 76 tests`与实测`87`差`11`
- **修复**：`TestAssemblies.cs`加`TotalTestsDocComment`单源，`docs`统一读`ArchTests`计数

---

## 五、PASS（架构合规已闭合）

| 守卫 | 证据 | 结论 |
| :--- | :--- | :--- |
| **3-Layer** `Controller→Service→Repository→DbContext` | `Controllers`仅`Sender.Send/GetOperator/HandleResult`，`grep AppDbContext`在`Controllers`零命中 | ✅ |
| **模块隔离** | `grep "using LYBT.Module" Modules`仅同模块，跨模块经`IXxxCrossModuleService`；`A-31-C8`已删`ICrossModuleService`统一门面 | ✅ |
| **Desktop分层** | `DP01不依赖Infrastructure`, `DP02不含DTO`, `DP04 ViewModel继承` 全绿 | ✅ |
| **LocalWebAPI统一服务层** | `P20 OnlyHaveDependenciesOn`, `P21 ADR-0010 6模块引用`, `P22 ApiController` 全绿 | ✅ |
| **Mapperly Target** | 12 `Mapper`全`RequiredMappingStrategy=Target` | ✅ |
| **API版本化** | `P09b Controllers Should Use V1 Routes` + `P09c Must Be In Controllers Namespace` 全绿 | ✅ |

---

## 六、修复清单

| 优先级 | 缺陷 | 文件 | 改动 |
| :--- | :--- | :--- | :--- |
| **P0** | C1 0/0门禁 | `tests/LYBT.Tests.Desktop/Unit/**/*.cs` + `01-system-overview.md` | 删6重复`using`，文档`76→87` |
| **P1** | H1 Export契约分叉 | `IApiClientHerbs.cs:77` + `CatalogController.cs:164` | 统一`ApiResponse<List>` vs `HttpResponseMessage` |
| **P2** | M1 豁免收敛 | `ArchTests.cs:41` | 逐步移除`Herb`幻影类型 |
| **P2** | M3 文档基线 | `01-system-overview.md` | 测试数单源化 |

> **验证**：`dotnet build --no-incremental` 0/0 + `dotnet test tests/LYBT.Tests.Architecture` 87/87 + `docs grep "76 tests"`零命中 + 手工`GET /api/v1/herbs/export`→`ApiResponse`解析→`BatchImport`回灌。
