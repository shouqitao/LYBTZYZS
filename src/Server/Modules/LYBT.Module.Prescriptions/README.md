# LYBT.Module.Prescriptions - 处方管理模块

## 📦 项目定位

- **层级**:Server端
- **类型**:业务模块(处方管理)
- **职责**:提供中医处方的完整生命周期管理,包括处方开具、药材配置、编号生成、状态管理、金额计算、历史查询等功能。作为医疗案例系统的核心输出环节,处方管理连接诊断、患者和药材三大模块,实现从诊断到用药的完整闭环。支持从验方快速创建处方,提升医生开方效率。采用标准三层架构（Controller → Service → Repository）,确保业务逻辑清晰、数据访问高效。

## 📂 代码结构

```
LYBT.Module.Prescriptions/
├── PrescriptionsModule.cs             # 模块依赖注入注册
│   └── AddPrescriptionsModule()       # 依赖注入配置(仓储+服务+验证器)
├── Interfaces/                        # 模块接口定义
│   ├── IPrescriptionRepository.cs     # 处方仓储接口(7个方法)
│   └── IPrescriptionNumberService.cs  # 处方编号服务接口(3个方法)
├── Services/                          # 业务逻辑实现
│   ├── PrescriptionService.cs         # 处方服务(5个方法)
│   │   ├── GetByIdAsync()             # 按ID查询处方详情
│   │   ├── GetByMedicalCaseIdAsync()  # 按医疗案例ID查询处方列表
│   │   ├── SearchPrescriptionsAsync() # 搜索处方(多条件)
│   │   ├── GetPatientRecentPrescriptionsAsync() # 获取患者近期处方
│   │   └── CalculateTotalAmount()     # 计算处方总金额
│   └── PrescriptionNumberService.cs   # 处方编号服务(3个方法)
│       ├── GenerateNumberAsync()      # 生成处方编号(日期+序号)
│       ├── ValidateNumberFormat()     # 验证处方编号格式
│       └── GetMaxSequenceForDateAsync() # 获取指定日期最大序列号
├── Repositories/                      # 数据仓储实现
│   └── PrescriptionRepository.cs      # 处方仓储(7个方法)
│       ├── GetByIdWithItemsAsync()    # 查询处方及药材条目
│       ├── GetPagedWithDetailsAsync() # 分页查询详情(含统计)
│       ├── GetByPatientIdAsync()      # 按患者ID查询处方
│       ├── GetByMedicalCaseIdAsync()  # 按医疗案例ID查询处方
│       ├── GetPrescriptionNumbersByPrefixAsync() # 按前缀查询编号
│       ├── GetAllAsync()              # 获取所有处方
│       └── FindAsync()                # 条件查询(Lambda表达式)
├── Validators/                        # FluentValidation验证器
│   ├── PrescriptionCreateDtoValidator.cs # 创建处方DTO验证
│   └── PrescriptionEditDtoValidator.cs   # 编辑处方DTO验证
└── Mapping/                           # AutoMapper映射配置
    └── PrescriptionMappingProfile.cs  # Entity ↔ DTO映射规则
```

**说明**:
- **PrescriptionsModule**:依赖注入注册中心,统一注册仓储、服务和验证器
- **PrescriptionService**:5个方法覆盖处方的查询、搜索、金额计算等核心功能
- **PrescriptionNumberService**:专门负责处方编号的生成和验证(日期+序列号格式)
- **PrescriptionRepository**:7个方法提供多维度数据查询能力(患者、医案、编号等)
- **Validators**:FluentValidation验证器确保DTO数据完整性（药材必填、剂量范围）
- **Mapping**:AutoMapper配置统一处理Entity与DTO的转换

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Entities** - 数据实体定义(PrescriptionModel、PrescriptionItemModel)
2. **LYBT.Infrastructure** - 基础设施(AppDbContext、BaseRepository)
3. **LYBT.Shared.Models** - 共享DTO模型(PrescriptionDto、CreatePrescriptionDto等)
4. **LYBT.Server.Interfaces** - Server端接口定义(IPrescriptionService、IPrescriptionRepository)
5. **LYBT.Module.Patients** - 患者模块(处方关联患者)
6. **LYBT.Module.MedicalCase** - 医疗案例模块(处方关联医案)
7. **LYBT.Module.Consultation** - 诊断模块(处方基于诊断开具)
8. **LYBT.Module.Herbs** - 药材模块(处方包含药材条目)
9. **LYBT.Module.Formula** - 验方模块(从验方创建处方)

### 被依赖项目
1. **LYBT.WebAPI** - Web服务层通过PrescriptionsController暴露API
2. **测试项目**:
   - LYBT.Module.Prescriptions.Tests（单元测试）
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
- **LINQ**: 复杂查询表达式(分页、搜索、关联查询)
- **异步编程**: 全异步方法(async/await),提升性能

##  快速开始

此项目是一个类库,作为后端服务的一部分被 `LYBT.WebAPI` 项目引用和托管。无法独立运行。

```bash
# 构建此项目
dotnet build src/Server/Modules/LYBT.Module.Prescriptions/LYBT.Module.Prescriptions.csproj
```

**集成说明**:

### 1. 注册处方模块(在Startup.cs中)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 注册处方模块(自动注册仓储+服务+验证器)
        services.AddPrescriptionsModule();
    }
}
```

### 2. API Controller集成(在LYBT.WebAPI中)
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class PrescriptionsController : ControllerBase
{
    private readonly IPrescriptionService _prescriptionService;

    public PrescriptionsController(IPrescriptionService prescriptionService)
    {
        _prescriptionService = prescriptionService;
    }

    // 按医疗案例ID查询处方列表
    [HttpGet("medical-case/{medicalCaseId}")]
    public async Task<IActionResult> GetByMedicalCaseId(Guid medicalCaseId)
    {
        var prescriptions = await _prescriptionService.GetByMedicalCaseIdAsync(medicalCaseId);
        return Ok(prescriptions);
    }

    // 搜索处方(支持多条件)
    [HttpGet("search")]
    public async Task<IActionResult> SearchPrescriptions(
        [FromQuery] string? patientName = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _prescriptionService.SearchPrescriptionsAsync(
            patientName, startDate, endDate, pageIndex, pageSize
        );
        return Ok(result);
    }
}
```

### 3. 处方编号自动生成(日期+序列号)
```csharp
public class PrescriptionNumberService : IPrescriptionNumberService
{
    // 生成处方编号(格式:CF-YYYYMMDD-0001)
    public async Task<string> GenerateNumberAsync(DateTime prescriptionDate)
    {
        var dateStr = prescriptionDate.ToString("yyyyMMdd");
        var prefix = $"CF-{dateStr}-";

        // 获取今日已有编号的最大序列号
        var maxSequence = await GetMaxSequenceForDateAsync(prescriptionDate);
        var nextSequence = maxSequence + 1;

        // 生成4位序列号(补0)
        var sequenceStr = nextSequence.ToString("D4");
        return $"{prefix}{sequenceStr}";
    }

    // 获取指定日期的最大序列号
    private async Task<int> GetMaxSequenceForDateAsync(DateTime date)
    {
        var dateStr = date.ToString("yyyyMMdd");
        var prefix = $"CF-{dateStr}-";

        // 查询今日所有处方编号
        var numbers = await _prescriptionRepository
            .GetPrescriptionNumbersByPrefixAsync(prefix);

        if (!numbers.Any())
            return 0;

        // 提取序列号并取最大值
        var sequences = numbers
            .Select(n => int.Parse(n.Substring(prefix.Length)))
            .ToList();

        return sequences.Max();
    }

    // 验证处方编号格式
    public bool ValidateNumberFormat(string prescriptionNumber)
    {
        if (string.IsNullOrWhiteSpace(prescriptionNumber))
            return false;

        // 格式:CF-YYYYMMDD-0001 (总长度17字符)
        if (prescriptionNumber.Length != 17)
            return false;

        // 前缀验证
        if (!prescriptionNumber.StartsWith("CF-"))
            return false;

        // 日期部分验证(位置3-10)
        var datePart = prescriptionNumber.Substring(3, 8);
        if (!DateTime.TryParseExact(datePart, "yyyyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _))
            return false;

        // 序列号部分验证(位置12-15)
        var sequencePart = prescriptionNumber.Substring(12, 4);
        return int.TryParse(sequencePart, out var sequence) && sequence > 0;
    }
}
```

### 4. 处方创建与药材配置
```csharp
public class PrescriptionService : IPrescriptionService
{
    // 从验方创建处方
    public async Task<PrescriptionDto> CreateFromFormulaAsync(
        Guid formulaId,
        Guid medicalCaseId)
    {
        // 查询验方及药材
        var formula = await _formulaRepository.GetByIdWithHerbsAsync(formulaId);
        if (formula == null) throw new NotFoundException("验方不存在");

        // 查询医疗案例
        var medicalCase = await _medicalCaseRepository.GetByIdAsync(medicalCaseId);
        if (medicalCase == null) throw new NotFoundException("医疗案例不存在");

        // 生成处方编号
        var prescriptionNumber = await _numberService.GenerateNumberAsync(DateTime.Now);

        // 创建处方
        var prescription = new PrescriptionModel
        {
            PrescriptionNumber = prescriptionNumber,
            MedicalCaseId = medicalCaseId,
            PatientId = medicalCase.PatientId,
            ConsultationId = medicalCase.ConsultationId,
            Status = PrescriptionStatus.Draft,
            Notes = $"基于验方【{formula.Name}】创建",
            Items = formula.HerbItems.Select(item => new PrescriptionItemModel
            {
                HerbId = item.HerbId,
                Dosage = item.Dosage,
                Unit = item.Unit,
                Notes = item.Notes
            }).ToList()
        };

        // 计算总金额
        prescription.TotalAmount = CalculateTotalAmount(prescription.Items);

        // 保存处方
        await _repository.AddAsync(prescription);
        return _mapper.Map<PrescriptionDto>(prescription);
    }

    // 计算处方总金额
    private decimal CalculateTotalAmount(List<PrescriptionItemModel> items)
    {
        decimal total = 0;

        foreach (var item in items)
        {
            // 获取药材单价
            var herb = await _herbRepository.GetByIdAsync(item.HerbId);
            if (herb?.UnitPrice.HasValue == true)
            {
                total += herb.UnitPrice.Value * item.Dosage;
            }
        }

        return total;
    }
}

// FluentValidation验证器
public class PrescriptionCreateDtoValidator : AbstractValidator<CreatePrescriptionDto>
{
    public PrescriptionCreateDtoValidator()
    {
        RuleFor(x => x.MedicalCaseId)
            .NotEmpty().WithMessage("医疗案例ID不能为空");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("处方药材不能为空")
            .Must(items => items.All(i => i.Dosage > 0))
            .WithMessage("药材剂量必须大于0");

        RuleFor(x => x.Items)
            .Must(items => items.All(i => !string.IsNullOrWhiteSpace(i.Unit)))
            .WithMessage("药材单位不能为空");
    }
}
```

### 5. 处方状态管理(状态机)
```csharp
// 处方状态枚举
public enum PrescriptionStatus
{
    Draft = 1,          // 草稿(可编辑)
    Confirmed = 2,      // 已确认(不可编辑)
    Dispensed = 3       // 已配药(完成)
}

// 状态迁移控制
public class PrescriptionService : IPrescriptionService
{
    // 确认处方(Draft → Confirmed)
    public async Task ConfirmAsync(Guid prescriptionId)
    {
        var prescription = await _repository.GetByIdAsync(prescriptionId);
        if (prescription == null) throw new NotFoundException("处方不存在");

        // 验证状态迁移
        if (prescription.Status != PrescriptionStatus.Draft)
        {
            throw new InvalidOperationException("只有草稿状态的处方可以确认");
        }

        // 更新状态
        prescription.Status = PrescriptionStatus.Confirmed;
        prescription.ConfirmedAt = DateTime.Now;
        await _repository.UpdateAsync(prescription);
    }

    // 标记已配药(Confirmed → Dispensed)
    public async Task MarkAsDispensedAsync(Guid prescriptionId)
    {
        var prescription = await _repository.GetByIdAsync(prescriptionId);
        if (prescription == null) throw new NotFoundException("处方不存在");

        // 验证状态迁移
        if (prescription.Status != PrescriptionStatus.Confirmed)
        {
            throw new InvalidOperationException("只有已确认的处方可以标记为已配药");
        }

        // 更新状态
        prescription.Status = PrescriptionStatus.Dispensed;
        prescription.DispensedAt = DateTime.Now;
        await _repository.UpdateAsync(prescription);
    }
}
```

### 6. 患者历史处方查询(复诊支持)
```csharp
public class PrescriptionRepository : BaseRepository<PrescriptionModel>, IPrescriptionRepository
{
    // 按患者ID查询处方历史(按日期倒序)
    public async Task<List<PrescriptionModel>> GetByPatientIdAsync(
        Guid patientId,
        int limit = 10)
    {
        return await _dbSet
            .Include(p => p.Items)              // 包含药材条目
                .ThenInclude(i => i.Herb)       // 包含药材信息
            .Include(p => p.MedicalCase)        // 包含医疗案例
            .Where(p => p.PatientId == patientId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }
}

// 在Service层使用
public async Task<PagedResult<PrescriptionDto>> GetPatientRecentPrescriptionsAsync(
    Guid patientId,
    int pageIndex = 1,
    int pageSize = 10)
{
    var patient = await _patientRepository.GetByIdAsync(patientId);
    if (patient == null) throw new NotFoundException("患者不存在");

    // 查询患者历史处方
    var prescriptions = await _repository.GetByPatientIdAsync(
        patientId,
        pageIndex * pageSize
    );

    // 投影到DTO
    var prescriptionDtos = prescriptions
        .Skip((pageIndex - 1) * pageSize)
        .Take(pageSize)
        .Select(p => new PrescriptionDto
        {
            Id = p.Id,
            PrescriptionNumber = p.PrescriptionNumber,
            Status = p.Status,
            TotalAmount = p.TotalAmount,
            Items = p.Items.Select(i => new PrescriptionItemDto
            {
                HerbName = i.Herb.Name,
                Dosage = i.Dosage,
                Unit = i.Unit
            }).ToList()
        }).ToList();

    return new PagedResult<PrescriptionDto>
    {
        Items = prescriptionDtos,
        TotalCount = prescriptions.Count,
        PageIndex = pageIndex,
        PageSize = pageSize
    };
}
```

### 7. 处方搜索功能(多条件组合)
```csharp
public class PrescriptionService : IPrescriptionService
{
    // 搜索处方(支持患者姓名、日期范围)
    public async Task<PagedResult<PrescriptionDto>> SearchPrescriptionsAsync(
        string? patientName = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageIndex = 1,
        int pageSize = 10)
    {
        // 构建查询表达式
        Expression<Func<PrescriptionModel, bool>> filter = p => !p.IsDeleted;

        // 患者姓名过滤
        if (!string.IsNullOrWhiteSpace(patientName))
        {
            filter = filter.And(p => p.Patient.Name.Contains(patientName));
        }

        // 日期范围过滤
        if (startDate.HasValue)
        {
            filter = filter.And(p => p.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            filter = filter.And(p => p.CreatedAt <= endDate.Value);
        }

        // 执行查询
        var prescriptions = await _repository.FindAsync(filter);

        // 分页和投影
        var pagedPrescriptions = prescriptions
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<PrescriptionDto>
        {
            Items = _mapper.Map<List<PrescriptionDto>>(pagedPrescriptions),
            TotalCount = prescriptions.Count,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }
}
```

## 🔌 API 接口

此模块的业务逻辑通过 `LYBT.WebAPI` 项目中的 `PrescriptionsController` 对外暴露。

- **API路由前缀**: `/api/v1/prescriptions`

**主要端点**:
- `GET /api/v1/prescriptions/{id}` - 按ID查询处方详情
- `GET /api/v1/prescriptions/medical-case/{medicalCaseId}` - 按医疗案例ID查询处方列表
- `GET /api/v1/prescriptions/patient/{patientId}/recent` - 获取患者近期处方
- `GET /api/v1/prescriptions/search` - 搜索处方(多条件)
- `POST /api/v1/prescriptions` - 创建处方
- `POST /api/v1/prescriptions/from-formula` - 从验方创建处方
- `PUT /api/v1/prescriptions/{id}` - 更新处方(仅草稿状态)
- `PUT /api/v1/prescriptions/{id}/confirm` - 确认处方
- `PUT /api/v1/prescriptions/{id}/dispense` - 标记已配药
- `DELETE /api/v1/prescriptions/{id}` - 删除处方(软删除)

**完整API定义**请参考 `IPrescriptionService` 接口和 `PrescriptionsController` 的实现。

## 📚 详细文档

- **完整模块文档**:[docs/reference/modules/prescriptions/](../../../../docs/reference/modules/prescriptions/) *(待创建)*
- **架构设计**:[docs/explanation/architecture/server/prescriptions-design.md](../../../../docs/explanation/architecture/server/prescriptions-design.md) *(待创建)*
- **开发指南**:[docs/how-to-guides/server/prescriptions-development.md](../../../../docs/how-to-guides/server/prescriptions-development.md) *(待创建)*

---

**最后更新**:2025-10-29
**维护负责**:Server端开发组
