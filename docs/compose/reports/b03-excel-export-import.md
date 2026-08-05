---
feature: b03-excel-export-import
status: delivered
specs: []
plans:
  - docs/compose/plans/2026-08-05-b03-excel-export-import.md
branch: master
commits: 4d70b487a..74ba2ea50
---

# B-03 Excel 导出/导入/模板下载 — Final Report

## What Was Built

为 LYBTZYZS Server 端实现了 Patients/Herbs/Formulas 三实体的 Excel 导出、模板下载与 Excel 批量导入，共 9 个新端点，并落地通用 `ExcelService`（NPOI XSSFWorkbook，.xlsx 格式）。此前 Server 端只有 JSON 批量导入（Herbs/Formulas），Desktop 的 Refit 定义（`ExportFormulasAsync`/`ExportHerbsAsync`/`ExportPatientsAsync`/`ExportTemplateAsync`）一直无对应实现，本次全部接通。端点路由与 Desktop Refit 定义完全匹配。

## Architecture

**新增文件：**
- `src/Server/Services/LYBT.WebAPI/Services/ExcelService.cs` — 通用工具服务（单例注册），三方法：
  - `ExportToExcel<T>(data, sheetName, columnMapping: Dictionary<string, Func<T, object?>>)` → `byte[]`
  - `GenerateTemplate(sheetName, columnHeaders: Dictionary<string, string>)` → `byte[]`（表头 + 示例行）
  - `ParseExcel<T>(stream, propertySetters: Dictionary<string, Action<T, string>>)` → `List<T>`（第一行表头，跳过空行）
- `src/Server/Modules/LYBT.Module.Patients/Application/Commands/BatchImportPatientsCommand.cs` + `BatchImportPatientsCommandHandler.cs` — 患者批量导入命令，与 `BatchImportHerbsCommandHandler` 同构（Skip/Update/Error 重复策略 + 逐条聚合结果，失败详情用现有 `PatientImportFailureDto`）。

**端点（9 个，权限 = 方法级 `[Authorize(AdminOrSuperAdmin)]` 覆盖类级）：**

| 实体 | 导出 GET | 模板 GET | Excel 导入 POST |
|------|----------|----------|-----------------|
| 患者 | `/api/v1/patients/export?keyword=` | `/api/v1/patients/import-template` | `/api/v1/patients/batch-import-excel` |
| 药材 | `/api/v1/herbs/export?keyword=` | `/api/v1/herbs/import-template` | `/api/v1/herbs/batch-import-excel` |
| 验方 | `/api/v1/formulas/export?category=` | `/api/v1/formulas/import-template` | `/api/v1/formulas/batch-import-excel` |

**数据流：**
- 导出：Controller 私有 helper 分页循环 `GetPagedAsync` 收集 ID → 逐条 `GetByIdAsync` 拿 Detail DTO（List DTO 缺少出生日期/身份证号/性味/功效/组成等导出字段）→ `ExportToExcel` → `File(bytes, ExcelContentType, 文件名.xlsx)`。验方按 `category` 内存过滤。
- 模板：`GenerateTemplate` 直接生成（含示例行，方便用户参考格式）。
- 导入：`IFormFile` 上传 → `ParseExcel` 按列名映射到 Input DTO → **复用现有命令**：Herbs 走 `BatchImportHerbsCommand`（拼音自动生成、重复策略），Formulas 走 `BatchImportFormulasCommand`（药材名/拼音匹配、验方校验），Patients 走新增的 `BatchImportPatientsCommand`。返回 `ImportResultDto` 子类（成功/失败/跳过数 + 失败详情列表）。

**Excel 列定义（以现有 DTO/实体实际字段为准）：**
- 患者（`PatientDetailDto`）：姓名、性别、出生日期、身份证号、电话、拼音码、状态
- 药材（`HerbDetailDto`）：名称、拼音码、分类、性味、功效、用法用量、单位、单价、产地、备注
- 验方（`FormulaDetailDto`）：名称、分类、描述、组成（JSON `[{HerbName,Dosage,Unit}]`）、功效、用法、性味归经、主治、禁忌症、是否共享

### Design Decisions

- **模板路由为 `/import-template` 而非 `/export-template`**：任务描述写 export-template，但 Desktop Refit 定义（硬约束）是 `import-template`，以 Refit 为准。
- **导入复用现有命令而非新建 Excel 专属服务**：避免导入逻辑双路径；患者因无现有命令，新增与药材同构的命令。
- **DateTime 列以文本 `yyyy-MM-dd` 写出**：TDD 测试暴露 NPOI XSSF 的真实坑——给单个 cell 赋自定义 `DataFormat` 会污染 workbook 格式表，序列化重载后其他数字列（如用量 10）被误判为日期（`1900-01-10`）。文本输出彻底规避。
- **任务列中的「地址/过敏史/病史（患者）」「禁忌（药材）」不导出**：模型中不存在这些字段；「描述/分类（验方）」导出有但导入 DTO（`FormulaImportItemDto`）无，导入时忽略。
- **不改 `IHerbService`/`IFormulaService`/`IPatientService` 接口**：前两者已下沉 `LYBT.Desktop.Contracts`，改动会触碰「不改 Desktop」约束；导出通过现有 Service 组合实现。

## Usage

- 导出：`GET /api/v1/patients/export?keyword=张三`（带 JWT）→ 下载 xlsx。非 Admin 导出患者仅含启用状态（与列表一致）。
- 模板：`GET /api/v1/herbs/import-template` → 下载含示例行的模板。
- 导入：`POST /api/v1/formulas/batch-import-excel`，`multipart/form-data`，字段 `file`（xlsx）+ `strategy`（可选 `Skip`/`Update`/`Error`，默认 Skip，验方无此参数）；返回 `{ successCount, failureCount, skippedCount, failures: [{rowNumber, reason, ...}] }`。

## Verification

- `dotnet build LYBTZYZS.sln --no-incremental`：0 错误 0 警告。
- `dotnet test tests/LYBT.Tests.Architecture/`：92/92 通过（Controller 返回 IActionResult/FileResult、类级 [Authorize]、Services 后缀等守卫全绿）。
- `tests/LYBT.Tests.Server/Unit/WebAPI/ExcelServiceTests.cs`：4/4 通过（导出→解析往返、模板表头+示例、空数据导出、跳过空行；全程 TDD，先见失败后见通过）。
- 患者/药材/验方 9 端点在 Controller 内逐项目编译验证通过。

## Journey Log

- [lesson] NPOI XSSF 的 `DataFormat` 是 workbook 级格式表索引，给单 cell 赋自定义格式会污染其他数字列（重载后被 `DateUtil.IsCellDateFormatted` 误判）——TDD 在 3 轮迭代中捕获，最终改为日期文本输出。
- [lesson] NPOI 2.7.2 `ICell.DateCellValue` 返回 `DateTime?`、numeric cell `ToString()` 输出 `"10.0"` 而非 `"10"`——用 `NumberToTextConverter.ToText` 还原最短表示。
- [lesson] 任务描述与 Refit 定义冲突时以 Refit 为硬约束（`import-template`）。

## Source Materials

| File | Role | Notes |
|------|------|-------|
| `docs/compose/plans/2026-08-05-b03-excel-export-import.md` | Implementation plan | 8 任务全部完成 |
| `docs/03-architecture/13b-api-endpoints.md` | API 端点权威表 | 已补充 9 端点 |
| `docs/03-architecture/13-project-master-plan.md` | 项目总账 | B-03 ✅ + §九 决策记录 |
