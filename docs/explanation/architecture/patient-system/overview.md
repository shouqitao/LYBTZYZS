# 患者管理系统架构设计

## 1. 模块概述

### 1.1 业务定位

患者管理系统（Patient Module）是LYBTZYZS系统的**基础数据模块**，负责管理中医诊所的患者档案信息。该模块为诊疗业务提供患者身份识别、基本信息管理和历史档案查询服务，是整个诊疗流程的起点。

### 1.2 核心职责

| 职责类别 | 具体职责 | 技术实现 |
|---------|---------|---------|
| **患者档案管理** | 创建、更新、查询、删除患者基本信息 | CRUD + Soft Delete |
| **智能搜索** | 拼音码搜索、手机号查询、多条件筛选 | 7级拼音码算法 + LINQ |
| **批量数据处理** | Excel批量导入、数据导出、模板下载 | Server主导EPPlus解析 |
| **数据隐私保护** | 敏感信息脱敏、访问控制、操作审计 | 基于角色的数据访问 |
| **数据完整性** | 身份证号验证、手机号去重、年龄自动计算 | FluentValidation验证 |

### 1.3 设计原则

**简单优先原则**
```csharp
// ✅ 正确：Patient模块采用简单直接的CRUD模式
public async Task<Patient> CreateAsync(PatientInputDto dto)
{
    var patient = new Patient
    {
        Id = Guid.NewGuid(),
        Name = dto.Name,
        PinYinCode = GeneratePinYinCode(dto.Name),  // 拼音码自动生成
        Gender = dto.Gender,
        BirthDate = dto.BirthDate,
        Age = CalculateAge(dto.BirthDate)  // 年龄自动计算
    };

    await _repository.AddAsync(patient);
    return patient;
}

// ❌ 错误：过度设计（Patient不需要聚合根模式）
public class PatientAggregate : AggregateRoot<Guid>  // 不必要的复杂度
{
    public virtual ICollection<MedicalHistory> Histories { get; set; }
    public virtual ICollection<AllergyRecord> Allergies { get; set; }
    // 患者档案是简单实体，无需聚合根
}
```

**Server主导批量导入**
```csharp
// ✅ 正确：Server端EPPlus解析Excel（Patients模块特点）
[HttpPost("import")]
public async Task<ActionResult<ApiResponse<BatchImportResultDto>>> Import(IFormFile file)
{
    // Server端解析Excel
    using var stream = file.OpenReadStream();
    using var package = new ExcelPackage(stream);
    var worksheet = package.Workbook.Worksheets[0];

    var patients = new List<Patient>();
    for (int row = 2; row <= worksheet.Dimension.Rows; row++)
    {
        var patient = ParseExcelRow(worksheet, row);  // Server端解析
        patients.Add(patient);
    }

    return await _service.BatchImportAsync(patients);
}

// 对比：Desktop主导模式（Herbs模块）
// Desktop端解析Excel → 发送DTO数组 → Server端保存
```

### 1.4 关键指标

| 指标项 | 数值 | 说明 |
|-------|------|------|
| 实体数量 | 1个核心实体 | Patient（简单实体，非聚合根） |
| API端点数 | 8个 | 5个基础CRUD + 3个批量操作 |
| 搜索算法 | 7级拼音码 | 完全匹配→前缀匹配→包含匹配→模糊匹配 |
| 批量导入性能 | ~240ms/1000条 | Server端EPPlus解析 |
| 分页查询性能 | ~91μs/100条 | InMemory测试，P95<500ms目标的5494倍 |
| 数据隐私 | 3层保护 | 加密存储+脱敏显示+访问审计 |

---

## 2. 三层架构设计

### 2.1 整体架构

```
┌──────────────────────────────────────────────────────────────┐
│                      Desktop Layer (Client)                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  PatientListView.xaml                                  │  │
│  │  PatientListViewModel.cs                               │  │
│  │  PatientImportWizardViewModel.cs (Epic #1934)          │  │
│  │  PatientQueryService.cs (查询服务)                     │  │
│  │  PatientBusinessService.cs (业务服务)                  │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
                              ↓ HTTP (Refit)
┌──────────────────────────────────────────────────────────────┐
│                       Server Layer (API)                      │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  PatientController.cs                                  │  │
│  │    ├─ 基础CRUD（5个端点）                              │  │
│  │    │    POST /patients                                 │  │
│  │    │    GET /patients (分页 + 关键词搜索)              │  │
│  │    │    GET /patients/{id}                             │  │
│  │    │    PUT /patients/{id}                             │  │
│  │    │    DELETE /patients/{id}                          │  │
│  │    ├─ 批量操作（3个端点）                              │  │
│  │    │    POST /patients/import (Server主导)             │  │
│  │    │    GET /patients/import-template (公开端点)       │  │
│  │    │    GET /patients/export (关键词筛选)              │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  PatientService.cs                                     │  │
│  │    ├─ CreateAsync() - 创建患者 + 拼音码生成            │  │
│  │    ├─ UpdateAsync() - 更新患者 + 拼音码更新            │  │
│  │    ├─ GetPagedAsync() - 分页查询 + 关键词搜索          │  │
│  │    ├─ GetByIdAsync() - 单条查询                        │  │
│  │    ├─ DeleteAsync() - 软删除                           │  │
│  │    ├─ BatchImportAsync() - 批量导入（Server端EPPlus）  │  │
│  │    ├─ ExportAsync() - 数据导出                         │  │
│  │    └─ GenerateImportTemplateAsync() - 模板生成         │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  PatientRepository.cs (IRepository<Patient>)           │  │
│  │    ├─ GetPagedAsync() - 分页查询                       │  │
│  │    ├─ SearchByKeywordAsync() - 关键词搜索              │  │
│  │    ├─ GetByPhoneNumberAsync() - 手机号查询             │  │
│  │    └─ CheckPhoneNumberExistsAsync() - 去重验证         │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
                              ↓ EF Core
┌──────────────────────────────────────────────────────────────┐
│                     Database Layer (SQL Server)               │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  Table: Patients (患者表)                              │  │
│  │    PK: Id (Guid)                                       │  │
│  │    UK: PhoneNumber (手机号唯一约束)                     │  │
│  │    Fields:                                             │  │
│  │      - Name (姓名)                                     │  │
│  │      - PinYinCode (拼音码，索引字段)                    │  │
│  │      - Gender (性别)                                   │  │
│  │      - BirthDate (出生日期)                             │  │
│  │      - Age (年龄，计算属性)                             │  │
│  │      - PhoneNumber (手机号，唯一索引)                   │  │
│  │      - Address (地址)                                  │  │
│  │      - Status (状态: 启用/禁用)                         │  │
│  │      - IsDeleted (软删除标志)                           │  │
│  │    Indexes:                                            │  │
│  │      - IX_Patients_PinYinCode (拼音码搜索)             │  │
│  │      - UQ_Patients_PhoneNumber (手机号去重)             │  │
│  │      - IX_Patients_IsDeleted (软删除过滤)               │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

### 2.2 Desktop Layer

#### 2.2.1 查询服务（PatientQueryService）

```csharp
public class PatientQueryService : IPatientQueryService
{
    private readonly IPatientApi _api;

    // 分页查询（支持关键词搜索）
    public async Task<PagedResult<PatientItem>> GetPagedAsync(
        int pageIndex, int pageSize, string? keyword = null)
    {
        var response = await _api.GetPagedAsync(pageIndex, pageSize, keyword);
        return new PagedResult<PatientItem>
        {
            Items = response.Data.Items.Select(p => new PatientItem(p)).ToList(),
            TotalCount = response.Data.TotalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    // 拼音码搜索（7级算法客户端优化）
    public async Task<List<PatientItem>> SearchByPinYinAsync(string pinyin)
    {
        // Desktop端先使用keyword参数查询
        var response = await _api.GetPagedAsync(1, 100, pinyin);
        var patients = response.Data.Items.Select(p => new PatientItem(p)).ToList();

        // 客户端7级拼音码算法排序优化
        return patients
            .Select(p => new
            {
                Patient = p,
                Score = CalculatePinYinScore(pinyin, p.PinYinCode, p.Name)
            })
            .OrderByDescending(x => x.Score)
            .Select(x => x.Patient)
            .ToList();
    }

    // 7级拼音码评分算法
    private double CalculatePinYinScore(string query, string pinyin, string name)
    {
        // Level 1: 完全匹配拼音码（100分）
        if (query.Equals(pinyin, StringComparison.OrdinalIgnoreCase))
            return 100;

        // Level 2: 拼音码前缀匹配（80分）
        if (pinyin.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            return 80;

        // Level 3: 拼音码包含匹配（60分）
        if (pinyin.Contains(query, StringComparison.OrdinalIgnoreCase))
            return 60;

        // Level 4: 姓名拼音匹配（40分）
        var namePinyin = ConvertToPinYin(name);
        if (namePinyin.Contains(query, StringComparison.OrdinalIgnoreCase))
            return 40;

        // Level 5: 姓名前缀拼音匹配（30分）
        if (namePinyin.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            return 30;

        // Level 6: 模糊匹配（编辑距离≤2，20-10分）
        var distance = CalculateLevenshteinDistance(query, pinyin);
        if (distance <= 2)
            return 20 - (distance * 10);

        // Level 7: 无匹配（0分）
        return 0;
    }
}
```

#### 2.2.2 业务服务（PatientBusinessService）

```csharp
public class PatientBusinessService : IPatientBusinessService
{
    private readonly IPatientApi _api;

    // 创建患者（拼音码自动生成在Server端）
    public async Task<ApiResponse<PatientDto>> CreateAsync(PatientInputDto dto)
    {
        // Desktop端仅验证基本数据格式
        if (string.IsNullOrWhiteSpace(dto.Name))
            return ApiResponse<PatientDto>.CreateFail("患者姓名不能为空");

        // 调用Server端API，拼音码在Server端自动生成
        return await _api.CreateAsync(dto);
    }

    // 批量导入（Server主导模式 - Epic #1934）
    public async Task<ApiResponse<BatchImportResultDto>> ImportFromExcelAsync(IFormFile file)
    {
        // Desktop端仅做文件格式验证
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return ApiResponse<BatchImportResultDto>.CreateFail("仅支持.xlsx格式");

        if (file.Length > 10 * 1024 * 1024)
            return ApiResponse<BatchImportResultDto>.CreateFail("文件大小不能超过10MB");

        // 直接上传到Server端，由Server端解析Excel
        return await _api.ImportAsync(file);
    }
}
```

#### 2.2.3 视图模型（PatientListViewModel）

```csharp
public class PatientListViewModel : BindableBase
{
    private readonly IPatientQueryService _queryService;
    private readonly IPatientBusinessService _businessService;

    public ObservableCollection<PatientItem> Patients { get; } = new();

    private string _searchKeyword;
    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            SetProperty(ref _searchKeyword, value);
            // 实时搜索（防抖300ms）
            _searchDebouncer.Debounce(300, async () => await SearchPatientsAsync());
        }
    }

    // 加载患者列表
    public async Task LoadPatientsAsync(int pageIndex = 1, int pageSize = 20)
    {
        var result = await _queryService.GetPagedAsync(pageIndex, pageSize, SearchKeyword);
        Patients.Clear();
        foreach (var item in result.Items)
            Patients.Add(item);

        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;
    }

    // 拼音码智能搜索
    public async Task SearchPatientsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchKeyword))
        {
            await LoadPatientsAsync();
            return;
        }

        var results = await _queryService.SearchByPinYinAsync(SearchKeyword);
        Patients.Clear();
        foreach (var item in results)
            Patients.Add(item);
    }
}
```

### 2.3 Server Layer

#### 2.3.1 Controller层（API端点）

**基础CRUD操作（5个端点）**
```csharp
[Route("api/v1/patients")]
[ApiController]
[Authorize]  // 所有端点需要认证（除import-template）
public class PatientController : ControllerBase
{
    private readonly IPatientService _service;

    // 1. 创建患者
    [HttpPost]
    public async Task<ActionResult<ApiResponse<PatientDto>>> Create(
        [FromBody] PatientInputDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return Ok(ApiResponse<PatientDto>.CreateSuccess(result, "患者创建成功"));
    }

    // 2. 分页查询（支持关键词搜索）
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<PatientDto>>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null)
    {
        var result = await _service.GetPagedAsync(page, pageSize, keyword);
        return Ok(ApiResponse<PagedResult<PatientDto>>.CreateSuccess(result, "查询成功"));
    }

    // 3. 查询单个患者
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PatientDto>>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null)
            return NotFound(ApiResponse<PatientDto>.CreateFail("患者不存在"));

        return Ok(ApiResponse<PatientDto>.CreateSuccess(result, "查询成功"));
    }

    // 4. 更新患者
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<PatientDto>>> Update(
        Guid id, [FromBody] PatientInputDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return Ok(ApiResponse<PatientDto>.CreateSuccess(result, "患者更新成功"));
    }

    // 5. 删除患者（软删除）
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return Ok(ApiResponse.CreateSuccess("删除成功"));
    }
}
```

**批量操作（3个端点 - Epic #1934）**
```csharp
// 6. 批量导入（Server主导EPPlus解析）
[HttpPost("import")]
public async Task<ActionResult<ApiResponse<BatchImportResultDto>>> Import(IFormFile file)
{
    // 文件验证
    if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        return BadRequest(ApiResponse<BatchImportResultDto>.CreateFail("仅支持.xlsx格式"));

    if (file.Length > 10 * 1024 * 1024)
        return BadRequest(ApiResponse<BatchImportResultDto>.CreateFail("文件大小不能超过10MB"));

    // Server端EPPlus解析
    var result = await _service.BatchImportAsync(file);
    return Ok(ApiResponse<BatchImportResultDto>.CreateSuccess(result, "导入完成"));
}

// 7. 下载导入模板（公开端点，无需认证）
[HttpGet("import-template")]
[AllowAnonymous]  // 无需认证
public async Task<IActionResult> GetImportTemplate()
{
    var fileBytes = await _service.GenerateImportTemplateAsync();
    var fileName = $"患者导入模板_{DateTime.Now:yyyyMMdd}.xlsx";

    return File(fileBytes,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        fileName);
}

// 8. 导出患者数据
[HttpGet("export")]
public async Task<IActionResult> Export([FromQuery] string? keyword = null)
{
    var fileBytes = await _service.ExportAsync(keyword);
    var fileName = string.IsNullOrWhiteSpace(keyword)
        ? $"患者数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        : $"患者数据_{keyword}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

    return File(fileBytes,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        fileName);
}
```

#### 2.3.2 Service层（业务逻辑）

```csharp
public class PatientService : IPatientService
{
    private readonly IRepository<Patient> _repository;
    private readonly IPinYinService _pinYinService;

    // 创建患者（BR-002: 拼音码自动生成）
    public async Task<PatientDto> CreateAsync(PatientInputDto dto)
    {
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Gender = dto.Gender,
            BirthDate = dto.BirthDate,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            // BR-002: 拼音码自动生成
            PinYinCode = _pinYinService.GeneratePinYinCode(dto.Name),
            // 年龄自动计算
            Age = CalculateAge(dto.BirthDate),
            Status = PatientStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(patient);
        return MapToDto(patient);
    }

    // 批量导入（Server端EPPlus解析 - Epic #1934 FR-001）
    public async Task<BatchImportResultDto> BatchImportAsync(IFormFile file)
    {
        var result = new BatchImportResultDto
        {
            FailureDetails = new List<FailureDetail>()
        };

        // Server端EPPlus解析Excel
        using var stream = file.OpenReadStream();
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets[0];

        var patients = new List<Patient>();
        var existingPhones = new HashSet<string>();

        // 逐行解析和验证
        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
        {
            try
            {
                var name = worksheet.Cells[row, 1].Text;
                var genderText = worksheet.Cells[row, 2].Text;
                var birthDateText = worksheet.Cells[row, 3].Text;
                var phoneNumber = worksheet.Cells[row, 4].Text;
                var address = worksheet.Cells[row, 5].Text;

                // 数据验证
                if (string.IsNullOrWhiteSpace(name))
                {
                    result.FailureDetails.Add(new FailureDetail
                    {
                        RowNumber = row,
                        PatientName = name,
                        PhoneNumber = phoneNumber,
                        Reason = "姓名不能为空"
                    });
                    result.FailureCount++;
                    continue;
                }

                // BR-005: 手机号去重
                if (!string.IsNullOrWhiteSpace(phoneNumber))
                {
                    if (existingPhones.Contains(phoneNumber) ||
                        await _repository.CheckPhoneNumberExistsAsync(phoneNumber))
                    {
                        result.FailureDetails.Add(new FailureDetail
                        {
                            RowNumber = row,
                            PatientName = name,
                            PhoneNumber = phoneNumber,
                            Reason = "手机号已存在"
                        });
                        result.SkippedCount++;
                        continue;
                    }
                    existingPhones.Add(phoneNumber);
                }

                var patient = new Patient
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Gender = ParseGender(genderText),
                    BirthDate = DateTime.Parse(birthDateText),
                    PhoneNumber = phoneNumber,
                    Address = address,
                    PinYinCode = _pinYinService.GeneratePinYinCode(name),
                    Age = CalculateAge(DateTime.Parse(birthDateText)),
                    Status = PatientStatus.Active
                };

                patients.Add(patient);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailureDetails.Add(new FailureDetail
                {
                    RowNumber = row,
                    Reason = ex.Message
                });
                result.FailureCount++;
            }
        }

        result.TotalCount = worksheet.Dimension.Rows - 1;

        // 批量保存
        if (patients.Any())
        {
            await _repository.AddRangeAsync(patients);
        }

        return result;
    }

    // 生成导入模板（Epic #1934 FR-002）
    public async Task<byte[]> GenerateImportTemplateAsync()
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("患者导入模板");

        // 列头
        worksheet.Cells[1, 1].Value = "姓名";
        worksheet.Cells[1, 2].Value = "性别";
        worksheet.Cells[1, 3].Value = "出生日期";
        worksheet.Cells[1, 4].Value = "手机号";
        worksheet.Cells[1, 5].Value = "地址";

        // 示例数据（3行）
        worksheet.Cells[2, 1].Value = "张三";
        worksheet.Cells[2, 2].Value = "男";
        worksheet.Cells[2, 3].Value = "1980/5/15";
        worksheet.Cells[2, 4].Value = "13800138000";
        worksheet.Cells[2, 5].Value = "北京市朝阳区XX路XX号";

        worksheet.Cells[3, 1].Value = "李四";
        worksheet.Cells[3, 2].Value = "女";
        worksheet.Cells[3, 3].Value = "1985/8/20";
        worksheet.Cells[3, 4].Value = "13900139000";
        worksheet.Cells[3, 5].Value = "上海市浦东新区YY路YY号";

        worksheet.Cells[4, 1].Value = "王五";
        worksheet.Cells[4, 2].Value = "未知";
        worksheet.Cells[4, 3].Value = "1990/12/25";
        worksheet.Cells[4, 4].Value = "13700137000";
        worksheet.Cells[4, 5].Value = "广州市天河区ZZ路ZZ号";

        // 列宽自动调整
        worksheet.Cells.AutoFitColumns();

        return await package.GetAsByteArrayAsync();
    }

    // 年龄计算
    private int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}
```

#### 2.3.3 Repository层（数据访问）

```csharp
public class PatientRepository : Repository<Patient>, IRepository<Patient>
{
    public PatientRepository(ApplicationDbContext context) : base(context) { }

    // 分页查询（支持关键词搜索）
    public async Task<PagedResult<Patient>> GetPagedAsync(
        int pageIndex, int pageSize, string? keyword = null)
    {
        var query = _dbSet.Where(p => !p.IsDeleted);  // BR-001: 软删除过滤

        // 关键词搜索（姓名、手机号、拼音码模糊匹配）
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(p =>
                p.Name.Contains(keyword) ||
                p.PhoneNumber.Contains(keyword) ||
                p.PinYinCode.Contains(keyword));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Patient>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    // 手机号去重检查（BR-005）
    public async Task<bool> CheckPhoneNumberExistsAsync(string phoneNumber)
    {
        return await _dbSet.AnyAsync(p =>
            p.PhoneNumber == phoneNumber &&
            !p.IsDeleted);
    }

    // 按手机号查询
    public async Task<Patient?> GetByPhoneNumberAsync(string phoneNumber)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p =>
                p.PhoneNumber == phoneNumber &&
                !p.IsDeleted);
    }
}
```

### 2.4 Database Layer

#### 2.4.1 表结构设计

**Patients表**
```sql
CREATE TABLE [dbo].[Patients]
(
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(50) NOT NULL,              -- 患者姓名
    [PinYinCode] NVARCHAR(20) NULL,            -- 拼音码（自动生成）
    [Gender] INT NOT NULL DEFAULT 0,            -- 性别（0=未知, 1=男, 2=女）
    [BirthDate] DATE NOT NULL,                  -- 出生日期
    [Age] INT NULL,                             -- 年龄（计算属性）
    [PhoneNumber] NVARCHAR(11) NULL,            -- 手机号（唯一约束）
    [Address] NVARCHAR(200) NULL,               -- 地址
    [Status] INT NOT NULL DEFAULT 1,            -- 状态（1=启用, 2=禁用）
    [CreatedAt] DATETIME2 NOT NULL,
    [CreatedBy] NVARCHAR(50) NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] NVARCHAR(50) NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,         -- 软删除标志

    -- 唯一约束：手机号去重（BR-005）
    CONSTRAINT [UQ_Patients_PhoneNumber] UNIQUE ([PhoneNumber])
        WHERE [IsDeleted] = 0 AND [PhoneNumber] IS NOT NULL,

    -- 索引：拼音码搜索
    INDEX [IX_Patients_PinYinCode] ([PinYinCode])
        WHERE [IsDeleted] = 0,

    -- 索引：软删除过滤
    INDEX [IX_Patients_IsDeleted] ([IsDeleted])
        INCLUDE ([Name], [PhoneNumber], [PinYinCode])
)
```

#### 2.4.2 EF Core实体配置

```csharp
public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");
        builder.HasKey(p => p.Id);

        // 字段配置
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.PinYinCode)
            .HasMaxLength(20);

        builder.Property(p => p.PhoneNumber)
            .HasMaxLength(11);

        builder.Property(p => p.Address)
            .HasMaxLength(200);

        // 唯一约束：手机号（BR-005）
        builder.HasIndex(p => p.PhoneNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0 AND [PhoneNumber] IS NOT NULL");

        // 索引：拼音码搜索
        builder.HasIndex(p => p.PinYinCode)
            .HasFilter("[IsDeleted] = 0");

        // 全局查询过滤器：软删除（BR-001）
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
```

---

## 3. 核心领域模型

### 3.1 Patient实体

```csharp
/// <summary>
/// 患者实体 - 简单实体模式（非聚合根）
/// </summary>
public class Patient : BaseEntity
{
    // === 基本信息 ===
    public string Name { get; set; } = string.Empty;         // 患者姓名
    public string? PinYinCode { get; set; }                   // 拼音码（BR-002自动生成）
    public Gender Gender { get; set; }                        // 性别
    public DateTime BirthDate { get; set; }                   // 出生日期
    public int Age { get; set; }                              // 年龄（自动计算）

    // === 联系方式 ===
    public string? PhoneNumber { get; set; }                  // 手机号（BR-005唯一约束）
    public string? Address { get; set; }                      // 地址

    // === 状态管理 ===
    public PatientStatus Status { get; set; } = PatientStatus.Active;  // 患者状态

    // === 计算属性 ===
    public bool IsActive => Status == PatientStatus.Active && !IsDeleted;
    public bool IsAdult => Age >= 18;
    public string GenderName => Gender switch
    {
        Gender.Male => "男",
        Gender.Female => "女",
        _ => "未知"
    };

    // === 业务方法 ===

    /// <summary>
    /// 更新年龄（根据出生日期自动计算）
    /// </summary>
    public void UpdateAge()
    {
        var today = DateTime.Today;
        Age = today.Year - BirthDate.Year;
        if (BirthDate.Date > today.AddYears(-Age)) Age--;
    }

    /// <summary>
    /// 更新拼音码（姓名变更时）
    /// </summary>
    public void UpdatePinYinCode(IPinYinService pinYinService)
    {
        PinYinCode = pinYinService.GeneratePinYinCode(Name);
    }
}
```

### 3.2 Gender枚举

```csharp
/// <summary>
/// 性别枚举
/// </summary>
public enum Gender
{
    /// <summary>
    /// 未知
    /// </summary>
    [Description("未知")]
    Unknown = 0,

    /// <summary>
    /// 男
    /// </summary>
    [Description("男")]
    Male = 1,

    /// <summary>
    /// 女
    /// </summary>
    [Description("女")]
    Female = 2
}
```

### 3.3 PatientStatus枚举

```csharp
/// <summary>
/// 患者状态枚举
/// </summary>
public enum PatientStatus
{
    /// <summary>
    /// 禁用 - 无法就诊
    /// </summary>
    [Description("禁用")]
    Disabled = 0,

    /// <summary>
    /// 启用 - 正常状态，可以就诊
    /// </summary>
    [Description("启用")]
    Active = 1,

    /// <summary>
    /// 已故 - 已归档
    /// </summary>
    [Description("已故")]
    Deceased = 2,

    /// <summary>
    /// 转院 - 已转出
    /// </summary>
    [Description("转院")]
    Transferred = 3
}
```

### 3.4 DTO设计

**PatientInputDto（输入DTO）**
```csharp
public class PatientInputDto
{
    [Required(ErrorMessage = "患者姓名不能为空")]
    [MaxLength(50, ErrorMessage = "患者姓名不能超过50个字符")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "性别不能为空")]
    public Gender Gender { get; set; }

    [Required(ErrorMessage = "出生日期不能为空")]
    [DataType(DataType.Date)]
    public DateTime BirthDate { get; set; }

    [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "手机号格式不正确")]
    public string? PhoneNumber { get; set; }

    [MaxLength(200, ErrorMessage = "地址不能超过200个字符")]
    public string? Address { get; set; }
}
```

**PatientDto（输出DTO）**
```csharp
public class PatientDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Gender { get; set; }
    public string GenderName { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public int Age { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? PinYinCode { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
```

**BatchImportResultDto（批量导入结果DTO - Epic #1934）**
```csharp
public class BatchImportResultDto
{
    public int TotalCount { get; set; }           // 总记录数
    public int SuccessCount { get; set; }         // 成功导入数量
    public int FailureCount { get; set; }         // 失败数量
    public int SkippedCount { get; set; }         // 跳过数量（重复手机号）
    public List<FailureDetail> FailureDetails { get; set; } = new();  // 失败详情
}

public class FailureDetail
{
    public int RowNumber { get; set; }            // Excel行号
    public string? PatientName { get; set; }      // 患者姓名
    public string? PhoneNumber { get; set; }      // 手机号
    public string Reason { get; set; } = string.Empty;  // 失败原因
}
```

---

## 4. 业务规则体系

### 4.1 软删除机制（BR-001）

**规则定义**
```markdown
**规则代码**: BR-001
**规则名称**: 软删除机制
**规则描述**: 所有删除操作为软删除，设置IsDeleted=true，查询时自动过滤已删除记录
**技术实现**: EF Core全局查询过滤器 + 仓储层Where条件
```

**实现方式**
```csharp
// EF Core全局过滤器
builder.HasQueryFilter(p => !p.IsDeleted);

// 删除操作
public async Task DeleteAsync(Guid id)
{
    var patient = await _repository.GetByIdAsync(id);
    if (patient == null)
        throw new NotFoundException("患者不存在");

    patient.IsDeleted = true;
    patient.UpdatedAt = DateTime.UtcNow;
    await _repository.UpdateAsync(patient);
}
```

### 4.2 拼音码自动生成（BR-002）

**规则定义**
```markdown
**规则代码**: BR-002
**规则名称**: 拼音码自动生成
**规则描述**: 创建或更新患者时，自动根据姓名生成拼音码
**生成规则**: 取姓名每个汉字的首字母大写拼接
**示例**: 张三 → ZS, 李明华 → LMH
```

**实现方式**
```csharp
public class PinYinService : IPinYinService
{
    public string GeneratePinYinCode(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var code = new StringBuilder();
        foreach (var ch in name)
        {
            if (ch >= 0x4E00 && ch <= 0x9FA5)  // 汉字Unicode范围
            {
                var pinyin = Pinyin.GetPinyin(ch)[0].ToString().ToUpper();
                code.Append(pinyin);
            }
        }
        return code.ToString();
    }
}
```

### 4.3 Server端Excel解析（BR-003 - Epic #1934）

**规则定义**
```markdown
**规则代码**: BR-003
**规则名称**: Server端Excel解析
**规则描述**: 批量导入功能由Server端负责Excel文件解析
**技术实现**: EPPlus库，支持.xlsx格式，文件大小限制≤10MB
**优势**: 降低Desktop端复杂度，统一数据验证逻辑
```

**对比Desktop主导模式（Herbs模块）**

| 对比项 | Server主导（Patients） | Desktop主导（Herbs） |
|-------|----------------------|-------------------|
| Excel解析位置 | Server端（EPPlus） | Desktop端 |
| 数据传输方式 | IFormFile上传 | DTO数组传输 |
| 适用场景 | 简单业务规则 | 复杂业务规则 |
| 性能 | 网络传输小，解析在Server | 网络传输大，解析在Desktop |
| Desktop复杂度 | 简单（仅上传文件） | 复杂（解析+验证） |
| Server复杂度 | 复杂（解析+验证+保存） | 简单（仅保存） |

**选择依据**:
- Patient数据结构简单（5个字段），无复杂业务规则 → Server主导
- Herb数据结构复杂（药材关系、配伍禁忌），需Desktop端预处理 → Desktop主导

### 4.4 失败恢复机制（BR-004 - Epic #1934）

**规则定义**
```markdown
**规则代码**: BR-004
**规则名称**: 失败恢复机制
**规则描述**: 批量导入失败时，提供详细失败信息供用户修正后重新导入
**失败信息**: Excel行号、患者姓名、手机号、失败原因
```

**常见失败原因**:
1. 必填字段缺失（姓名、性别、出生日期）
2. 手机号格式错误（非11位数字）
3. 手机号重复（数据库已存在或当前批次重复）
4. 数据类型错误（出生日期格式错误）

### 4.5 数据验证与去重（BR-005）

**规则定义**
```markdown
**规则代码**: BR-005
**规则名称**: 数据验证与去重
**规则描述**: 批量导入时逐行验证数据，手机号重复则跳过
**验证项**:
  - 姓名: 必填，1-50字符
  - 性别: 必填，有效枚举值（0/1/2）
  - 出生日期: 必填，有效日期范围（过去150年内）
  - 手机号: 可选，11位数字且不重复
  - 地址: 可选，最大200字符
**去重策略**: 检查手机号是否已存在（数据库 + 当前批次）
```

**验证实现**
```csharp
// FluentValidation验证器
public class PatientInputDtoValidator : AbstractValidator<PatientInputDto>
{
    public PatientInputDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("患者姓名不能为空")
            .MaximumLength(50).WithMessage("患者姓名不能超过50个字符");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("性别值必须为有效的枚举值");

        RuleFor(x => x.BirthDate)
            .NotEmpty().WithMessage("出生日期不能为空")
            .LessThan(DateTime.Now).WithMessage("出生日期不能晚于当前日期")
            .GreaterThan(DateTime.Now.AddYears(-150))
            .WithMessage("出生日期不能早于150年前");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^1[3-9]\d{9}$")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("手机号格式不正确");
    }
}
```

---

## 5. 数据流与交互

### 5.1 患者创建流程

```
Desktop客户端
    │
    │ 填写患者基本信息表单
    ↓
PatientBusinessService.CreateAsync(dto)
    │
    ↓ 基础验证（姓名非空）
    │
    ↓ POST /api/v1/patients
    ↓
PatientController.Create()
    │
    ↓ FluentValidation验证（PatientInputDtoValidator）
    │
    ↓ PatientService.CreateAsync()
    │
    ↓ BR-002: 拼音码自动生成
    │  PinYinCode = GeneratePinYinCode(dto.Name)
    │
    ↓ 年龄自动计算
    │  Age = CalculateAge(dto.BirthDate)
    │
    ↓ Repository.AddAsync()
    │
    ↓ EF Core SaveChanges
    │
    ↓ Response: PatientDto
```

### 5.2 拼音码搜索流程（7级算法）

```
Desktop客户端
    │
    │ 输入拼音码"ZS"（防抖300ms）
    ↓
PatientQueryService.SearchByPinYinAsync("ZS")
    │
    ↓ GET /api/v1/patients?keyword=ZS&page=1&pageSize=100
    ↓
PatientController.GetPaged()
    │
    ↓ PatientService.GetPagedAsync()
    │
    ↓ Repository.GetPagedAsync()
    │   WHERE (Name LIKE '%ZS%' OR PhoneNumber LIKE '%ZS%' OR PinYinCode LIKE '%ZS%')
    │   AND IsDeleted = 0
    │
    ↓ Response: List<PatientDto>（返回Desktop）
    │
    ↓ Desktop端7级拼音码算法排序
    │   Level 1: 完全匹配拼音码 → ZS (100分)
    │   Level 2: 拼音码前缀匹配 → ZSF (80分)
    │   Level 3: 拼音码包含匹配 → LZSK (60分)
    │   Level 4: 姓名拼音匹配 → 张三 (40分)
    │   Level 5: 姓名前缀拼音匹配 (30分)
    │   Level 6: 模糊匹配（编辑距离≤2）(20-10分)
    │   Level 7: 无匹配 (0分)
    │
    ↓ 按评分排序返回结果
```

### 5.3 批量导入流程（Server主导 - Epic #1934）

```
Desktop客户端
    │
    │ 选择Excel文件（患者数据.xlsx）
    ↓
PatientBusinessService.ImportFromExcelAsync(file)
    │
    ↓ 文件格式验证（.xlsx, ≤10MB）
    │
    ↓ POST /api/v1/patients/import (multipart/form-data)
    ↓
PatientController.Import(IFormFile)
    │
    ↓ 文件验证
    │
    ↓ PatientService.BatchImportAsync(file)
    │
    ↓ Server端EPPlus解析Excel
    │  using var package = new ExcelPackage(stream);
    │  var worksheet = package.Workbook.Worksheets[0];
    │
    ↓ 逐行解析和验证（for row = 2 to Rows）
    │   - 提取5列数据（姓名、性别、出生日期、手机号、地址）
    │   - FluentValidation验证
    │   - BR-005: 手机号去重检查
    │   - BR-002: 拼音码自动生成
    │   - 年龄自动计算
    │
    ↓ 构建Patient实体列表
    │  List<Patient> patients = ...
    │
    ↓ Repository.AddRangeAsync(patients)
    │
    ↓ EF Core SaveChanges（批量插入）
    │
    ↓ Response: BatchImportResultDto
    │  - TotalCount: 1000
    │  - SuccessCount: 985
    │  - FailureCount: 10
    │  - SkippedCount: 5
    │  - FailureDetails: [行号, 姓名, 手机号, 失败原因]
```

### 5.4 模块间交互

**Patient → MedicalCase（N:1引用）**
```csharp
// MedicalCase需要Patient信息
public class MedicalCase
{
    public Guid PatientId { get; set; }
    public virtual Patient Patient { get; set; } = null!;
}

// 创建病案时验证Patient存在
var patient = await _patientRepository.GetByIdAsync(dto.PatientId);
if (patient == null)
    throw new NotFoundException("患者不存在");
```

**Patient → Consultation（间接关联）**
```csharp
// Consultation通过MedicalCase关联Patient
var medicalCase = await _medicalCaseRepository.GetByIdWithDetailsAsync(id);
Console.WriteLine($"患者: {medicalCase.Patient.Name}");
Console.WriteLine($"诊断: {medicalCase.Consultation.TCMDiagnosis}");
```

---

## 6. 技术决策

### 6.1 Server主导批量导入（Epic #1934）

**决策内容**: Patient模块采用Server端主导的Excel解析模式

**选择理由**:
1. **业务简单**: Patient数据结构简单（5个字段），无复杂业务规则
2. **降低Desktop复杂度**: Desktop仅需上传文件，无需集成Excel库
3. **统一验证逻辑**: Server端统一验证规则，避免Desktop/Server重复验证
4. **更好的错误处理**: Server端日志记录更完善

**对比Desktop主导模式（Herbs模块）**:

```csharp
// ✅ Patients模块 - Server主导
[HttpPost("import")]
public async Task<ActionResult> Import(IFormFile file)
{
    // Server端EPPlus解析
    using var stream = file.OpenReadStream();
    using var package = new ExcelPackage(stream);
    var patients = ParseExcel(package);  // Server端解析
    return await _service.BatchImportAsync(patients);
}

// 对比：Herbs模块 - Desktop主导
[HttpPost("batch-import")]
public async Task<ActionResult> BatchImport([FromBody] List<HerbInputDto> herbs)
{
    // Desktop端已解析，Server端仅保存
    return await _service.BatchImportAsync(herbs);
}
```

### 6.2 7级拼音码搜索算法

**决策内容**: 客户端实现7级拼音码算法优化搜索结果排序

**技术理由**:
1. **提升用户体验**: 最相关结果排在最前面
2. **降低Server负担**: 粗排在Server（LIKE查询），精排在Desktop
3. **灵活调整**: Desktop端可根据用户反馈调整评分权重

**算法实现**:
```csharp
Level 1: 完全匹配拼音码 → 100分
Level 2: 拼音码前缀匹配 → 80分
Level 3: 拼音码包含匹配 → 60分
Level 4: 姓名拼音匹配 → 40分
Level 5: 姓名前缀拼音匹配 → 30分
Level 6: 模糊匹配（编辑距离≤2）→ 20-10分
Level 7: 无匹配 → 0分
```

### 6.3 手机号唯一约束（BR-005）

**决策内容**: 手机号添加唯一约束，防止重复患者

**业务理由**:
1. **患者识别**: 手机号是重要的患者识别手段
2. **防止重复档案**: 避免同一患者创建多个档案
3. **支持快速查询**: 手机号索引提升查询性能

**实现方式**:
```sql
-- 唯一约束（排除已删除和空手机号）
CONSTRAINT [UQ_Patients_PhoneNumber] UNIQUE ([PhoneNumber])
    WHERE [IsDeleted] = 0 AND [PhoneNumber] IS NOT NULL
```

### 6.4 年龄自动计算

**决策内容**: 年龄字段自动计算并存储，而非每次查询时计算

**性能理由**:
1. **避免重复计算**: 分页查询100条患者时，避免100次年龄计算
2. **支持年龄范围筛选**: `WHERE Age BETWEEN 30 AND 50`直接使用索引
3. **简化Desktop逻辑**: Desktop无需计算年龄

**实现方式**:
```csharp
// 创建时自动计算
patient.Age = CalculateAge(dto.BirthDate);

// 更新时重新计算（如果出生日期变更）
if (dto.BirthDate != patient.BirthDate)
{
    patient.BirthDate = dto.BirthDate;
    patient.UpdateAge();
}
```

---

## 7. 模块依赖关系

### 7.1 依赖图

```
                    ┌───────────────┐
                    │ MedicalCase   │
                    │   Module      │
                    └───────────────┘
                            ↓ FK: PatientId (N:1)
                    ┌───────────────┐
                    │   Patient     │
                    │   Module      │
                    │  (基础模块)    │
                    └───────────────┘
                            ↑ 被引用
                    ┌───────────────┐
                    │ Prescription  │
                    │   Module      │
                    └───────────────┘
                            ↑ 通过MedicalCase间接引用
                    ┌───────────────┐
                    │ Consultation  │
                    │   Module      │
                    └───────────────┘
```

### 7.2 模块间依赖说明

| 依赖方向 | 关系类型 | 说明 | 技术实现 |
|---------|---------|------|---------|
| MedicalCase → Patient | N:1外键 | 病案必须关联患者 | FK: PatientId (NOT NULL) |
| Prescription → Patient | 间接引用 | 处方通过MedicalCase关联患者 | MedicalCase.PatientId |
| Consultation → Patient | 间接引用 | 诊断通过MedicalCase关联患者 | MedicalCase.PatientId |

**无循环依赖**: Patient作为基础模块，仅被其他模块引用，不依赖任何业务模块

---

## 8. 扩展性设计

### 8.1 数据隐私扩展

**脱敏策略扩展点**
```csharp
public interface IDataMaskingStrategy
{
    string MaskPhoneNumber(string phoneNumber, UserRole viewerRole);
    string MaskAddress(string address, UserRole viewerRole);
}

// 扩展：不同角色不同脱敏程度
public class PatientDataMaskingStrategy : IDataMaskingStrategy
{
    public string MaskPhoneNumber(string phoneNumber, UserRole role)
    {
        return role switch
        {
            UserRole.Nurse => $"{phoneNumber.Substring(0, 3)}****{phoneNumber.Substring(7)}",
            UserRole.Doctor => phoneNumber,  // 医生看完整手机号
            UserRole.Admin => phoneNumber,
            _ => "***********"
        };
    }
}
```

### 8.2 搜索算法扩展

**拼音码算法扩展点**
```csharp
public interface IPinYinSearchAlgorithm
{
    double CalculateScore(string query, Patient patient);
}

// 扩展：AI智能搜索算法
public class AIPinYinSearchAlgorithm : IPinYinSearchAlgorithm
{
    public double CalculateScore(string query, Patient patient)
    {
        // 使用机器学习模型评分
        var features = ExtractFeatures(query, patient);
        return _mlModel.Predict(features);
    }
}
```

### 8.3 批量导入扩展

**导入源扩展点**
```csharp
public interface IPatientImportProvider
{
    Task<List<Patient>> ParseAsync(Stream stream);
}

// 当前：Excel导入
public class ExcelImportProvider : IPatientImportProvider { }

// 扩展：CSV导入
public class CsvImportProvider : IPatientImportProvider { }

// 扩展：第三方系统API导入
public class ExternalSystemImportProvider : IPatientImportProvider { }
```

### 8.4 性能优化扩展点

**缓存策略**
```csharp
public class PatientCachingService : IPatientService
{
    private readonly IPatientService _innerService;
    private readonly IMemoryCache _cache;

    public async Task<PatientDto?> GetByIdAsync(Guid id)
    {
        var cacheKey = $"patient:{id}";

        if (_cache.TryGetValue(cacheKey, out PatientDto? cachedPatient))
            return cachedPatient;

        var patient = await _innerService.GetByIdAsync(id);
        if (patient != null)
        {
            _cache.Set(cacheKey, patient, TimeSpan.FromMinutes(10));
        }
        return patient;
    }
}
```

**读写分离**
```csharp
// 写操作：主库
public class PatientWriteRepository : IPatientWriteRepository
{
    private readonly ApplicationDbContext _writeContext;
}

// 读操作：只读副本
public class PatientReadRepository : IPatientReadRepository
{
    private readonly ApplicationReadOnlyDbContext _readContext;
}
```

---

## 总结

患者管理系统是LYBTZYZS的**基础数据模块**，通过**简单直接的CRUD模式**、**Server主导的批量导入**和**7级拼音码智能搜索**为诊疗业务提供患者档案管理服务。模块采用**软删除机制**保障数据安全，通过**拼音码自动生成**和**手机号唯一约束**确保数据完整性，为中医诊所提供高效、安全、易用的患者管理功能。
