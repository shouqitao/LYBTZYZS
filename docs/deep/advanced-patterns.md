# 高级设计模式指南

**基于凌隐宝堂中医诊所项目实践的高级设计模式深度解析** - 提升代码质量、可维护性和扩展性的进阶指南

## 🎯 设计模式概览

### 模式分类体系
```
                    ┌─────────────────────────────────────────┐
                    │           Creational Patterns            │
                    │             (创建型模式)                   │
                    │  • Singleton • Factory • Builder          │
                    │  • Prototype • Abstract Factory         │
                    └─────────────────────────────────────────┘
                                      │
                    ┌─────────────────────┼─────────────────────┐
                    │   Structural        │ Behavioral       │
                    │   Patterns         │   Patterns       │
                    │  (结构型模式)       │  (行为型模式)       │
                    │  • Adapter • Proxy │  • Strategy • Observer│
                    │  • Decorator • Facade│  • Command • Visitor │
                    │  • Bridge • Composite │  • Iterator • Mediator│
                    │  • Flyweight         │  • Template Method│
                    └─────────────────────┴─────────────────────┘�
```

### 模式应用场景矩阵
| 模式类型 | 业务场景 | LYBT应用 | 复杂度 | 推荐程度 |
|---------|---------|----------|--------|----------|
| **Singleton** | 系统配置 | ✅ 配置管理、缓存服务 | 低 | ⭐⭐⭐⭐⭐ |
| **Factory** | 对象创建 | ✅ 服务实例、数据访问 | 中 | ⭐⭐⭐⭐⭐ |
| **Builder** | 复杂对象 | ✅ 处方构建、医案记录 | 高 | ⭐⭐⭐⭐ |
| **Strategy** | 算法切换 | ✅ 价格计算、诊断逻辑 | 中 | ⭐⭐⭐⭐⭐ |
| **Observer** | 事件通知 | ✅ 状态变更、数据同步 | 中 | ⭐⭐⭐⭐ |
| **Decorator** | 功能扩展 | ✅ 处方增强、服务包装 | 高 | ⭐⭐⭐ |
| **Command** | 操作封装 | ✅ 批量操作、撤销重做 | 中 | ⭐⭐⭐⭐ |
| **Mediator** | 协调通信 | ✅ 模块间通信、事件处理 | 高 | ⭐⭐⭐⭐ |

## 🏗️ 创建型模式 (Creational Patterns)

### 1. Singleton 模式 - 单例配置管理

#### 场景应用：系统配置管理
```csharp
/// <summary>
/// 系统配置管理器 - 单例模式
/// 确保整个应用生命周期中只有一个配置实例
/// </summary>
public sealed class ConfigurationManager
{
    private static readonly Lazy<ConfigurationManager> _instance = 
        new Lazy<ConfigurationManager>(() => new ConfigurationManager());
    
    private readonly Dictionary<string, object> _configurations;
    private readonly ILogger<ConfigurationManager> _logger;
    private readonly IOptionsMonitor<AppSettings> _optionsMonitor;
    
    private ConfigurationManager()
    {
        _configurations = new Dictionary<string, object>();
        _logger = null!; // 实际实现中通过DI获取
        _optionsMonitor = null!;
        
        InitializeConfiguration();
    }

    public static ConfigurationManager Instance => _instance.Value;

    /// <summary>
    /// 获取配置实例
    /// </summary>
    public static ConfigurationManager GetInstance() => Instance;

    /// <summary>
    /// 获取配置值
    /// </summary>
    public T GetConfiguration<T>(string key) where T : class
    {
        if (_configurations.TryGetValue(key, out var config))
        {
            return config as T;
        }
        
        throw new InvalidOperationException($"配置项 '{key}' 不存在");
    }

    /// <summary>
    /// 设置配置值
    /// </summary>
    public void SetConfiguration<T>(string key, T value) where T : class
    {
        _configurations[key] = value;
        _logger.LogInformation("配置项已更新: {Key} = {Value}", key, value);
    }

    /// <summary>
    /// 监听配置变更
    /// </summary>
    public IDisposable OnChange<T>(string key, Action<T> onChange)
    {
        return _optionsMonitor.OnChange(settings =>
        {
            var currentValue = GetConfiguration<T>(key);
            var newValue = settings.GetType().GetProperties()
                .FirstOrDefault(p => p.Name == key)?.GetValue(settings);
            
            if (!Equals(currentValue, newValue))
            {
                SetConfiguration(key, (T)newValue!);
                onChange?.Invoke((T)newValue!);
            }
        });
    }

    /// <summary>
    /// 初始化配置
    /// </summary>
    private void InitializeConfiguration()
    {
        // 加载默认配置
        SetConfiguration("Database", new DatabaseConfiguration());
        SetConfiguration("Jwt", new JwtConfiguration());
        SetConfiguration("Cache", new CacheConfiguration());
        SetConfiguration("Logging", new LoggingConfiguration());
        SetConfiguration("Backup", new BackupConfiguration());
        
        _logger.LogInformation("配置管理器初始化完成");
    }
}

/// <summary>
/// 数据库配置
/// </summary>
public class DatabaseConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public int CommandTimeout { get; set; } = 30;
    public int MaxRetryCount { get; set; } = 3;
    public bool EnableRetryOnFailure { get; set; } = true;
    public bool EnableQueryLogging { get; set; } = false;
}

/// <summary>
/// JWT配置
/// </summary>
public class JwtConfiguration
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; set; } = "LYBT.TCM";
    public string Audience { get; set; set; } = "LYBT.Client";
    public TimeSpan AccessTokenExpiration { get; set; set; } = TimeSpan.FromHours(2);
    public TimeSpan RefreshTokenExpiration { get; set; set; = TimeSpan.FromDays(7);
    public int ClockSkew { get; set; set; } = 5;
}
```

### 2. Factory 模式 - 服务实例创建

#### 场景应用：数据访问工厂
```csharp
/// <summary>
/// 数据访问工厂接口
/// </summary>
public interface IDataAccessFactory
{
    T CreateRepository<T>() where T : class;
    IUnitOfWork CreateUnitOfWork();
    IDbContext CreateDbContext();
}

/// <summary>
/// 数据访问工厂实现
/// </summary>
public class DataAccessFactory : IDataAccessFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataAccessFactory> _logger;
    private readonly Dictionary<Type, object> _repositories;

    public DataAccessFactory(IServiceProvider serviceProvider, ILogger<DataAccessFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _repositories = new Dictionary<Type, object>();
    }

    /// <summary>
    /// 创建仓储实例
    /// </summary>
    public T CreateRepository<T>() where T : class
    {
        var repositoryType = typeof(T);
        
        if (_repositories.TryGetValue(repositoryType, out var cachedRepository))
        {
            return (T)cachedRepository;
        }

        // 使用依赖注入创建实例
        var repository = _serviceProvider.GetService<T>();
        if (repository == null)
        {
            _logger.LogError("无法创建仓储实例: {RepositoryType}", repositoryType.Name);
            throw new InvalidOperationException($"无法创建仓储实例: {repositoryType.Name}");
        }

        _repositories[repositoryType] = repository;
        return repository;
    }

    /// <summary>
    /// 创建工作单元
    /// </summary>
    public IUnitOfWork CreateUnitOfWork()
    {
        return _serviceProvider.GetRequiredService<IUnitOfWork>();
    }

    /// <summary>
    /// 创建数据库上下文
    /// </summary>
    public IDbContext CreateDbContext()
    {
        return _serviceProvider.GetRequiredService<IDbContext>();
    }
}

/// <summary>
/// 抽象工厂扩展
/// </summary>
public abstract class RepositoryFactoryBase
{
    protected readonly IServiceProvider _serviceProvider;
    protected readonly ILogger _logger;

    protected RepositoryFactoryBase(IServiceProvider serviceProvider, ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected T CreateService<T>() where T : class
    {
        return _serviceProvider.GetRequiredService<T>();
    }
}

/// <summary>
/// 患者业务工厂
/// </summary>
public class PatientRepositoryFactory : RepositoryFactoryBase
{
    public PatientRepositoryFactory(IServiceProvider serviceProvider, ILogger<PatientRepositoryFactory> logger)
        : base(serviceProvider, logger)
    {
    }

    public IPatientRepository CreateRepository()
    {
        return CreateService<IPatientRepository>();
    }

    public IPatientSearchService CreateSearchService()
    {
        return CreateService<IPatientSearchService>();
    }

    public IPatientValidationService CreateValidationService()
    {
        return CreateService<IPatientValidationService>();
    }
}
```

### 3. Builder 模式 - 复杂对象构建

#### 场景应用：处方构建器
```csharp
/// <summary>
/// 处方构建器 - 实现复杂处方的分步构建
/// </summary>
public class PrescriptionBuilder
{
    private readonly Prescription _prescription;
    private readonly List<PrescriptionItemBuilder> _itemBuilders;

    public PrescriptionBuilder()
    {
        _prescription = new Prescription();
        _itemBuilders = new List<PrescriptionItemBuilder>();
    }

    /// <summary>
    /// 设置基本信息
    /// </summary>
    public PrescriptionBuilder WithBasicInfo(Guid medicalCaseId, Guid patientId, Guid doctorId)
    {
        _prescription.MedicalCaseId = medicalCaseId;
        _prescription.PatientId = patientId;
        _prescription.DoctorId = doctorId;
        _prescription.PrescriptionNo = GeneratePrescriptionNumber();
        _prescription.Status = PrescriptionStatus.Draft;
        _prescription.CreatedAt = DateTime.Now;
        return this;
    }

    /// <summary>
    /// 设置适应症
    /// </summary>
    public PrescriptionBuilder WithIndication(string indication)
    {
        _prescription.Indication = indication;
        return this;
    }

    /// <summary>
    /// 设置用法用量
    /// </summary>
    public PrescriptionBuilder WithDosageInstructions(string instructions, int dosageCount = 7)
    {
        _prescription.UsageInstructions = instructions;
        _prescription.DosageCount = dosageCount;
        return this;
    }

    /// <summary>
    /// 设置折扣
    /// </summary>
    public PrescriptionBuilder WithDiscount(decimal discount)
    {
        if (discount < 0.5m || discount > 1.0m)
            throw new ArgumentException("折扣必须在0.5到1.0之间");

        _prescription.Discount = discount;
        return this;
    }

    /// <summary>
    /// 添加药材项
    /// </summary>
    public PrescriptionItemBuilder AddHerb(Guid herbId)
    {
        var itemBuilder = new PrescriptionItemBuilder(herbId);
        _itemBuilders.Add(itemBuilder);
        return itemBuilder;
    }

    /// <summary>
    /// 从验方模板添加药材
    /// </summary>
    public PrescriptionBuilder AddFromFormula(Guid formulaId, IFormulaService formulaService)
    {
        var formula = formulaService.GetByIdAsync(formulaId).Result;
        if (!formula.IsSuccess)
            throw new InvalidOperationException($"验方不存在: {formulaId}");

        foreach (var formulaItem in formula.Data!.Items)
        {
            AddHerb(formulaItem.HerbId)
                .WithQuantity(formulaItem.Quantity)
                .WithUnit(formulaItem.Unit);
        }

        _prescription.FormulaSource = PrescriptionSource.FromFormula;
        _prescription.FormulaId = formulaId;
        return this;
    }

    /// <summary>
    /// 构建处方
    /// </summary>
    public Prescription Build()
    {
        ValidatePrescription();
        
        // 构建处方项
        _prescription.Items = _itemBuilders
            .Select(builder => builder.Build())
            .ToList();

        // 计算总金额
        _prescription.TotalAmount = CalculateTotalAmount();

        // 设置医嘱
        if (string.IsNullOrEmpty(_prescription.Advice))
        {
            _prescription.Advice = GenerateDefaultAdvice();
        }

        return _prescription;
    }

    /// <summary>
    /// 验证处方
    /// </summary>
    private void ValidatePrescription()
    {
        if (_prescription.MedicalCaseId == Guid.Empty)
            throw new InvalidOperationException("医案ID不能为空");

        if (_prescription.PatientId == Guid.Empty)
            throw new InvalidOperationException("患者ID不能为空");

        if (_prescription.DoctorId == Guid.Empty)
            throw new InvalidOperationException("医生ID不能为空");

        if (!_itemBuilders.Any())
            throw new InvalidOperationException("处方必须包含至少一种药材");

        if (_prescription.DosageCount <= 0)
            throw new InvalidOperationException("帖数必须大于0");

        if (_prescription.Discount < 0.5m || _prescription.Discount > 1.0m)
            throw new InvalidOperationException("折扣必须在0.5到1.0之间");
    }

    /// <summary>
    /// 计算总金额
    /// </summary>
    private decimal CalculateTotalAmount()
    {
        return _itemBuilders
            .Sum(builder => builder.CalculateSubtotal(_prescription.DosageCount));
    }

    /// <summary>
    /// 生成处方编号
    /// </summary>
    private string GeneratePrescriptionNumber()
    {
        var today = DateTime.Now.ToString("yyyyMMdd");
        var prefix = "RX";
        
        // 简化实现：实际应用中需要考虑并发问题
        var sequence = DateTime.Now.Millisecond / 100 + 1;
        return $"{prefix}{today}{sequence:D4}";
    }

    /// <summary>
    /// 生成默认医嘱
    /// </summary>
    private string GenerateDefaultAdvice()
    {
        return "请遵医嘱服用，如有不适请及时就医。";
    }
}

/// <summary>
/// 处方项构建器
/// </summary>
public class PrescriptionItemBuilder
{
    private readonly PrescriptionItem _item;
    private readonly List<Action<PrescriptionItem>> _operations;

    public PrescriptionItemBuilder(Guid herbId)
    {
        _item = new PrescriptionItem
        {
            Id = Guid.NewGuid(),
            HerbId = herbId,
            SortOrder = 0
        };
        _operations = new List<Action<PrescriptionItem>>();
    }

    /// <summary>
    /// 设置数量
    /// </summary>
    public PrescriptionItemBuilder WithQuantity(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("数量必须大于0");

        _operations.Add(item => item.Quantity = quantity);
        return this;
    }

    /// <summary>
    /// 设置单位
    /// </summary>
    public PrescriptionItemBuilder WithUnit(string unit)
    {
        if (string.IsNullOrEmpty(unit))
            throw new ArgumentException("单位不能为空");

        _operations.Add(item => item.Unit = unit);
        return this;
    }

    /// <summary>
    /// 设置单价
    /// </summary>
    public PrescriptionItemBuilder WithUnitPrice(decimal unitPrice)
    {
        if (unitPrice <= 0)
            throw new ArgumentException("单价必须大于0");

        _operations.Add(item => item.UnitPrice = unitPrice);
        return this;
    }

    /// <summary>
    /// 设置特殊用法
    /// </summary>
    public PrescriptionItemBuilder WithSpecialUsage(string usage)
    {
        _operations.Add(item => item.Usage = usage);
        return this;
    }

    /// <summary>
    /// 设置药物地位（君臣佐使）
    /// </summary>
    public PrescriptionItemBuilder WithPosition(string position)
    {
        _operations.Add(item => item.Position = position);
        return this;
    }

    /// <summary>
    /// 设置排序
    /// </summary>
    public PrescriptionItemBuilder WithSortOrder(int sortOrder)
    {
        _operations.Add(item => item.SortOrder = sortOrder);
        return this;
    }

    /// <summary>
    /// 构建处方项
    /// </summary>
    public PrescriptionItem Build()
    {
        // 应用所有操作
        foreach (var operation in _operations)
        {
            operation(_item);
        }

        // 设置创建时间
        _item.CreatedAt = DateTime.Now;

        return _item;
    }

    /// <summary>
    /// 计算小计金额
    /// </summary>
    public decimal CalculateSubtotal(int dosageCount)
    {
        return _item.Quantity * _item.UnitPrice * dosageCount;
    }
}
```

## 🔄 行为型模式 (Behavioral Patterns)

### 1. Strategy 模式 - 算法策略切换

#### 场景应用：价格计算策略
```csharp
/// <summary>
/// 价格计算策略接口
/// </summary>
public interface IPriceCalculationStrategy
{
    decimal CalculatePrice(Prescription prescription);
    string GetStrategyName();
    bool IsApplicable(Prescription prescription);
}

/// <summary>
/// 标准价格计算策略
/// </summary>
public class StandardPriceCalculationStrategy : IPriceCalculationStrategy
{
    private readonly ILogger<StandardPriceCalculationStrategy> _logger;

    public StandardPriceCalculationStrategy(ILogger<StandardPriceCalculationStrategy> logger)
    {
        _logger = logger;
    }

    public decimal CalculatePrice(Prescription prescription)
    {
        if (prescription.Items == null || !prescription.Items.Any())
            return 0;

        decimal total = 0;
        foreach (var item in prescription.Items)
        {
            var subtotal = item.Quantity * item.UnitPrice * prescription.DosageCount;
            total += subtotal;
        }

        var discountedTotal = total * prescription.Discount;
        
        _logger.LogInformation("处方 {PrescriptionId} 标准价格计算: {TotalAmount} (折扣: {Discount:P1})", 
            prescription.Id, discountedTotal, prescription.Discount);
        
        return discountedTotal;
    }

    public string GetStrategyName()
    {
        return "标准价格计算";
    }

    public bool IsApplicable(Prescription prescription)
    {
        return true; // 标准策略总是适用
    }
}

/// <summary>
/// 会员折扣价格计算策略
/// </summary>
public class MemberDiscountPriceCalculationStrategy : IPriceCalculationStrategy
{
    private readonly IPatientService _patientService;
    private readonly ILogger<MemberDiscountPriceCalculationStrategy> _logger;
    private const decimal MEMBER_DISCOUNT_RATE = 0.9m; // 会员折扣率

    public MemberDiscountPriceCalculationStrategy(
        IPatientService patientService,
        ILogger<MemberDiscountPriceCalculationStrategy> logger)
    {
        _patientService = patientService;
        _logger = logger;
    }

    public decimal CalculatePrice(Prescription prescription)
    {
        // 检查患者是否为会员
        var isMember = await IsPatientMemberAsync(prescription.PatientId);
        if (!isMember)
        {
            return new StandardPriceCalculationStrategy(_logger).CalculatePrice(prescription);
        }

        // 先按标准方式计算
        var standardStrategy = new StandardPriceCalculationStrategy(_logger);
        var standardTotal = standardStrategy.CalculatePrice(prescription);

        // 应用会员折扣
        var memberDiscountedTotal = standardTotal * MEMBER_DISCOUNT_RATE;
        
        _logger.LogInformation("处方 {PrescriptionId} 会员折扣价格计算: {TotalAmount} (折扣: {Discount:P1})", 
            prescription.Id, memberDiscountedTotal, MEMBER_DISCOUNT_RATE);
        
        return memberDiscountedTotal;
    }

    public string GetStrategyName()
    {
        return "会员折扣价格计算";
    }

    public bool IsApplicable(Prescription prescription)
    {
        return true; // 检查逻辑在实现中进行
    }

    private async Task<bool> IsPatientMemberAsync(Guid patientId)
    {
        try
        {
            var patient = await _patientService.GetByIdAsync(patientId);
            return patient.IsSuccess && patient.Data!.IsMember;
        }
        catch
        {
            _logger.LogError("检查患者会员状态失败: {PatientId}", patientId);
            return false;
        }
    }
}

/// <summary>
/// 批量采购价格计算策略
/// </summary>
public class BulkPurchasePriceCalculationStrategy : IPriceCalculationStrategy
{
    private readonly IHerbService _herbService;
    private readonly ILogger<BulkPurchasePriceCalculationStrategy> _logger;
    private const decimal BULK_DISCOUNT_THRESHOLD = 100m; // 批量采购折扣阈值
    private const decimal BULK_DISCOUNT_RATE = 0.95m; // 批量采购折扣率

    public BulkPurchasePriceCalculationStrategy(
        IHerbService herbService,
        ILogger<BulkPurchasePriceCalculationStrategy> logger)
    {
        _herbService = herbService;
        _logger = logger;
    }

    public decimal CalculatePrice(Prescription prescription)
    {
        // 检查是否满足批量采购条件
        var isBulkPurchase = await IsBulkPurchaseAsync(prescription);
        if (!isBulkPurchase)
        {
            return new StandardPriceCalculationStrategy(_logger).CalculatePrice(prescription);
        }

        // 先按标准方式计算
        var standardStrategy = new StandardPriceCalculationStrategy(_logger);
        var standardTotal = standardStrategy.CalculatePrice(prescription);

        // 应用批量采购折扣
        var bulkDiscountedTotal = standardTotal * BULK_DISCOUNT_RATE;
        
        _logger.LogInformation("处方 {PrescriptionId} 批量采购价格计算: {TotalAmount} (折扣: {Discount:P1})", 
            prescription.Id, bulkDiscountedTotal, BULK_DISCOUNT_RATE);
        
        return bulkDiscountedTotal;
    }

    public string GetStrategyName()
    {
        return "批量采购价格计算";
    }

    public bool IsApplicable(Prescription prescription)
    {
        return true; // 检查逻辑在实现中进行
    }

    private async Task<bool> IsBulkPurchaseAsync(Prescription prescription)
    {
        if (prescription.Items == null || !prescription.Items.Any())
            return false;

        try
        {
            // 检查是否所有药材都存在
            foreach (var item in prescription.Items)
            {
                var herb = await _herbService.GetByIdAsync(item.HerbId);
                if (!herb.IsSuccess)
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            _logger.LogError("检查批量采购条件失败: {PrescriptionId}", prescription.Id);
            return false;
        }
    }
}

/// <summary>
/// 价格计算上下文
/// </summary>
public class PriceCalculationContext
{
    private readonly Dictionary<Type, IPriceCalculationStrategy> _strategies;
    private readonly ILogger<PriceCalculationContext> _logger;

    public PriceCalculationContext(ILogger<PriceCalculationContext> logger)
    {
        _logger = logger;
        _strategies = new Dictionary<Type, IPriceCalculationStrategy>
        {
            { typeof(StandardPriceCalculationStrategy), new StandardPriceCalculationStrategy(logger) },
            { typeof(MemberDiscountPriceCalculationStrategy), new MemberDiscountPriceCalculationStrategy(
                logger, null!) }, // 实际实现中需要注入服务
            { typeof(BulkPurchasePriceCalculationStrategy), new BulkPurchasePriceCalculationStrategy(
                logger, null!) } // 实际实现中需要注入服务
        };
    }

    /// <summary>
    /// 注册价格计算策略
    /// </summary>
    public void RegisterStrategy<T>() where T : IPriceCalculationStrategy
    {
        var strategyType = typeof(T);
        var strategy = Activator.CreateInstance<T>();
        _strategies[strategyType] = strategy;
    }

    /// <summary>
    /// 注册价格计算策略实例
    /// </summary>
    public void RegisterStrategy(IPriceCalculationStrategy strategy)
    {
        _strategies[strategy.GetType()] = strategy;
    }

    /// <summary>
    /// 计算处方价格
    /// </summary>
    public async Task<decimal> CalculatePriceAsync(Prescription prescription)
    {
        // 找行价格计算链
        var applicableStrategies = _strategies.Values
            .Where(s => s.IsApplicable(prescription))
            .OrderByDescending(s => GetStrategyPriority(s.GetType()));

        foreach (var strategy in applicableStrategies)
        {
            var price = strategy.CalculatePrice(prescription);
            
            _logger.LogInformation("使用价格策略 {StrategyName} 计算处方 {PrescriptionId} 的价格: {Price}", 
                strategy.GetStrategyName(), prescription.Id, price);

            return price;
        }

        // 如果没有适用的策略，使用默认策略
        var defaultStrategy = _strategies[typeof(StandardPriceCalculationStrategy)];
        return defaultStrategy.CalculatePrice(prescription);
    }

    /// <summary>
    /// 获取策略优先级
    /// </summary>
    private int GetStrategyPriority(Type strategyType)
    {
        return strategyType switch
        {
            typeof(BulkPurchasePriceCalculationStrategy) => 3, // 最高优先级
            typeof(MemberDiscountPriceCalculationStrategy) => 2,
            typeof(StandardPriceCalculationStrategy) => 1, // 最低优先级
            _ => 0
        };
    }
}
```

### 2. Observer 模式 - 事件通知系统

#### 场景应用：医案状态变更通知
```csharp
/// <summary>
/// 医案状态变更事件
/// </summary>
public class MedicalCaseStatusChangedEvent
{
    public Guid MedicalCaseId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public MedicalCaseStatus OldStatus { get; set; }
    public MedicalCaseStatus NewStatus { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string Reason { get; set; } = string.Empty;

    public MedicalCaseStatusChangedEvent(
        Guid medicalCaseId, 
        Guid patientId, 
        Guid doctorId,
        MedicalCaseStatus oldStatus, 
        MedicalCaseStatus newStatus,
        string changedBy,
        string reason = "")
    {
        MedicalCaseId = medicalCaseId;
        PatientId = patientId;
        DoctorId = doctorId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        ChangedBy = changedBy;
        ChangedAt = DateTime.Now;
        Reason = reason;
    }
}

/// <summary>
/// 医案状态变更观察者接口
/// </summary>
public interface IMedicalCaseObserver
{
    Task OnStatusChangedAsync(MedicalCaseStatusChangedEvent @event);
    string ObserverName { get; }
}

/// <summary>
/// 处方管理观察者 - 监听医案状态变更
/// </summary>
public class PrescriptionManagementObserver : IMedicalCaseObserver
{
    private readonly IPrescriptionService _prescriptionService;
    private readonly ILogger<PrescriptionManagementObserver> _logger;

    public PrescriptionManagementObserver(
        IPrescriptionService prescriptionService,
        ILogger<PrescriptionObserver> logger)
    {
        _prescriptionService = prescriptionService;
        _logger = logger;
    }

    public string ObserverName => "处方管理观察者";

    public async Task OnStatusChangedAsync(MedicalCaseStatusChangedEvent @event)
    {
        try
        {
            _logger.LogInformation("收到医案状态变更通知: {MedicalCaseId} - {OldStatus} → {NewStatus}", 
                @event.MedicalCaseId, @event.OldStatus, @event.NewStatus);

            // 医案完成时，验证处方
            if (@event.NewStatus == MedicalCaseStatus.Completed)
            {
                await ValidatePrescriptionsAsync(@event.MedicalCaseId);
            }

            // 医案取消时，取消相关处方
            if (@event.NewStatus == MedicalCaseStatus.Cancelled)
            {
                await CancelPrescriptionsAsync(@event.MedicalCaseId);
            }

            // 医案归档时，生成处方报告
            if (@event.NewStatus == MedicalCaseStatus.Archived)
            {
                await GeneratePrescriptionReportAsync(@event.MedicalCaseId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理医案状态变更通知失败: {MedicalCaseId}", @event.MedicalCaseId);
        }
    }

    /// <summary>
    /// 验证处方
    /// </summary>
    private async Task ValidatePrescriptionsAsync(Guid medicalCaseId)
    {
        var prescriptions = await _prescriptionService.GetByMedicalCaseIdAsync(medicalCaseId);
        
        foreach (var prescription in prescriptions.Data)
        {
            if (prescription.Status == PrescriptionStatus.Draft)
            {
                _logger.LogWarning("发现未确认的处方: {PrescriptionId}", prescription.Id);
                // 可以发送通知或采取其他措施
            }
        }
    }

    /// <summary>
    /// 取消处方
    /// </summary>
    private async Task CancelPrescriptionsAsync(Guid medicalCaseId)
    {
        var prescriptions = await _prescriptionService.GetByMedicalCaseIdAsync(medicalCaseId);
        
        foreach (var prescription in prescriptions.Data)
        {
            if (prescription.Status != PrescriptionStatus.Completed)
            {
                await _prescriptionService.UpdateStatusAsync(prescription.Id, PrescriptionStatus.Cancelled);
                _logger.LogInformation("处方已取消: {PrescriptionId}", prescription.Id);
            }
        }
    }

    /// <summary>
    /// 生成处方报告
    /// </summary>
    private async Task GeneratePrescriptionReportAsync(Guid medicalCaseId)
    {
        var prescriptions = await _prescriptionService.GetByMedicalCaseIdAsync(medicalCaseId);
        var reportService = _serviceProvider.GetService<IPrescriptionReportService>();
        
        if (reportService != null)
        {
            await reportService.GenerateReportAsync(medicalCaseId, prescriptions.Data);
            _logger.LogInformation("已生成处方报告: {MedicalCaseId}", medicalCaseId);
        }
    }
}

/// <summary>
/// 患者管理观察者 - 监听医案状态变更
/// </summary>
public class PatientManagementObserver : IMedicalCaseObserver
{
    private readonly IPatientService _patientService;
    private readonly ILogger<PatientManagementObserver> _logger;

    public PatientManagementObserver(
        IPatientService patientService,
        ILogger<PatientManagementObserver> logger)
    {
        _patientService = patientService;
        _logger = logger;
    }

    public string ObserverName => "患者管理观察者";

    public async Task OnStatusChangedAsync(MedicalCaseStatusChangedEvent @event)
    {
        try
        {
            _logger.LogInformation("收到医案状态变更通知: {MedicalCaseId} - {OldStatus} → {NewStatus}", 
                @event.MedicalCaseId, @event.OldStatus, @event.NewStatus);

            // 更新患者最近就诊信息
            if (@event.NewStatus == MedicalCaseStatus.Completed)
            {
                await UpdatePatientLastVisitAsync(@event.PatientId, @event.MedicalCaseId);
            }

            // 发送医案状态变更通知给患者
            if (@event.NewStatus == MedicalCaseStatus.Completed || 
                @event.NewStatus == MedicalCaseStatus.Archived)
            {
                await NotifyPatientAsync(@event);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理医案状态变更通知失败: {MedicalCaseId}", @event.MedicalCaseId);
        }
    }

    /// <summary>
    /// 更新患者最近就诊信息
    /// </summary>
    private async Task UpdatePatientLastVisitAsync(Guid patientId, Guid medicalCaseId)
    {
        var patient = await _patientService.GetByIdAsync(patientId);
        if (patient.IsSuccess)
        {
            var updateDto = new PatientUpdateDto
            {
                LastVisitAt = DateTime.Now,
                LastMedicalCaseId = medicalCaseId
            };
            
            await _patientService.UpdateAsync(patientId, updateDto);
            _logger.LogInformation("更新患者最近就诊信息: {PatientId}", patientId);
        }
    }

    /// <summary>
    /// 通知患者医案状态变更
    /// </summary>
    private async Task NotifyPatientAsync(MedicalCaseStatusChangedEvent @event)
    {
        var notificationService = _serviceProvider.GetService<INotificationService>();
        
        if (notificationService != null)
        {
            var notification = new MedicalCaseNotification
            {
                Type = @event.NewStatus switch
                {
                    MedicalCaseStatus.Completed => "医案完成",
                    MedicalCaseStatus.Archived => "医案归档",
                    MedicalCaseStatus.Cancelled => "医案取消",
                    _ => "医案状态变更"
                },
                PatientId = @event.PatientId,
                MedicalCaseId = @event.MedicalCaseId,
                Message = $"您的医案状态已变更为{@event.NewStatus}",
                Timestamp = @event.ChangedAt
            };

            await notificationService.SendNotificationAsync(@event.PatientId, notification);
        }
    }
}

/// <summary>
/// 医案状态变更事件发布者
/// </summary>
public class MedicalCaseEventPublisher
{
    private readonly List<IMedicalCaseObserver> _observers;
    private readonly ILogger<MedicalCaseEventPublisher> _logger;
    private readonly SemaphoreSlim _semaphore;

    public MedicalCaseEventPublisher(ILogger<MedicalCaseEventPublisher> logger)
    {
        _observers = new List<IMedicalCaseObserver>();
        _logger = logger;
        _semaphore = new SemaphoreSlim(1, 1); // 确保线程安全
    }

    /// <summary>
    /// 注册观察者
    /// </summary>
    public void RegisterObserver(IMedicalCaseObserver observer)
    {
        lock (_observers)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
                _logger.LogInformation("注册医案状态观察者: {ObserverName}", observer.ObserverName);
            }
        }
    }

    /// <summary>
    /// 移除观察者
    /// </summary>
    public void UnregisterObserver(IMedicalCaseObserver observer)
    {
        lock (_observers)
        {
            if (_observers.Contains(observer))
            {
                _observers.Remove(observer);
                _logger.LogInformation("移除医案状态观察者: {ObserverName}", observer.ObserverName);
            }
        }
    }

    /// <summary>
    /// 发布医案状态变更事件
    /// </summary>
    public async Task PublishStatusChangedAsync(MedicalCaseStatusChangedEvent @event)
    {
        await _semaphore.WaitAsync();
        
        try
        {
            _logger.LogInformation("发布医案状态变更事件: {MedicalCaseId} - {OldStatus} → {NewStatus}", 
                @event.MedicalCaseId, @event.OldStatus, @event.NewStatus);

            // 并行通知所有观察者
            var tasks = _observers.Select(observer => 
                observer.OnStatusChangedAsync(@event));
            
            await Task.WhenAll(tasks);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

## 🏗️ 结构型模式 (Structural Patterns)

### 1. Decorator 模式 - 功能增强

#### 场景应用：处方功能增强
```csharp
/// <summary>
/// 处方增强器基类
/// </summary>
public abstract class PrescriptionDecorator
{
    protected readonly Prescription _prescription;
    protected readonly Prescription _enhancedPrescription;

    protected PrescriptionDecorator(Prescription prescription)
    {
        _prescription = prescription ?? throw new ArgumentNullException(nameof(prescription));
        _enhancedPrescription = (Prescription)_prescription.Clone();
    }

    /// <summary>
    /// 获取增强后的处方
    /// </summary>
    public virtual Prescription GetPrescription()
    {
        return _enhancedPrescription;
    }

    /// <summary>
    /// 获取原始处方
    /// </summary>
    public Prescription GetOriginalPrescription()
    {
        return _prescription;
    }
}

/// <summary>
/// 药材过敏检查增强器
/// </summary>
public class AllergyCheckDecorator : PrescriptionDecorator
{
    private readonly IAllergyCheckService _allergyCheckService;
    private readonly ILogger<AllergyCheckDecorator> _logger;

    public AllergyCheckDecorator(
        Prescription prescription,
        IAllergyCheckService allergyCheckService,
        ILogger<AllergyCheckDecorator> logger) 
        : base(prescription)
    {
        _allergyCheckService = allergyCheckService;
        _logger = logger;
    }

    public override Prescription GetPrescription()
    {
        // 执行过敏检查
        var enhancedPrescription = (Prescription)base.GetPrescription();
        
        var allergyIssues = new List<string>();
        foreach (var item in enhancedPrescription.Items)
        {
            var allergies = await _allergyCheckService.CheckHerbAllergiesAsync(item.HerbId, _enhancedPrescription.PatientId);
            if (allergies.Any())
            {
                allergyIssues.Add($"{item.HerbName}: {string.Join(", ", allergies)}");
            }
        }

        if (allergyIssues.Any())
        {
            enhancedPrescription.Status = PrescriptionStatus.RequiresReview;
            enhancedPrescription.AllergyWarnings = allergyIssues;
            enhancedPrescription.Notes = $"过敏警告: {string.Join("; ", allergyIssues)}";
            
            _logger.LogWarning("处方 {PrescriptionId} 检测到过敏风险: {Warnings}", 
                enhancedPrescription.Id, string.Join("; ", allergyIssues));
        }
        else
        {
            enhancedPrescription.AllergyWarnings = new List<string>();
        }

        return enhancedPrescription;
    }
}

/// <summary>
/// 药材配伍检查增强器
/// </summary>
public class CompatibilityCheckDecorator : PrescriptionDecorator
{
    private readonly IHerbCompatibilityService _compatibilityService;
    private readonly ILogger<CompatibilityCheckDecorator> _logger;

    public CompatibilityCheckDecorator(
        Prescription prescription,
        IHerbCompatibilityService compatibilityService,
        ILogger<CompatibilityCheckDecorator> logger) 
        : base(prescription)
    {
        _compatibilityService = compatibilityService;
        _logger = logger;
    }

    public override Prescription GetPrescription()
    {
        // 获取当前处方
        var enhancedPrescription = (Prescription)base.GetPrescription();
        
        // 提取药材ID列表
        var herbIds = enhancedPrescription.Items.Select(item => item.HerbId).ToList();
        
        if (herbIds.Count < 2)
        {
            return enhancedPrescription; // 单一味无需配伍检查
        }

        // 执行配伍检查
        var compatibilityIssues = await _compatibilityService.CheckCompatibilityAsync(herbIds);
        
        if (compatibilityIssues.Any())
        {
            enhancedPrescription.Status = PrescriptionStatus.RequiresReview;
            enhancedPrescription.CompatibilityWarnings = compatibilityIssues;
            enhancedPrescription.Notes = $"配伍警告: {string.Join("; ", compatibilityIssues)}";
            
            _logger.LogWarning("处方 {PrescriptionId} 检测到配伍风险: {Warnings}", 
                enhancedPrescription.Id, string.Join("; ", compatibilityIssues));
        }
        else
        {
            enhancedPrescription.CompatibilityWarnings = new List<string>();
        }

        return enhancedPrescription;
    }
}

/// <summary>
/// 处方装饰器工厂
/// </summary>
public class PrescriptionDecoratorFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PrescriptionDecoratorFactory> _logger;

    public PrescriptionDecoratorFactory(IServiceProvider serviceProvider, ILogger<PrescriptionFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 创建增强处方
    /// </summary>
    public Prescription CreateEnhancedPrescription(
        Prescription prescription,
        bool enableAllergyCheck = true,
        bool enableCompatibilityCheck = true)
    {
        Prescription enhancedPrescription = prescription;

        if (enableAllergyCheck)
        {
            var allergyCheckService = _serviceProvider.GetService<IAllergyCheckService>();
            enhancedPrescription = new AllergyCheckDecorator(enhancedPrescription, allergyCheckService, _logger);
        }

        if (enableCompatibilityCheck)
        {
            var compatibilityService = _serviceProvider.GetService<IHerbCompatibilityService>();
            enhancedPrescription = new CompatibilityCheckDecorator(enhancedPrescription, compatibilityService, _logger);
        }

        return enhancedPrescription;
    }

    /// <summary>
    /// 根据配置创建增强处方
    /// </summary>
    public Prescription CreateEnhancedPrescription(Prescription prescription, PrescriptionEnhancementConfig config)
    {
        return CreateEnhancedPrescription(
            prescription,
            config.EnableAllergyCheck,
            config.EnableCompatibilityCheck);
    }
}

/// <summary>
/// 处方增强配置
/// </summary>
public class PrescriptionEnhancementConfig
{
    public bool EnableAllergyCheck { get; set; } = true;
    public bool EnableCompatibilityCheck { get; set; } = true;
}
```

### 2. Proxy 模式 - 访问控制

#### 场景应用：数据访问代理
```csharp
/// <summary>
/// 患者数据访问代理
/// </summary>
public class PatientRepositoryProxy : IPatientRepository
{
    private readonly IPatientRepository _repository;
    private readonly ILogger<PatientRepositoryProxy> _logger;
    private readonly ICacheService _cacheService;
    private readonly IPermissionService _permissionService;
    private readonly IAuditService _auditService;

    public PatientRepositoryProxy(
        IPatientRepository repository,
        ILogger<PatientRepositoryProxy> logger,
        ICacheService cacheService,
        IPermissionService permissionService,
        IAuditService auditService)
    {
        _repository = repository;
        _logger = logger;
        _cacheService = cacheService;
        _permissionService = permissionService;
        _auditService = auditService;
    }

    public async Task<Patient?> GetByIdAsync(Guid id)
    {
        try
        {
            // 记录访问尝试
            await _auditService.LogDataAccessAsync(new DataAccessEvent
            {
                EntityName = "Patient",
                Action = "GetById",
                RecordId = id.ToString(),
                UserId = GetCurrentUser(),
                IPAddress = GetCurrentIPAddress()
            });

            // 检查权限
            if (!await _permissionService.HasPermissionAsync(GetCurrentUser(), GetUserRole(), Permissions.PatientRead))
            {
                _logger.LogWarning("用户 {UserId} 无权限查看患者信息", GetCurrentUser());
                throw new UnauthorizedAccessException("无权限查看患者信息");
            }

            // 尝试从缓存获取
            var cacheKey = $"patient:{id}";
            var cachedPatient = await _cacheService.GetAsync<Patient>(cacheKey);
            if (cachedPatient != null)
            {
                _logger.LogDebug("从缓存获取患者信息: {PatientId}", id);
                return cachedPatient;
            }

            // 从数据库获取
            var patient = await _repository.GetByIdAsync(id);
            
            // 缓存结果
            if (patient != null)
            {
                await _cacheService.SetAsync(cacheKey, patient, TimeSpan.FromMinutes(15));
            }

            return patient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者信息失败: {PatientId}", id);
            throw;
        }
    }

    public async Task<List<Patient>> GetAllAsync()
    {
        try
        {
            // 记录访问尝试
            await _auditService.LogDataAccessAsync(new DataAccessEvent
            {
                EntityName = "Patient",
                Action = "GetAll",
                UserId = GetCurrentUser(),
                IPAddress = GetCurrentIPAddress()
            });

            // 检查权限
            if (!await _permissionService.HasPermissionAsync(GetCurrentUser(), GetUserRole(), Permissions.PatientRead))
            {
                _logger.LogWarning("用户 {UserId} 无权限查看患者列表", GetCurrentUser());
                throw new UnauthorizedAccessException("无权限查看患者列表");
            }

            // 从数据库获取
            var patients = await _repository.GetAllAsync();
            
            // 应用数据访问过滤
            var filteredPatients = await ApplyDataFilterAsync(patients);
            
            return filteredPatients;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者列表失败");
            throw;
        }
    }

    public async Task<PagedResult<Patient>> GetPagedAsync(int page, int pageSize, string? keyword = null)
    {
        try
        {
            // 记录访问尝试
            await _auditService.LogDataAccessAsync(new DataAccessEvent
            {
                EntityName = "Patient",
                Action = "GetPaged",
                RecordId = $"page={page},pageSize={pageSize},keyword={keyword}",
                UserId = GetCurrentUser(),
                IPAddress = GetCurrentIPAddress()
            });

            // 检查权限
            if (!await _permissionService.HasPermissionAsync(GetCurrentUser(), GetUserRole(), Permissions.PatientRead))
            {
                _logger.LogWarning("用户 {UserId} 无权限查看患者分页列表", GetCurrentUser());
                throw new UnauthorizedAccessException("无权限查看患者分页列表");
            }

            // 从数据库获取
            var pagedResult = await _repository.GetPagedAsync(page, pageSize, keyword);
            
            // 应用数据访问过滤
            var filteredItems = await ApplyDataFilterAsync(pagedResult.Items);
            
            var filteredResult = new PagedResult<Patient>
            {
                Items = filteredItems,
                TotalCount = pagedResult.TotalCount,
                CurrentPage = pagedResult.CurrentPage,
                PageSize = pagedResult.PageSize
            };

            return filteredResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者分页列表失败");
            throw;
        }
    }

    public async Task<Patient> AddAsync(Patient entity)
    {
        try
        {
            // 记录访问尝试
            await _auditService.LogDataAccessAsync(new DataAccessEvent
            {
                EntityName = "Patient",
                Action = "Add",
                RecordId = entity.Id.ToString(),
                UserId = GetCurrentUser(),
                IPAddress = GetCurrentIPAddress(),
                AdditionalData = new
                {
                    PatientName = entity.Name,
                    PhoneNumber = entity.PhoneNumber
                }
            });

            // 检查权限
            if (!await _permissionService.HasPermissionAsync(GetCurrentUser(), GetUserRole(), Permissions.PatientCreate))
            {
                _logger.LogWarning("用户 {UserId} 无权限创建患者", GetCurrentUser());
                throw new UnauthorizedAccessException("无权限创建患者");
            }

            // 检查数据完整性
            await ValidatePatientAsync(entity);

            // 创建患者
            var result = await _repository.AddAsync(entity);
            
            // 清除相关缓存
            await InvalidatePatientCacheAsync(entity.Id);
            
            _logger.LogInformation("患者创建成功: {PatientId} - {PatientName}", 
                result.Id, result.Name);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建患者失败: {PatientId}", entity?.Id);
            throw;
        }
    }

    public async Task<Patient> UpdateAsync(Patient entity)
    {
        try
        {
            // 记录访问尝试
            await _auditService.LogDataAccessAsync(new DataAccessEvent
            {
                EntityName = "Patient",
                Action = "Update",
                RecordId = entity.Id.ToString(),
                UserId = GetCurrentUser(),
                IPAddress = GetCurrentIPAddress(),
                AdditionalData = new
                {
                    PatientName = entity.Name,
                    PhoneNumber = entity.PhoneNumber
                }
            });

            // 检查权限
            if (!await _permissionService.HasPermissionAsync(GetCurrentUser(), GetUserRole(), Permissions.PatientUpdate))
            {
                _logger.LogWarning("用户 {UserId} 无权限更新患者", GetCurrentUser());
                throw new UnauthorizedAccessException("无权限更新患者");
            }

            // 检查数据完整性
            await ValidatePatientAsync(entity);

            // 更新患者
            var result = await _repository.UpdateAsync(entity);
            
            // 清除相关缓存
            await InvalidatePatientCacheAsync(entity.Id);
            
            _logger.LogInformation("患者更新成功: {PatientId} - {PatientName}", 
                result.Id, result.Name);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新患者失败: {PatientId}", entity?.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            // 记录访问尝试
            await _auditService.LogDataAccessAsync(new DataAccessEvent
            {
                EntityName = "Patient",
                Action = "Delete",
                RecordId = id.ToString(),
                UserId = GetCurrentUser(),
                IPAddress = GetCurrentIPAddress()
            });

            // 检查权限
            if (!await _permissionService.HasPermissionAsync(GetCurrentUser(), GetUserRole(), Permissions.PatientDelete))
            {
                _logger.LogWarning("用户 {UserId} 无权限删除患者", GetCurrentUser());
                throw new UnauthorizedAccessException("无权限删除患者");
            }

            // 检查是否可以删除（有关联数据）
            var canDelete = await CanDeletePatientAsync(id);
            if (!canDelete)
            {
                throw new InvalidOperationException("患者存在关联数据，无法删除");
            }

            // 软删除
            var result = await _repository.DeleteAsync(id);
            
            // 清除相关缓存
            await InvalidatePatientCacheAsync(id);
            
            if (result)
            {
                _logger.LogInformation("患者删除成功: {PatientId}", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除患者失败: {PatientId}", id);
            throw;
        }
    }

    /// <summary>
    /// 应用数据访问过滤
    /// </summary>
    private async Task<List<T>> ApplyDataFilterAsync<T>(List<T> items) where T : class
    {
        // 根据用户权限过滤数据
        var userId = GetCurrentUser();
        var userRole = GetUserRole();

        // 超级管理员可以看到所有数据
        if (userRole == UserRole.SuperAdmin)
        {
            return items;
        }

        // 普通用户只能看到自己相关的数据
        var filteredItems = new List<T>();
        foreach (var item in items)
        {
            // 根据实体类型应用不同的过滤规则
            if (item is Patient patient)
            {
                // 医生只能看到自己负责的患者
                if (userRole == UserRole.Doctor)
                {
                    // 检查是否是医生负责的患者
                    if (await IsDoctorResponsiblePatientAsync(userId, patient.Id))
                    {
                        filteredItems.Add(item);
                    }
                }
            }
            else if (item is MedicalCase medicalCase)
            {
                // 医生只能看到自己创建的医案
                if (medicalCase.DoctorId == userId)
                {
                    filteredItems.Add(item);
                }
            }
        }

        return filteredItems;
    }

    /// <summary>
    /// 验证患者数据完整性
    /// </summary>
    private async Task ValidatePatientAsync(Patient patient)
    {
        // 检查必填字段
        if (string.IsNullOrWhiteSpace(patient.Name))
            throw new ValidationException("患者姓名不能为空");

        // 检查手机号格式
        if (!Regex.IsMatch(patient.PhoneNumber, @"^1[3-9]\d{9}$"))
            throw new ValidationException("手机号格式不正确");

        // 检查身份证号格式（如果提供）
        if (!string.IsNullOrEmpty(patient.IdNumber) && 
            !Regex.IsMatch(patient.IdNumber, @"^\d{17}[\dX]$"))
            throw new ValidationException("身份证号格式不正确");

        // 检查重复
        var existingPatient = await _repository.GetByPhoneAsync(patient.PhoneNumber);
        if (existingPatient != null && existingPatient.Id != patient.Id)
        {
            throw new ValidationException("手机号已存在");
        }
    }

    /// <summary>
    /// 检查患者是否可以删除
    /// </summary>
    private async Task<bool> CanDeletePatientAsync(Guid patientId)
    {
        // 检查是否有相关的医案
        var medicalCases = await _serviceProvider.GetService<IMedicalCaseRepository>()
            .GetByPatientIdAsync(patientId);

        if (medicalCases.Any(mc => mc.Status != MedicalCaseStatus.Archived))
        {
            return false;
        }

        // 检查是否有相关的处方
        var prescriptions = await _serviceProvider.GetService<IPrescriptionRepository>()
            .GetByPatientIdAsync(patientId);

        if (prescriptions.Any(p => p.Status == PrescriptionStatus.Completed || p.Status == PrescriptionStatus.Dispensed))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 检查医生是否负责该患者
    /// </summary>
    private async Task<bool> IsDoctorResponsiblePatientAsync(Guid doctorId, Guid patientId)
    {
        var medicalCases = await _serviceProvider.GetService<IMedicalCaseRepository>()
            .GetByPatientIdAsync(patientId);

        return medicalCases.Any(mc => mc.DoctorId == doctorId);
    }

    /// <summary>
    /// 清除患者缓存
    /// </summary>
    private async Task InvalidatePatientCacheAsync(Guid patientId)
    {
        var cacheKeys = new[]
        {
            $"patient:{patientId}",
            $"patient:search:{patientId}",
            $"patients:list"
        };

        foreach (var key in cacheKeys)
        {
            await _cacheService.RemoveAsync(key);
        }
    }

    private Guid GetCurrentUser()
    {
        // 从当前上下文获取用户ID
        return Guid.Empty; // 实际实现中需要从HttpContext或当前用户上下文获取
    }

    private UserRole GetUserRole()
    {
        // 从当前上下文获取用户角色
        return UserRole.User; // 实际实现中需要从HttpContext或当前用户上下文获取
    }

    private string GetCurrentIPAddress()
    {
        // 从当前上下文获取IP地址
        return "127.0.0.1"; // 实际实现中需要从HttpContext获取
    }
}
```

## 🔧 组合模式应用

### 医案构建器 + 策略模式

#### 完整的处方创建流程
```csharp
/// <summary>
/// 医案管理服务 - 集成多种设计模式
/// </summary>
public class MedicalCaseService : IMedicalCaseService
{
    private readonly IMedicalCaseRepository _repository;
    private readonly IPriceCalculationContext _priceCalculationContext;
    private readonly MedicalCaseEventPublisher _eventPublisher;
    private readonly PrescriptionDecoratorFactory _decoratorFactory;
    private readonly ILogger<MedicalCaseService> _logger;

    public MedicalCaseService(
        IMedicalCaseRepository repository,
        IPriceCalculationContext priceCalculationContext,
        MedicalCaseEventPublisher eventPublisher,
        PrescriptionDecoratorFactory decoratorFactory,
        ILogger<MedicalCaseService> logger)
    {
        _repository = repository;
        _priceCalculationContext = priceCalculationContext;
        _eventPublisher = eventPublisher;
        _decoratorFactory = decoratorFactory;
        _logger = logger;
    }

    /// <summary>
    /// 创建医案并关联处方
    /// </summary>
    public async Task<MedicalCaseResult> CreateMedicalCaseWithPrescriptionsAsync(
        MedicalCaseCreateDto medicalCaseDto,
        List<PrescriptionCreateDto> prescriptionDtos,
        string doctorId)
    {
        try
        {
            _logger.LogInformation("开始创建医案并关联处方: PatientId={PatientId}, 处方数量={PrescriptionCount}", 
                medicalCaseDto.PatientId, prescriptionDtos.Count);

            // 1. 创建医案
            var medicalCase = new MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = medicalCaseDto.PatientId,
                DoctorId = Guid.Parse(doctorId),
                Title = medicalCaseDto.Title,
                ChiefComplaint = medicalCaseDto.ChiefComplaint,
                Status = MedicalCaseStatus.Registered,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            medicalCase = await _repository.AddAsync(medicalCase);

            // 2. 创建处方
            var prescriptions = new List<Prescription>();
            foreach (var prescriptionDto in prescriptionDtos)
            {
                var prescription = new Prescription
                {
                    Id = Guid.NewGuid(),
                    MedicalCaseId = medicalCase.Id,
                    PatientId = medicalCase.PatientId,
                    DoctorId = Guid.Parse(doctorId),
                    Indication = prescriptionDto.Indication,
                    DosageCount = prescriptionDto.DosageCount,
                    Status = PrescriptionStatus.Draft,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                // 使用构建器模式创建处方项
                var prescriptionBuilder = new PrescriptionBuilder();
                foreach (var itemDto in prescriptionDto.Items)
                {
                    prescriptionBuilder
                        .AddHerb(itemDto.HerbId)
                        .WithQuantity(itemDto.Quantity)
                        .WithUnit(itemDto.Unit)
                        .WithUnitPrice(itemDto.UnitPrice);
                }

                prescription = prescriptionBuilder.Build();
                prescription = await _repository.AddAsync(prescription);
                prescriptions.Add(prescription);
            }

            // 3. 创建增强处方（应用装饰器模式）
            var enhancedPrescriptions = prescriptions.Select(p =>
                _decoratorFactory.CreateEnhancedPrescription(
                    p,
                    enableAllergyCheck: true,
                    enableCompatibilityCheck: true
                )).ToList();

            // 4. 更新医案状态
            medicalCase.Status = MedicalCase.InTreatment;
            medicalCase.UpdatedAt = DateTime.Now;
            await _repository.UpdateAsync(medicalCase);

            // 5. 发布医案创建事件
            await _eventPublisher.PublishStatusChangedAsync(new MedicalCaseStatusChangedEvent(
                medicalCase.Id,
                medicalCase.PatientId,
                medicalCase.DoctorId,
                MedicalCaseStatus.Registered,
                MedicalCase.InTreatment,
                doctorId,
                "医案创建"
            ));

            // 6. 计算总价
            var totalPrice = await CalculateTotalPriceAsync(enhancedPrescriptions);
            
            _logger.LogInformation("医案创建成功: {MedicalCaseId}, 处方数量: {Count}, 总价: {TotalPrice}", 
                medicalCase.Id, enhancedPrescriptions.Count, totalPrice);

            return new MedicalCaseResult
            {
                MedicalCase = medicalCase,
                Prescriptions = enhancedPrescriptions,
                TotalPrice = totalPrice
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建医案并关联处方失败");
            throw;
        }
    }

    /// <summary>
    /// 计算处方总价
    /// </summary>
    private async Task<decimal> CalculateTotalPriceAsync(List<Prescription> prescriptions)
    {
        decimal total = 0;
        
        foreach (var prescription in prescriptions)
        {
            // 使用策略模式计算价格
            var price = await _priceCalculationContext.CalculatePriceAsync(prescription);
            total += price;
        }

        return total;
    }
}

/// <summary>
/// 医案创建结果
/// </summary>
public class MedicalCaseResult
{
    public MedicalCase MedicalCase { get; set; }
    public List<Prescription> Prescriptions { get; set; }
    public decimal TotalPrice { get; set; }
}
```

## 📊 高级模式应用案例

### 1. 复杂工作流编排

#### 患者完整诊疗工作流
```csharp
/// <summary>
/// 医生诊疗工作流编排器
/// </summary>
public class DiagnosisWorkflowOrchestrator
{
    private readonly List<IWorkflowStep> _steps;
    private readonly ILogger<DiagnosisWorkflowOrchestrator> _logger;

    public DiagnosisWorkflowOrchestrator(ILogger<DiagnosisWorkflowOrchestrator> logger)
    {
        _logger = logger;
        _steps = new List<IWorkflowStep>();
        
        InitializeSteps();
    }

    /// <summary>
    /// 添加工作流步骤
    /// </summary>
    public DiagnosisWorkflowOrchestrator AddStep(IWorkflowStep step)
    {
        _steps.Add(step);
        return this;
    }

    /// <summary>
    /// 移除工作流步骤
    /// </summary>
    public DiagnosisWorkflowOrchestrator RemoveStep(string stepName)
    {
        var step = _steps.FirstOrDefault(s => s.StepName == stepName);
        if (step != null)
        {
            _steps.Remove(step);
        }
        return this;
    }

    /// <summary>
    /// 执行完整工作流
    /// </summary>
    public async Task<WorkflowResult> ExecuteAsync(DiagnosisWorkflowContext context)
    {
        var result = new WorkflowResult { Success = true, Steps = new List<WorkflowStepResult>() };

        try
        {
            _logger.LogInformation("开始执行诊疗工作流: PatientId={PatientId}, MedicalCaseId={MedicalCaseId}", 
                context.PatientId, context.MedicalCaseId);

            foreach (var step in _steps)
            {
                var stepResult = await step.ExecuteAsync(context);
                
                result.Steps.Add(new WorkflowStepResult
                {
                    StepName = step.StepName,
                    Success = stepResult.Success,
                    Message = stepResult.Message,
                    Timestamp = DateTime.Now
                });

                if (!stepResult.Success)
                {
                    _logger.LogError("工作流步骤失败: {StepName} - {Error}", step.StepName, stepResult.Message);
                    result.Success = false;
                    result.ErrorMessage = stepResult.Message;
                    break;
                }

                // 更新上下文
                context = stepResult.Context ?? context;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "诊疗工作流执行失败");
            result.Success = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// 初始化工作流步骤
    /// </summary>
    private void InitializeSteps()
    {
        // 1. 患者接诊
        AddStep(new PatientRegistrationStep());

        // 2. 四诊合参
        AddStep(new FourDiagnosticExaminationStep());

        // 3. 辨证论治
        AddStep SyndromeDifferentiationStep());

        // 4. 处方开方
        AddStep PrescriptionCreationStep());

        // 5. 完成医案
        AddStep MedicalCaseCompletionStep());

        // 6. 患后随访
        AddStep FollowUpStep());
    }
}

/// <summary>
/// 工作流上下文
/// </summary>
public class DiagnosisWorkflowContext
{
    public Guid PatientId { get; set; }
    public Guid MedicalCaseId { get; set; }
    public Guid DoctorId { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();

    // 诊疗数据
    public InspectionResult? InspectionResult { get; set; }
    public AuscultationResult? AuscultationResult { get; set; }
    public InquiryResult? InquiryResult { get; set; }
    public PalpationResult? PalpationResult { get; set; }
    public Diagnosis? Diagnosis { get; set; }
    public TreatmentPrinciple? TreatmentPrinciple { get; set; }

    // 处方数据
    public List<PrescriptionCreateDto> PrescriptionItems { get; set; } = new();
    public string Indication { get; set; } = string.Empty;
    public int DosageCount { get; set; } = 7;
    public decimal Discount { get; set; } = 1.0m;
}

/// <summary>
/// 患者注册步骤
/// </summary>
public class PatientRegistrationStep : IWorkflowStep
{
    public string StepName => "患者注册";

    public async Task<WorkflowStepResult> ExecuteAsync(DiagnosisWorkflowContext context)
    {
        try
        {
            // 验证患者信息
            if (context.PatientId == Guid.Empty)
            {
                return new WorkflowStepResult { 
                    Success = false, 
                    Message = "患者ID不能为空" 
                };
            }

            // 更新工作流状态
            context.Data["Step"] = "患者注册";
            
            // 执行业务逻辑
            // 这里可以添加患者信息更新、状态变更等操作
            
            return new WorkflowStepResult { 
                Success = true,
                Context = context
            };
        }
        catch (Exception ex)
        {
            return new WorkflowStepResult { 
                Success = false, 
                Message = $"患者注册失败: {ex.Message}" 
            };
        }
    }
}
```

### 2. 复杂事件处理

#### 事件处理链模式
```csharp
/// <summary>
/// 事件处理器链管理器
/// </summary>
public class EventHandlerChain
{
    private readonly List<IEventHandler> _handlers;
    private readonly ILogger<EventHandlerChain> _logger;

    public EventHandlerChain(ILogger<EventHandlerChain> logger)
    {
        _handlers = new List<IEventHandler>();
        _logger = logger;
    }

    /// <summary>
    /// 添加事件处理器
    /// </summary>
    public EventHandlerChain AddHandler(IEventHandler handler)
    {
        _handlers.Add(handler);
        _logger.LogInformation("添加事件处理器: {HandlerType}", handler.GetType().Name);
        return this;
    }

    /// <summary>
    /// 处理事件
    /// </summary>
    public async Task<EventHandlerResult> HandleEventAsync(IDomainEvent @event)
    {
        var result = new EventHandlerResult { Success = true };

        try
        {
            _logger.LogInformation("开始处理事件: {EventType}, ID: {EventId}", @event.GetType().Name);

            // 按顺序执行所有处理器
            foreach (var handler in _handlers)
            {
                var handlerResult = await handler.HandleAsync(@event);
                
                if (!handlerResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = handlerResult.ErrorMessage;
                    break;
                }

                // 更新事件数据
                if (handlerResult.UpdatedEvent != null)
                {
                    @event = handlerResult.UpdatedEvent;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "事件处理失败: {EventType}", @event.GetType().Name);
            return new EventHandlerResult { 
                Success = false, 
                ErrorMessage = ex.Message 
            };
        }
    }
}

/// <summary>
/// 事件处理结果
/// </summary>
public class EventHandlerResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public IDomainEvent? UpdatedEvent { get; set; }
}

/// <summary>
/// 医案状态变更事件处理器
/// </summary>
public class MedicalCaseStatusChangedHandler : IEventHandler
{
    private readonly IPrescriptionService _prescriptionService;
    private readonly IPatientService _patientService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<MedicalCaseStatusChangedHandler> _logger;

    public MedicalCaseStatusChangedHandler(
        IPrescriptionService prescriptionService,
        IPatientService patientService,
        INotificationService notificationService,
        ILogger<MedicalCaseStatusChangedHandler> logger)
    {
        _prescriptionService = prescriptionService;
        _patientService = patientService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<EventHandlerResult> HandleAsync(IDomainEvent @event)
    {
        try
        {
            if (@event is MedicalCaseStatusChangedEvent statusEvent)
            {
                await HandleStatusChangedAsync(statusEvent);
            }

            return new EventHandlerResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理医案状态变更事件失败");
            return new EventHandlerResult { 
                Success = false, 
                ErrorMessage = ex.Message 
            };
        }
    }

    /// <summary>
    /// 处理状态变更
    /// </summary>
    private async Task HandleStatusChangedAsync(MedicalCaseStatusChangedEvent statusEvent)
    {
        // 更新处方状态
        if (statusEvent.NewStatus == MedicalCaseStatus.Cancelled)
        {
            var prescriptions = await _prescriptionService.GetByMedicalCaseIdAsync(statusEvent.MedicalCaseId);
            foreach (var prescription in prescriptions.Data)
            {
                await _prescriptionService.UpdateStatusAsync(prescription.Id, PrescriptionStatus.Cancelled);
            }
        }

        // 发送通知
        await _notificationService.SendNotificationAsync(statusEvent.PatientId, 
            new NotificationMessage
            {
                Type = "医案状态通知",
                Title = statusEvent.NewStatus switch
                {
                    MedicalCaseStatus.Completed => "医案完成",
                    MedicalCaseStatus.Cancelled => "医案取消",
                    MedicalCaseStatus.Archived => "医案归档",
                    _ => "医案状态变更"
                },
                Message = $"您的医案状态已变更为{statusEvent.NewStatus}",
                Timestamp = statusEvent.ChangedAt
            });
    }
}
```

---

## 📚 高级设计模式最佳实践

### ✅ 使用原则

1. **单一职责**
   - 每个模式解决特定问题
   - 避免过度复杂
   - 保持代码简洁易懂

2. **开闭原则**
   - 对扩展开放
   - 对修改封闭
   - 通过接口编程

3. **依赖倒置**
   - 依赖抽象而非具体实现
   - 使用依赖注入
   - 提高可测试性

4. **组合优先于继承**
   - 使用组合模式而不是继承
   - 提高灵活性
   - 避免继承层次过深

### ❌ 避免问题

1. **模式滥用**
   - 不要为了使用而使用设计模式
   - 避免过度工程
   - 保持简单有效

2. **实现复杂化**
   - 避免过度抽象
   - 保持实现简洁
   - 避免过度优化

3. **性能影响**
   - 避免不必要的性能开销
   - 考虑缓存策略
   - 监控性能指标

### 📋 学习路径建议

1. **初学者**
   - 掌握Singleton、Factory、Strategy
   - 理解基本应用场景
   - 实践简单项目

2. **进阶开发者**
   - 掌握Decorator、Proxy、Observer
   - 理解高级应用场景
   - 优化现有代码

3. **架构师**
   - 掌握所有常用模式
   - 设计模式组合使用
   - 指导团队开发

---

*此高级设计模式指南基于凌隐宝堂中医诊所项目的实际实践编写，深入解析了设计模式在中医诊所管理系统中的具体应用。通过学习这些模式，可以显著提升代码质量和架构设计能力。*