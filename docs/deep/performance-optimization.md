# DEEP-002: 性能优化指南

## 概述

凌隐宝堂中医诊所管理系统在处理大量患者数据、复杂的中药处方计算和多用户并发访问时，需要系统性的性能优化策略。本文档基于实际项目代码，提供全面的性能优化方案，涵盖数据库优化、内存管理、并发处理、缓存策略和前端性能优化。

## 数据库性能优化

### 1. 查询优化策略

#### 问题背景
中医诊所系统常见的性能瓶颈：
- 患者历史记录查询（10年以上的数据）
- 复杂处方价格计算（涉及多种折扣规则）
- 药材库存实时查询
- 医生排班与预约冲突检测

#### 优化方案

**1.1 索引优化**

```sql
-- 患者表关键索引
CREATE INDEX IX_Patients_IdentificationNumber ON Patients(IdentificationNumber);
CREATE INDEX IX_Patients_Name_DateOfBirth ON Patients(Name, DateOfBirth);
CREATE INDEX IX_Patients_Phone ON Patients(PhoneNumber);
CREATE INDEX IX_Patients_Status_CreatedDate ON Patients(Status, CreatedDate);

-- 医案表复合索引
CREATE INDEX IX_MedicalCases_PatientId_Date ON MedicalCases(PatientID, VisitDate DESC);
CREATE INDEX IX_MedicalCases_DoctorId_Date ON MedicalCases(DoctorID, VisitDate DESC);
CREATE INDEX IX_MedicalCases_Status_Date ON MedicalCases(Status, CreatedDate);

-- 处方表优化索引
CREATE INDEX IX_Prescriptions_MedicalCaseId ON Prescriptions(MedicalCaseID);
CREATE INDEX IX_Prescriptions_DoctorId_CreatedDate ON Prescriptions(DoctorID, CreatedDate DESC);
CREATE INDEX IX_Prescriptions_Status_TotalAmount ON Prescriptions(Status, TotalAmount);

-- 处方明细表索引
CREATE INDEX IX_PrescriptionItems_PrescriptionId ON PrescriptionItems(PrescriptionID);
CREATE INDEX IX_PrescriptionItems_HerbId ON PrescriptionItems(HerbID);
CREATE INDEX IX_PrescriptionItems_HerbId_Active ON PrescriptionItems(HerbID, IsActive);

-- 药材表索引
CREATE INDEX IX_Herbs_Name_Code ON Herbs(Name, HerbCode);
CREATE INDEX IX_Herbs_Category_Active ON Herbs(Category, IsActive);
CREATE INDEX IX_Herbs_Stock_Price ON Herbs(CurrentStock, UnitPrice);

-- 库存变动记录索引
CREATE INDEX IX_HerbInventory_HerbId_Date ON HerbInventory(HerbID, TransactionDate DESC);
```

**1.2 查询优化示例**

```csharp
// 优化前：N+1查询问题
public async Task<PatientDetailDto> GetPatientDetailAsync(int patientId)
{
    var patient = await _context.Patients.FindAsync(patientId);
    var medicalCases = await _context.MedicalCases
        .Where(mc => mc.PatientID == patientId)
        .ToListAsync();

    var patientDetail = new PatientDetailDto
    {
        Patient = patient,
        MedicalCases = medicalCases.Select(mc => new MedicalCaseDto
        {
            // 每个医案都会单独查询处方 - N+1问题
            Prescriptions = _context.Prescriptions
                .Where(p => p.MedicalCaseID == mc.ID)
                .ToList()
        }).ToList()
    };

    return patientDetail;
}

// 优化后：使用Include和Select优化
public async Task<PatientDetailDto> GetPatientDetailOptimizedAsync(int patientId)
{
    var patientDetail = await _context.Patients
        .Where(p => p.ID == patientId)
        .Select(p => new PatientDetailDto
        {
            Patient = new PatientBasicDto
            {
                ID = p.ID,
                Name = p.Name,
                Gender = p.Gender,
                DateOfBirth = p.DateOfBirth,
                PhoneNumber = p.PhoneNumber,
                IdentificationNumber = p.IdentificationNumber,
                Address = p.Address,
                Status = p.Status
            },
            MedicalCases = p.MedicalCases
                .OrderByDescending(mc => mc.VisitDate)
                .Select(mc => new MedicalCaseDto
                {
                    ID = mc.ID,
                    VisitDate = mc.VisitDate,
                    ChiefComplaint = mc.ChiefComplaint,
                    Diagnosis = mc.Diagnosis,
                    TreatmentPrinciple = mc.TreatmentPrinciple,
                    DoctorName = mc.Doctor.Name,
                    Prescriptions = mc.Prescriptions
                        .OrderBy(p => p.CreatedDate)
                        .Select(p => new PrescriptionDto
                        {
                            ID = p.ID,
                            PrescriptionDate = p.PrescriptionDate,
                            TotalAmount = p.TotalAmount,
                            Status = p.Status,
                            ItemCount = p.PrescriptionItems.Count
                        }).ToList()
                }).ToList(),
            TotalMedicalCases = p.MedicalCases.Count,
            LastVisitDate = p.MedicalCases
                .OrderByDescending(mc => mc.VisitDate)
                .Select(mc => (DateTime?)mc.VisitDate)
                .FirstOrDefault()
        })
        .FirstOrDefaultAsync();

    return patientDetail;
}
```

**1.3 分页查询优化**

```csharp
// 优化的分页查询方法
public async Task<PagedResult<MedicalCaseListDto>> GetMedicalCasesPagedAsync(
    MedicalCaseQueryParameters parameters)
{
    var query = _context.MedicalCases.AsQueryable();

    // 应用筛选条件
    if (parameters.PatientId.HasValue)
        query = query.Where(mc => mc.PatientID == parameters.PatientId);

    if (parameters.DoctorId.HasValue)
        query = query.Where(mc => mc.DoctorID == parameters.DoctorId);

    if (parameters.StartDate.HasValue)
        query = query.Where(mc => mc.VisitDate >= parameters.StartDate);

    if (parameters.EndDate.HasValue)
        query = query.Where(mc => mc.VisitDate <= parameters.EndDate);

    if (!string.IsNullOrEmpty(parameters.Keyword))
    {
        query = query.Where(mc =>
            mc.ChiefComplaint.Contains(parameters.Keyword) ||
            mc.Diagnosis.Contains(parameters.Keyword) ||
            mc.TreatmentPrinciple.Contains(parameters.Keyword));
    }

    // 使用KeySet分页替代OFFSET分页（大数据量时性能更好）
    if (parameters.LastId.HasValue && parameters.LastVisitDate.HasValue)
    {
        query = query.Where(mc =>
            mc.VisitDate < parameters.LastVisitDate ||
            (mc.VisitDate == parameters.LastVisitDate && mc.ID < parameters.LastId));
    }

    var totalCount = await query.CountAsync();

    var items = await query
        .OrderByDescending(mc => mc.VisitDate)
        .ThenByDescending(mc => mc.ID)
        .Take(parameters.PageSize + 1) // 多取一条判断是否有下一页
        .Select(mc => new MedicalCaseListDto
        {
            ID = mc.ID,
            PatientName = mc.Patient.Name,
            PatientAge = DateTime.Today.Year - mc.Patient.DateOfBirth.Year,
            Gender = mc.Patient.Gender,
            VisitDate = mc.VisitDate,
            ChiefComplaint = mc.ChiefComplaint.Length > 50
                ? mc.ChiefComplaint.Substring(0, 50) + "..."
                : mc.ChiefComplaint,
            Diagnosis = mc.Diagnosis,
            DoctorName = mc.Doctor.Name,
            PrescriptionCount = mc.Prescriptions.Count,
            TotalAmount = mc.Prescriptions.Sum(p => p.TotalAmount),
            Status = mc.Status
        })
        .ToListAsync();

    var hasNextPage = items.Count > parameters.PageSize;
    if (hasNextPage)
        items.RemoveAt(items.Count - 1);

    return new PagedResult<MedicalCaseListDto>
    {
        Items = items,
        TotalCount = totalCount,
        HasNextPage = hasNextPage,
        PageSize = parameters.PageSize
    };
}
```

### 2. 数据库连接优化

#### 2.1 连接池配置

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBT_Clinic;Trusted_Connection=true;MultipleActiveResultSets=true;Max Pool Size=200;Min Pool Size=10;Connection Timeout=30;Command Timeout=300;"
  }
}
```

#### 2.2 EF Core 上下文优化

```csharp
public class LYBTClinicDbContext : DbContext
{
    private readonly IConfiguration _configuration;

    public LYBTClinicDbContext(DbContextOptions<LYBTClinicDbContext> options,
        IConfiguration configuration) : base(options)
    {
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(_configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions
                    .EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null)
                    .CommandTimeout(300));
        }

        // 性能优化配置
        optionsBuilder.EnableSensitiveDataLogging(false);
        optionsBuilder.EnableServiceProviderCaching();
        optionsBuilder.EnableDetailedErrors(false);

        // 查询分割优化（复杂查询）
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置表分割以提高性能
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("Patients");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.IdentificationNumber).HasMaxLength(18);
            entity.HasIndex(e => e.IdentificationNumber).IsUnique();
        });

        // 配置批量操作优化
        modelBuilder.Entity<PrescriptionItem>(entity =>
        {
            entity.Property(e => e.UnitPrice).HasPrecision(10, 2);
            entity.Property(e => e.Subtotal).HasPrecision(10, 2);
        });

        // 配置查询过滤器
        modelBuilder.Entity<Patient>().HasQueryFilter(e => e.Status != "Deleted");
        modelBuilder.Entity<MedicalCase>().HasQueryFilter(e => e.Status != "Deleted");
        modelBuilder.Entity<Prescription>().HasQueryFilter(e => e.Status != "Deleted");
    }
}
```

## 内存管理优化

### 1. 对象池模式应用

```csharp
// StringBuilder 对象池
public class StringBuilderPool
{
    private static readonly ObjectPool<StringBuilder> _pool =
        new DefaultObjectPoolProvider().CreateStringBuilderPool();

    public static StringBuilder Get() => _pool.Get();
    public static void Return(StringBuilder sb) => _pool.Return(sb);

    public static string BuildString(Func<StringBuilder, StringBuilder> buildFunc)
    {
        var sb = Get();
        try
        {
            return buildFunc(sb).ToString();
        }
        finally
        {
            Return(sb);
        }
    }
}

// 使用示例
public class PrescriptionReportGenerator
{
    public string GeneratePrescriptionReport(Prescription prescription)
    {
        return StringBuilderPool.BuildString(sb =>
        {
            sb.AppendLine($"处方编号: {prescription.ID}");
            sb.AppendLine($"开具日期: {prescription.PrescriptionDate:yyyy-MM-dd}");
            sb.AppendLine($"医生: {prescription.Doctor.Name}");
            sb.AppendLine("药材清单:");

            foreach (var item in prescription.PrescriptionItems)
            {
                sb.AppendLine($"  {item.Herb.Name} {item.Quantity}{item.Unit} × {item.UnitPrice:C} = {item.Subtotal:C}");
            }

            sb.AppendLine($"总计: {prescription.TotalAmount:C}");
            return sb;
        });
    }
}
```

### 2. 内存缓存策略

```csharp
// 分层缓存管理器
public class CacheManager : ICacheManager
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<CacheManager> _logger;
    private readonly CacheOptions _options;

    public CacheManager(IMemoryCache memoryCache,
        ILogger<CacheManager> logger,
        IOptions<CacheOptions> options)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _options = options.Value;
    }

    // 药材信息缓存（相对稳定）
    public async Task<IReadOnlyList<HerbDto>> GetHerbsAsync()
    {
        const string cacheKey = "herbs:all";

        if (_memoryCache.TryGetValue(cacheKey, out IReadOnlyList<HerbDto> cachedHerbs))
        {
            _logger.LogDebug("从缓存获取药材列表");
            return cachedHerbs;
        }

        _logger.LogInformation("从数据库加载药材列表");
        var herbs = await LoadHerbsFromDatabaseAsync();

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
            SlidingExpiration = TimeSpan.FromMinutes(30),
            Priority = CacheItemPriority.Normal,
            Size = herbs.Count
        };

        _memoryCache.Set(cacheKey, herbs, cacheOptions);
        return herbs;
    }

    // 患者基本信息缓存（变化频率中等）
    public async Task<PatientBasicDto> GetPatientBasicAsync(int patientId)
    {
        var cacheKey = $"patient:basic:{patientId}";

        if (_memoryCache.TryGetValue(cacheKey, out PatientBasicDto cachedPatient))
        {
            return cachedPatient;
        }

        var patient = await LoadPatientBasicFromDatabaseAsync(patientId);
        if (patient != null)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
                SlidingExpiration = TimeSpan.FromMinutes(5),
                Priority = CacheItemPriority.Normal
            };

            _memoryCache.Set(cacheKey, patient, cacheOptions);
        }

        return patient;
    }

    // 价格计算缓存（实时性要求高）
    public Task<decimal> CalculatePrescriptionPriceAsync(int prescriptionId)
    {
        // 价格计算不缓存，确保实时性
        return CalculatePriceFromDatabaseAsync(prescriptionId);
    }

    // 缓存失效方法
    public void InvalidatePatientCache(int patientId)
    {
        _memoryCache.Remove($"patient:basic:{patientId}");
        _memoryCache.Remove($"patient:detail:{patientId}");
        _logger.LogInformation($"已失效患者 {patientId} 的缓存");
    }

    public void InvalidateHerbCache()
    {
        _memoryCache.Remove("herbs:all");
        _logger.LogInformation("已失效药材缓存");
    }
}

// 缓存配置
public class CacheOptions
{
    public int DefaultExpirationMinutes { get; set; } = 30;
    public int SlidingExpirationMinutes { get; set; } = 10;
    public int MaxCacheSize { get; set; } = 1000;
}
```

### 3. 大数据处理优化

```csharp
// 流式处理大量数据
public class DataExportService
{
    private readonly LYBTClinicDbContext _context;
    private readonly ILogger<DataExportService> _logger;

    public async Task ExportMedicalCasesToCsvAsync(Stream outputStream,
        MedicalCaseExportFilter filter)
    {
        using var writer = new StreamWriter(outputStream, Encoding.UTF8);

        // 写入CSV头部
        await WriteCsvHeaderAsync(writer);

        // 使用IAsyncEnumerable流式处理，避免内存溢出
        await foreach (var medicalCase in GetMedicalCasesStreamAsync(filter))
        {
            await WriteMedicalCaseToCsvAsync(writer, medicalCase);
        }

        await writer.FlushAsync();
    }

    private async IAsyncEnumerable<MedicalCaseExportDto> GetMedicalCasesStreamAsync(
        MedicalCaseExportFilter filter)
    {
        var query = _context.MedicalCases
            .AsNoTracking()
            .Where(mc => mc.Status != "Deleted");

        if (filter.StartDate.HasValue)
            query = query.Where(mc => mc.VisitDate >= filter.StartDate);

        if (filter.EndDate.HasValue)
            query = query.Where(mc => mc.VisitDate <= filter.EndDate);

        if (filter.DoctorId.HasValue)
            query = query.Where(mc => mc.DoctorID == filter.DoctorId);

        // 使用AsAsyncEnumerable进行流式处理
        await foreach (var medicalCase in query
            .OrderBy(mc => mc.VisitDate)
            .Select(mc => new MedicalCaseExportDto
            {
                ID = mc.ID,
                PatientName = mc.Patient.Name,
                PatientAge = DateTime.Today.Year - mc.Patient.DateOfBirth.Year,
                Gender = mc.Patient.Gender,
                VisitDate = mc.VisitDate,
                ChiefComplaint = mc.ChiefComplaint,
                Diagnosis = mc.Diagnosis,
                DoctorName = mc.Doctor.Name,
                PrescriptionCount = mc.Prescriptions.Count,
                TotalAmount = mc.Prescriptions.Sum(p => p.TotalAmount)
            })
            .AsAsyncEnumerable())
        {
            yield return medicalCase;
        }
    }

    private async Task WriteCsvHeaderAsync(StreamWriter writer)
    {
        await writer.WriteLineAsync("ID,患者姓名,年龄,性别,就诊日期,主诉,诊断,医生,处方数量,总金额");
    }

    private async Task WriteMedicalCaseToCsvAsync(StreamWriter writer,
        MedicalCaseExportDto medicalCase)
    {
        var line = $"{medicalCase.ID}," +
                  $"\"{EscapeCsvField(medicalCase.PatientName)}\"," +
                  $"{medicalCase.PatientAge}," +
                  $"{medicalCase.Gender}," +
                  $"{medicalCase.VisitDate:yyyy-MM-dd}," +
                  $"\"{EscapeCsvField(medicalCase.ChiefComplaint)}\"," +
                  $"\"{EscapeCsvField(medicalCase.Diagnosis)}\"," +
                  $"\"{EscapeCsvField(medicalCase.DoctorName)}\"," +
                  $"{medicalCase.PrescriptionCount}," +
                  $"{medicalCase.TotalAmount:F2}";

        await writer.WriteLineAsync(line);
    }

    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        return field.Replace("\"", "\"\"");
    }
}
```

## 并发处理优化

### 1. 异步编程最佳实践

```csharp
// 并发安全的处方计算服务
public class PrescriptionCalculationService
{
    private readonly IHerbPriceService _priceService;
    private readonly IDiscountService _discountService;
    private readonly ILogger<PrescriptionCalculationService> _logger;
    private readonly SemaphoreSlim _semaphore;

    public PrescriptionCalculationService(
        IHerbPriceService priceService,
        IDiscountService discountService,
        ILogger<PrescriptionCalculationService> logger)
    {
        _priceService = priceService;
        _discountService = discountService;
        _logger = logger;
        _semaphore = new SemaphoreSlim(Environment.ProcessorCount * 2,
            Environment.ProcessorCount * 2);
    }

    public async Task<PrescriptionCalculationResult> CalculatePrescriptionAsync(
        PrescriptionCalculationRequest request)
    {
        await _semaphore.WaitAsync();
        try
        {
            _logger.LogInformation("开始计算处方 {PrescriptionId}", request.PrescriptionId);

            // 并行获取药材价格
            var herbPriceTasks = request.Items.Select(async item =>
            {
                var price = await _priceService.GetCurrentPriceAsync(item.HerbId);
                return new { Item = item, Price = price };
            });

            var herbPrices = await Task.WhenAll(herbPriceTasks);

            // 计算基础总价
            decimal subtotal = 0;
            var calculatedItems = new List<CalculatedPrescriptionItem>();

            foreach (var herbPrice in herbPrices)
            {
                var itemSubtotal = herbPrice.Item.Quantity * herbPrice.Price;
                subtotal += itemSubtotal;

                calculatedItems.Add(new CalculatedPrescriptionItem
                {
                    HerbId = herbPrice.Item.HerbId,
                    HerbName = herbPrice.Item.HerbName,
                    Quantity = herbPrice.Item.Quantity,
                    Unit = herbPrice.Item.Unit,
                    UnitPrice = herbPrice.Price,
                    Subtotal = itemSubtotal
                });
            }

            // 应用折扣规则
            var discountResult = await _discountService.CalculateDiscountAsync(
                new DiscountCalculationRequest
                {
                    PatientId = request.PatientId,
                    DoctorId = request.DoctorId,
                    Subtotal = subtotal,
                    PrescriptionDate = request.PrescriptionDate,
                    IsFirstTime = request.IsFirstTimePatient
                });

            var totalAmount = subtotal - discountResult.DiscountAmount;

            return new PrescriptionCalculationResult
            {
                Items = calculatedItems,
                Subtotal = subtotal,
                DiscountAmount = discountResult.DiscountAmount,
                DiscountReason = discountResult.DiscountReason,
                TotalAmount = totalAmount,
                CalculationTime = DateTime.UtcNow
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    // 批量计算多个处方
    public async Task<List<PrescriptionCalculationResult>> CalculateMultiplePrescriptionsAsync(
        List<PrescriptionCalculationRequest> requests)
    {
        var results = new ConcurrentBag<PrescriptionCalculationResult>();
        var tasks = requests.Select(async request =>
        {
            try
            {
                var result = await CalculatePrescriptionAsync(request);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算处方 {PrescriptionId} 失败", request.PrescriptionId);
                // 添加错误结果
                results.Add(new PrescriptionCalculationResult
                {
                    PrescriptionId = request.PrescriptionId,
                    ErrorMessage = ex.Message,
                    CalculationTime = DateTime.UtcNow
                });
            }
        });

        await Task.WhenAll(tasks);
        return results.OrderBy(r => r.PrescriptionId).ToList();
    }
}
```

### 2. 数据库并发控制

```csharp
// 使用乐观并发控制处理并发更新
public class MedicalCaseService
{
    private readonly LYBTClinicDbContext _context;
    private readonly ILogger<MedicalCaseService> _logger;

    public async Task<UpdateMedicalCaseResult> UpdateMedicalCaseAsync(
        UpdateMedicalCaseRequest request, string userId)
    {
        var retryCount = 0;
        const int maxRetries = 3;

        while (retryCount < maxRetries)
        {
            try
            {
                var medicalCase = await _context.MedicalCases
                    .Include(mc => mc.Prescriptions)
                    .FirstOrDefaultAsync(mc => mc.ID == request.Id);

                if (medicalCase == null)
                    return UpdateMedicalCaseResult.NotFound($"未找到ID为 {request.Id} 的医案");

                // 检查并发版本
                if (medicalCase.RowVersion != request.RowVersion)
                {
                    // 获取最新的数据用于冲突解决
                    var latestMedicalCase = await _context.MedicalCases
                        .AsNoTracking()
                        .FirstOrDefaultAsync(mc => mc.ID == request.Id);

                    return UpdateMedicalCaseResult.Conflict(latestMedicalCase);
                }

                // 应用更新
                medicalCase.ChiefComplaint = request.ChiefComplaint;
                medicalCase.CurrentIllnessHistory = request.CurrentIllnessHistory;
                medicalCase.PastHistory = request.PastHistory;
                medicalCase.PersonalHistory = request.PersonalHistory;
                medicalCase.FamilyHistory = request.FamilyHistory;
                medicalCase.PhysicalExamination = request.PhysicalExamination;
                medicalCase.Diagnosis = request.Diagnosis;
                medicalCase.TreatmentPrinciple = request.TreatmentPrinciple;
                medicalCase.UpdatedBy = userId;
                medicalCase.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return UpdateMedicalCaseResult.Success(medicalCase);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                retryCount++;
                _logger.LogWarning(ex, "更新医案 {MedicalCaseId} 发生并发冲突，重试 {RetryCount}/{MaxRetries}",
                    request.Id, retryCount, maxRetries);

                if (retryCount >= maxRetries)
                {
                    // 获取最新版本用于客户端冲突解决
                    var latestMedicalCase = await _context.MedicalCases
                        .AsNoTracking()
                        .FirstOrDefaultAsync(mc => mc.ID == request.Id);

                    return UpdateMedicalCaseResult.Conflict(latestMedicalCase);
                }

                // 等待随机时间后重试
                await Task.Delay(TimeSpan.FromMilliseconds(100 * retryCount));
            }
        }

        return UpdateMedicalCaseResult.Error("更新医案失败，已达到最大重试次数");
    }
}

// DTO定义
public class UpdateMedicalCaseRequest
{
    public int Id { get; set; }
    public byte[] RowVersion { get; set; }
    public string ChiefComplaint { get; set; }
    public string CurrentIllnessHistory { get; set; }
    public string PastHistory { get; set; }
    public string PersonalHistory { get; set; }
    public string FamilyHistory { get; set; }
    public string PhysicalExamination { get; set; }
    public string Diagnosis { get; set; }
    public string TreatmentPrinciple { get; set; }
}

public class UpdateMedicalCaseResult
{
    public bool Success { get; private set; }
    public MedicalCase MedicalCase { get; private set; }
    public string Message { get; private set; }
    public bool Conflict { get; private set; }

    public static UpdateMedicalCaseResult Success(MedicalCase medicalCase)
        => new() { Success = true, MedicalCase = medicalCase };

    public static UpdateMedicalCaseResult NotFound(string message)
        => new() { Success = false, Message = message };

    public static UpdateMedicalCaseResult Conflict(MedicalCase latestMedicalCase)
        => new() { Success = false, Conflict = true, MedicalCase = latestMedicalCase };

    public static UpdateMedicalCaseResult Error(string message)
        => new() { Success = false, Message = message };
}
```

## 前端性能优化

### 1. WPF客户端优化

```csharp
// 虚拟化列表优化
public class VirtualizedMedicalCaseListView : UserControl
{
    private readonly VirtualizingStackPanel _virtualizingPanel;

    public VirtualizedMedicalCaseListView()
    {
        InitializeComponent();
        InitializeVirtualization();
    }

    private void InitializeVirtualization()
    {
        // 启用虚拟化
        MedicalCaseListBox.VirtualizingPanel.IsVirtualizing = true;
        MedicalCaseListBox.VirtualizingPanel.VirtualizationMode = VirtualizationMode.Recycling;
        MedicalCaseListBox.VirtualizingPanel.IsContainerVirtualizable = true;
        MedicalCaseListBox.VirtualizingPanel.ScrollUnit = ScrollUnit.Pixel;

        // 设置容器回收
        MedicalCaseListBox.VirtualizingPanel.IsContainerVirtualizable = true;
        MedicalCaseListBox.VirtualizingPanel.RecycleContainers = true;

        // 优化渲染
        MedicalCaseListBox.EnableRowVirtualization = true;
        MedicalCaseListBox.EnableColumnVirtualization = false;
    }
}

// 数据绑定优化
public class OptimizedMedicalCaseViewModel : ObservableObject
{
    private readonly IMedicalCaseService _service;
    private readonly ObservableCollection<MedicalCaseListItem> _items;
    private readonly DispatcherTimer _searchTimer;

    public ObservableCollection<MedicalCaseListItem> Items => _items;

    private string _searchText;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                // 使用防抖搜索
                _searchTimer.Stop();
                _searchTimer.Start();
            }
        }
    }

    public OptimizedMedicalCaseViewModel(IMedicalCaseService service)
    {
        _service = service;
        _items = new ObservableCollection<MedicalCaseListItem>();

        _searchTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _searchTimer.Tick += OnSearchTimerTick;

        LoadDataAsync();
    }

    private async void OnSearchTimerTick(object sender, EventArgs e)
    {
        _searchTimer.Stop();
        await SearchAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            var result = await _service.GetMedicalCasesAsync(new MedicalCaseQueryParameters
            {
                PageSize = 50,
                Page = 1
            });

            _items.Clear();
            foreach (var item in result.Items)
            {
                _items.Add(item);
            }
        }
        catch (Exception ex)
        {
            // 处理错误
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SearchAsync()
    {
        try
        {
            IsLoading = true;

            var result = await _service.SearchMedicalCasesAsync(new SearchMedicalCasesRequest
            {
                Keyword = SearchText,
                PageSize = 50
            });

            _items.Clear();
            foreach (var item in result.Items)
            {
                _items.Add(item);
            }
        }
        catch (Exception ex)
        {
            // 处理错误
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### 2. 异步数据加载

```csharp
// 分页加载服务
public class PagedDataService<T> where T : class
{
    private readonly Func<PagedRequest, Task<PagedResult<T>>> _loadPageFunc;
    private readonly ObservableCollection<T> _items;
    private readonly SemaphoreSlim _loadingSemaphore;

    private bool _hasMorePages = true;
    private int _currentPage = 0;
    private readonly int _pageSize;

    public ObservableCollection<T> Items => _items;
    public bool HasMorePages => _hasMorePages;
    public bool IsLoading { get; private set; }

    public PagedDataService(Func<PagedRequest, Task<PagedResult<T>>> loadPageFunc, int pageSize = 50)
    {
        _loadPageFunc = loadPageFunc;
        _pageSize = pageSize;
        _items = new ObservableCollection<T>();
        _loadingSemaphore = new SemaphoreSlim(1, 1);
    }

    public async Task LoadFirstPageAsync()
    {
        _items.Clear();
        _currentPage = 0;
        _hasMorePages = true;
        await LoadNextPageAsync();
    }

    public async Task LoadNextPageAsync()
    {
        if (!_hasMorePages || IsLoading)
            return;

        await _loadingSemaphore.WaitAsync();
        try
        {
            IsLoading = true;

            var request = new PagedRequest
            {
                Page = ++_currentPage,
                PageSize = _pageSize
            };

            var result = await _loadPageFunc(request);

            foreach (var item in result.Items)
            {
                _items.Add(item);
            }

            _hasMorePages = result.HasNextPage;
        }
        finally
        {
            IsLoading = false;
            _loadingSemaphore.Release();
        }
    }

    public void Refresh()
    {
        _currentPage = 0;
        _hasMorePages = true;
        LoadFirstPageAsync();
    }
}
```

## 性能监控与诊断

### 1. 性能计数器

```csharp
// 性能监控服务
public class PerformanceMonitorService
{
    private readonly ILogger<PerformanceMonitorService> _logger;
    private readonly ConcurrentDictionary<string, PerformanceMetric> _metrics;
    private readonly Timer _reportingTimer;

    public PerformanceMonitorService(ILogger<PerformanceMonitorService> logger)
    {
        _logger = logger;
        _metrics = new ConcurrentDictionary<string, PerformanceMetric>();

        // 每分钟报告一次性能指标
        _reportingTimer = new Timer(ReportMetrics, null,
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public IDisposable MeasureOperation(string operationName)
    {
        return new OperationTimer(this, operationName);
    }

    internal void RecordOperation(string operationName, TimeSpan duration)
    {
        _metrics.AddOrUpdate(operationName,
            new PerformanceMetric { Count = 1, TotalDuration = duration, AverageDuration = duration },
            (key, existing) => new PerformanceMetric
            {
                Count = existing.Count + 1,
                TotalDuration = existing.TotalDuration + duration,
                AverageDuration = (existing.TotalDuration + duration) / (existing.Count + 1)
            });
    }

    private void ReportMetrics(object state)
    {
        foreach (var metric in _metrics)
        {
            _logger.LogInformation(
                "操作 {OperationName}: 调用次数 {Count}, 平均耗时 {AverageDuration}ms, 总耗时 {TotalDuration}ms",
                metric.Key,
                metric.Value.Count,
                metric.Value.AverageDuration.TotalMilliseconds,
                metric.Value.TotalDuration.TotalMilliseconds);
        }
    }
}

public class OperationTimer : IDisposable
{
    private readonly PerformanceMonitorService _monitor;
    private readonly string _operationName;
    private readonly Stopwatch _stopwatch;

    public OperationTimer(PerformanceMonitorService monitor, string operationName)
    {
        _monitor = monitor;
        _operationName = operationName;
        _stopwatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        _monitor.RecordOperation(_operationName, _stopwatch.Elapsed);
    }
}

// 使用示例
public class MedicalCaseService
{
    private readonly PerformanceMonitorService _performanceMonitor;

    public async Task<MedicalCaseDto> GetMedicalCaseAsync(int id)
    {
        using var timer = _performanceMonitor.MeasureOperation("GetMedicalCase");

        var medicalCase = await _context.MedicalCases
            .Include(mc => mc.Patient)
            .Include(mc => mc.Doctor)
            .Include(mc => mc.Prescriptions)
            .FirstOrDefaultAsync(mc => mc.ID == id);

        return _mapper.Map<MedicalCaseDto>(medicalCase);
    }
}
```

### 2. 内存使用监控

```csharp
// 内存监控服务
public class MemoryMonitoringService
{
    private readonly ILogger<MemoryMonitoringService> _logger;
    private readonly Timer _monitoringTimer;

    public MemoryMonitoringService(ILogger<MemoryMonitoringService> logger)
    {
        _logger = logger;

        // 每30秒检查一次内存使用情况
        _monitoringTimer = new Timer(MonitorMemoryUsage, null,
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    private void MonitorMemoryUsage(object state)
    {
        var process = Process.GetCurrentProcess();
        var memoryUsage = process.WorkingSet64 / 1024 / 1024; // MB
        var gcMemory = GC.GetTotalMemory(false) / 1024 / 1024; // MB

        _logger.LogInformation(
            "内存使用情况 - 进程内存: {ProcessMemory}MB, GC内存: {GCMemory}MB, Gen0: {Gen0}, Gen1: {Gen1}, Gen2: {Gen2}",
            memoryUsage,
            gcMemory,
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2));

        // 内存使用过高时触发垃圾回收
        if (memoryUsage > 500) // 500MB
        {
            _logger.LogWarning("内存使用过高，触发垃圾回收");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
```

## 性能优化检查清单

### 数据库优化
- [ ] 为频繁查询字段创建适当索引
- [ ] 使用分页查询替代全表扫描
- [ ] 避免N+1查询问题，合理使用Include和Select
- [ ] 配置合适的连接池大小
- [ ] 使用查询分割处理复杂查询
- [ ] 启用查询跟踪优化（NoTracking）

### 内存管理
- [ ] 实现对象池减少GC压力
- [ ] 使用分层缓存策略
- [ ] 及时释放大对象和 IDisposable 资源
- [ ] 使用流式处理处理大数据集
- [ ] 监控内存使用情况，及时触发GC

### 并发处理
- [ ] 合理使用异步编程模式
- [ ] 实现并发控制避免竞态条件
- [ ] 使用信号量控制并发访问数量
- [ ] 实现重试机制处理并发冲突

### 前端优化
- [ ] 启用UI虚拟化处理大数据列表
- [ ] 实现防抖机制优化搜索体验
- [ ] 使用分页加载减少内存占用
- [ ] 优化数据绑定和UI渲染性能

### 监控诊断
- [ ] 实现性能计数器监控关键操作
- [ ] 监控内存使用和GC情况
- [ ] 记录慢查询和性能瓶颈
- [ ] 建立性能基线和告警机制

通过以上优化策略，凌隐宝堂中医诊所管理系统能够在处理大量患者数据、复杂业务逻辑和多用户并发访问时保持良好的性能表现。