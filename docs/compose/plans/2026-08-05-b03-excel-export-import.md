# B-03 Excel 导出/导入/模板下载 实现计划

> [!NOTE]
> This document may not reflect the current implementation.
> See the final report for up-to-date state:
> [Final Report](../reports/b03-excel-export-import.md)

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 LYBTZYZS Server 端实现 Patients/Herbs/Formulas 三实体的 Excel 导出、模板下载、Excel 批量导入（9 个新端点），并落地通用 `ExcelService`。

**Architecture:** 在 WebAPI 层（`LYBT.WebAPI`）新增 NPOI 驱动的通用 `ExcelService`（导出/模板/解析三方法）。三个 Controller 各新增 3 个端点：导出（GET，复用现有 Service 分页+详情）、模板（GET，纯生成）、Excel 导入（POST multipart，解析后复用现有 `BatchImportHerbsCommand`/`BatchImportFormulasCommand`；患者模块新增 `BatchImportPatientsCommand`，与药材命令同构）。路由严格匹配 Desktop Refit 定义。

**Tech Stack:** .NET 8 / ASP.NET Core / NPOI 2.7.2 (XSSFWorkbook, .xlsx) / MediatR / EF Core

## Global Constraints

- 端点路由必须与 Desktop Refit 定义**完全匹配**：`/export`、`/import-template`（**注意**：Refit 定义的是 `import-template` 而非 `export-template`，以 Refit 为准）、`/batch-import-excel`（新增，Refit 无此端点，按任务命名）。
- 权限：patients export/template = 类级 `DoctorOrAdminOrReceptionist`（已有，不加）；herbs/formulas export/template = 类级 `DoctorOrAdmin`（已有，不加）；`batch-import-excel` = 方法级 `AdminOrSuperAdmin`。
- **不改动 Desktop 代码**；**不改动现有 JSON `batch-import` 端点**（新增 Excel 端点，不替换）。
- 不改动 `IHerbService`/`IFormulaService`/`IPatientService` 接口（`IFormulaService`/`IHerbService` 已下沉到 `LYBT.Desktop.Contracts`，改动会触碰 Desktop 约束）。
- Build 必须 0 错误 0 警告：`dotnet build LYBTZYZS.sln --no-incremental`。
- 架构测试必须通过：`dotnet test tests/LYBT.Tests.Architecture/`（约束：Controller 返回 IActionResult / 继承 BaseApiController / 类级 [Authorize] / Services 命名空间类名以 Service 结尾）。
- Excel 列以**现有 DTO/实体实际字段**为准（任务列的「地址/过敏史/病史（患者）」「禁忌（药材）」在现有模型中不存在，不导出/导入）。
- `ExcelService` 泛型方法签名中可空值用 `Func<T, object?>`（`Func<T, object>` 返回 null 会触发 CS8603 警告，破坏 0 警告基线）。
- NPOI 通过中央包管理（`Directory.Packages.props`）引入，csproj 中不写版本号。
- 完成后 `git add` + `git commit`（不 push），中文 commit message；同步更新 `13b-api-endpoints.md` 与 `13-project-master-plan.md`。

---

### Task 1: NPOI 包安装（中央管理 + WebAPI 引用）

**Covers:** 目标 1

**Files:**
- Modify: `Directory.Packages.props`（"Office and File Processing" 分组加 NPOI 版本钉）
- Modify: `src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj`（加 PackageReference）

- [ ] **Step 1: 在 `Directory.Packages.props` 的 `<ItemGroup Label="Office and File Processing">` 中加入 NPOI 版本钉**

```xml
<ItemGroup Label="Office and File Processing">
    <!-- D1: PDF 处方导出 -->
    <PackageVersion Include="QuestPDF" Version="2025.12.4" />
    <!-- B-03: Excel 导出/导入 (NPOI, .xlsx) -->
    <PackageVersion Include="NPOI" Version="2.7.2" />
    ...
</ItemGroup>
```

- [ ] **Step 2: 在 `LYBT.WebAPI.csproj` 的包引用 ItemGroup 中加入 NPOI（中央管理，不写版本）**

```xml
<ItemGroup>
    <!-- B-03: Excel 导出/导入 -->
    <PackageReference Include="NPOI" />
    ...
</ItemGroup>
```

- [ ] **Step 3: 验证还原与编译**

Run: `dotnet build src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj`
Expected: Build succeeded，0 错误

- [ ] **Step 4: Commit**

```bash
git add Directory.Packages.props src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj
git commit -m "build: 引入 NPOI 包（B-03 Excel 导出/导入）"
```

---

### Task 2: ExcelService 通用工具 + 单元测试（TDD）

**Covers:** 目标 2

**Files:**
- Create: `src/Server/Services/LYBT.WebAPI/Services/ExcelService.cs`
- Test: `tests/LYBT.Tests.Server/Unit/WebAPI/ExcelServiceTests.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Program.cs`（DI 注册 `services.AddSingleton<ExcelService>()`）

**Interfaces:**
- Produces: `public class ExcelService`（`namespace LYBT.WebAPI.Services`）
  - `public const string ExcelContentType`（值 `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`）
  - `public byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName, Dictionary<string, Func<T, object?>> columnMapping)`
  - `public byte[] GenerateTemplate(string sheetName, Dictionary<string, string> columnHeaders)`（第一行表头=key，第二行示例=value）
  - `public List<T> ParseExcel<T>(Stream excelStream, Dictionary<string, Action<T, string>> propertySetters) where T : new()`

- [ ] **Step 1: 写失败的单元测试**

```csharp
using LYBT.WebAPI.Services;

namespace LYBT.Tests.Server.Unit.WebAPI;

/// <summary>
/// ExcelService 通用工具单元测试（无数据库依赖）。
/// </summary>
public class ExcelServiceTests
{
    private readonly ExcelService _service = new();

    private sealed record SampleItem(string Name, int Amount, DateTime? Date);

    [Fact]
    public void ExportToExcel_Should_Produce_Parseable_Workbook()
    {
        var data = new[]
        {
            new SampleItem("当归", 10, new DateTime(2026, 1, 1)),
            new SampleItem("甘草", 3, null)
        };

        var bytes = _service.ExportToExcel(data, "药材", new Dictionary<string, Func<SampleItem, object?>>
        {
            ["名称"] = i => i.Name,
            ["用量"] = i => i.Amount,
            ["日期"] = i => i.Date
        });

        Assert.True(bytes.Length > 0);

        var parsed = _service.ParseExcel(new MemoryStream(bytes), new Dictionary<string, Action<SampleItem, string>>
        {
            ["名称"] = (i, v) => i = i with { Name = v },
            ["用量"] = (i, v) => i = i with { Amount = int.Parse(v) },
            ["日期"] = (i, v) => i = i with { Date = string.IsNullOrWhiteSpace(v) ? null : DateTime.Parse(v) }
        });

        // 注意：Action<T,string> 无法替换 record 值，改用可写类断言
    }
}
```

> ⚠️ **测试修正（执行时直接用此版）**：record 不可变无法被 `Action<T,string>` 写入，测试用可写普通类：

```csharp
using LYBT.WebAPI.Services;

namespace LYBT.Tests.Server.Unit.WebAPI;

/// <summary>
/// ExcelService 通用工具单元测试（无数据库依赖）。
/// </summary>
public class ExcelServiceTests
{
    private readonly ExcelService _service = new();

    private sealed class SampleRow
    {
        public string Name { get; set; } = string.Empty;
        public int Amount { get; set; }
        public DateTime? Date { get; set; }
    }

    [Fact]
    public void Export_Then_Parse_Should_RoundTrip_Data()
    {
        var data = new[]
        {
            new SampleRow { Name = "当归", Amount = 10, Date = new DateTime(2026, 1, 1) },
            new SampleRow { Name = "甘草", Amount = 3, Date = null }
        };

        var bytes = _service.ExportToExcel(data, "药材", new Dictionary<string, Func<SampleRow, object?>>
        {
            ["名称"] = i => i.Name,
            ["用量"] = i => i.Amount,
            ["日期"] = i => i.Date
        });

        Assert.True(bytes.Length > 0);

        var parsed = _service.ParseExcel(new MemoryStream(bytes), new Dictionary<string, Action<SampleRow, string>>
        {
            ["名称"] = (i, v) => i.Name = v,
            ["用量"] = (i, v) => i.Amount = int.Parse(v),
            ["日期"] = (i, v) => i.Date = string.IsNullOrWhiteSpace(v) ? null : DateTime.Parse(v)
        });

        Assert.Equal(2, parsed.Count);
        Assert.Equal("当归", parsed[0].Name);
        Assert.Equal(10, parsed[0].Amount);
        Assert.Equal(new DateTime(2026, 1, 1), parsed[0].Date);
        Assert.Equal("甘草", parsed[1].Name);
        Assert.Equal(3, parsed[1].Amount);
        Assert.Null(parsed[1].Date);
    }

    [Fact]
    public void GenerateTemplate_Should_Produce_Header_And_Sample_Row()
    {
        var bytes = _service.GenerateTemplate("患者", new Dictionary<string, string>
        {
            ["姓名"] = "张三",
            ["性别"] = "男"
        });

        var parsed = _service.ParseExcel(new MemoryStream(bytes), new Dictionary<string, Action<SampleRow, string>>
        {
            ["姓名"] = (i, v) => i.Name = v
        });

        Assert.Single(parsed);
        Assert.Equal("张三", parsed[0].Name);
    }

    [Fact]
    public void Export_Empty_Data_Should_Produce_Header_Only()
    {
        var bytes = _service.ExportToExcel(Array.Empty<SampleRow>(), "空表", new Dictionary<string, Func<SampleRow, object?>>
        {
            ["名称"] = i => i.Name
        });

        var parsed = _service.ParseExcel(new MemoryStream(bytes), new Dictionary<string, Action<SampleRow, string>>
        {
            ["名称"] = (i, v) => i.Name = v
        });

        Assert.Empty(parsed);
    }

    [Fact]
    public void ParseExcel_Should_Skip_Empty_Rows()
    {
        var workbook = new NPOI.XSSF.UserModel.XSSFWorkbook();
        var sheet = workbook.CreateSheet("测试");
        var header = sheet.CreateRow(0);
        header.CreateCell(0).SetCellValue("名称");
        header.CreateCell(1).SetCellValue("用量");
        sheet.CreateRow(1).CreateCell(0).SetCellValue("当归");
        sheet.CreateRow(2); // 空行
        sheet.CreateRow(3).CreateCell(0).SetCellValue("甘草");
        using var ms = new MemoryStream();
        workbook.Write(ms);

        var parsed = _service.ParseExcel(new MemoryStream(ms.ToArray()), new Dictionary<string, Action<SampleRow, string>>
        {
            ["名称"] = (i, v) => i.Name = v,
            ["用量"] = (i, v) => i.Amount = string.IsNullOrWhiteSpace(v) ? 0 : int.Parse(v)
        });

        Assert.Equal(2, parsed.Count);
        Assert.Equal("当归", parsed[0].Name);
        Assert.Equal("甘草", parsed[1].Name);
    }
}
```

> 注意：测试项目 `tests/LYBT.Tests.Server/LYBT.Tests.Server.csproj` 必须已引用 `LYBT.WebAPI` 项目（集成测试宿主），执行前用 grep 确认；若未引用则添加 ProjectReference。

- [ ] **Step 2: 运行测试验证失败**

Run: `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~ExcelServiceTests"`
Expected: 编译失败（ExcelService 不存在）或 0 通过

- [ ] **Step 3: 实现 ExcelService**

`src/Server/Services/LYBT.WebAPI/Services/ExcelService.cs` 完整内容：

```csharp
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace LYBT.WebAPI.Services;

/// <summary>
/// Excel 通用工具服务（NPOI XSSFWorkbook，.xlsx 格式）— 提供数据导出、模板生成与文件解析。
/// </summary>
public class ExcelService
{
    /// <summary>xlsx MIME 类型</summary>
    public const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>
    /// 将数据导出为 Excel 文件字节流。columnMapping：表头 → 取值委托。
    /// </summary>
    public byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName, Dictionary<string, Func<T, object?>> columnMapping)
    {
        var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet(sheetName);
        var headerStyle = CreateHeaderStyle(workbook);

        var headers = columnMapping.Keys.ToList();
        var headerRow = sheet.CreateRow(0);
        for (var i = 0; i < headers.Count; i++)
        {
            var cell = headerRow.CreateCell(i);
            cell.SetCellValue(headers[i]);
            cell.CellStyle = headerStyle;
        }

        var rowIndex = 1;
        foreach (var item in data)
        {
            var row = sheet.CreateRow(rowIndex++);
            var colIndex = 0;
            foreach (var selector in columnMapping.Values)
                SetCellValue(row.CreateCell(colIndex++), selector(item));
        }

        AutoSizeColumns(sheet, headers.Count);

        using var ms = new MemoryStream();
        workbook.Write(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// 生成导入模板：第一行为表头，第二行为示例数据。columnHeaders：表头 → 示例值。
    /// </summary>
    public byte[] GenerateTemplate(string sheetName, Dictionary<string, string> columnHeaders)
    {
        var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet(sheetName);
        var headerStyle = CreateHeaderStyle(workbook);

        var headers = columnHeaders.Keys.ToList();
        var headerRow = sheet.CreateRow(0);
        for (var i = 0; i < headers.Count; i++)
        {
            var cell = headerRow.CreateCell(i);
            cell.SetCellValue(headers[i]);
            cell.CellStyle = headerStyle;
        }

        var sampleRow = sheet.CreateRow(1);
        for (var i = 0; i < headers.Count; i++)
            sampleRow.CreateCell(i).SetCellValue(columnHeaders[headers[i]] ?? string.Empty);

        AutoSizeColumns(sheet, headers.Count);

        using var ms = new MemoryStream();
        workbook.Write(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// 解析 Excel 文件：第一行为表头（匹配 propertySetters 的 key），第二行起为数据，跳过空行。
    /// </summary>
    public List<T> ParseExcel<T>(Stream excelStream, Dictionary<string, Action<T, string>> propertySetters) where T : new()
    {
        var result = new List<T>();
        using var workbook = new XSSFWorkbook(excelStream);
        var sheet = workbook.GetSheetAt(0);
        if (sheet == null) return result;

        var headerRow = sheet.GetRow(sheet.FirstRowNum);
        if (headerRow == null) return result;

        var columnIndexByHeader = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var col = headerRow.FirstCellNum; col < headerRow.LastCellNum; col++)
        {
            var header = headerRow.GetCell(col)?.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(header))
                columnIndexByHeader[header] = col;
        }

        for (var row = sheet.FirstRowNum + 1; row <= sheet.LastRowNum; row++)
        {
            var dataRow = sheet.GetRow(row);
            if (dataRow == null || IsRowEmpty(dataRow)) continue;

            var item = new T();
            foreach (var (header, setter) in propertySetters)
            {
                if (!columnIndexByHeader.TryGetValue(header, out var col)) continue;
                setter(item, GetCellString(dataRow.GetCell(col)));
            }
            result.Add(item);
        }

        return result;
    }

    private static CellStyle CreateHeaderStyle(XSSFWorkbook workbook)
    {
        var style = workbook.CreateCellStyle();
        var font = workbook.CreateFont();
        font.IsBold = true;
        style.SetFont(font);
        style.FillForegroundColor = IndexedColors.Grey25Percent.Index;
        style.FillPattern = FillPattern.SolidForeground;
        return style;
    }

    private static void SetCellValue(ICell cell, object? value)
    {
        switch (value)
        {
            case null:
                break;
            case DateTime date:
                cell.SetCellValue(date);
                cell.CellStyle.DataFormat = cell.Sheet.Workbook.CreateDataFormat().GetFormat("yyyy-MM-dd");
                break;
            case bool b:
                cell.SetCellValue(b);
                break;
            case decimal d:
                cell.SetCellValue((double)d);
                break;
            case double dbl:
                cell.SetCellValue(dbl);
                break;
            case int i:
                cell.SetCellValue(i);
                break;
            case long l:
                cell.SetCellValue(l);
                break;
            case float f:
                cell.SetCellValue(f);
                break;
            case Enum e:
                cell.SetCellValue(e.ToString());
                break;
            default:
                cell.SetCellValue(value.ToString() ?? string.Empty);
                break;
        }
    }

    private static string GetCellString(ICell? cell)
    {
        if (cell == null) return string.Empty;
        if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
            return cell.DateCellValue.ToString("yyyy-MM-dd");
        return cell.ToString()?.Trim() ?? string.Empty;
    }

    private static bool IsRowEmpty(IRow row)
    {
        for (var col = row.FirstCellNum; col < row.LastCellNum; col++)
        {
            if (row.GetCell(col) != null && !string.IsNullOrWhiteSpace(GetCellString(row.GetCell(col))))
                return false;
        }
        return true;
    }

    private static void AutoSizeColumns(ISheet sheet, int columnCount)
    {
        for (var i = 0; i < columnCount; i++)
            sheet.AutoSizeColumn(i);
    }
}
```

- [ ] **Step 4: Program.cs 注册 DI**

在 `src/Server/Services/LYBT.WebAPI/Program.cs` 的服务注册区（约 line 159-163 附近）加入：

```csharp
builder.Services.AddSingleton<ExcelService>();
```

并确认 `using LYBT.WebAPI.Services;`（Program.cs 同项目，GlobalUsings 或显式 using 视现有文件而定）。

- [ ] **Step 5: 运行测试验证通过**

Run: `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~ExcelServiceTests"`
Expected: 4/4 通过（若测试项目缺 WebAPI 引用，先补 ProjectReference 再跑）

- [ ] **Step 6: Commit**

```bash
git add src/Server/Services/LYBT.WebAPI/Services/ExcelService.cs src/Server/Services/LYBT.WebAPI/Program.cs tests/LYBT.Tests.Server/Unit/WebAPI/ExcelServiceTests.cs
git commit -m "feat: 新增 ExcelService 通用导出/模板/解析工具（B-03）"
```

---

### Task 3: 患者批量导入命令（BatchImportPatientsCommand + Handler）

**Covers:** 目标 5（患者导入，复用创建验证：姓名重复检查）

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Patients/Application/Commands/BatchImportPatientsCommand.cs`
- Create: `src/Server/Modules/LYBT.Module.Patients/Application/Commands/BatchImportPatientsCommandHandler.cs`

**Interfaces:**
- Produces: `public record BatchImportPatientsCommand(List<PatientInputDto> Patients, DuplicateStrategy Strategy, Guid CurrentUserId) : IRequest<Result<PatientBatchImportResultDto>>`（namespace `LYBT.Module.Patients.Application.Commands`）
- Consumes: `IPatientRepository`（`ExistsByNameAsync` / `GetPagedAsync(page,pageSize,keyword,status,ct)` / `UpdateAsync` / `AddAsync`）、`PatientMapper.ToEntity(dto, createdBy)`、`PatientImportFailureDto`（字段 `OriginalRowNumber`/`FailureReason`/`FieldName`/`OriginalValue`/`SuggestedFix`/`DataSnapshot`）、`ErrorCode.ValidationFailed`

- [ ] **Step 1: 创建命令 record**

```csharp
using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 批量导入患者命令（Excel/JSON 共用导入路径）。
/// </summary>
public record BatchImportPatientsCommand(
    List<PatientInputDto> Patients,
    DuplicateStrategy Strategy,
    Guid CurrentUserId
) : IRequest<Result<PatientBatchImportResultDto>>;
```

- [ ] **Step 2: 创建 Handler（与 BatchImportHerbsCommandHandler 同构：Skip/Update/Error 重复策略 + 逐条聚合结果）**

```csharp
using MediatR;
using LYBT.Module.Patients.Application.Mappers;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 批量导入患者命令处理器 — 与药材批量导入同构（Skip/Update/Error 重复策略）。
/// </summary>
public class BatchImportPatientsCommandHandler : IRequestHandler<BatchImportPatientsCommand, Result<PatientBatchImportResultDto>>
{
    private readonly IPatientRepository _patientRepository;

    public BatchImportPatientsCommandHandler(IPatientRepository patientRepository)
    {
        _patientRepository = patientRepository;
    }

    public async Task<Result<PatientBatchImportResultDto>> Handle(
        BatchImportPatientsCommand request, CancellationToken cancellationToken)
    {
        const int MAX_IMPORT_SIZE = 10000;

        var result = new PatientBatchImportResultDto
        {
            ImportTime = DateTime.UtcNow
        };

        if (request.Patients.Count > MAX_IMPORT_SIZE)
        {
            return Result<PatientBatchImportResultDto>.Failure(ErrorCode.ValidationFailed, $"批量导入最多支持{MAX_IMPORT_SIZE}条记录");
        }

        for (var i = 0; i < request.Patients.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dto = request.Patients[i];
            var rowNumber = i + 2;

            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    result.FailureCount++;
                    result.Failures.Add(new PatientImportFailureDto
                    {
                        OriginalRowNumber = rowNumber,
                        FailureReason = "患者姓名不能为空",
                        FieldName = "Name",
                        OriginalValue = dto.Name,
                        SuggestedFix = "填写患者姓名",
                        DataSnapshot = dto
                    });
                    continue;
                }

                var exists = await _patientRepository.ExistsByNameAsync(dto.Name, ct: cancellationToken);

                if (exists)
                {
                    switch (request.Strategy)
                    {
                        case DuplicateStrategy.Skip:
                            result.SkippedCount++;
                            continue;

                        case DuplicateStrategy.Update:
                            var existingPaged = await _patientRepository.GetPagedAsync(1, 1, dto.Name, null, cancellationToken);
                            var existing = existingPaged.Items.FirstOrDefault();
                            if (existing != null)
                            {
                                existing.UpdateProfile(
                                    dto.Name, dto.Gender, dto.BirthDate,
                                    dto.PhoneNumber, dto.IdNumber, dto.PinYinCode,
                                    request.CurrentUserId);
                                await _patientRepository.UpdateAsync(existing, cancellationToken);
                                result.SuccessCount++;
                            }
                            continue;

                        case DuplicateStrategy.Error:
                            result.FailureCount++;
                            result.Failures.Add(new PatientImportFailureDto
                            {
                                OriginalRowNumber = rowNumber,
                                FailureReason = "患者姓名重复",
                                FieldName = "Name",
                                OriginalValue = dto.Name,
                                SuggestedFix = "修改姓名或调整导入策略",
                                DataSnapshot = dto
                            });
                            continue;
                    }
                }

                var patient = PatientMapper.ToEntity(dto, request.CurrentUserId);
                await _patientRepository.AddAsync(patient, cancellationToken);
                result.SuccessCount++;
            }
            catch
            {
                result.FailureCount++;
                result.Failures.Add(new PatientImportFailureDto
                {
                    OriginalRowNumber = rowNumber,
                    FailureReason = "导入失败",
                    FieldName = "Name",
                    OriginalValue = dto.Name,
                    SuggestedFix = "数据处理异常，请检查数据格式",
                    DataSnapshot = dto
                });
            }
        }

        return Result<PatientBatchImportResultDto>.Success(result);
    }
}
```

- [ ] **Step 3: 验证编译**

Run: `dotnet build src/Server/Modules/LYBT.Module.Patients/LYBT.Module.Patients.csproj`
Expected: Build succeeded，0 错误 0 警告

- [ ] **Step 4: Commit**

```bash
git add src/Server/Modules/LYBT.Module.Patients/Application/Commands/BatchImportPatientsCommand.cs src/Server/Modules/LYBT.Module.Patients/Application/Commands/BatchImportPatientsCommandHandler.cs
git commit -m "feat: 患者模块新增 BatchImportPatientsCommand（B-03 Excel 导入复用）"
```

---

### Task 4: PatientsController — export / import-template / batch-import-excel

**Covers:** 目标 3（Patients）、目标 4、目标 5

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs`

**Interfaces:**
- Consumes: `ExcelService`（注入）、`IPatientService.GetPagedAsync`/`GetByIdAsync`、`Sender.Send(new BatchImportPatientsCommand(...))`、`RoleConstants`（`LYBT.Infrastructure.Constants` 已 using）、`BaseApiController` 的 `ValidationFail`/`BusinessFail`/`Success`/`GetOperator`/`LogOperation`
- Produces: 3 个端点 + 私有 `GetAllDetailsAsync(string? keyword, CancellationToken)`

- [ ] **Step 1: 构造函数注入 ExcelService + using**

```csharp
using LYBT.WebAPI.Services;
```

构造函数改为：

```csharp
private readonly IPatientService _patientService;
private readonly ExcelService _excelService;

public PatientsController(ISender sender, ILogger<PatientsController> logger, IPatientService patientService, ExcelService excelService)
    : base(sender, logger)
{
    _patientService = patientService;
    _excelService = excelService;
}
```

- [ ] **Step 2: 新增 3 个端点 + 私有拉取方法（追加到类尾，`CheckOwnershipAsync` 之后）**

```csharp
/// <summary>
/// 导出患者数据到 Excel（keyword 可选；非 Admin 仅导出启用状态）
/// </summary>
[HttpGet("export")]
[ProducesResponseType(typeof(FileResult), 200)]
public async Task<IActionResult> Export([FromQuery] string? keyword, CancellationToken ct)
{
    var patients = await GetAllDetailsAsync(keyword, ct);
    if (patients == null) return BusinessFail("导出失败");

    var bytes = _excelService.ExportToExcel(patients, "患者", new Dictionary<string, Func<PatientDetailDto, object?>>
    {
        ["姓名"] = p => p.Name,
        ["性别"] = p => p.Gender,
        ["出生日期"] = p => p.BirthDate,
        ["身份证号"] = p => p.IdNumber,
        ["电话"] = p => p.PhoneNumber,
        ["拼音码"] = p => p.PinYinCode,
        ["状态"] = p => p.Status
    });

    return File(bytes, ExcelService.ExcelContentType, $"患者导出_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
}

/// <summary>
/// 下载患者导入模板（表头 + 示例行）
/// </summary>
[HttpGet("import-template")]
[ProducesResponseType(typeof(FileResult), 200)]
public IActionResult ImportTemplate()
{
    var bytes = _excelService.GenerateTemplate("患者", new Dictionary<string, string>
    {
        ["姓名"] = "张三",
        ["性别"] = "男",
        ["出生日期"] = "1990-01-01",
        ["身份证号"] = "110101199001010011",
        ["电话"] = "13800138000",
        ["拼音码"] = "zhangsan"
    });

    return File(bytes, ExcelService.ExcelContentType, "患者导入模板.xlsx");
}

/// <summary>
/// 从 Excel 文件批量导入患者（仅 Admin+）
/// </summary>
[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
[HttpPost("batch-import-excel")]
[Consumes("multipart/form-data")]
[EnableRateLimiting("ApiCalls")]
[ProducesResponseType(typeof(ApiResponse<PatientBatchImportResultDto>), 200)]
public async Task<IActionResult> BatchImportExcel(
    IFormFile file,
    [FromForm] DuplicateStrategy strategy = DuplicateStrategy.Skip,
    CancellationToken ct = default)
{
    if (file == null || file.Length == 0)
        return ValidationFail("请上传 Excel 文件");

    List<PatientInputDto> patients;
    try
    {
        using var stream = file.OpenReadStream();
        patients = _excelService.ParseExcel(stream, new Dictionary<string, Action<PatientInputDto, string>>
        {
            ["姓名"] = (d, v) => d.Name = v,
            ["性别"] = (d, v) => d.Gender = ParseGender(v),
            ["出生日期"] = (d, v) => d.BirthDate = DateTime.TryParse(v, out var date) ? date : null,
            ["身份证号"] = (d, v) => d.IdNumber = v,
            ["电话"] = (d, v) => d.PhoneNumber = v,
            ["拼音码"] = (d, v) => d.PinYinCode = v
        });
    }
    catch
    {
        return ValidationFail("Excel 文件解析失败，请使用系统模板");
    }

    if (patients.Count == 0)
        return ValidationFail("导入列表不能为空");

    var (operatorId, _, _) = GetOperator();
    var result = await Sender.Send(new BatchImportPatientsCommand(patients, strategy, operatorId), ct);
    if (!result.IsSuccess || result.Value == null)
        return BusinessFail(result.Error ?? "导入失败");

    LogOperation("批量导入患者(Excel)", new { Count = patients.Count, Strategy = strategy }, null);
    return Success(result.Value, $"成功导入 {result.Value.SuccessCount} 条患者");
}

/// <summary>
/// 全量拉取患者详情（分页循环 + 逐条详情），非 Admin 仅取启用状态。失败返回 null。
/// </summary>
private async Task<List<PatientDetailDto>?> GetAllDetailsAsync(string? keyword, CancellationToken ct)
{
    var isAdmin = User?.IsInRole(RoleConstants.Admin) == true || User?.IsInRole(RoleConstants.SuperAdmin) == true;
    var details = new List<PatientDetailDto>();
    const int pageSize = 100;
    var page = 1;

    while (true)
    {
        var paged = await _patientService.GetPagedAsync(page, pageSize, keyword, filterDisabled: !isAdmin, ct);
        if (!paged.IsSuccess || paged.Value == null) return null;
        if (paged.Value.Items.Count == 0) break;

        foreach (var item in paged.Value.Items)
        {
            var detail = await _patientService.GetByIdAsync(item.Id, ct);
            if (detail.IsSuccess && detail.Value != null) details.Add(detail.Value);
        }

        if (details.Count >= paged.Value.TotalCount || paged.Value.Items.Count < pageSize) break;
        page++;
    }

    return details;
}

/// <summary>
/// 解析性别列（男/女/未知 或枚举名）
/// </summary>
private static Gender ParseGender(string value)
{
    return value switch
    {
        "男" or "Male" => Gender.Male,
        "女" or "Female" => Gender.Female,
        _ => Gender.Unknown
    };
}
```

> 注意：`Gender`、`DuplicateStrategy` 已有 `using LYBT.Shared.Models.Enums;`；`PatientInputDto`/`PatientDetailDto`/`PatientBatchImportResultDto` 已有 `using LYBT.Shared.Models.Contracts.Patients;`；`ApiResponse` 已有 `using LYBT.Shared.Models.Contracts.Common;`。

- [ ] **Step 3: 验证编译**

Run: `dotnet build src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj`
Expected: 0 错误 0 警告

- [ ] **Step 4: Commit**

```bash
git add src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs
git commit -m "feat: 患者 Excel 导出/模板/批量导入端点（B-03）"
```

---

### Task 5: HerbsController — export / import-template / batch-import-excel

**Covers:** 目标 3（Herbs）、目标 4、目标 5

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs`

**Interfaces:**
- Consumes: `ExcelService`、`IHerbService.GetPagedAsync`/`GetByIdAsync`、`Sender.Send(new BatchImportHerbsCommand(herbs, strategy, operatorId))`（复用现有命令，包含拼音生成与重复策略验证）
- Produces: 3 个端点 + 私有 `GetAllDetailsAsync(string? keyword, CancellationToken)`

- [ ] **Step 1: 构造函数注入 ExcelService**

```csharp
private readonly IHerbService _herbService;
private readonly ExcelService _excelService;

public HerbsController(ISender sender, ILogger<HerbsController> logger, IHerbService herbService, ExcelService excelService)
    : base(sender, logger)
{
    _herbService = herbService;
    _excelService = excelService;
}
```

- [ ] **Step 2: 新增 3 个端点 + 私有拉取方法（类尾追加）**

```csharp
/// <summary>
/// 导出药材数据到 Excel（keyword 可选）
/// </summary>
[HttpGet("export")]
[ProducesResponseType(typeof(FileResult), 200)]
public async Task<IActionResult> Export([FromQuery] string? keyword, CancellationToken ct)
{
    var herbs = await GetAllDetailsAsync(keyword, ct);
    if (herbs == null) return BusinessFail("导出失败");

    var bytes = _excelService.ExportToExcel(herbs, "药材", new Dictionary<string, Func<HerbDetailDto, object?>>
    {
        ["名称"] = h => h.Name,
        ["拼音码"] = h => h.PinYinCode,
        ["分类"] = h => h.Category,
        ["性味"] = h => h.Properties,
        ["功效"] = h => h.Effect,
        ["用法用量"] = h => h.Usage,
        ["单位"] = h => h.Unit,
        ["单价"] = h => h.Price,
        ["产地"] = h => h.Origin,
        ["备注"] = h => h.Remark
    });

    return File(bytes, ExcelService.ExcelContentType, $"药材导出_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
}

/// <summary>
/// 下载药材导入模板（表头 + 示例行）
/// </summary>
[HttpGet("import-template")]
[ProducesResponseType(typeof(FileResult), 200)]
public IActionResult ImportTemplate()
{
    var bytes = _excelService.GenerateTemplate("药材", new Dictionary<string, string>
    {
        ["名称"] = "当归",
        ["拼音码"] = "danggui",
        ["分类"] = "补血药",
        ["性味"] = "甘、辛、温",
        ["功效"] = "补血活血，调经止痛",
        ["用法用量"] = "6-12g，煎服",
        ["单位"] = "克",
        ["单价"] = "0.05",
        ["产地"] = "甘肃",
        ["备注"] = ""
    });

    return File(bytes, ExcelService.ExcelContentType, "药材导入模板.xlsx");
}

/// <summary>
/// 从 Excel 文件批量导入药材（仅 Admin+），复用 BatchImportHerbsCommand（拼音自动生成 + 重复策略）
/// </summary>
[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
[HttpPost("batch-import-excel")]
[Consumes("multipart/form-data")]
[EnableRateLimiting("ApiCalls")]
[ProducesResponseType(typeof(ApiResponse<HerbBatchImportResultDto>), 200)]
public async Task<IActionResult> BatchImportExcel(
    IFormFile file,
    [FromForm] DuplicateStrategy strategy = DuplicateStrategy.Skip,
    CancellationToken ct = default)
{
    if (file == null || file.Length == 0)
        return ValidationFail("请上传 Excel 文件");

    List<HerbInputDto> herbs;
    try
    {
        using var stream = file.OpenReadStream();
        herbs = _excelService.ParseExcel(stream, new Dictionary<string, Action<HerbInputDto, string>>
        {
            ["名称"] = (d, v) => d.Name = v,
            ["拼音码"] = (d, v) => d.PinYinCode = v,
            ["分类"] = (d, v) => d.Category = v,
            ["性味"] = (d, v) => d.Properties = v,
            ["功效"] = (d, v) => d.Effect = v,
            ["用法用量"] = (d, v) => d.Usage = v,
            ["单位"] = (d, v) => d.Unit = string.IsNullOrWhiteSpace(v) ? "克" : v,
            ["单价"] = (d, v) => d.Price = decimal.TryParse(v, out var p) ? p : 0,
            ["产地"] = (d, v) => d.Origin = v,
            ["备注"] = (d, v) => d.Remark = v
        });
    }
    catch
    {
        return ValidationFail("Excel 文件解析失败，请使用系统模板");
    }

    if (herbs.Count == 0)
        return ValidationFail("导入列表不能为空");

    var (operatorId, _, _) = GetOperator();
    var result = await Sender.Send(new BatchImportHerbsCommand(herbs, strategy, operatorId), ct);
    if (!result.IsSuccess || result.Value == null)
        return BusinessFail(result.Error ?? "导入失败");

    LogOperation("批量导入药材(Excel)", new { Count = herbs.Count, Strategy = strategy }, null);
    return Success(result.Value, $"成功导入 {result.Value.SuccessCount} 条药材");
}

/// <summary>
/// 全量拉取药材详情（分页循环 + 逐条详情）。失败返回 null。
/// </summary>
private async Task<List<HerbDetailDto>?> GetAllDetailsAsync(string? keyword, CancellationToken ct)
{
    var details = new List<HerbDetailDto>();
    const int pageSize = 100;
    var page = 1;

    while (true)
    {
        var paged = await _herbService.GetPagedAsync(page, pageSize, keyword, ct);
        if (!paged.IsSuccess || paged.Value == null) return null;
        if (paged.Value.Items.Count == 0) break;

        foreach (var item in paged.Value.Items)
        {
            var detail = await _herbService.GetByIdAsync(item.Id, ct);
            if (detail.IsSuccess && detail.Value != null) details.Add(detail.Value);
        }

        if (details.Count >= paged.Value.TotalCount || paged.Value.Items.Count < pageSize) break;
        page++;
    }

    return details;
}
```

- [ ] **Step 3: 验证编译**

Run: `dotnet build src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj`
Expected: 0 错误 0 警告

- [ ] **Step 4: Commit**

```bash
git add src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs
git commit -m "feat: 药材 Excel 导出/模板/批量导入端点（B-03）"
```

---

### Task 6: FormulasController — export / import-template / batch-import-excel

**Covers:** 目标 3（Formulas）、目标 4、目标 5

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs`

**Interfaces:**
- Consumes: `ExcelService`、`IFormulaService.GetPagedAsync`/`GetByIdAsync`、`Sender.Send(new BatchImportFormulasCommand(formulas, fileName))`（复用现有命令：药材名/拼音匹配 + 验方校验）、`System.Text.Json`
- Produces: 3 个端点 + 私有 `GetAllDetailsAsync(string? category, CancellationToken)`
- 组成列格式：JSON（`[{HerbName,Dosage,Unit,...}]`），导入时反序列化为 `List<FormulaHerbImportItemDto>`

- [ ] **Step 1: 构造函数注入 ExcelService + using**

```csharp
using System.Text.Json;
using LYBT.WebAPI.Services;
```

构造函数：

```csharp
private readonly IFormulaService _formulaService;
private readonly ExcelService _excelService;

public FormulasController(ISender sender, ILogger<FormulasController> logger, IFormulaService formulaService, ExcelService excelService)
    : base(sender, logger)
{
    _formulaService = formulaService;
    _excelService = excelService;
}
```

- [ ] **Step 2: 新增 3 个端点 + 私有拉取方法（类尾追加）**

```csharp
private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

/// <summary>
/// 导出验方数据到 Excel（category 可选，按分类过滤）
/// </summary>
[HttpGet("export")]
[ProducesResponseType(typeof(FileResult), 200)]
public async Task<IActionResult> Export([FromQuery] string? category, CancellationToken ct)
{
    var formulas = await GetAllDetailsAsync(category, ct);
    if (formulas == null) return BusinessFail("导出失败");

    var bytes = _excelService.ExportToExcel(formulas, "验方", new Dictionary<string, Func<FormulaDetailDto, object?>>
    {
        ["名称"] = f => f.Name,
        ["分类"] = f => f.Category,
        ["描述"] = f => f.Description,
        ["组成"] = f => f.Herbs == null || f.Herbs.Count == 0
            ? string.Empty
            : JsonSerializer.Serialize(f.Herbs.Select(h => new { h.HerbName, h.Dosage, h.Unit, h.Preparation }), JsonOptions),
        ["功效"] = f => f.Effect,
        ["用法"] = f => f.Usage,
        ["性味归经"] = f => f.Property,
        ["主治"] = f => f.Indications,
        ["禁忌症"] = f => f.Contraindications,
        ["是否共享"] = f => f.IsShared
    });

    return File(bytes, ExcelService.ExcelContentType, $"验方导出_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
}

/// <summary>
/// 下载验方导入模板（表头 + 示例行）
/// </summary>
[HttpGet("import-template")]
[ProducesResponseType(typeof(FileResult), 200)]
public IActionResult ImportTemplate()
{
    var bytes = _excelService.GenerateTemplate("验方", new Dictionary<string, string>
    {
        ["名称"] = "四物汤",
        ["分类"] = "补血剂",
        ["描述"] = "补血调经",
        ["组成"] = "[{\"HerbName\":\"当归\",\"Dosage\":10,\"Unit\":\"g\"},{\"HerbName\":\"川芎\",\"Dosage\":8,\"Unit\":\"g\"}]",
        ["功效"] = "补血调血",
        ["用法"] = "水煎服",
        ["性味归经"] = "甘、温",
        ["主治"] = "血虚证",
        ["禁忌症"] = "",
        ["是否共享"] = "false"
    });

    return File(bytes, ExcelService.ExcelContentType, "验方导入模板.xlsx");
}

/// <summary>
/// 从 Excel 文件批量导入验方（仅 Admin+），复用 BatchImportFormulasCommand（药材名/拼音匹配 + 验方校验）
/// </summary>
[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
[HttpPost("batch-import-excel")]
[Consumes("multipart/form-data")]
[EnableRateLimiting("ApiCalls")]
[ProducesResponseType(typeof(ApiResponse<FormulaBatchImportResultDto>), 200)]
public async Task<IActionResult> BatchImportExcel(
    IFormFile file,
    CancellationToken ct = default)
{
    if (file == null || file.Length == 0)
        return ValidationFail("请上传 Excel 文件");

    List<FormulaImportItemDto> formulas;
    try
    {
        using var stream = file.OpenReadStream();
        formulas = _excelService.ParseExcel(stream, new Dictionary<string, Action<FormulaImportItemDto, string>>
        {
            ["名称"] = (d, v) => d.Name = v,
            ["描述"] = (d, v) => d.Description = v,
            ["组成"] = (d, v) => d.Herbs = ParseHerbs(v),
            ["功效"] = (d, v) => d.Effect = v,
            ["用法"] = (d, v) => d.Usage = v,
            ["性味归经"] = (d, v) => d.Property = v,
            ["主治"] = (d, v) => d.Indications = v,
            ["禁忌症"] = (d, v) => d.Contraindications = v,
            ["是否共享"] = (d, v) => d.IsShared = bool.TryParse(v, out var b) && b,
            ["分类"] = (_, _) => { } // 现有导入 DTO 无分类字段，忽略
        });
    }
    catch
    {
        return ValidationFail("Excel 文件解析失败，请使用系统模板");
    }

    if (formulas.Count == 0)
        return ValidationFail("导入数据不能为空");

    var result = await Sender.Send(new BatchImportFormulasCommand(formulas, file.FileName), ct);
    if (!result.IsSuccess || result.Value == null)
        return BusinessFail(result.Error ?? "导入失败");

    LogOperation("批量导入验方(Excel)", new { Count = formulas.Count, FileName = file.FileName }, null);
    return Success(result.Value, result.Value.Message);
}

/// <summary>
/// 全量拉取验方详情（分页循环 + 逐条详情），可选按分类过滤。失败返回 null。
/// </summary>
private async Task<List<FormulaDetailDto>?> GetAllDetailsAsync(string? category, CancellationToken ct)
{
    var details = new List<FormulaDetailDto>();
    const int pageSize = 100;
    var page = 1;

    while (true)
    {
        var paged = await _formulaService.GetPagedAsync(page, pageSize, null, ct);
        if (!paged.IsSuccess || paged.Value == null) return null;
        if (paged.Value.Items.Count == 0) break;

        foreach (var item in paged.Value.Items)
        {
            var detail = await _formulaService.GetByIdAsync(item.Id, ct);
            if (detail.IsSuccess && detail.Value != null) details.Add(detail.Value);
        }

        if (details.Count >= paged.Value.TotalCount || paged.Value.Items.Count < pageSize) break;
        page++;
    }

    return string.IsNullOrWhiteSpace(category)
        ? details
        : details.Where(f => string.Equals(f.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();
}

/// <summary>
/// 解析组成列 JSON（[{HerbName,Dosage,Unit}]），空值返回空列表
/// </summary>
private static List<FormulaHerbImportItemDto> ParseHerbs(string value)
{
    if (string.IsNullOrWhiteSpace(value)) return new List<FormulaHerbImportItemDto>();
    try
    {
        return JsonSerializer.Deserialize<List<FormulaHerbImportItemDto>>(value, JsonOptions) ?? new List<FormulaHerbImportItemDto>();
    }
    catch
    {
        return new List<FormulaHerbImportItemDto>();
    }
}
```

> 注意：`FormulaImportItemDto`/`FormulaDetailDto`/`FormulaBatchImportResultDto`/`FormulaHerbImportItemDto` 均有 `using LYBT.Shared.Models.Contracts.Formula;`；`ApiResponse` 已 using。

- [ ] **Step 3: 验证编译**

Run: `dotnet build src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj`
Expected: 0 错误 0 警告

- [ ] **Step 4: Commit**

```bash
git add src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs
git commit -m "feat: 验方 Excel 导出/模板/批量导入端点（B-03）"
```

---

### Task 7: 文档同步（13b 端点表 + 总账状态）

**Covers:** AGENTS.md §10.1 维护规则（文档-代码一致性）

**Files:**
- Modify: `docs/03-architecture/13b-api-endpoints.md`
- Modify: `docs/03-architecture/13-project-master-plan.md`（**在最终 build/test 通过并 commit 后填写 SHA**）

- [ ] **Step 1: 13b 患者表（§3.3）追加 3 行**

在 `POST | /batch-delete | 批量删除 | Admin+` 行后追加：

```markdown
| GET | /export?keyword= | 导出患者 Excel | Doctor/Receptionist/Admin |
| GET | /import-template | 下载导入模板 | Doctor/Receptionist/Admin |
| POST | /batch-import-excel | Excel 批量导入 | Admin+ |
```

- [ ] **Step 2: 13b 药材表（§3.4）追加 3 行**

在 `POST | /batch-import | 批量导入（JSON） | Admin+` 行后追加：

```markdown
| GET | /export?keyword= | 导出药材 Excel | Doctor/Admin |
| GET | /import-template | 下载导入模板 | Doctor/Admin |
| POST | /batch-import-excel | Excel 批量导入 | Admin+ |
```

- [ ] **Step 3: 13b 验方表（§3.5）追加 3 行**

在 `POST | /batch-import | 批量导入（JSON） | Admin+` 行后追加：

```markdown
| GET | /export?category= | 导出验方 Excel | Doctor/Admin |
| GET | /import-template | 下载导入模板 | Doctor/Admin |
| POST | /batch-import-excel | Excel 批量导入 | Admin+ |
```

- [ ] **Step 4: 13b Commit**

```bash
git add docs/03-architecture/13b-api-endpoints.md
git commit -m "docs: 13b 端点表补充 Excel 导出/导入端点（B-03）"
```

- [ ] **Step 5: 总账状态表更新（最后一步统一提交）**

在 `13-project-master-plan.md` §六 B 类表 B-03 行与 §八 状态跟踪表 B-03 行，将 `⬜` 改为 `✅`，填入完成日期与最终 commit SHA。若需记录决策变更（如 import-template 路由以 Refit 为准），在 §九 追加一行。

---

### Task 8: 全量验证 + 最终提交

**Covers:** 验收标准

- [ ] **Step 1: 全量编译**

Run: `dotnet build LYBTZYZS.sln --no-incremental`
Expected: 0 错误 0 警告（含存量，必须全绿）

- [ ] **Step 2: 架构测试**

Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: 全部通过（92/92 基线，新增端点不违反 Controller 约束：导出/模板返回 `FileResult`（IActionResult 子类）、`batch-import-excel` 返回 `IActionResult`、类级 [Authorize] 已有）

- [ ] **Step 3: ExcelService 单测（若 Task 2 已跑过可跳过）**

Run: `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~ExcelServiceTests"`
Expected: 4/4 通过

- [ ] **Step 4: 最终提交（含总账更新）**

```bash
git add docs/03-architecture/13-project-master-plan.md
git commit -m "docs: 总账标记 B-03 完成（Excel 导出/导入）"
```

- [ ] **Step 5: 汇报**

汇报内容：新增文件清单、9 个端点清单、build/test 结果、Commit SHA 列表。

---

## Self-Review

**1. Spec 覆盖：**
- 目标 1（NPOI 安装）→ Task 1 ✓
- 目标 2（ExcelService 三方法 + XSSFWorkbook）→ Task 2 ✓
- 目标 3（三实体 × 3 端点 = 9 端点）→ Tasks 4/5/6 ✓
- 目标 4（Excel 列定义，按现有 DTO 字段）→ Tasks 4/5/6（列与 DTO 实际字段一致；任务描述中「地址/过敏史/病史」「禁忌」字段在模型中不存在，已按「参照现有 DTO 字段」约束取舍）✓
- 目标 5（导入逐行验证 + 复用现有验证 + 返回成功/失败/错误详情）→ Tasks 3/4/5/6（Herbs/Formulas 复用现有命令的拼音生成/药材匹配/验方校验；患者新增同构命令；`ImportResultDto` 基类自带 SuccessCount/FailureCount/Failures）✓
- 约束（Refit 路由匹配 / 权限策略 / 不改 Desktop / 不改 JSON 导入 / 0 错误 0 警告 / 架构测试）→ Global Constraints + Tasks ✓

**2. 占位符扫描：** 无 TBD/TODO；所有代码步骤含完整代码。

**3. 类型一致性：**
- `ExcelService` 三方法签名在 Task 2 定义、Tasks 4/5/6 使用，一致 ✓
- `BatchImportPatientsCommand(Patients, Strategy, CurrentUserId)` 在 Task 3 定义、Task 4 调用，一致 ✓
- `BatchImportHerbsCommand(Herbs, Strategy, CurrentUserId)` / `BatchImportFormulasCommand(Formulas, FileName)` 为现有定义，Tasks 5/6 直接复用 ✓
- `PatientImportFailureDto` 字段（OriginalRowNumber/FailureReason/...）与现有定义一致 ✓
- `ParseGender`/`ParseHerbs` 私有辅助在各 Controller 内定义与调用一致 ✓
- 路由：`import-template`（Refit 权威）→ 三 Controller 一致；`batch-import-excel`（任务命名）→ 三 Controller 一致 ✓
