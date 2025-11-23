# LYBT.Module.Patients - 患者档案管理模块

## 📦 项目定位

- **层级**:Server端
- **类型**:业务模块(患者档案管理)
- **职责**:提供患者档案的完整生命周期管理,包括基本信息维护、就诊历史追踪、健康信息记录、手机号唯一性验证、Excel导入导出等功能。作为医疗案例系统的基础数据源,患者档案是所有诊疗流程的起点。采用标准三层架构（Controller → Service → Repository）,确保业务逻辑清晰、数据访问高效。

##  代码结构

```
LYBT.Module.Patients/
├── PatientsModule.cs                  # 模块依赖注入注册
│   └── AddPatientsModule()            # 依赖注入配置(仓储+服务+验证器)
├── Interfaces/                        # 模块接口定义
│   └── IPatientRepository.cs          # 患者仓储接口(9个方法)
├── Services/                          # 业务逻辑实现
│   └── PatientService.cs              # 患者服务(9个方法)
│       ├── GetPagedAsync()            # 分页查询患者
│       ├── GetByIdAsync()             # 按ID查询患者详情
│       ├── CreateAsync()              # 创建患者档案
│       ├── UpdateAsync()              # 更新患者档案
│       ├── SearchAsync()              # 搜索患者(按姓名/手机)
│       ├── DeleteAsync()              # 删除患者(软删除)
│       ├── ImportFromExcelAsync()     # Excel导入患者
│       └── GenerateImportTemplate()   # 生成Excel导入模板
├── Repositories/                      # 数据仓储实现
│   └── PatientRepository.cs           # 患者仓储(9个方法+2个辅助类)
│       ├── GetByNameAsync()           # 按姓名精确查询
│       ├── GetPatientWithVisitsAsync() # 查询患者及就诊历史
│       ├── GetPatientSummariesAsync() # 获取患者摘要列表
│       ├── SearchPatientsAsync()      # 搜索患者(多条件)
│       ├── GetPatientsByIdsAsync()    # 按ID列表批量查询
│       ├── PhoneNumberExistsAsync()   # 检查手机号是否存在
│       ├── GetStatisticsAsync()       # 获取患者统计信息
│       ├── UpdateLastVisitDateAsync() # 更新最后就诊日期
│       ├── PatientSummary             # 患者摘要类(辅助)
│       └── PatientStatistics          # 患者统计类(辅助)
├── Validators/                        # FluentValidation验证器
│   ├── PatientCreateDtoValidator.cs   # 创建患者DTO验证
│   └── PatientUpdateDtoValidator.cs   # 更新患者DTO验证
└── Mapping/                           # AutoMapper映射配置
    └── PatientMappingProfile.cs       # Entity ↔ DTO映射规则
```

**说明**:
- **PatientsModule**:依赖注入注册中心,统一注册仓储、服务和验证器
- **PatientService**:9个方法覆盖患者档案的增删改查、搜索、批量导入导出等功能
- **PatientRepository**:9个方法提供多维度数据查询能力(姓名、手机、就诊历史、统计等)
- **Validators**:FluentValidation验证器确保DTO数据完整性（姓名必填、手机号格式、年龄范围）
- **Mapping**:AutoMapper配置统一处理Entity与DTO的转换
- **辅助类**:PatientSummary和PatientStatistics提供聚合查询结果

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Entities** - 数据实体定义(PatientModel)
2. **LYBT.Infrastructure** - 基础设施(AppDbContext、BaseRepository)
3. **LYBT.Shared.Models** - 共享DTO模型(PatientDto、CreatePatientDto等)
4. **LYBT.Server.Interfaces** - Server端接口定义(IPatientService、IPatientRepository)

### 被依赖项目
1. **LYBT.Module.Prescriptions** - 处方模块依赖患者信息
2. **LYBT.WebAPI** - Web服务层通过PatientsController暴露API
3. **测试项目**:
   - LYBT.Module.Patients.Tests（单元测试）
   - LYBT.Server.ArchTests（架构测试）
   - LYBT.Shared.Models.Tests（DTO测试）

### NuGet包
- **FluentValidation** (11.x) - DTO验证框架
- **AutoMapper** (13.x) - 对象映射框架
- **Microsoft.Extensions.DependencyInjection** (8.0.x) - 依赖注入容器

## 🛠 技术栈

- **.NET 8**: 基础框架
- **Entity Framework Core 8**: 通过Repository模式间接使用,用于数据持久化
- **AutoMapper 13.x**: Entity与DTO之间的自动映射
- **FluentValidation 11.x**: DTO数据验证框架
- **LINQ**: 复杂查询表达式(分页、搜索、统计)
- **异步编程**: 全异步方法(async/await),提升性能

##  快速开始

此项目是一个类库,作为后端服务的一部分被 `LYBT.WebAPI` 项目引用和托管。无法独立运行。

```bash
# 构建此项目
dotnet build src/Server/Modules/LYBT.Module.Patients/LYBT.Module.Patients.csproj
```

**集成说明**:

### 1. 注册患者模块(在Startup.cs中)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 注册患者模块(自动注册仓储+服务+验证器)
        services.AddPatientsModule();
    }
}
```

### 2. API Controller集成(在LYBT.WebAPI中)
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    // 分页查询患者
    [HttpGet]
    public async Task<IActionResult> GetPatients(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null)
    {
        var result = await _patientService.GetPagedAsync(
            pageIndex, pageSize, searchTerm
        );
        return Ok(result);
    }

    // 创建患者
    [HttpPost]
    public async Task<IActionResult> CreatePatient([FromBody] CreatePatientDto dto)
    {
        var patientDto = await _patientService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetPatientById), new { id = patientDto.Id }, patientDto);
    }
}
```

### 3. 患者档案创建与验证
```csharp
public class PatientService : IPatientService
{
    public async Task<PatientDto> CreateAsync(CreatePatientDto dto)
    {
        // 验证手机号唯一性
        if (!string.IsNullOrEmpty(dto.PhoneNumber))
        {
            var phoneExists = await _repository.PhoneNumberExistsAsync(dto.PhoneNumber);
            if (phoneExists)
            {
                throw new InvalidOperationException("手机号已存在");
            }
        }

        // 创建患者实体
        var patient = _mapper.Map<PatientModel>(dto);
        patient.Id = Guid.NewGuid();
        patient.CreatedAt = DateTime.Now;

        // 保存到数据库
        await _repository.AddAsync(patient);
        return _mapper.Map<PatientDto>(patient);
    }
}

// FluentValidation验证器
public class PatientCreateDtoValidator : AbstractValidator<CreatePatientDto>
{
    public PatientCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("患者姓名不能为空")
            .MaximumLength(50).WithMessage("患者姓名长度不能超过50字符");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("性别值无效");

        RuleFor(x => x.Age)
            .InclusiveBetween(0, 150).WithMessage("年龄必须在0-150之间");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^1[3-9]\d{9}$").When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("手机号码格式不正确");
    }
}
```

### 4. 就诊历史查询(关联MedicalCase)
```csharp
public class PatientRepository : BaseRepository<PatientModel>, IPatientRepository
{
    // 查询患者及其就诊历史
    public async Task<PatientModel?> GetPatientWithVisitsAsync(Guid patientId)
    {
        return await _dbSet
            .Include(p => p.MedicalCases)           // 包含医疗案例
                .ThenInclude(mc => mc.Consultation) // 包含诊断记录
            .Include(p => p.MedicalCases)
                .ThenInclude(mc => mc.Prescriptions) // 包含处方
            .FirstOrDefaultAsync(p => p.Id == patientId);
    }

    // 获取患者摘要列表(优化的轻量级查询)
    public async Task<List<PatientSummary>> GetPatientSummariesAsync(
        int pageIndex,
        int pageSize,
        string? searchTerm = null)
    {
        var query = _dbSet.AsQueryable();

        // 搜索过滤
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p =>
                p.Name.Contains(searchTerm) ||
                (p.PhoneNumber != null && p.PhoneNumber.Contains(searchTerm))
            );
        }

        // 投影到PatientSummary(减少数据传输)
        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PatientSummary
            {
                Id = p.Id,
                Name = p.Name,
                Gender = p.Gender,
                Age = p.Age,
                PhoneNumber = p.PhoneNumber,
                LastVisitDate = p.LastVisitDate,
                TotalVisits = p.MedicalCases.Count
            })
            .ToListAsync();
    }
}
```

### 5. Excel批量导入患者
```csharp
// 在PatientsController中
[HttpPost("import")]
public async Task<IActionResult> ImportPatients(IFormFile file)
{
    using var stream = file.OpenReadStream();
    var result = await _patientService.ImportFromExcelAsync(stream);

    return Ok(new
    {
        SuccessCount = result.Succeeded.Count,
        FailedCount = result.Failed.Count,
        Errors = result.Failed.Select(f => new
        {
            f.RowNumber,
            f.ErrorMessage,
            f.Data
        })
    });
}

// PatientService实现数据验证
private async Task<ImportResult> ImportFromExcelAsync(Stream stream)
{
    var result = new ImportResult();
    var patients = ParseExcelData(stream);

    foreach (var (rowNumber, patient) in patients)
    {
        try
        {
            // 验证必填项
            if (string.IsNullOrWhiteSpace(patient.Name))
            {
                result.Failed.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    ErrorMessage = "患者姓名不能为空",
                    Data = patient
                });
                continue;
            }

            // 检查手机号重复
            if (!string.IsNullOrEmpty(patient.PhoneNumber))
            {
                var phoneExists = await _repository.PhoneNumberExistsAsync(patient.PhoneNumber);
                if (phoneExists)
                {
                    result.Failed.Add(new ImportError
                    {
                        RowNumber = rowNumber,
                        ErrorMessage = $"手机号已存在:{patient.PhoneNumber}",
                        Data = patient
                    });
                    continue;
                }
            }

            // 保存患者
            await _repository.AddAsync(patient);
            result.Succeeded.Add(patient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"导入患者失败:行{rowNumber}");
            result.Failed.Add(new ImportError
            {
                RowNumber = rowNumber,
                ErrorMessage = ex.Message,
                Data = patient
            });
        }
    }

    return result;
}
```

### 6. 患者统计功能
```csharp
public class PatientRepository : BaseRepository<PatientModel>, IPatientRepository
{
    // 获取患者统计信息
    public async Task<PatientStatistics> GetStatisticsAsync()
    {
        var totalPatients = await _dbSet.CountAsync(p => !p.IsDeleted);
        var activePatients = await _dbSet.CountAsync(p =>
            !p.IsDeleted &&
            p.LastVisitDate.HasValue &&
            p.LastVisitDate.Value >= DateTime.Now.AddMonths(-3)
        );

        var averageAge = await _dbSet
            .Where(p => !p.IsDeleted && p.Age > 0)
            .AverageAsync(p => (double)p.Age);

        var genderDistribution = await _dbSet
            .Where(p => !p.IsDeleted)
            .GroupBy(p => p.Gender)
            .Select(g => new { Gender = g.Key, Count = g.Count() })
            .ToListAsync();

        return new PatientStatistics
        {
            TotalPatients = totalPatients,
            ActivePatients = activePatients,
            AverageAge = Math.Round(averageAge, 1),
            GenderDistribution = genderDistribution.ToDictionary(
                x => x.Gender.ToString(),
                x => x.Count
            )
        };
    }
}

// 辅助类：患者统计
public class PatientStatistics
{
    public int TotalPatients { get; set; }          // 总患者数
    public int ActivePatients { get; set; }         // 活跃患者数(3个月内就诊)
    public double AverageAge { get; set; }          // 平均年龄
    public Dictionary<string, int> GenderDistribution { get; set; } // 性别分布
}
```

### 7. 手机号唯一性验证
```csharp
public class PatientRepository : BaseRepository<PatientModel>, IPatientRepository
{
    // 检查手机号是否已存在(支持排除特定患者)
    public async Task<bool> PhoneNumberExistsAsync(
        string phoneNumber,
        Guid? excludePatientId = null)
    {
        var query = _dbSet
            .Where(p => !p.IsDeleted && p.PhoneNumber == phoneNumber);

        // 更新时排除当前患者自己的手机号
        if (excludePatientId.HasValue)
        {
            query = query.Where(p => p.Id != excludePatientId.Value);
        }

        return await query.AnyAsync();
    }
}

// 在Service层使用
public async Task<PatientDto> UpdateAsync(Guid id, UpdatePatientDto dto)
{
    var patient = await _repository.GetByIdAsync(id);
    if (patient == null) throw new NotFoundException("患者不存在");

    // 验证手机号唯一性(排除自己)
    if (!string.IsNullOrEmpty(dto.PhoneNumber) &&
        dto.PhoneNumber != patient.PhoneNumber)
    {
        var phoneExists = await _repository.PhoneNumberExistsAsync(
            dto.PhoneNumber,
            excludePatientId: id
        );
        if (phoneExists)
        {
            throw new InvalidOperationException("手机号已被其他患者使用");
        }
    }

    // 更新患者信息
    _mapper.Map(dto, patient);
    patient.UpdatedAt = DateTime.Now;
    await _repository.UpdateAsync(patient);
    return _mapper.Map<PatientDto>(patient);
}
```

## 🔌 API 接口

此模块的业务逻辑通过 `LYBT.WebAPI` 项目中的 `PatientsController` 对外暴露。

- **API路由前缀**: `/api/v1/patients`

**主要端点**:
- `GET /api/v1/patients` - 分页查询患者
- `GET /api/v1/patients/{id}` - 按ID查询患者详情
- `POST /api/v1/patients` - 创建患者档案
- `PUT /api/v1/patients/{id}` - 更新患者档案
- `DELETE /api/v1/patients/{id}` - 删除患者(软删除)
- `GET /api/v1/patients/search` - 搜索患者(按姓名/手机)
- `POST /api/v1/patients/import` - Excel导入患者
- `GET /api/v1/patients/export` - 导出患者到Excel
- `GET /api/v1/patients/template` - 下载Excel导入模板
- `GET /api/v1/patients/statistics` - 获取患者统计信息

**完整API定义**请参考 `IPatientService` 接口和 `PatientsController` 的实现。

## 🐛 Bug修复记录

### 2025-11-19: 修复患者管理模块500错误 - DI注册缺失 (Commit: 5bd5307f9)

**问题描述**:
- 患者管理模块加载时返回500 Internal Server Error
- 客户端抛出 `Refit.ValidationApiException: Response status code does not indicate success: 500`
- 服务端日志显示 `InvalidOperationException` (抛出4次)
- API端点: `GET /api/v1/patients`

**根因分析**:
- `PatientsController` 构造函数需要两个接口依赖: `IPatientService` 和 `IPatientServiceOptimized`
- `PatientsModule.cs` 仅注册了 `IPatientService`，缺少 `IPatientServiceOptimized` 的DI注册
- ASP.NET Core DI容器无法解析 `IPatientServiceOptimized`，导致Controller激活失败
- 使用5 Why分析法 + 模块对比法进行系统性排查

**解决方案**:
- 文件: `src/Server/Modules/LYBT.Module.Patients/PatientsModule.cs`
- 新增行: 28
- 代码: `services.AddScoped<IPatientServiceOptimized, PatientService>();  // Phase 3 优化版本接口`
- 说明: 一个Service类实现多个接口时，需要在DI容器中分别注册每个接口到同一实现类

**架构背景 - Phase 3双轨制优化**:
- **IPatientService**: 传统DTO映射策略 - 兼容旧代码
- **IPatientServiceOptimized**: Entity直接返回策略 - 消除双重映射，性能优化
- `PatientService` 类同时实现两个接口，提供两种查询模式

**经验教训**:
1. ASP.NET Core DI容器无法自动推断接口关系，必须显式注册每个接口
2. Controller构造函数的接口依赖必须在Module中完整注册
3. 实施架构优化时，必须同步更新DI配置
4. 500错误 + InvalidOperationException通常指向DI解析失败

**影响范围**: 修复患者管理模块完全无法使用的Critical Bug

---

## 📚 详细文档

- **完整模块文档**:[docs/reference/modules/patients/](../../../../docs/reference/modules/patients/) *(待创建)*
- **架构设计**:[docs/explanation/architecture/server/patients-design.md](../../../../docs/explanation/architecture/server/patients-design.md) *(待创建)*
- **开发指南**:[docs/how-to-guides/server/patients-development.md](../../../../docs/how-to-guides/server/patients-development.md) *(待创建)*

---

**最后更新**:2025-10-29
**维护负责**:Server端开发组
