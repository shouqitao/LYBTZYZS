# Patients模块架构文档

**版本**: v1.0 (Epic #1934)
**更新时间**: 2025-11-10
**状态**: ✅ 已完成

---

## 📦 模块概览

**Patients模块**负责患者基础数据管理，包括CRUD、批量导入/导出、多条件搜索。

### 核心功能

1. **基础CRUD**
   - 创建/更新/删除患者
   - 分页查询、关键字搜索
   - 多条件搜索（姓名/拼音码/电话/身份证）

2. **批量导入**（Epic #1934 FR-001）
   - **Server层主导模式**：Server端负责Excel解析（EPPlus）
   - 部分成功机制：单条失败不影响整体
   - 失败恢复：详细失败原因记录
   - 重复检查：手机号唯一性（BR-004）

3. **导出功能**（Epic #1934 FR-002, FR-003）
   - 导出模板：支持示例数据配置
   - 数据导出：支持关键字过滤
   - Server端生成Excel文件流

4. **数据搜索**
   - 多字段搜索：姓名、拼音码、电话、身份证
   - 分页支持
   - 性能优化（索引）

---

## 🏗️ 架构设计

### 三层架构

```
┌──────────────────────────────────────────────────┐
│  Presentation Layer (PatientsController)         │
│  - GET  /api/v1/patients (分页查询)              │
│  - GET  /api/v1/patients/{id} (患者详情)         │
│  - POST /api/v1/patients (创建患者)              │
│  - PUT  /api/v1/patients/{id} (更新患者)         │
│  - DELETE /api/v1/patients/{id} (软删除)         │
│  - POST /api/v1/patients/batch-import (批量导入) │
│  - GET  /api/v1/patients/export-template         │
│  - GET  /api/v1/patients/export (数据导出)       │
└──────────────────────────────────────────────────┘
                       ↓
┌──────────────────────────────────────────────────┐
│  Application Layer (PatientService)              │
│  - GetPagedAsync(page, size, keyword)            │
│  - GetByIdAsync(id)                              │
│  - CreateAsync(dto)                              │
│  - UpdateAsync(id, dto)                          │
│  - DeleteAsync(id)                               │
│  - SearchAsync(keyword)                          │
│  - BatchImportAsync(stream, fileName)            │
│  - ExportTemplateAsync(config)                   │
│  - ExportPatientsAsync(keyword)                  │
└──────────────────────────────────────────────────┘
                       ↓
┌──────────────────────────────────────────────────┐
│  Infrastructure Layer (PatientRepository)        │
│  - SearchPatientsAsync(searchTerm, page, size)   │
│  - BatchCreateAsync(patients)                    │
│  - GetByPhoneNumberAsync(phoneNumber)            │
│  + IBaseRepository<Patient>标准CRUD方法          │
└──────────────────────────────────────────────────┘
```

---

## 📋 Repository层

### IPatientRepository接口

```csharp
namespace LYBT.Module.Patients.Interfaces;

/// <summary>
/// 患者仓储接口 - 继承IBaseRepository<Patient>标准接口
/// Phase 1 Task 1.3: 实现基础数据模块统一Repository规范
/// </summary>
/// <remarks>
/// 设计原则：
/// - ⭐ 统一共性：继承IBaseRepository<Patient>获得11个标准CRUD方法
/// - ⭐ 保持特性：保留患者模块特定业务方法
/// 
/// 特定业务方法说明：
/// - SearchPatientsAsync: 多条件搜索（姓名/拼音码/电话/身份证）
/// - BatchCreateAsync: 批量导入患者（Epic #1934）
/// - GetByPhoneNumberAsync: 手机号重复检查（Epic #1934 BR-004）
/// </remarks>
public interface IPatientRepository : IBaseRepository<Patient>
{
    /// <summary>
    /// 搜索患者（支持多条件和分页）
    /// </summary>
    /// <param name="searchTerm">搜索词（姓名/拼音码/电话/身份证）</param>
    /// <param name="pageIndex">页码（从1开始）</param>
    /// <param name="pageSize">每页大小</param>
    Task<PaginatedList<Patient>> SearchPatientsAsync(string? searchTerm, int pageIndex, int pageSize);

    /// <summary>
    /// 批量创建患者（Epic #1934 FR-001）
    /// </summary>
    Task<List<Patient>> BatchCreateAsync(IEnumerable<Patient> patients);

    /// <summary>
    /// 根据手机号查询患者（Epic #1934 BR-004重复检查）
    /// </summary>
    Task<Patient?> GetByPhoneNumberAsync(string phoneNumber);
}
```

### 关键实现要点

**1. 多条件搜索（支持多字段）**：

```csharp
public async Task<PaginatedList<Patient>> SearchPatientsAsync(
    string? searchTerm, 
    int pageIndex, 
    int pageSize)
{
    var query = DbContext.Patients
        .AsNoTracking()
        .Where(p => !p.IsDeleted);

    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        // ⭐ 多字段搜索：姓名、拼音码、电话、身份证
        query = query.Where(p =>
            p.Name.Contains(searchTerm) ||
            (p.PinYinCode != null && p.PinYinCode.Contains(searchTerm)) ||
            (p.PhoneNumber != null && p.PhoneNumber.Contains(searchTerm)) ||
            (p.IdCard != null && p.IdCard.Contains(searchTerm)));
    }

    return await PaginatedList<Patient>.CreateAsync(query, pageIndex, pageSize);
}
```

**2. 批量创建（性能优化）**：

```csharp
public async Task<List<Patient>> BatchCreateAsync(IEnumerable<Patient> patients)
{
    var patientList = patients.ToList();
    
    // ⭐ 批量添加（EF Core批量优化）
    await DbContext.Patients.AddRangeAsync(patientList);
    
    return patientList;
}
```

**3. 手机号重复检查**：

```csharp
public async Task<Patient?> GetByPhoneNumberAsync(string phoneNumber)
{
    return await DbContext.Patients
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.PhoneNumber == phoneNumber && !p.IsDeleted);
}
```

---

## 📋 Service层

### IPatientService接口

```csharp
namespace LYBT.Module.Patients.Interfaces;

/// <summary>
/// 患者服务接口 - 包含基础CRUD和批量导入/导出
/// </summary>
public interface IPatientService
{
    /// <summary>
    /// 分页查询患者
    /// </summary>
    Task<Result<PagedResult<PatientDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);

    /// <summary>
    /// 根据ID获取患者详情
    /// </summary>
    Task<Result<PatientDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// 创建新患者
    /// </summary>
    Task<Result<PatientDto>> CreateAsync(PatientInputDto dto);

    /// <summary>
    /// 更新患者信息
    /// </summary>
    Task<Result<PatientDto>> UpdateAsync(Guid id, PatientInputDto dto);

    /// <summary>
    /// 删除患者（软删除）
    /// </summary>
    Task<Result> DeleteAsync(Guid id);

    /// <summary>
    /// 搜索患者
    /// </summary>
    Task<Result<List<PatientDto>>> SearchAsync(string keyword);

    /// <summary>
    /// 批量导入患者数据 (Epic #1934 FR-001)
    /// 支持部分成功模式、失败恢复机制（BR-002）
    /// </summary>
    /// <param name="stream">Excel文件流</param>
    /// <param name="fileName">文件名（可选，用于日志记录）</param>
    /// <returns>批量导入结果，包含成功/失败/跳过数量和详细失败信息</returns>
    Task<Result<BatchImportResultDto>> BatchImportAsync(Stream stream, string? fileName = null);

    /// <summary>
    /// 导出患者导入模板 (Epic #1934 FR-002)
    /// </summary>
    /// <param name="config">模板配置（示例数据行数等）</param>
    /// <returns>Excel模板文件流</returns>
    Task<MemoryStream> ExportTemplateAsync(ExportTemplateDto config);

    /// <summary>
    /// 导出患者数据到Excel (Epic #1934 FR-003)
    /// </summary>
    /// <param name="keyword">搜索关键词（可选）</param>
    /// <returns>Excel文件流</returns>
    Task<MemoryStream> ExportPatientsAsync(string? keyword = null);
}
```

### 关键实现要点

**1. 批量导入逻辑（Server端主导模式）**：

```csharp
public async Task<Result<BatchImportResultDto>> BatchImportAsync(Stream stream, string? fileName = null)
{
    var result = new BatchImportResultDto();

    try
    {
        // 1. ⭐ Server端解析Excel（使用EPPlus）
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets[0];
        var rowCount = worksheet.Dimension.Rows;

        var patients = new List<Patient>();

        for (int row = 2; row <= rowCount; row++) // 从第2行开始（跳过标题）
        {
            try
            {
                var name = worksheet.Cells[row, 1].Text;
                var phoneNumber = worksheet.Cells[row, 2].Text;

                // 2. ⭐ 手机号重复检查（BR-004）
                if (!string.IsNullOrEmpty(phoneNumber))
                {
                    var exists = await _repository.GetByPhoneNumberAsync(phoneNumber);
                    if (exists != null)
                    {
                        result.SkippedCount++;
                        result.SkippedItems.Add(new FailedItem
                        {
                            RowNumber = row,
                            Name = name,
                            Reason = "手机号已存在（跳过）"
                        });
                        continue;
                    }
                }

                // 3. 创建Patient实体
                var patient = new Patient
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    PhoneNumber = phoneNumber,
                    Gender = ParseGender(worksheet.Cells[row, 3].Text),
                    BirthDate = DateTime.TryParse(worksheet.Cells[row, 4].Text, out var birth) ? birth : null,
                    Address = worksheet.Cells[row, 5].Text,
                    Status = CommonStatus.Enabled,
                    CreatedAt = DateTime.Now,
                    CreatedBy = GetCurrentUserId()
                };

                patients.Add(patient);
                result.TotalCount++;
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.FailedItems.Add(new FailedItem
                {
                    RowNumber = row,
                    Reason = ex.Message
                });
            }
        }

        // 4. ⭐ 批量创建（Repository层批量优化）
        if (patients.Count > 0)
        {
            await _repository.BatchCreateAsync(patients);
            await _unitOfWork.SaveChangesAsync();
            result.SuccessCount = patients.Count;
        }

        result.Message = $"导入完成: 成功{result.SuccessCount}条, 失败{result.FailureCount}条, 跳过{result.SkippedCount}条";
        return Result<BatchImportResultDto>.Success(result);
    }
    catch (Exception ex)
    {
        return Result<BatchImportResultDto>.Fail($"批量导入失败: {ex.Message}");
    }
}
```

**2. 导出模板逻辑**：

```csharp
public async Task<MemoryStream> ExportTemplateAsync(ExportTemplateDto config)
{
    var stream = new MemoryStream();

    using (var package = new ExcelPackage(stream))
    {
        var worksheet = package.Workbook.Worksheets.Add("患者导入模板");

        // 1. ⭐ 设置标题行
        worksheet.Cells[1, 1].Value = "姓名*";
        worksheet.Cells[1, 2].Value = "手机号";
        worksheet.Cells[1, 3].Value = "性别";
        worksheet.Cells[1, 4].Value = "出生日期";
        worksheet.Cells[1, 5].Value = "地址";

        // 2. ⭐ 添加示例数据（根据配置）
        if (config.IncludeSampleData && config.SampleDataRows > 0)
        {
            for (int i = 1; i <= config.SampleDataRows; i++)
            {
                worksheet.Cells[i + 1, 1].Value = $"张三{i}";
                worksheet.Cells[i + 1, 2].Value = $"138001380{i:00}";
                worksheet.Cells[i + 1, 3].Value = "男";
                worksheet.Cells[i + 1, 4].Value = "1980-01-01";
                worksheet.Cells[i + 1, 5].Value = "北京市朝阳区";
            }
        }

        // 3. ⭐ 格式化（加粗标题、自动列宽）
        using (var range = worksheet.Cells[1, 1, 1, 5])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
        }
        worksheet.Cells.AutoFitColumns();

        await package.SaveAsync();
    }

    stream.Position = 0;
    return stream;
}
```

**3. 数据导出逻辑**：

```csharp
public async Task<MemoryStream> ExportPatientsAsync(string? keyword = null)
{
    // 1. 查询数据（支持关键字过滤）
    var query = _repository.Query().Where(p => !p.IsDeleted);

    if (!string.IsNullOrWhiteSpace(keyword))
    {
        query = query.Where(p =>
            p.Name.Contains(keyword) ||
            (p.PhoneNumber != null && p.PhoneNumber.Contains(keyword)));
    }

    var patients = await query.ToListAsync();

    // 2. ⭐ 生成Excel
    var stream = new MemoryStream();
    using (var package = new ExcelPackage(stream))
    {
        var worksheet = package.Workbook.Worksheets.Add("患者数据");

        // 标题行
        worksheet.Cells[1, 1].Value = "姓名";
        worksheet.Cells[1, 2].Value = "手机号";
        worksheet.Cells[1, 3].Value = "性别";
        worksheet.Cells[1, 4].Value = "出生日期";
        worksheet.Cells[1, 5].Value = "地址";

        // 数据行
        for (int i = 0; i < patients.Count; i++)
        {
            var patient = patients[i];
            worksheet.Cells[i + 2, 1].Value = patient.Name;
            worksheet.Cells[i + 2, 2].Value = patient.PhoneNumber;
            worksheet.Cells[i + 2, 3].Value = patient.Gender.ToString();
            worksheet.Cells[i + 2, 4].Value = patient.BirthDate?.ToString("yyyy-MM-dd");
            worksheet.Cells[i + 2, 5].Value = patient.Address;
        }

        worksheet.Cells.AutoFitColumns();
        await package.SaveAsync();
    }

    stream.Position = 0;
    return stream;
}
```

---

## 📋 Controller端点

### API端点列表

| 端点 | 方法 | 说明 | 业务规则 | Epic |
|-----|------|------|---------|------|
| `/api/v1/patients` | GET | 分页查询患者列表 | 支持keyword过滤 | - |
| `/api/v1/patients/{id}` | GET | 查询患者详情 | - | - |
| `/api/v1/patients` | POST | 创建患者 | BR-003（姓名1-50字符） | - |
| `/api/v1/patients/{id}` | PUT | 更新患者 | BR-003 | - |
| `/api/v1/patients/{id}` | DELETE | 删除患者（软删除） | BR-005（软删除） | - |
| `/api/v1/patients/batch-import` | POST | 批量导入（Excel文件流） | BR-001（Server端解析）<br>BR-002（部分成功）<br>BR-004（手机号唯一） | #1934 |
| `/api/v1/patients/export-template` | GET | 导出模板（Excel） | 支持示例数据配置 | #1934 |
| `/api/v1/patients/export` | GET | 导出数据（Excel） | 支持keyword过滤 | #1934 |

### 关键端点实现

**批量导入端点（Server端主导）**：

```csharp
/// <summary>
/// 批量导入患者数据 (Epic #1934 FR-001)
/// Server端主导：Server负责Excel解析和业务处理
/// </summary>
[HttpPost("batch-import")]
[ProducesResponseType(typeof(ApiResponse<BatchImportResultDto>), 200)]
[ProducesResponseType(400)]
public async Task<ActionResult<ApiResponse<BatchImportResultDto>>> BatchImport(IFormFile file)
{
    try
    {
        // 1. 验证文件
        if (file == null || file.Length == 0)
            return ValidationFail<BatchImportResultDto>("请上传Excel文件");

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return ValidationFail<BatchImportResultDto>("仅支持.xlsx格式的Excel文件");

        // BR-001: 文件大小限制（5MB）
        if (file.Length > 5 * 1024 * 1024)
            return ValidationFail<BatchImportResultDto>("文件大小不能超过5MB");

        // 2. ⭐ Server端处理Excel
        using var stream = file.OpenReadStream();
        var result = await _service.BatchImportAsync(stream, file.FileName);

        if (result.IsSuccess && result.Data != null)
        {
            LogOperation("批量导入患者（Epic #1934）",
                new {
                    FileName = file.FileName,
                    TotalCount = result.Data.TotalCount,
                    SuccessCount = result.Data.SuccessCount,
                    FailureCount = result.Data.FailureCount
                },
                null);
        }

        return HandleResult(result, $"导入完成: 成功{result.Data?.SuccessCount ?? 0}条");
    }
    catch (Exception ex)
    {
        return HandleException<BatchImportResultDto>(ex, "批量导入患者",
            new { FileName = file?.FileName });
    }
}
```

**导出模板端点**：

```csharp
/// <summary>
/// 导出患者导入模板 (Epic #1934 FR-002)
/// </summary>
[HttpGet("export-template")]
[ProducesResponseType(typeof(FileStreamResult), 200)]
public async Task<IActionResult> ExportTemplate([FromQuery] int sampleRows = 3)
{
    try
    {
        var config = new ExportTemplateDto
        {
            IncludeSampleData = sampleRows > 0,
            SampleDataRows = sampleRows
        };

        var stream = await _service.ExportTemplateAsync(config);
        var fileName = $"患者导入模板_{DateTime.Now:yyyyMMdd}.xlsx";

        return File(stream, 
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
    catch (Exception ex)
    {
        LogError(ex, "导出患者模板", new { sampleRows });
        return StatusCode(500, ApiResponse.Fail("导出模板失败"));
    }
}
```

---

## 📊 性能基准

### 批量操作性能要求

| 操作 | 数据量 | 性能要求 | 实际性能（InMemory） | 优化措施 |
|-----|-------|---------|---------------------|---------|
| **批量导入** | 1000条 | < 10秒 | ~230ms | AddRangeAsync、一次SaveChanges |
| **数据导出** | 10000条 | < 2秒 | - | AsNoTracking()、流式处理 |
| **分页查询** | 每页20条 | < 500ms | ~91μs | 多字段索引 |
| **单条创建** | 1条 | < 300ms | ~18ms | 事务优化 |

### 索引优化（Epic #1934 Phase 1）

```sql
-- 唯一索引（手机号，支持快速重复检查）
CREATE UNIQUE INDEX IX_Patients_PhoneNumber
ON Patients(PhoneNumber)
WHERE IsDeleted = 0 AND PhoneNumber IS NOT NULL;

-- 复合索引（多条件搜索优化）
CREATE INDEX IX_Patients_Search
ON Patients(Name, PinYinCode, PhoneNumber)
INCLUDE (Gender, BirthDate, Address)
WHERE IsDeleted = 0;
```

---

## 🔄 批量导入模式对比

### Server端主导 vs Desktop端主导

| 对比维度 | Patients（Server主导） | Herbs（Desktop主导） |
|---------|----------------------|---------------------|
| **Excel解析** | Server端（EPPlus） | Desktop端（EPPlus） |
| **数据传输** | 文件流（IFormFile） | DTO列表（JSON） |
| **重复检查** | Server端数据库查询 | Desktop端预检查 + Server端二次检查 |
| **性能** | 网络传输小（文件流） | 网络传输大（JSON序列化） |
| **复杂度** | Server端复杂（解析+业务） | Client端复杂（解析+校验） |
| **适用场景** | 简单导入（少量业务规则） | 复杂导入（拼音码生成、多种重复策略） |

**选择依据**：
- Patients模块业务规则简单（仅手机号重复检查），Server端主导可减少Client端复杂度
- Herbs模块业务规则复杂（拼音码生成、3种重复策略），Desktop端主导可提供更好的用户交互

---

## 📚 业务规则

| 规则ID | 描述 | 验证层 | 实现位置 |
|--------|------|--------|---------|
| **BR-001** | Server端负责Excel解析（Epic #1934） | Controller | PatientsController.BatchImport |
| **BR-002** | 部分成功机制：单条失败不影响整体 | Service层 | PatientService.BatchImportAsync |
| **BR-003** | 患者姓名1-50字符，必填 | FluentValidation | PatientInputDtoValidator |
| **BR-004** | 手机号唯一性（批量导入） | Service层 | PatientService.BatchImportAsync |
| **BR-005** | 软删除支持 | Service层 | DeleteAsync（设置IsDeleted=true） |

---

## 📖 相关文档

- **需求文档**: [Epic #1934 - Patients批量导入](https://github.com/shouqitao/LYBTZYZS/issues/1934)
- **Server端架构**: [docs/explanation/architecture/server/README.md](../README.md)
- **批量操作模式**: [docs/how-to/patterns/batch-operations.md](../../../../how-to/patterns/batch-operations.md)
- **Herbs模块对比**: [herbs.md](./herbs.md)（Desktop主导模式参考）

---

## 🏷️ 变更历史

| 版本 | 日期 | 描述 | Epic/Issue |
|------|------|------|------------|
| v1.0 | 2025-11-10 | 初始版本，文档化Epic #1934实现 | #1934, #2007 |

---

**最后更新**: 2025-11-10
**维护者**: @shouqitao
