# P2 修复代码 Review 报告 — 2026-08-19

> **Commit**: `e8108804b`  P2 修复：权限口径对齐、验方导出筛选参数、文档同步  
> **审查范围**：`src/Server/Services/LYBT.WebAPI/Controllers/CatalogController.cs` / `src/Client/Desktop/LocalWebAPI/Controllers/CatalogController.cs` / `IFormulaApi` / `ICatalogQueryService` / `docs/02-requirements` / `docs/03-architecture/13c-current-status.md` / `docs/02-requirements/13-traceability-matrix.md`  
> **审查方式**：只读审查（不修改代码），对照任务书 `.hermes-task-p2-fixes-doc-sync.md` 逐项核验 + `git show` / `dotnet build --no-incremental` / `dotnet test` 实测

---

## 1. 结论概览

| 维度 | 结论 | 备注 |
|------|------|------|
| **T3 权限口径** | ✅ 通过 | 7/7 端点双端已补 `AdminOrSuperAdmin`，与患者 `batch-import` 口径一致；无遗漏 |
| **T5 导出参数+明细** | ✅ 通过 | `IFormulaApi:71` `?category=` 与双端 `FormulaExport(category)` 已对齐；导出 DTO 已补 `Herbs` 明细 |
| **T7 文档同步** | ✅ 通过 | 需求文档 Excel 残留已清、虚构接口已删、追溯矩阵/13c 已同步至 v1.11 |
| **通用** | ⚠️ 轻微观察项3条 | 无语法错误、build 0/0、架构 87/87；3条改进建议不阻断发布 |

**总体判定**：**通过**，可发布；3条观察项进 backlog（低优先级）。

---

## 2. T3 权限口径对齐

### 2.1 检查清单（任务书要求 3 项批次）

| 端点 | Remote `CatalogController.cs` | Local `CatalogController.cs` | 策略 | 与患者 `batch-import` 一致性 |
|------|-------------------------------|------------------------------|------|------------------------------|
| `POST /herbs/batch-import` `BatchImport` | `src/Server/.../CatalogController.cs:379` `[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]` | `src/Client/.../CatalogController.cs:236` 同 | `AdminOrSuperAdmin` | ✅ 患者 `PatientsController.cs:343` 同策略，模块间已一致 |
| `GET /herbs/import-template` | `74` 同 | `328` 同 | `AdminOrSuperAdmin` | ✅ |
| `GET /herbs/export` | `180` 同 | `414` 同 | `AdminOrSuperAdmin` | ✅ 新增，对齐患者 `export` 逻辑 |
| `GET /herbs/export-all` | `161` 同 | `432` 同 | `AdminOrSuperAdmin` | ✅ 额外收口（任务书未显式点名但应收口，已做） |
| `POST /formulas/batch-import` `ImportFormulas` | `814` 同 | `764` 同 | `AdminOrSuperAdmin` | ✅ |
| `GET /formulas/import-template` | `532` 同 | `484` 同 | `AdminOrSuperAdmin` | ✅ |
| `GET /formulas/export` | `595` 同 | `546` 同 | `AdminOrSuperAdmin` | ✅ |

**实现方式说明**：任务书写法 ` [Authorize(Roles = "Admin,SuperAdmin")]` 与实际 ` [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]` 等价——后者为本仓库既定策略式授权（`AuthenticationServiceCollectionExtensions.cs:147` 注册 `AdminOrSuperAdmin` → `RequireRole(Admin,SuperAdmin)`），与患者端一致，**非缺陷**。建议任务书措辞后续统一为 `PolicyConstants.AdminOrSuperAdmin`。

### 2.2 有无遗漏

- 全量 grep `CatalogController` 的 `batch-import|import-template|export` 9 个端点均有显式 `AdminOrSuperAdmin`，无遗漏。
- 类级 `DoctorOrAdmin` 保留（只读列表/详情仍 Doctor 可查），方法级收紧符合“最小权限”且与 `04-permissions.md` 权限权威一致。

### 2.3 观察项

- **低**：患者 `import-template`/`export` 仍继承类级 `DoctorOrAdminOrReceptionist`（含 Receptionist），未收紧至 `AdminOrSuperAdmin`。本次任务书未要求改患者，但与“写操作仅 Admin”原则略不一致；建议后续单独评估是否收紧患者导出权限（与脱敏管道配合）。

---

## 3. T5 验方导出筛选参数对齐 + 明细补全

### 3.1 参数名一致性

| 侧 | 文件:行 | 参数名 | 结论 |
|----|---------|--------|------|
| 客户端 Refit | `IFormulaApi.cs:71` `ExportFormulasAsync([Query] string? category)` | `category` |  |
| 客户端 Http 适配 | `FormulasHttpApiClient.cs:52` `?category=` | `category` |  |
| 服务端 Remote | `CatalogController.cs:598` `FormulaExport([FromQuery] string? category)` | `category` | ✅ 对齐 |
| 服务端 Local | `CatalogController.cs:548` 同 | `category` | ✅ 对齐 |

**此前缺陷**：`keyword` vs `category` 错位导致传参永不生效（desktop-deep-code-review F6）；本次已修复。

### 3.2 导出 DTO 明细

- **Before**：`ProducesResponseType(List<FormulaListDto>)` + `GetPagedAsync` → `HerbCount` 仅数量，无明细 → US-FORM-013 “每行含药材组成详情”不满足。
- **After**：`ProducesResponseType(List<FormulaDetailDto>)` + `ExportDetailsAsync` → `CatalogQueryService.cs:52` `Repository.GetPagedAsync(1,10000,keyword,category)` → `Select(_toDetail)` → `ToFormulaDetailDto` 含 `Herbs: List<FormulaHerbItemDto>`（`HerbName/Dosage/Unit/Usage/DecocteMethod/IsValidated`）。满足 US-FORM-013。
- **变更面**：`ICatalogQueryService.cs:10` 新增 `GetPagedAsync(category重载)` + `ExportDetailsAsync`；`CatalogQueryService` 对 `HerbRepository`/`FormulaRepository` 的 `Category` 直通已验证（`FormulaRepository.cs:103` `Category.Contains`，`HerbRepository.cs:39` 同）。
- **兼容性**：客户端 `ExportFormulasAsync` 返 `HttpResponseMessage` 存文件，不强类型反序列化 `List<FormulaListDto>`，故 DTO 形状变更不产生编译期破坏；若有外部消费者按旧 `ListDto` 解析则需感知（当前仓库内无此类消费者）。

### 3.3 观察项

- **中（测试覆盖）**：现有守卫 `ImportExportJsonTests.FormulaTemplateAndExport_ReturnJson_NotFile` 仅断言 `ApiResponse` 前缀与非 `FileResult`，未断言 `Herbs` 明细存在；无 `category` 过滤单测（`category=补益剂` 仅返回该分类）。建议新增：① `FormulaExport_Returns_Herbs_Detail`（assert 首条 `Herbs.Count>0`）；② `FormulaExport_ByCategory_Filters`（种子两分类→按 category 仅返回目标）；③ 权限守卫 `CatalogControllerPermissionTests`（反射断言 7 端点 `AdminOrSuperAdmin`，对标 `PatientsController` 已有）。
- **低（接口重载）**：`ICatalogQueryService` 现有两重载 `GetPagedAsync` 同名不同 arity 易混淆；后续可收敛为单重载 `GetPagedAsync(..., string? category=null, ...)` 并逐步迁移旧调用为命名参数，已有实现通过 `return GetPagedAsync(...,null,...)` 转发，无功能风险。

---

## 4. T7 文档同步

| 检查项 | 文件:行 | 现状 | 结论 |
|--------|---------|------|------|
| 05-herbs US-HERB-007/013 验收标准去 Excel | `05-herbs.md:222` `返回 JSON 数组（application/json，2026-08-13：Excel→JSON）` / `416` 同 | 已改 JSON，无 `Excel`/`xlsx` 残留 | ✅ |
| 05-herbs 虚构接口 `IHerbImportExportService` | 全仓 grep  `IHerbImportExportService` 在 `05-herbs.md` **0 命中**（仅 `docs/03-architecture/modules/herbs.md:144` 归档注释残留，非需求文档） | 需求文档已删 | ✅ |
| 04-patients 虚构接口 `IPatientImportExportService` | `04-patients.md` **0 命中**（仅 `docs/03-architecture/modules/...` / `LocalWebAPI/README` 归档残留） | 需求文档已删 | ✅ |
| 06-formulas US-FORM-013 | `06-formulas.md:463` 状态 `✅ 已实现（...2026-08-19 P2：导出含 Herbs 明细 + category...）`；标题/“作为”句 `导出 Excel→导出 JSON` | 已同步 | ✅ |
| 追溯矩阵 Desktop 列 | `13-traceability-matrix.md:98/104` HERB-007/013 `✅/✅` 保持；`124` FORM-013 `⚠️→✅`（`FORM-013 ... Herbs 明细 + Admin`）+ Header `v1.10→v1.11` + 统计 `FORM 13/1→14/0, 合计141/3→142/2` + 变更记录新增 v1.11 行 | 已同步 | ✅ |
| 13c-current-status | `13c-current-status.md:208` #112 补“双端表述滞后”注记 + `225` 新增 #130 P2 行（4 列表格，含验证 `build 0/0 arch 87/87`） | 已修正 | ✅ |

**结论**：任务书 T7 要求3项全部落地；`06-formulas` 的 `AllowAnonymous` 矛盾已于 P1 清理，本次无需重复。

---

## 5. 通用检查

| 项 | 检查方法 | 结果 |
|----|----------|------|
| 语法错误 | `dotnet build LYBTZYZS.sln --no-incremental` 0 错误0警告（实测 2026-08-19 `e8108804b` 后） | ✅ |
| 架构守卫 | `dotnet test tests/LYBT.Tests.Architecture --no-build` 87/87 | ✅ |
| 导入导出守卫 | `dotnet test --filter ImportExportJsonTests` 6/6；`--filter "ImportExportRouteParityTests|LocalImportExportJsonTests"` 7/7 | ✅ |
| 未提交改动 | `git status --short` 仅 `.pi/hindsight` + `?? .commandcode/` + `?? desktop-doc-code-audit` 未跟踪，与 P2 变更无关；`git diff --cached --stat` 12 文件即 commit 内容 | ✅ 无遗漏提交 |
| 新语法风险 | 新增 `ExportDetailsAsync` 为 `internal` 服务层方法，`CatalogController` 仅通过接口调用，无 `DbContext` 直注，保持 3-Layer | ✅ |

---

## 6. 遗留观察项汇总（不阻断发布，进 backlog）

| # | 级别 | 描述 | 建议 |
|---|------|------|------|
| O-COMM-01 | 低 | 任务书措辞 `Roles` vs 代码 `Policy` 不一致 | 任务书/04-permissions 统一为 `PolicyConstants.AdminOrSuperAdmin` |
| O-PAT-01 | 低 | 患者 `import-template`/`export` 未收紧至 Admin | 单独评估是否与脱敏管道一起收紧 |
| O-TEST-01 | 中 | 缺少 `Herbs` 明细 + `category` 过滤 + 权限反射单测 | 新增 3 单测：`FormulaExport_HerbsDetail` / `FormulaExport_CategoryFilter` / `CatalogControllerPermissionTests` |
| O-API-01 | 低 | `ICatalogQueryService` 双重载易混淆 | 后续收敛为单重载 `category=null` 可选参数 |
| O-DOC-01 | 低 | `13c #130` 状态“待提交” vs 实际已提交 `e8108804b` | 下次批次将 `待提交→已提交`（或留作已提交示例，低优先级） |

---

## 7. 附件

- **Commit**：`e8108804b6f7411a40e6dcdfb364c418a15c5ca6`（12 files +76/-36）
- **验证命令**：
  ```
  dotnet build LYBTZYZS.sln --no-incremental
  dotnet test tests/LYBT.Tests.Architecture --no-build
  dotnet test tests/LYBT.Tests.Server --filter "ImportExportJsonTests" --no-build
  dotnet test tests/LYBT.Tests.Desktop --filter "ImportExportRouteParityTests|LocalImportExportJsonTests" --no-build
  ```

---

*报告生成：review-only，不修改代码；如需修复 O-TEST-01/O-PAT-01 请另建任务书。*
