# 患者管理功能完善 - 技术设计文档

## 文档元数据

| 属性 | 值 |
|-----|-----|
| **文档版本** | v1.0 |
| **需求文档** | [患者管理功能完善需求分析](patient-management-enhancement-requirements.md) v1.0 |
| **作者** | Claude Code |
| **创建日期** | 2025-11-09 |
| **最后更新** | 2025-11-09 |
| **状态** | ✅ 待审查 |
| **相关Issue** | TBD（待lybtzyzs-task-breakdown生成） |

---

## 1. 概述

### 1.1 设计目标

基于[患者管理功能完善需求分析](patient-management-enhancement-requirements.md)，本设计文档提供完整的技术实现方案，包括：

1. **批量导入功能**（FR-001）：支持Excel批量导入患者，包含完整的失败恢复机制
2. **批量导出功能**（FR-002）：支持导出患者数据到Excel，包括模板导出
3. **患者详情查看**（FR-003）：优化患者详情展示
4. **患者信息编辑**（FR-004）：完善编辑功能
5. **列表UI优化**（FR-005）：自适应列宽和操作列右对齐
6. **工具栏按钮集成**（FR-006）：新增导入/导出按钮

### 1.2 设计范围

**包含范围**：
- Server端三层架构实现（Controller/Service/Repository）
- Client端MVVM实现（ViewModel/View）
- Shared层DTOs和Validators
- 完整的BR-002失败恢复流程（6步）
- Excel导入/导出基础设施

**不包含范围**：
- 患者数据导入历史记录持久化（BR-004明确不记录历史）
- 复杂的Excel格式兼容（仅支持.xlsx标准格式）
- 批量编辑/删除功能（超出需求范围）

### 1.3 架构约束

**遵循原则**：
- ✅ 三层架构对齐（Server/Client/Shared）
- ✅ InputDto统一模式（Epic #1736 Phase 3）
- ✅ Validators共享（Epic #1773）
- ✅ Repository内部可见性（Epic #1600 Phase 3）
- ✅ Service直接实现接口（无抽象基类）
- ✅ BR-006调用链完整性验证

**技术约束**：
- ✅ 允许：EPPlus（MIT许可）, EF Core 8.0, .NET 8.0
- ❌ 禁止：Redis, CQRS, MediatR, Event Sourcing（MVP Constitution）

---

## 2. 架构设计

### 2.1 整体架构

```
┌─────────────────────────────────────────────────────────────┐
│                      Client端 (WPF)                         │
├─────────────────────────────────────────────────────────────┤
│  PatientManagementView.xaml                                 │
│    ├─ UnifiedManagementToolBar (新增3个按钮)                │
│    ├─ UnifiedManagementTable (FR-005优化)                  │
│    └─ ImportResultDialog (BR-002步骤2)                     │
├─────────────────────────────────────────────────────────────┤
│  PatientManagementViewModel                                 │
│    ├─ ImportPatientsCommand (FR-001)                       │
│    ├─ ExportTemplateCommand (FR-002)                       │
│    ├─ ExportPatientsCommand (FR-002)                       │
│    └─ ExportFailuresAsync (BR-002步骤3)                    │
├─────────────────────────────────────────────────────────────┤
│  IPatientApi (Refit HTTP Client)                           │
│    ├─ BatchImportAsync                                      │
│    ├─ BatchExportAsync                                      │
│    └─ ExportFailuresAsync                                   │
└─────────────────────────────────────────────────────────────┘
                           ↕ HTTP
┌─────────────────────────────────────────────────────────────┐
│                    Shared层 (跨端共享)                       │
├─────────────────────────────────────────────────────────────┤
│  DTOs                                                       │
│    ├─ PatientInputDto (统一输入DTO)                        │
│    ├─ BatchImportResultDto                                 │
│    ├─ ImportFailureDetailDto                               │
│    └─ ExportTemplateDto                                    │
├─────────────────────────────────────────────────────────────┤
│  Validators (FluentValidation)                             │
│    └─ PatientInputDtoValidator (8个验证点)                 │
└─────────────────────────────────────────────────────────────┘
                           ↕ 引用
┌─────────────────────────────────────────────────────────────┐
│                   Server端 (ASP.NET Core)                   │
├─────────────────────────────────────────────────────────────┤
│  PatientsController (Presentation Layer)                   │
│    ├─ POST /api/patients/batch-import                      │
│    ├─ GET /api/patients/batch-export                       │
│    └─ POST /api/patients/export-failures                   │
├─────────────────────────────────────────────────────────────┤
│  PatientService (Application Layer)                        │
│    ├─ BatchImportAsync (核心业务逻辑)                       │
│    ├─ BatchExportAsync                                      │
│    ├─ ExportFailuresAsync                                   │
│    └─ ExportTemplateAsync                                   │
├─────────────────────────────────────────────────────────────┤
│  PatientRepository (Infrastructure Layer)                  │
│    ├─ ExistsByPhoneAsync (BR-004重复检查)                  │
│    ├─ GetAllAsync (批量导出)                                │
│    └─ GetTotalCountAsync (统计)                             │
└─────────────────────────────────────────────────────────────┘
                           ↕ EF Core
┌─────────────────────────────────────────────────────────────┐
│              SQL Server 2022 (Patients表)                   │
│    现有Schema（无需变更，复用Epic #1600设计）               │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 依赖关系

```
Controller ──→ Service ──→ Repository ──→ EF Core ──→ Database
    ↓            ↓            ↓
  IMapper    IValidator   AppDbContext
    ↓            ↓
  AutoMapper FluentValidation
```

**关键依赖**：
- EPPlus 7.x：Excel文件处理（MIT许可）
- FluentValidation 11.x：DTO验证
- AutoMapper 12.x：对象映射
- Refit 7.x：HTTP客户端

---

## 3. API端点设计

### 3.1 Write Layer

#### 3.1.1 批量导入患者

```http
POST /api/patients/batch-import
Content-Type: multipart/form-data
```

**请求参数**：
```csharp
public class BatchImportRequest
{
    [Required]
    public IFormFile File { get; set; }  // .xlsx文件
}
```

**响应**：
```json
{
  "successCount": 70,
  "failureCount": 30,
  "skippedCount": 0,
  "failures": [
    {
      "originalRowNumber": 5,
      "failureReason": "手机号格式不正确",
      "fieldName": "PhoneNumber",
      "originalValue": "123456",
      "suggestedFix": "请输入11位有效手机号，如：13800138000",
      "dataSnapshot": {
        "name": "张三",
        "gender": 1,
        "dateOfBirth": "1990-01-01",
        "phoneNumber": "123456",
        ...
      }
    },
    ...
  ],
  "importTime": "2025-11-09T14:30:00"
}
```

**业务规则**：
- BR-001：8个验证点（必填字段、格式验证、类型转换、业务规则）
- BR-002：部分成功模式（失败不中断流程）
- BR-003：最多1000行数据
- BR-004：手机号重复检查（自动跳过）

#### 3.1.2 导出失败数据

```http
POST /api/patients/export-failures
Content-Type: application/json
```

**请求体**：
```json
{
  "failures": [
    {
      "originalRowNumber": 5,
      "failureReason": "手机号格式不正确",
      "fieldName": "PhoneNumber",
      "originalValue": "123456",
      "suggestedFix": "请输入11位有效手机号",
      "dataSnapshot": { ... }
    },
    ...
  ]
}
```

**响应**：
```
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
Content-Disposition: attachment; filename="导入失败数据_20251109143000.xlsx"

[Excel二进制数据]
```

**Excel格式**（BR-002步骤3）：
| 行号 | 失败原因 | 失败字段 | 原始值 | 修复建议 | 姓名 | 性别 | 出生日期 | 手机号 | ... |
|-----|---------|---------|-------|---------|-----|-----|---------|-------|-----|
| 5   | 手机号格式不正确 | PhoneNumber | 123456 | 请输入11位有效手机号 | 张三 | 男 | 1990-01-01 | 123456 | ... |

### 3.2 Read Layer

#### 3.2.1 批量导出患者

```http
GET /api/patients/batch-export
```

**查询参数**：无（导出所有患者，最多10000条）

**响应**：
```
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
Content-Disposition: attachment; filename="患者数据导出_20251109143000.xlsx"

[Excel二进制数据]
```

**Excel格式**：
| 姓名 | 性别 | 出生日期 | 手机号 | 身份证号 | 地址 | 过敏史 | 既往病史 |
|-----|-----|---------|-------|---------|-----|-------|---------|
| 张三 | 男  | 1990-01-01 | 13800138000 | 110101199001011234 | ... | ... | ... |

#### 3.2.2 导出导入模板

```http
GET /api/patients/export-template?includeSampleData=true&sampleRowCount=3
```

**查询参数**：
- `includeSampleData`（bool）：是否包含示例数据（默认true）
- `sampleRowCount`（int）：示例行数（默认3，最多10）

**响应**：Excel模板文件（含示例数据）

#### 3.2.3 现有端点（复用）

```http
GET /api/patients/{id}          # FR-003：查看患者详情
GET /api/patients               # FR-005：分页查询列表
```

### 3.3 Helper Layer

无新增Helper端点（权限验证复用现有CanEdit逻辑）

---

## 4. DTO设计

### 4.1 PatientInputDto（统一输入DTO）

```csharp
namespace LYBT.Shared.DTOs.Patients;

/// <summary>
/// 患者输入DTO（统一用于创建和更新）
/// Epic #1736 Phase 3：InputDto统一模式
/// </summary>
public class PatientInputDto
{
    /// <summary>
    /// 患者ID（更新时必填，创建时为null）
    /// </summary>
    [DisplayName("患者ID")]
    public Guid? Id { get; set; }

    /// <summary>
    /// 患者姓名
    /// </summary>
    [Required(ErrorMessage = "患者姓名不能为空")]
    [StringLength(50, ErrorMessage = "患者姓名长度不能超过50个字符")]
    [DisplayName("患者姓名")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 性别
    /// </summary>
    [Required(ErrorMessage = "性别不能为空")]
    [DisplayName("性别")]
    public Gender Gender { get; set; }

    /// <summary>
    /// 出生日期
    /// </summary>
    [Required(ErrorMessage = "出生日期不能为空")]
    [DisplayName("出生日期")]
    public DateTime DateOfBirth { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    [Required(ErrorMessage = "手机号不能为空")]
    [Phone(ErrorMessage = "手机号格式不正确")]
    [StringLength(20, ErrorMessage = "手机号长度不能超过20个字符")]
    [DisplayName("手机号")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// 身份证号（可选）
    /// </summary>
    [StringLength(18, ErrorMessage = "身份证号长度不能超过18个字符")]
    [DisplayName("身份证号")]
    public string? IdNumber { get; set; }

    /// <summary>
    /// 地址（可选）
    /// </summary>
    [StringLength(200, ErrorMessage = "地址长度不能超过200个字符")]
    [DisplayName("地址")]
    public string? Address { get; set; }

    /// <summary>
    /// 过敏史（可选）
    /// </summary>
    [StringLength(500, ErrorMessage = "过敏史长度不能超过500个字符")]
    [DisplayName("过敏史")]
    public string? Allergies { get; set; }

    /// <summary>
    /// 既往病史（可选）
    /// </summary>
    [StringLength(1000, ErrorMessage = "既往病史长度不能超过1000个字符")]
    [DisplayName("既往病史")]
    public string? MedicalHistory { get; set; }
}
```

### 4.2 BatchImportResultDto（批量导入结果）

```csharp
namespace LYBT.Shared.DTOs.Patients;

/// <summary>
/// 批量导入结果DTO
/// </summary>
public class BatchImportResultDto
{
    /// <summary>
    /// 成功导入数量
    /// </summary>
    [DisplayName("成功数量")]
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败数量
    /// </summary>
    [DisplayName("失败数量")]
    public int FailureCount { get; set; }

    /// <summary>
    /// 跳过数量（重复数据）
    /// </summary>
    [DisplayName("跳过数量")]
    public int SkippedCount { get; set; }

    /// <summary>
    /// 失败详情列表
    /// </summary>
    [DisplayName("失败详情")]
    public List<ImportFailureDetailDto> Failures { get; set; } = new();

    /// <summary>
    /// 导入时间
    /// </summary>
    [DisplayName("导入时间")]
    public DateTime ImportTime { get; set; }
}
```

### 4.3 ImportFailureDetailDto（失败详情）

```csharp
namespace LYBT.Shared.DTOs.Patients;

/// <summary>
/// 导入失败详情DTO
/// BR-002：支持快速定位和修复
/// </summary>
public class ImportFailureDetailDto
{
    /// <summary>
    /// Excel原始行号（从1开始，标题行为1，数据从2开始）
    /// </summary>
    [DisplayName("原始行号")]
    public int OriginalRowNumber { get; set; }

    /// <summary>
    /// 失败原因
    /// </summary>
    [DisplayName("失败原因")]
    public string FailureReason { get; set; } = string.Empty;

    /// <summary>
    /// 失败字段名称
    /// </summary>
    [DisplayName("失败字段")]
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// 原始值
    /// </summary>
    [DisplayName("原始值")]
    public string OriginalValue { get; set; } = string.Empty;

    /// <summary>
    /// 修复建议
    /// </summary>
    [DisplayName("修复建议")]
    public string SuggestedFix { get; set; } = string.Empty;

    /// <summary>
    /// 数据快照（整行数据）
    /// </summary>
    [DisplayName("数据快照")]
    public PatientInputDto DataSnapshot { get; set; } = new();
}
```

### 4.4 ExportTemplateDto（导出模板配置）

```csharp
namespace LYBT.Shared.DTOs.Patients;

/// <summary>
/// 导出模板配置DTO
/// </summary>
public class ExportTemplateDto
{
    /// <summary>
    /// 是否包含示例数据
    /// </summary>
    [DisplayName("包含示例数据")]
    public bool IncludeSampleData { get; set; } = true;

    /// <summary>
    /// 示例行数（默认3行）
    /// </summary>
    [Range(1, 10, ErrorMessage = "示例行数必须在1-10之间")]
    [DisplayName("示例行数")]
    public int SampleRowCount { get; set; } = 3;
}
```

---

## 5. Validator设计

### 5.1 PatientInputDtoValidator

```csharp
namespace LYBT.Shared.Validators.Patients;

/// <summary>
/// 患者输入DTO验证器
/// Epic #1773：Validators共享（前后端共享验证规则）
/// BR-001：8个验证点
/// </summary>
public class PatientInputDtoValidator : AbstractValidator<PatientInputDto>
{
    public PatientInputDtoValidator()
    {
        // 1. 姓名验证
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("患者姓名不能为空")
            .MaximumLength(50).WithMessage("患者姓名长度不能超过50个字符")
            .Must(BeValidChineseName).WithMessage("姓名格式不正确，请输入中文姓名");

        // 2. 性别验证
        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("性别值无效");

        // 3. 出生日期验证
        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("出生日期不能为空")
            .LessThanOrEqualTo(DateTime.Now).WithMessage("出生日期不能晚于当前日期")
            .GreaterThanOrEqualTo(DateTime.Now.AddYears(-150)).WithMessage("出生日期不合理（年龄不能超过150岁）");

        // 4. 手机号验证
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("手机号不能为空")
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确，请输入11位有效手机号");

        // 5. 身份证号验证（可选）
        When(x => !string.IsNullOrWhiteSpace(x.IdNumber), () =>
        {
            RuleFor(x => x.IdNumber)
                .MaximumLength(18).WithMessage("身份证号长度不能超过18个字符")
                .Matches(@"^\d{17}[\dXx]$").WithMessage("身份证号格式不正确")
                .Must(BeValidIdNumber).WithMessage("身份证号校验位不正确");
        });

        // 6. 地址验证（可选）
        When(x => !string.IsNullOrWhiteSpace(x.Address), () =>
        {
            RuleFor(x => x.Address)
                .MaximumLength(200).WithMessage("地址长度不能超过200个字符");
        });

        // 7. 过敏史验证（可选）
        When(x => !string.IsNullOrWhiteSpace(x.Allergies), () =>
        {
            RuleFor(x => x.Allergies)
                .MaximumLength(500).WithMessage("过敏史长度不能超过500个字符");
        });

        // 8. 既往病史验证（可选）
        When(x => !string.IsNullOrWhiteSpace(x.MedicalHistory), () =>
        {
            RuleFor(x => x.MedicalHistory)
                .MaximumLength(1000).WithMessage("既往病史长度不能超过1000个字符");
        });
    }

    private bool BeValidChineseName(string name)
    {
        // 中文姓名验证：2-50个中文字符，可包含·（少数民族）
        return Regex.IsMatch(name, @"^[\u4e00-\u9fa5·]{2,50}$");
    }

    private bool BeValidIdNumber(string? idNumber)
    {
        if (string.IsNullOrWhiteSpace(idNumber) || idNumber.Length != 18)
            return false;

        // 身份证号校验位算法（GB 11643-1999）
        var weights = new[] { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
        var checkCodes = new[] { '1', '0', 'X', '9', '8', '7', '6', '5', '4', '3', '2' };

        var sum = 0;
        for (var i = 0; i < 17; i++)
        {
            if (!char.IsDigit(idNumber[i]))
                return false;
            sum += (idNumber[i] - '0') * weights[i];
        }

        var checkCode = checkCodes[sum % 11];
        return char.ToUpper(idNumber[17]) == checkCode;
    }
}
```

---

## 6. 代码示例

### 6.1 Controller层

```csharp
namespace LYBT.Server.Modules.LYBT.Server.Patients.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _service;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(
        IPatientService service,
        ILogger<PatientsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// FR-001：批量导入患者
    /// </summary>
    [HttpPost("batch-import")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(BatchImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BatchImport(IFormFile file)
    {
        // 参数验证
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("批量导入失败：未上传文件");
            return BadRequest("请上传Excel文件");
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("批量导入失败：文件格式不正确，文件名={FileName}", file.FileName);
            return BadRequest("仅支持.xlsx格式的Excel文件");
        }

        if (file.Length > 10 * 1024 * 1024) // 10MB限制
        {
            _logger.LogWarning("批量导入失败：文件过大，大小={Size}MB", file.Length / 1024.0 / 1024.0);
            return BadRequest("文件大小不能超过10MB");
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _service.BatchImportAsync(stream, file.FileName);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("批量导入失败：{Message}", result.Message);
                return BadRequest(result.Message);
            }

            _logger.LogInformation(
                "批量导入完成：成功={SuccessCount}，失败={FailureCount}，跳过={SkippedCount}",
                result.Data.SuccessCount,
                result.Data.FailureCount,
                result.Data.SkippedCount);

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量导入异常：{FileName}", file.FileName);
            return StatusCode(StatusCodes.Status500InternalServerError, "批量导入失败，请联系管理员");
        }
    }

    /// <summary>
    /// FR-002：批量导出患者
    /// </summary>
    [HttpGet("batch-export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BatchExport()
    {
        try
        {
            var result = await _service.BatchExportAsync();

            if (!result.IsSuccess)
            {
                _logger.LogWarning("批量导出失败：{Message}", result.Message);
                return BadRequest(result.Message);
            }

            _logger.LogInformation("批量导出完成：文件大小={Size}KB", result.Data.Length / 1024.0);

            return File(
                result.Data,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"患者数据导出_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量导出异常");
            return StatusCode(StatusCodes.Status500InternalServerError, "批量导出失败，请联系管理员");
        }
    }

    /// <summary>
    /// BR-002步骤3：导出失败数据
    /// </summary>
    [HttpPost("export-failures")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportFailures([FromBody] List<ImportFailureDetailDto> failures)
    {
        if (failures == null || failures.Count == 0)
        {
            _logger.LogWarning("导出失败数据失败：失败列表为空");
            return BadRequest("失败列表不能为空");
        }

        try
        {
            var result = await _service.ExportFailuresAsync(failures);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("导出失败数据失败：{Message}", result.Message);
                return BadRequest(result.Message);
            }

            _logger.LogInformation("导出失败数据完成：失败数量={Count}", failures.Count);

            return File(
                result.Data,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"导入失败数据_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出失败数据异常");
            return StatusCode(StatusCodes.Status500InternalServerError, "导出失败数据失败，请联系管理员");
        }
    }

    /// <summary>
    /// 导出导入模板
    /// </summary>
    [HttpGet("export-template")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportTemplate(
        [FromQuery] bool includeSampleData = true,
        [FromQuery] int sampleRowCount = 3)
    {
        try
        {
            var config = new ExportTemplateDto
            {
                IncludeSampleData = includeSampleData,
                SampleRowCount = Math.Clamp(sampleRowCount, 1, 10)
            };

            var result = await _service.ExportTemplateAsync(config);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("导出模板失败：{Message}", result.Message);
                return BadRequest(result.Message);
            }

            _logger.LogInformation("导出模板完成：包含示例数据={IncludeSample}", includeSampleData);

            return File(
                result.Data,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"患者导入模板_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出模板异常");
            return StatusCode(StatusCodes.Status500InternalServerError, "导出模板失败，请联系管理员");
        }
    }
}
```

### 6.2 Service层（核心业务逻辑）

```csharp
namespace LYBT.Server.Modules.LYBT.Server.Patients.Services;

public class PatientService : IPatientService
{
    private readonly IPatientRepository _repository;
    private readonly IMapper _mapper;
    private readonly IValidator<PatientInputDto> _validator;
    private readonly ILogger<PatientService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public PatientService(
        IPatientRepository repository,
        IMapper mapper,
        IValidator<PatientInputDto> validator,
        ILogger<PatientService> logger,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _mapper = mapper;
        _validator = validator;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// FR-001：批量导入患者
    /// BR-001：8个验证点
    /// BR-002：部分成功模式
    /// BR-003：最多1000行
    /// BR-004：重复检查
    /// </summary>
    public async Task<ServiceResult<BatchImportResultDto>> BatchImportAsync(
        Stream excelStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = new BatchImportResultDto
            {
                ImportTime = DateTime.Now
            };

            // 使用EPPlus读取Excel
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage(excelStream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                _logger.LogWarning("批量导入失败：Excel文件中没有工作表");
                return ServiceResult<BatchImportResultDto>.Failure("Excel文件中没有工作表");
            }

            var lastRow = worksheet.Dimension?.End.Row ?? 0;
            if (lastRow <= 1)
            {
                _logger.LogWarning("批量导入失败：Excel文件中没有数据");
                return ServiceResult<BatchImportResultDto>.Failure("Excel文件中没有数据");
            }

            // BR-003：最多1000行
            var dataRowCount = lastRow - 1; // 减去标题行
            if (dataRowCount > 1000)
            {
                _logger.LogWarning("批量导入失败：数据行数={RowCount}超过1000行限制", dataRowCount);
                return ServiceResult<BatchImportResultDto>.Failure("数据行数不能超过1000行");
            }

            _logger.LogInformation("开始批量导入：文件={FileName}，数据行数={RowCount}", fileName, dataRowCount);

            // 逐行处理
            for (int rowIndex = 2; rowIndex <= lastRow; rowIndex++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("批量导入被取消");
                    break;
                }

                try
                {
                    // 解析行数据
                    var input = ParseRowToInputDto(worksheet, rowIndex);

                    // BR-001：验证（8个验证点）
                    var validationResult = await _validator.ValidateAsync(input, cancellationToken);
                    if (!validationResult.IsValid)
                    {
                        var firstError = validationResult.Errors.First();
                        result.Failures.Add(new ImportFailureDetailDto
                        {
                            OriginalRowNumber = rowIndex,
                            FailureReason = firstError.ErrorMessage,
                            FieldName = firstError.PropertyName,
                            OriginalValue = firstError.AttemptedValue?.ToString() ?? string.Empty,
                            SuggestedFix = GetSuggestedFix(firstError.PropertyName, firstError.ErrorMessage),
                            DataSnapshot = input
                        });
                        result.FailureCount++;
                        continue;
                    }

                    // BR-004：重复性检查（手机号）
                    if (await _repository.ExistsByPhoneAsync(input.PhoneNumber))
                    {
                        _logger.LogDebug("行{RowNumber}：手机号{Phone}已存在，跳过", rowIndex, input.PhoneNumber);
                        result.SkippedCount++;
                        continue;
                    }

                    // 创建实体
                    var patient = _mapper.Map<Patient>(input);
                    patient.Id = Guid.NewGuid();
                    patient.CreatedAt = DateTime.Now;
                    patient.UpdatedAt = DateTime.Now;

                    await _repository.AddAsync(patient);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "行{RowNumber}处理异常", rowIndex);
                    result.Failures.Add(new ImportFailureDetailDto
                    {
                        OriginalRowNumber = rowIndex,
                        FailureReason = $"处理异常：{ex.Message}",
                        FieldName = "Unknown",
                        OriginalValue = string.Empty,
                        SuggestedFix = "请检查数据格式是否正确",
                        DataSnapshot = new PatientInputDto()
                    });
                    result.FailureCount++;
                }
            }

            // 保存所有成功的记录
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "批量导入完成：成功={Success}，失败={Failure}，跳过={Skipped}",
                result.SuccessCount,
                result.FailureCount,
                result.SkippedCount);

            return ServiceResult<BatchImportResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量导入异常：{FileName}", fileName);
            return ServiceResult<BatchImportResultDto>.Failure($"批量导入失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 解析Excel行数据为InputDto
    /// </summary>
    private PatientInputDto ParseRowToInputDto(ExcelWorksheet worksheet, int rowIndex)
    {
        return new PatientInputDto
        {
            Name = GetCellValue(worksheet, rowIndex, 1), // A列：姓名
            Gender = ParseGender(GetCellValue(worksheet, rowIndex, 2)), // B列：性别
            DateOfBirth = ParseDate(GetCellValue(worksheet, rowIndex, 3)), // C列：出生日期
            PhoneNumber = GetCellValue(worksheet, rowIndex, 4), // D列：手机号
            IdNumber = GetCellValue(worksheet, rowIndex, 5), // E列：身份证号
            Address = GetCellValue(worksheet, rowIndex, 6), // F列：地址
            Allergies = GetCellValue(worksheet, rowIndex, 7), // G列：过敏史
            MedicalHistory = GetCellValue(worksheet, rowIndex, 8) // H列：既往病史
        };
    }

    private string GetCellValue(ExcelWorksheet worksheet, int row, int col)
    {
        return worksheet.Cells[row, col].Value?.ToString()?.Trim() ?? string.Empty;
    }

    private Gender ParseGender(string value)
    {
        return value switch
        {
            "男" => Gender.Male,
            "女" => Gender.Female,
            "1" => Gender.Male,
            "2" => Gender.Female,
            _ => Gender.Male
        };
    }

    private DateTime ParseDate(string value)
    {
        if (DateTime.TryParse(value, out var date))
            return date;

        if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date2))
            return date2;

        return DateTime.MinValue;
    }

    private string GetSuggestedFix(string fieldName, string errorMessage)
    {
        return fieldName switch
        {
            nameof(PatientInputDto.Name) => "请输入2-50个中文字符的姓名",
            nameof(PatientInputDto.PhoneNumber) => "请输入11位有效手机号，如：13800138000",
            nameof(PatientInputDto.IdNumber) => "请输入18位有效身份证号",
            nameof(PatientInputDto.DateOfBirth) => "请输入有效日期，格式：yyyy-MM-dd",
            _ => "请检查数据格式"
        };
    }

    /// <summary>
    /// FR-002：批量导出患者
    /// </summary>
    public async Task<ServiceResult<byte[]>> BatchExportAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var patients = await _repository.GetAllAsync(maxCount: 10000);

            _logger.LogInformation("开始批量导出：患者数量={Count}", patients.Count);

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("患者数据");

            // 标题行
            var headers = new[] { "姓名", "性别", "出生日期", "手机号", "身份证号", "地址", "过敏史", "既往病史" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }

            // 数据行
            for (int i = 0; i < patients.Count; i++)
            {
                var patient = patients[i];
                var rowIndex = i + 2;

                worksheet.Cells[rowIndex, 1].Value = patient.Name;
                worksheet.Cells[rowIndex, 2].Value = patient.Gender == Gender.Male ? "男" : "女";
                worksheet.Cells[rowIndex, 3].Value = patient.DateOfBirth.ToString("yyyy-MM-dd");
                worksheet.Cells[rowIndex, 4].Value = patient.PhoneNumber;
                worksheet.Cells[rowIndex, 5].Value = patient.IdNumber;
                worksheet.Cells[rowIndex, 6].Value = patient.Address;
                worksheet.Cells[rowIndex, 7].Value = patient.Allergies;
                worksheet.Cells[rowIndex, 8].Value = patient.MedicalHistory;
            }

            // 自适应列宽
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var bytes = package.GetAsByteArray();

            _logger.LogInformation("批量导出完成：文件大小={Size}KB", bytes.Length / 1024.0);

            return ServiceResult<byte[]>.Success(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量导出异常");
            return ServiceResult<byte[]>.Failure($"批量导出失败：{ex.Message}");
        }
    }

    /// <summary>
    /// BR-002步骤3：导出失败数据
    /// </summary>
    public async Task<ServiceResult<byte[]>> ExportFailuresAsync(
        List<ImportFailureDetailDto> failures,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始导出失败数据：失败数量={Count}", failures.Count);

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("导入失败数据");

            // 标题行（包含失败信息列）
            var headers = new[]
            {
                "行号", "失败原因", "失败字段", "原始值", "修复建议",
                "姓名", "性别", "出生日期", "手机号", "身份证号", "地址", "过敏史", "既往病史"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }

            // 数据行
            for (int i = 0; i < failures.Count; i++)
            {
                var failure = failures[i];
                var rowIndex = i + 2;

                // 失败信息列
                worksheet.Cells[rowIndex, 1].Value = failure.OriginalRowNumber;
                worksheet.Cells[rowIndex, 2].Value = failure.FailureReason;
                worksheet.Cells[rowIndex, 3].Value = failure.FieldName;
                worksheet.Cells[rowIndex, 4].Value = failure.OriginalValue;
                worksheet.Cells[rowIndex, 5].Value = failure.SuggestedFix;

                // 数据快照列
                var snapshot = failure.DataSnapshot;
                worksheet.Cells[rowIndex, 6].Value = snapshot.Name;
                worksheet.Cells[rowIndex, 7].Value = snapshot.Gender == Gender.Male ? "男" : "女";
                worksheet.Cells[rowIndex, 8].Value = snapshot.DateOfBirth.ToString("yyyy-MM-dd");
                worksheet.Cells[rowIndex, 9].Value = snapshot.PhoneNumber;
                worksheet.Cells[rowIndex, 10].Value = snapshot.IdNumber;
                worksheet.Cells[rowIndex, 11].Value = snapshot.Address;
                worksheet.Cells[rowIndex, 12].Value = snapshot.Allergies;
                worksheet.Cells[rowIndex, 13].Value = snapshot.MedicalHistory;

                // 高亮失败原因列
                worksheet.Cells[rowIndex, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[rowIndex, 2].Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
            }

            // 自适应列宽
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var bytes = package.GetAsByteArray();

            _logger.LogInformation("导出失败数据完成：文件大小={Size}KB", bytes.Length / 1024.0);

            return ServiceResult<byte[]>.Success(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出失败数据异常");
            return ServiceResult<byte[]>.Failure($"导出失败数据失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 导出导入模板
    /// </summary>
    public async Task<ServiceResult<byte[]>> ExportTemplateAsync(
        ExportTemplateDto config,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "开始导出模板：包含示例={IncludeSample}，示例行数={SampleRows}",
                config.IncludeSampleData,
                config.SampleRowCount);

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("患者导入模板");

            // 标题行
            var headers = new[] { "姓名*", "性别*", "出生日期*", "手机号*", "身份证号", "地址", "过敏史", "既往病史" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
            }

            // 示例数据
            if (config.IncludeSampleData)
            {
                var sampleData = new[]
                {
                    new[] { "张三", "男", "1990-01-01", "13800138000", "110101199001011234", "北京市朝阳区", "青霉素过敏", "高血压" },
                    new[] { "李四", "女", "1985-05-15", "13900139000", "110101198505150012", "上海市浦东新区", "无", "糖尿病" },
                    new[] { "王五", "男", "2000-12-20", "13700137000", "", "广东省深圳市", "海鲜过敏", "" }
                };

                var rowsToAdd = Math.Min(config.SampleRowCount, sampleData.Length);
                for (int i = 0; i < rowsToAdd; i++)
                {
                    var rowIndex = i + 2;
                    for (int j = 0; j < sampleData[i].Length; j++)
                    {
                        worksheet.Cells[rowIndex, j + 1].Value = sampleData[i][j];
                        worksheet.Cells[rowIndex, j + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[rowIndex, j + 1].Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
                    }
                }
            }

            // 添加说明
            var notesRow = config.IncludeSampleData ? config.SampleRowCount + 3 : 3;
            worksheet.Cells[notesRow, 1].Value = "填写说明：";
            worksheet.Cells[notesRow, 1].Style.Font.Bold = true;
            worksheet.Cells[notesRow + 1, 1].Value = "1. 带*号的列为必填项";
            worksheet.Cells[notesRow + 2, 1].Value = "2. 性别填写：男 或 女";
            worksheet.Cells[notesRow + 3, 1].Value = "3. 出生日期格式：yyyy-MM-dd（如：1990-01-01）";
            worksheet.Cells[notesRow + 4, 1].Value = "4. 手机号格式：11位数字";
            worksheet.Cells[notesRow + 5, 1].Value = "5. 身份证号格式：18位（可选）";
            worksheet.Cells[notesRow + 6, 1].Value = "6. 最多支持1000行数据";

            // 自适应列宽
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var bytes = package.GetAsByteArray();

            _logger.LogInformation("导出模板完成：文件大小={Size}KB", bytes.Length / 1024.0);

            return ServiceResult<byte[]>.Success(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出模板异常");
            return ServiceResult<byte[]>.Failure($"导出模板失败：{ex.Message}");
        }
    }
}
```

### 6.3 Repository层

```csharp
namespace LYBT.Server.Modules.LYBT.Server.Patients.Repositories;

/// <summary>
/// 患者仓储实现
/// Epic #1600 Phase 3：Repository实现类internal可见性
/// </summary>
internal class PatientRepository : IPatientRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<PatientRepository> _logger;

    public PatientRepository(AppDbContext context, ILogger<PatientRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// BR-004：检查手机号是否已存在
    /// </summary>
    public async Task<bool> ExistsByPhoneAsync(string phoneNumber)
    {
        return await _context.Patients
            .AnyAsync(p => p.PhoneNumber == phoneNumber);
    }

    /// <summary>
    /// FR-002：获取所有患者（批量导出）
    /// </summary>
    public async Task<List<Patient>> GetAllAsync(int maxCount = 1000)
    {
        return await _context.Patients
            .OrderBy(p => p.CreatedAt)
            .Take(maxCount)
            .ToListAsync();
    }

    /// <summary>
    /// 获取患者总数
    /// </summary>
    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Patients.CountAsync();
    }

    // 其他现有方法（GetByIdAsync, GetPagedAsync, AddAsync, UpdateAsync, DeleteAsync）
    // ...
}
```

### 6.4 Client端ViewModel

```csharp
namespace LYBT.Desktop.Patients.ViewModels;

public class PatientManagementViewModel : ViewModelBase
{
    private readonly IPatientApi _patientApi;
    private readonly IDialogService _dialogService;
    private readonly IFileDialogService _fileDialogService;
    private readonly ILogger<PatientManagementViewModel> _logger;

    // 现有属性（复用）
    public ObservableCollection<PatientDto> Items { get; set; }
    public PatientDto? SelectedItem { get; set; }
    public string SearchText { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }

    // 现有命令（复用）
    public DelegateCommand SearchCommand { get; }
    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand<PatientDto> ViewDetailsCommand { get; }
    public DelegateCommand<PatientDto> EditCommand { get; }
    public DelegateCommand<PatientDto> DeleteCommand { get; }

    // 新增命令
    public DelegateCommand ImportPatientsCommand { get; }
    public DelegateCommand ExportTemplateCommand { get; }
    public DelegateCommand ExportPatientsCommand { get; }

    public PatientManagementViewModel(
        IPatientApi patientApi,
        IDialogService dialogService,
        IFileDialogService fileDialogService,
        ILogger<PatientManagementViewModel> logger)
    {
        _patientApi = patientApi;
        _dialogService = dialogService;
        _fileDialogService = fileDialogService;
        _logger = logger;

        // 初始化命令
        ImportPatientsCommand = new DelegateCommand(OnImportPatientsAsync);
        ExportTemplateCommand = new DelegateCommand(OnExportTemplateAsync);
        ExportPatientsCommand = new DelegateCommand(OnExportPatientsAsync);

        // 现有命令初始化...
    }

    /// <summary>
    /// FR-001：批量导入患者
    /// </summary>
    private async void OnImportPatientsAsync()
    {
        try
        {
            var filePath = _fileDialogService.OpenFileDialog(
                "选择Excel文件",
                "Excel文件 (*.xlsx)|*.xlsx");

            if (string.IsNullOrEmpty(filePath))
                return;

            _logger.LogInformation("开始批量导入：{FilePath}", filePath);

            // 调用API
            using var fileStream = File.OpenRead(filePath);
            var result = await _patientApi.BatchImportAsync(fileStream);

            _logger.LogInformation(
                "批量导入完成：成功={Success}，失败={Failure}，跳过={Skipped}",
                result.SuccessCount,
                result.FailureCount,
                result.SkippedCount);

            // BR-002步骤2：显示导入结果
            if (result.FailureCount > 0)
            {
                var dialogResult = _dialogService.ShowImportResultDialog(result);

                // BR-002步骤3：用户选择导出失败数据
                if (dialogResult == ImportResultAction.ExportFailures)
                {
                    await ExportFailuresAsync(result.Failures);
                }
            }
            else
            {
                _dialogService.ShowSuccess(
                    $"导入成功！\n成功导入：{result.SuccessCount} 条\n" +
                    (result.SkippedCount > 0 ? $"跳过重复：{result.SkippedCount} 条" : ""));
            }

            // 刷新列表
            await LoadPatientsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量导入异常");
            _dialogService.ShowError($"导入失败：{ex.Message}");
        }
    }

    /// <summary>
    /// BR-002步骤3：导出失败数据
    /// </summary>
    private async Task ExportFailuresAsync(List<ImportFailureDetailDto> failures)
    {
        try
        {
            var savePath = _fileDialogService.SaveFileDialog(
                "保存失败数据",
                $"导入失败数据_{DateTime.Now:yyyyMMddHHmmss}.xlsx",
                "Excel文件 (*.xlsx)|*.xlsx");

            if (string.IsNullOrEmpty(savePath))
                return;

            _logger.LogInformation("开始导出失败数据：{SavePath}", savePath);

            var fileBytes = await _patientApi.ExportFailuresAsync(failures);
            await File.WriteAllBytesAsync(savePath, fileBytes);

            _logger.LogInformation("导出失败数据完成");

            _dialogService.ShowInfo(
                $"失败数据已导出到：\n{savePath}\n\n" +
                "请修复数据后重新导入。\n" +
                "提示：修复后可选择性导入部分数据。");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出失败数据异常");
            _dialogService.ShowError($"导出失败数据失败：{ex.Message}");
        }
    }

    /// <summary>
    /// FR-002：导出导入模板
    /// </summary>
    private async void OnExportTemplateAsync()
    {
        try
        {
            var savePath = _fileDialogService.SaveFileDialog(
                "保存导入模板",
                $"患者导入模板_{DateTime.Now:yyyyMMddHHmmss}.xlsx",
                "Excel文件 (*.xlsx)|*.xlsx");

            if (string.IsNullOrEmpty(savePath))
                return;

            _logger.LogInformation("开始导出模板：{SavePath}", savePath);

            var fileBytes = await _patientApi.ExportTemplateAsync(includeSampleData: true, sampleRowCount: 3);
            await File.WriteAllBytesAsync(savePath, fileBytes);

            _logger.LogInformation("导出模板完成");

            _dialogService.ShowSuccess(
                $"模板已导出到：\n{savePath}\n\n" +
                "模板包含3行示例数据，请参考填写。");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出模板异常");
            _dialogService.ShowError($"导出模板失败：{ex.Message}");
        }
    }

    /// <summary>
    /// FR-002：批量导出患者
    /// </summary>
    private async void OnExportPatientsAsync()
    {
        try
        {
            var savePath = _fileDialogService.SaveFileDialog(
                "导出患者数据",
                $"患者数据导出_{DateTime.Now:yyyyMMddHHmmss}.xlsx",
                "Excel文件 (*.xlsx)|*.xlsx");

            if (string.IsNullOrEmpty(savePath))
                return;

            _logger.LogInformation("开始批量导出：{SavePath}", savePath);

            var fileBytes = await _patientApi.BatchExportAsync();
            await File.WriteAllBytesAsync(savePath, fileBytes);

            _logger.LogInformation("批量导出完成");

            _dialogService.ShowSuccess(
                $"导出成功！\n" +
                $"共导出：{TotalCount} 条患者数据\n" +
                $"保存位置：{savePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量导出异常");
            _dialogService.ShowError($"导出患者数据失败：{ex.Message}");
        }
    }
}
```

### 6.5 Client端XAML（FR-005, FR-006）

```xaml
<!-- PatientManagementView.xaml -->
<UserControl x:Class="LYBT.Desktop.Patients.Views.PatientManagementView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:prism="http://prismlibrary.com/"
             xmlns:controls="clr-namespace:LYBT.Desktop.Infrastructure.Controls;assembly=LYBT.Desktop.Infrastructure"
             prism:ViewModelLocator.AutoWireViewModel="True">

    <Grid Background="{StaticResource BackgroundBrush}">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />  <!-- 工具栏 -->
            <RowDefinition Height="*" />      <!-- 表格 -->
            <RowDefinition Height="Auto" />  <!-- 分页栏 -->
        </Grid.RowDefinitions>

        <!-- FR-006：统一工具栏（新增3个按钮） -->
        <controls:UnifiedManagementToolBar
            Grid.Row="0"
            SearchText="{Binding SearchText, Mode=TwoWay}"
            SearchCommand="{Binding SearchCommand}">

            <controls:UnifiedManagementToolBar.FilterContent>
                <StackPanel Orientation="Horizontal" />
            </controls:UnifiedManagementToolBar.FilterContent>

            <controls:UnifiedManagementToolBar.ActionButtons>
                <StackPanel Orientation="Horizontal">
                    <!-- 新增按钮 -->
                    <Button Content="📥 导入患者"
                            Style="{StaticResource SecondaryButton}"
                            Command="{Binding ImportPatientsCommand}"
                            ToolTip="从Excel文件批量导入患者"
                            Margin="{StaticResource SpacingSmall}" />
                    <Button Content="📄 导出模板"
                            Style="{StaticResource InfoButton}"
                            Command="{Binding ExportTemplateCommand}"
                            ToolTip="下载患者导入模板（含示例数据）"
                            Margin="{StaticResource SpacingSmall}" />
                    <Button Content="📤 导出患者"
                            Style="{StaticResource WarningButton}"
                            Command="{Binding ExportPatientsCommand}"
                            ToolTip="导出患者数据到Excel文件"
                            Margin="{StaticResource SpacingSmall}" />

                    <!-- 现有按钮 -->
                    <Button Content="+ 新增患者"
                            Style="{StaticResource SuccessButton}"
                            Command="{Binding AddPatientCommand}"
                            Margin="{StaticResource SpacingSmall}" />
                    <Button Content="刷新"
                            Style="{StaticResource SecondaryButton}"
                            Command="{Binding RefreshCommand}"
                            Margin="{StaticResource SpacingSmall}" />
                </StackPanel>
            </controls:UnifiedManagementToolBar.ActionButtons>
        </controls:UnifiedManagementToolBar>

        <!-- FR-005：统一数据表格（列宽优化） -->
        <controls:UnifiedManagementTable
            Grid.Row="1"
            ItemsSource="{Binding Items}"
            SelectedItem="{Binding SelectedItem, Mode=TwoWay}"
            EmptyStateText="暂无患者数据">

            <controls:UnifiedManagementTable.Columns>
                <!-- 姓名（固定宽度） -->
                <DataGridTextColumn Header="姓名"
                                    Binding="{Binding Name}"
                                    Width="100" />

                <!-- 性别（固定宽度） -->
                <DataGridTextColumn Header="性别"
                                    Binding="{Binding Gender, Converter={StaticResource EnumDescriptionConverter}}"
                                    Width="60" />

                <!-- 出生日期（固定宽度） -->
                <DataGridTextColumn Header="出生日期"
                                    Binding="{Binding DateOfBirth, StringFormat='yyyy-MM-dd'}"
                                    Width="100" />

                <!-- FR-005：手机号（自适应宽度） -->
                <DataGridTextColumn Header="手机号"
                                    Binding="{Binding PhoneNumber}"
                                    Width="*" />

                <!-- FR-005：身份证号（自适应宽度） -->
                <DataGridTextColumn Header="身份证号"
                                    Binding="{Binding IdNumber}"
                                    Width="*" />

                <!-- 地址（自适应宽度） -->
                <DataGridTextColumn Header="地址"
                                    Binding="{Binding Address}"
                                    Width="*" />

                <!-- FR-005：操作列（扩展为3个按钮，右对齐） -->
                <DataGridTemplateColumn Header="操作" Width="200">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal"
                                        HorizontalAlignment="Right"
                                        Margin="0,0,20,0">
                                <Button Content="查看"
                                        Style="{StaticResource InfoButton}"
                                        Padding="8,4"
                                        FontSize="12"
                                        Margin="2"
                                        Command="{Binding DataContext.ViewDetailsCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding}" />
                                <Button Content="编辑"
                                        Style="{StaticResource SuccessButton}"
                                        Padding="8,4"
                                        FontSize="12"
                                        Margin="2"
                                        Command="{Binding DataContext.EditCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding}" />
                                <Button Content="删除"
                                        Style="{StaticResource DangerButton}"
                                        Padding="8,4"
                                        FontSize="12"
                                        Margin="2"
                                        Command="{Binding DataContext.DeleteCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding}" />
                            </StackPanel>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </controls:UnifiedManagementTable.Columns>
        </controls:UnifiedManagementTable>

        <!-- 统一分页栏 -->
        <controls:UnifiedPaginationBar
            Grid.Row="2"
            CurrentPage="{Binding CurrentPage, Mode=TwoWay}"
            TotalPages="{Binding TotalPages}"
            PageSize="{Binding PageSize, Mode=TwoWay}"
            TotalCount="{Binding TotalCount}"
            FirstPageCommand="{Binding FirstPageCommand}"
            PreviousPageCommand="{Binding PreviousPageCommand}"
            NextPageCommand="{Binding NextPageCommand}"
            LastPageCommand="{Binding LastPageCommand}" />
    </Grid>
</UserControl>
```

---

## 7. 数据库Schema

**无变更**。复用Epic #1600现有Patients表设计，Schema已满足需求。

**现有表结构**：
```sql
CREATE TABLE Patients (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Name NVARCHAR(50) NOT NULL,
    Gender INT NOT NULL, -- 1=Male, 2=Female
    DateOfBirth DATETIME NOT NULL,
    PhoneNumber NVARCHAR(20) NOT NULL,
    IdNumber NVARCHAR(18) NULL,
    Address NVARCHAR(200) NULL,
    Allergies NVARCHAR(500) NULL,
    MedicalHistory NVARCHAR(1000) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    IsDeleted BIT NOT NULL DEFAULT 0
);

CREATE INDEX IX_Patients_PhoneNumber ON Patients(PhoneNumber);
CREATE INDEX IX_Patients_Name ON Patients(Name);
```

**验证点**：
- ✅ PhoneNumber字段长度足够（20个字符）
- ✅ 已有PhoneNumber索引（支持BR-004重复检查）
- ✅ 已有Name索引（支持搜索）
- ✅ 可选字段已正确标记为NULL

---

## 8. Phase拆分与实施顺序

### Phase 1：基础架构与数据模型（P0）

**工作量**：2-3天
**目标**：建立批量导入/导出的基础设施

**任务清单**：
1. ✅ 创建BatchImportResultDto, ImportFailureDetailDto, ExportTemplateDto（Shared.DTOs）
2. ✅ 创建PatientInputDto（统一输入DTO，Epic #1736）
3. ✅ 创建PatientInputDtoValidator（Shared.Validators，Epic #1773）
4. ✅ 扩展IPatientRepository接口（ExistsByPhoneAsync, GetAllAsync, GetTotalCountAsync）
5. ✅ 实现PatientRepository新方法
6. ✅ 配置AutoMapper映射规则（PatientInputDto ↔ Patient）
7. ✅ 添加EPPlus NuGet包依赖（Server + Client）

**验收标准**：
- [ ] 所有DTO类编译通过（0 errors, 0 warnings）
- [ ] PatientInputDtoValidator单元测试通过（8个验证点全覆盖）
- [ ] PatientRepository单元测试通过（ExistsByPhoneAsync, GetAllAsync）
- [ ] AutoMapper配置测试通过

**关键文件**：
- `src/Shared/DTOs/Patients/BatchImportResultDto.cs`
- `src/Shared/DTOs/Patients/ImportFailureDetailDto.cs`
- `src/Shared/DTOs/Patients/PatientInputDto.cs`
- `src/Shared/Validators/Patients/PatientInputDtoValidator.cs`
- `src/Server/Modules/Patients/Repositories/IPatientRepository.cs`
- `src/Server/Modules/Patients/Repositories/PatientRepository.cs`

---

### Phase 2：Server端业务逻辑（P0）

**工作量**：3-4天
**目标**：实现完整的批量导入/导出API
**依赖**：Phase 1完成

**任务清单**：
1. ✅ 实现IPatientService.BatchImportAsync（核心业务逻辑）
   - Excel解析
   - 逐行验证（8个验证点）
   - 重复性检查（手机号）
   - 部分成功模式（失败不中断）
   - 失败详情生成（原始行号+修复建议）
2. ✅ 实现IPatientService.BatchExportAsync
3. ✅ 实现IPatientService.ExportFailuresAsync
4. ✅ 实现IPatientService.ExportTemplateAsync
5. ✅ 实现PatientsController新端点
   - POST /api/patients/batch-import
   - GET /api/patients/batch-export
   - POST /api/patients/export-failures
   - GET /api/patients/export-template
6. ✅ 定义IPatientApi接口（Shared）
7. ✅ 实现PatientApi（Client，Refit）

**验收标准**：
- [ ] API端点Swagger文档生成正确
- [ ] Postman测试通过（100条测试数据：70成功+30失败）
- [ ] 失败数据导出包含原始行号和失败原因
- [ ] BR-006调用链完整性验证清单全部通过
- [ ] 性能测试通过（1000条导入 ≤ 30秒）

**关键文件**：
- `src/Server/Modules/Patients/Services/PatientService.cs`
- `src/Server/Modules/Patients/Controllers/PatientsController.cs`
- `src/Shared/APIs/IPatientApi.cs`
- `src/Client/Desktop/Infrastructure/APIs/PatientApi.cs`

---

### Phase 3：Client端UI集成（P1）

**工作量**：2-3天
**目标**：完成用户界面和交互流程
**依赖**：Phase 2完成

**任务清单**：
1. ✅ 扩展PatientManagementViewModel
   - ImportPatientsCommand
   - ExportTemplateCommand
   - ExportPatientsCommand
   - ExportFailuresAsync（BR-002步骤3）
2. ✅ 创建ImportResultDialog（BR-002步骤2）
   - 显示成功/失败/跳过数量
   - 失败详情列表
   - 导出失败数据按钮
   - 关闭按钮
3. ✅ 创建IFileDialogService接口和实现
4. ✅ 修改PatientManagementView.xaml
   - FR-006：工具栏新增3个按钮
   - FR-005：列表UI优化（自适应宽度+操作列右对齐）
5. ✅ 配置DI容器注册
   - IFileDialogService
   - IPatientApi（Refit）

**验收标准**：
- [ ] 用户可通过UI完成批量导入（100条数据）
- [ ] 导入失败时弹出结果对话框
- [ ] 可导出失败数据并重新导入（BR-002完整流程验证）
- [ ] 可导出模板（包含3行示例数据）
- [ ] 可导出现有患者数据
- [ ] 列表UI自适应窗口宽度
- [ ] 操作列3个按钮右对齐显示
- [ ] 手动测试通过（真实场景验证）

**关键文件**：
- `src/Client/Desktop/Modules/Patients/ViewModels/PatientManagementViewModel.cs`
- `src/Client/Desktop/Modules/Patients/Views/PatientManagementView.xaml`
- `src/Client/Desktop/Modules/Patients/Views/ImportResultDialog.xaml`
- `src/Client/Desktop/Infrastructure/Services/FileDialogService.cs`

---

### Phase 4：质量保障与文档（P2）

**工作量**：1-2天
**目标**：确保代码质量和文档完整
**依赖**：Phase 3完成

**任务清单**：
1. ✅ 单元测试编写
   - PatientServiceTests（BatchImportAsync, BatchExportAsync）
   - PatientRepositoryTests（ExistsByPhoneAsync, GetAllAsync）
   - PatientInputDtoValidatorTests（8个验证点）
2. ✅ 集成测试编写
   - PatientsControllerTests（API端点）
   - End-to-End测试（完整导入流程）
3. ✅ 文档更新
   - 更新docs/how-to/patient-management.md
   - 更新docs/reference/api/patients-api.md
   - 创建docs/how-to/batch-import-patients.md（用户指南）
4. ✅ 性能测试
   - 1000条数据导入性能测试（≤30秒）
   - 10000条数据导出性能测试（≤60秒）
5. ✅ lybtzyzs-quality-reporter质量检查

**验收标准**：
- [ ] 代码覆盖率 ≥ 80%
- [ ] 所有单元测试通过（0 failures）
- [ ] 所有集成测试通过（0 failures）
- [ ] 性能测试符合指标
- [ ] 文档已更新并通过lybtzyzs-doc-sync检查
- [ ] 质量报告评分 ≥ 85分
- [ ] lybtzyzs-arch-compliance检查通过
- [ ] lybtzyzs-mvp-compliance检查通过

**关键文件**：
- `tests/UnitTests/Server/Modules/Patients/PatientServiceTests.cs`
- `tests/IntegrationTests/API/PatientsControllerTests.cs`
- `docs/how-to/patient-management.md`
- `docs/how-to/batch-import-patients.md`
- `docs/reference/api/patients-api.md`

---

### 实施顺序建议

**严格依赖顺序**：Phase 1 → Phase 2 → Phase 3 → Phase 4

**关键里程碑**：
- **里程碑1（Day 3）**：Phase 1完成，基础架构就绪
- **里程碑2（Day 7）**：Phase 2完成，API可用
- **里程碑3（Day 10）**：Phase 3完成，用户可操作
- **里程碑4（Day 12）**：Phase 4完成，质量达标

---

## 9. 质量标准

### 9.1 代码质量

**编译标准**：
- ✅ 0 errors
- ✅ 0 warnings

**测试覆盖率**：
- ✅ 单元测试覆盖率 ≥ 80%
- ✅ 集成测试覆盖率 ≥ 60%

**代码规范**：
- ✅ 中文注释
- ✅ UTF-8 with BOM编码
- ✅ PascalCase命名（类型）
- ✅ _camelCase命名（私有字段）
- ✅ 仅构造函数注入DI
- ✅ 所有I/O操作必须async/await

### 9.2 架构合规性

**通过lybtzyzs-arch-compliance验证**：
- [ ] 三层架构职责划分正确
- [ ] 依赖方向：Controller → Service → Repository
- [ ] Repository实现类internal可见性
- [ ] Service直接实现接口（无抽象基类）
- [ ] InputDto统一模式（Epic #1736）
- [ ] Validators共享（Epic #1773）

### 9.3 MVP合规性

**通过lybtzyzs-mvp-compliance验证**：
- [ ] 允许技术：EPPlus（MIT许可）, EF Core 8.0, .NET 8.0
- [ ] 禁止技术：Redis, CQRS, MediatR, Event Sourcing
- [ ] 简单直接设计，无过度抽象
- [ ] 无超前设计模式

### 9.4 BR-006调用链完整性

**验证清单**：
- [ ] Server API endpoint implemented and tested
- [ ] IPatientApi interface defined (Shared)
- [ ] PatientApi (Refit) implemented (Client)
- [ ] IPatientRepository interface defined
- [ ] PatientRepository implemented
- [ ] PatientService implemented (non-Mock)
- [ ] PatientManagementViewModel implemented
- [ ] UI Command bound

### 9.5 性能指标

**性能要求**：
- ✅ 批量导入1000条数据：≤ 30秒
- ✅ 批量导出10000条数据：≤ 60秒
- ✅ Excel文件解析：≤ 5秒（1000行）
- ✅ 数据库批量插入：≤ 20秒（1000条）

### 9.6 用户体验标准

**BR-002失败恢复流程**：
- [ ] 导入失败时立即显示结果对话框
- [ ] 失败数据包含原始行号（快速定位）
- [ ] 失败原因详细且可操作（修复建议）
- [ ] 一键导出失败数据到Excel
- [ ] 增量导入支持（仅重新导入修复后的数据）
- [ ] 重复数据自动跳过（不阻塞流程）

### 9.7 安全性要求

**安全标准**：
- [ ] 文件类型白名单验证（仅.xlsx）
- [ ] 文件大小限制（最大10MB）
- [ ] 数据行数限制（最多1000行，BR-003）
- [ ] SQL注入防护（EF Core参数化查询）
- [ ] 输入验证（FluentValidation）
- [ ] 异常处理与日志记录

---

## 10. 风险识别与缓解

### 风险1：大文件导入超时

**风险描述**：用户尝试导入超大文件（>1000行）导致处理超时

**影响**：高
**概率**：中

**缓解措施**：
- ✅ BR-003限制1000行（前端和后端双重验证）
- ✅ 性能测试验证1000行导入时间 ≤ 30秒
- ✅ 文件大小限制10MB

### 风险2：并发导入数据冲突

**风险描述**：多用户同时导入相同手机号的患者，导致数据冲突

**影响**：中
**概率**：低

**缓解措施**：
- ✅ 手机号唯一性检查（BR-004）
- ✅ 事务管理（EF Core UnitOfWork）
- ✅ 重复数据自动跳过（不中断流程）

### 风险3：Excel格式不兼容

**风险描述**：用户使用非标准Excel格式（如WPS、LibreOffice），导致解析失败

**影响**：中
**概率**：中

**缓解措施**：
- ✅ 提供标准模板（ExportTemplateAsync）
- ✅ 严格格式验证（仅支持.xlsx）
- ✅ 示例数据提示（3行示例）
- ✅ 详细错误提示（解析失败时）

### 风险4：失败数据丢失

**风险描述**：导入失败后，用户未及时导出失败数据，关闭对话框后数据丢失

**影响**：高
**概率**：中

**缓解措施**：
- ✅ BR-002步骤3：自动导出失败数据
- ✅ 失败数据包含完整上下文（原始行号+数据快照）
- ✅ 对话框强制提示（"是否导出失败数据？"）
- ⚠️ 不记录历史（BR-004明确不实现）

### 风险5：性能瓶颈

**风险描述**：1000条数据导入时，数据库插入耗时过长

**影响**：中
**概率**：低

**缓解措施**：
- ✅ EF Core批量插入优化（一次SaveChanges）
- ✅ 已有PhoneNumber索引（加速重复检查）
- ✅ 性能测试验证（1000条 ≤ 30秒）
- ✅ 异步I/O操作（async/await）

---

## 11. 下一步

### 11.1 即将执行

1. ⏭️ 调用`lybtzyzs-task-breakdown` skill拆分实施任务
2. ⏭️ 调用`lybtzyzs-issue-template` skill批量生成GitHub Issues
3. ⏭️ 调用`lybtzyzs-design-arch-validator` skill验证设计架构合规性

### 11.2 实施建议

**执行流程**：
1. 创建feature分支：`feature/patient-management-enhancement`
2. 按Phase顺序实施（Phase 1 → 2 → 3 → 4）
3. 每个Phase完成后：
   - 运行单元测试
   - 运行集成测试
   - 提交代码到feature分支
   - 更新Issue状态
4. 全部Phase完成后：
   - 运行lybtzyzs-quality-reporter
   - 创建Pull Request
   - Code Review
   - 合并到master分支

**团队协作**：
- 开发者：按Phase顺序实施
- Code Reviewer：每个Phase完成后Review
- 测试人员：Phase 3完成后进行手动测试
- 产品负责人：Phase 4完成后验收

---

## 附录A：参考文档

- [患者管理功能完善需求分析](patient-management-enhancement-requirements.md) v1.0
- [Server端三层架构](architecture/server/README.md)
- [Client端MVVM架构](architecture/client/README.md)
- [Shared层架构](architecture/shared/README.md)
- [Epic #1736 InputDto统一模式](../how-to/dto-design-patterns.md)
- [Epic #1773 Validators共享](../how-to/shared-validation.md)
- [Epic #1600 Repository内部可见性](../how-to/repository-design.md)

---

## 附录B：变更历史

| 版本 | 日期 | 作者 | 变更内容 |
|-----|------|------|---------|
| v1.0 | 2025-11-09 | Claude Code | 初始版本：完整技术设计 |

---

**文档状态**：✅ 待审查
**下一步**：调用lybtzyzs-design-arch-validator验证架构合规性
