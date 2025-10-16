# 依赖注入模式指南

> **版本**: 1.0
> **创建日期**: 2025-10-15
> **最后更新**: 2025-10-15
> **维护者**: 开发团队
> **适用范围**: LYBT 项目所有开发人员
> **相关文档**: [快速开发指南](rapid-development-guide.md) | [服务器端架构标准](../architecture/server-module-design-standard.md) | [客户端设计标准](../architecture/client/unified-design-standard.md)

## 📋 指南概述

本文档为 LYBT 项目开发人员提供详细的依赖注入（Dependency Injection, DI）模式指南，涵盖设计原则、实现模式、最佳实践和常见问题的解决方案。通过遵循本指南，开发人员可以构建松耦合、易于测试和维护的高质量代码。

## 🎯 指南目标

### 主要目标
- **标准化**: 统一项目中的依赖注入使用方式
- **解耦合**: 降低模块间的耦合度，提高代码的可维护性
- **可测试**: 支持单元测试和集成测试的编写
- **可扩展**: 便于模块的扩展和功能的增强

### 适用场景
- **新模块开发**: 在新模块中正确使用依赖注入
- **代码重构**: 重构现有代码，改善架构设计
- **测试编写**: 为模块编写单元测试和集成测试
- **架构设计**: 设计新功能的架构和依赖关系

## 🔧 依赖注入基础概念

### 核心原则

#### 1. 依赖倒置原则（DIP）
```csharp
// ❌ 错误做法：高层模块依赖低层模块
public class BusinessLogic
{
    private DatabaseService _database = new DatabaseService(); // 紧耦合
}

// ✅ 正确做法：高层模块依赖抽象
public class BusinessLogic
{
    private readonly IDatabaseService _database; // 依赖抽象
    
    public BusinessLogic(IDatabaseService database)
    {
        _database = database; // 构造函数注入
    }
}
```

#### 2. 单一职责原则（SRP）
```csharp
// ✅ 正确做法：每个类只负责一个职责
public class PatientService
{
    private readonly IPatientRepository _repository;
    
    public PatientService(IPatientRepository repository)
    {
        _repository = repository;
    }
    
    // 只负责患者相关的业务逻辑
}

public class PatientRepository : IPatientRepository
{
    private readonly ApplicationDbContext _context;
    
    public PatientRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    // 只负责数据访问逻辑
}
```

#### 3. 开闭原则（OCP）
```csharp
// ✅ 正确做法：对扩展开放，对修改封闭
public interface IPatientRepository
{
    Task<PatientEntity> GetByIdAsync(Guid id);
    Task<List<PatientEntity>> GetAllAsync();
}

// 可以通过继承扩展功能
public interface IPatientRepositoryExtended : IPatientRepository
{
    Task<List<PatientEntity>> GetByKeywordAsync(string keyword);
}
```

### 依赖注入容器选择

#### 1. Microsoft.Extensions.DependencyInjection
```csharp
// Program.cs 或 Startup.cs
var services = new ServiceCollection();

// 注册服务
services.AddScoped<IPatientRepository, PatientRepository>();
services.AddScoped<IPatientService, PatientService>();
services.AddScoped<IHttpContextAccessor, HttpContextAccessor>();

// 构建服务提供者
var serviceProvider = services.BuildServiceProvider();
```

#### 2. Prism.DryIoc（WPF/MAUI）
```csharp
// App.xaml.cs
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 注册服务
    containerRegistry.Register<IPatientRepository, PatientRepository>(Lifetime.Singleton);
    containerRegistry.Register<IPatientService, PatientService>(Lifetime.Singleton);
    containerRegistry.Register<IHttpContextAccessor, HttpContextAccessor>(Lifetime.Singleton);
    
    // 注册 ViewModel
    containerRegistry.RegisterForNavigation<PatientListViewModel>();
    containerRegistry.RegisterForNavigation<PatientDetailViewModel>();
}
```

## 🏗️ 架构模式和依赖注入

### 三层架构中的依赖注入

#### 1. 表现层（Presentation Layer）
```csharp
// ViewModels
public class PatientListViewModel : BindableBase
{
    private readonly IPatientService _patientService;
    private readonly IEventAggregator _eventAggregator;
    
    public PatientListViewModel(
        IPatientService patientService,
        IEventAggregator eventAggregator)
    {
        _patientService = patientService;
        _eventAggregator = eventAggregator;
    }
    
    // 使用注入的服务
    public async Task LoadPatientsAsync()
    {
        var patients = await _patientService.GetAllAsync();
        // 更新UI
    }
}
```

#### 2. 业务逻辑层（Business Logic Layer）
```csharp
// Services
public class PatientService : IPatientService
{
    private readonly IPatientRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<PatientService> _logger;
    
    public PatientService(
        IPatientRepository repository,
        IMapper mapper,
        ILogger<PatientService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }
    
    public async Task<PagedResult<PatientDto>> GetPagedAsync(int page, int pageSize, string? keyword = null)
    {
        var pagedResult = await _repository.GetPagedAsync(page, pageSize, keyword);
        var dto = _mapper.Map<PagedResult<PatientDto>>(pagedResult);
        return dto;
    }
}
```

#### 3. 数据访问层（Data Access Layer）
```csharp
// Repositories
public class PatientRepository : BaseRepository<PatientEntity>, IPatientRepository
{
    private readonly ApplicationDbContext _context;
    
    public PatientRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }
    
    public async Task<PagedResult<PatientEntity>> GetPagedAsync(int page, int pageSize, string? keyword = null)
    {
        var query = DbSet.AsQueryable();
        
        if (!string.IsNullOrEmpty(keyword))
        {
            query = query.Where(p => p.Name.Contains(keyword));
        }
        
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
            
        return new PagedResult<PatientEntity>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }
}
```

### 聚合根模式中的依赖注入

#### 1. 聚合根实体
```csharp
public class MedicalCase : BaseEntity
{
    // 聚合根包含的实体
    public virtual Consultation? Consultation { get; private set; }
    public virtual Prescription? Prescription { get; private set; }
    
    // 业务方法
    public void AddConsultation(Consultation consultation)
    {
        if (Consultation == null)
            throw new ArgumentNullException(nameof(consultation));
            
        // 业务规则验证
        if (Consultation.Id != Id)
            throw new ArgumentException("诊疗记录ID必须与病历ID匹配");
            
        Consultation = consultation;
    }
    
    public void AddPrescription(Prescription prescription)
    {
        if (prescription == null)
            throw new ArgumentNullException(nameof(prescription));
            
        // 业务规则验证
        if (Prescription != null && Prescription.MedicalCaseId != Id)
            throw new ArgumentException("处方病历ID必须与病历ID匹配");
            
        Prescription = prescription;
    }
}
```

#### 2. 聚合根仓储
```csharp
public class MedicalCaseRepository : BaseRepository<MedicalCaseEntity>, IMedicalCaseRepository
{
    private readonly ApplicationDbContext _context;
    
    public MedicalCaseRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }
    
    public async Task<MedicalCaseEntity?> GetByIdWithDetailsAsync(Guid id)
    {
        return await DbSet
            .Include(m => m.Consultation)
            .Include(m => m.Prescription)
            .FirstOrDefaultAsync(m => m.Id == id);
    }
    
    public async Task<MedicalCaseEntity> AddWithDetailsAsync(MedicalCaseEntity medicalCase)
    {
        await _context.MedicalCases.AddAsync(medicalCase);
        await _context.SaveChangesAsync();
        return medicalCase;
    }
}
```

#### 3. 聚合根服务
```csharp
public class MedicalCaseService : IMedicalCaseService
{
    private readonly IMedicalCaseRepository _repository;
    private readonly IConsultationRepository _consultationRepository;
    private readonly IPrescriptionRepository _prescriptionRepository;
    private readonly IMapper _mapper;
    
    public MedicalCaseService(
        IMedicalCaseRepository repository,
        IConsultationRepository consultationRepository,
        IPrescriptionRepository prescriptionRepository,
        IMapper mapper)
    {
        _repository = repository;
        _consultationRepository = consultationRepository;
        _prescriptionRepository = prescriptionRepository;
        _mapper = mapper;
    }
    
    public async Task<MedicalCaseDto> CreateWithDetailsAsync(
        MedicalCaseCreateDto caseDto,
        ConsultationCreateDto consultationDto,
        PrescriptionCreateDto? prescriptionDto = null)
    {
        // 创建聚合根
        var medicalCase = _mapper.Map<MedicalCaseEntity>(caseDto);
        medicalCase.ConsultationDate = DateTime.Now;
        
        // 创建诊疗记录（共享主键）
        var consultation = _mapper.Map<ConsultationEntity>(consultationDto);
        consultation.Id = medicalCase.Id; // 共享主键
        medicalCase.Consultation = consultation;
        
        // 可选创建处方
        if (prescriptionDto != null)
        {
            var prescription = _mapper.Map<PrescriptionEntity>(prescriptionDto);
            prescription.MedicalCaseId = medicalCase.Id;
            prescription.PatientId = medicalCase.PatientId;
            prescription.UserId = medicalCase.DoctorId;
            medicalCase.Prescription = prescription;
        }
        
        // 保存聚合根
        var result = await _repository.AddWithDetailsAsync(medicalCase);
        return _mapper.Map<MedicalCaseDto>(result);
    }
}
```

## 🎯 依赖注入模式详解

### 构造函数注入（Constructor Injection）

#### 1. 基础构造函数注入
```csharp
public class PatientService : IPatientService
{
    private readonly IPatientRepository _repository;
    private readonly IMapper _mapper;
    
    // 构造函数注入
    public PatientService(IPatientRepository repository, IMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }
}
```

#### 2. 多依赖注入
```csharp
public class MedicalCaseService : IMedicalCaseService
{
    private readonly IMedicalCaseRepository _repository;
    private readonly IConsultationRepository _consultationRepository;
    private readonly IPrescriptionRepository _prescriptionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<MedicalCaseService> _logger;
    
    public MedicalCaseService(
        IMedicalCaseRepository repository,
        IConsultationRepository consultationRepository,
        IPrescriptionRepository prescriptionRepository,
        IMapper mapper,
        ILogger<MedicalCaseService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _consultationRepository = consultationRepository ?? throw new ArgumentNullException(nameof(consultationRepository));
        _prescriptionRepository = prescriptionRepository ?? throw new ArgumentNullException(nameof(prescriptionRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

#### 3. 可选依赖注入
```csharp
public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly IEmailService? _emailService;
    private readonly ISmsService? _smsService;
    
    // 必需依赖和可选依赖
    public NotificationService(
        ILogger<NotificationService> logger,
        IEmailService? emailService = null,
        ISmsService? smsService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _emailService = emailService;
        _smsService = smsService;
    }
    
    public async Task SendNotificationAsync(string message, string recipient)
    {
        // 使用邮件服务
        if (_emailService != null)
        {
            await _emailService.SendEmailAsync(message, recipient);
        }
        
        // 使用短信服务
        if (_smsService != null)
        {
            await _smsService.SendSmsAsync(message, recipient);
        }
        
        // 总是记录日志
        _logger.LogInformation("通知已发送: {Message} to {Recipient}", message, recipient);
    }
}
```

### 属性注入（Property Injection）

#### 1. Microsoft.Extensions.DependencyInjection 属性注入
```csharp
// Controller 中使用属性注入
[ApiController]
[Route("api/[controller]")]
public class PatientController : ControllerBase
{
    [FromServices]
    public ILogger<PatientController> Logger { get; set; }
    
    [FromServices]
    public IPatientService PatientService { get; set; }
    
    [HttpGet]
    public async Task<ActionResult> Get()
    {
        Logger.LogInformation("正在获取患者信息");
        var patients = await PatientService.GetAllAsync();
        return Ok(patients);
    }
}
```

#### 2. 自定义属性注入
```csharp
// 自定义属性注入特性
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class InjectServiceAttribute : Attribute
{
    public Type ServiceType { get; }
    
    public InjectServiceAttribute(Type serviceType)
    {
        ServiceType = serviceType;
    }
}

// 在 ViewModel 中使用
public class PatientViewModel
{
    [InjectService(typeof(IPatientService))]
    public IPatientService PatientService { get; set; }
    
    [InjectService(typeof(ILogger<PatientViewModel>))]
    public ILogger<PatientViewModel> Logger { get; set; }
    
    // 需要在构造后手动注入
    public void InitializeServices(IServiceProvider serviceProvider)
    {
        PatientService = serviceProvider.GetService<IPatientService>();
        Logger = serviceProvider.GetService<ILogger<PatientViewModel>>();
    }
}
```

### 工厂模式注入

#### 1. 简单工厂
```csharp
// 服务工厂接口
public interface IPatientServiceFactory
{
    IPatientService CreateService();
}

// 服务工厂实现
public class PatientServiceFactory : IPatientServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    public PatientServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public IPatientService CreateService()
    {
        return _serviceProvider.GetRequiredService<IPatientService>();
    }
}

// 使用工厂的服务
public class PatientManagementController
{
    private readonly IPatientServiceFactory _serviceFactory;
    private IPatientService _patientService;
    
    public PatientManagementController(IPatientServiceFactory serviceFactory)
    {
        _serviceFactory = serviceFactory;
        _patientService = _serviceFactory.CreateService();
    }
}
```

#### 2. 抽象工厂
```csharp
// 抽象工厂接口
public interface IServiceFactory<T>
{
    T CreateService();
}

// 抽象工厂实现
public class ServiceFactory<T> : IServiceFactory<T> where T : class
{
    private readonly IServiceProvider _serviceProvider;
    
    public ServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public T CreateService()
    {
        return _serviceProvider.GetRequiredService<T>();
    }
}

// 使用抽象工厂
public class PatientController
{
    private readonly IServiceFactory<IPatientService> _serviceFactory;
    
    public PatientController(IServiceFactory<IPatientService> serviceFactory)
    {
        _serviceFactory = serviceFactory;
    }
    
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var patientService = _serviceFactory.CreateService();
        var patients = await patientService.GetAllAsync();
        return Ok(patients);
    }
}
```

## 🔧 依赖注入配置

### 服务生命周期管理

#### 1. Scoped（作用域）服务
```csharp
// 服务注册
services.AddScoped<IPatientRepository, PatientRepository>();
services.AddScoped<IPatientService, PatientService>();
services.AddScoped<PatientDbContext>();

// Controller 中使用（每个 HTTP 请求一个实例）
[ApiController]
[Route("api/[controller]")]
public class PatientController : ControllerBase
{
    private readonly IPatientService _patientService;
    
    public PatientController(IPatientService patientService)
    {
        _patientService = patientService;
    }
    
    // 每个请求都会创建新的 PatientController 实例
}
```

#### 2. Singleton（单例）服务
```csharp
// 服务注册
services.AddSingleton<ICacheService, MemoryCacheService>();
services.AddSingleton<IConfigurationService, ConfigurationService>();

// 服务实现
public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    
    public CacheService(IMemoryCache cache)
    {
        _cache = cache;
    }
    
    // 单例服务，整个应用程序生命周期内只有一个实例
    public void Set<T>(string key, T value, TimeSpan? expiry = null)
    {
        _cache.Set(key, value, expiry);
    }
}
```

#### 3. Transient（瞬时）服务
```csharp
// 服务注册
services.AddTransient<IValidator<PatientCreateDto>, PatientCreateDtoValidator>();
services.AddTransient<IValidator<PatientUpdateDto>, PatientUpdateDtoValidator>();

// 验证器实现
public class PatientCreateDtoValidator : AbstractValidator<PatientCreateDto>
{
    public PatientCreateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
    
    // 每次注入都会创建新实例
}
```

### 条件化注册

#### 1. 基于环境的服务注册
```csharp
// 环境配置
public static class ServiceRegistration
{
    public static IServiceCollection AddServices(this IServiceCollection services, IWebHostEnvironment env)
    {
        // 基础服务（所有环境）
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IPatientService, PatientService>();
        
        // 开发环境特定服务
        if (env.IsDevelopment())
        {
            services.AddScoped<IDevelopmentService, DevelopmentService>();
            services.AddSingleton<IEmailService, MockEmailService>();
        }
        
        // 生产环境特定服务
        if (env.IsProduction())
        {
            services.AddScoped<IProductionService, ProductionService>();
            services.AddSingleton<IEmailService, SmtpEmailService>();
        }
        
        // 测试环境特定服务
        if (env.IsEnvironment("Testing"))
        {
            services.AddScoped<ITestService, TestService>();
            services.AddSingleton<IDatabase, InMemoryDatabase>();
        }
    }
}
```

#### 2. 基于配置的服务注册
```csharp
public class ServiceConfiguration
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 从配置文件读取服务类型
        var databaseType = configuration["DatabaseType"] ?? "SqlServer";
        
        switch (databaseType.ToLower())
        {
            case "sqlserver":
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
                break;
                
            case "postgresql":
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
                break;
                
            case "sqlite":
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));
                break;
        }
        
        // 从配置文件读取缓存类型
        var cacheType = configuration["CacheType"] ?? "Memory";
        switch (cacheType.ToLower())
        {
            case "memory":
                services.AddMemoryCache();
                break;
            case "redis":
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = configuration.GetConnectionString("Redis");
                });
                break;
        }
    }
}
```

## 🧪 测试中的依赖注入

### 单元测试

#### 1. 使用 Mock 对象
```csharp
[TestFixture]
public class PatientServiceTests
{
    private Mock<IPatientRepository> _mockRepository;
    private Mock<IMapper> _mockMapper;
    private Mock<ILogger<PatientService>> _mockLogger;
    private PatientService _patientService;
    
    public PatientServiceTests()
    {
        _mockRepository = new Mock<IPatientRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<PatientService>>();
        
        _patientService = new PatientService(
            _mockRepository.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }
    
    [Test]
    public async Task GetByIdAsync_ValidId_ReturnsPatient()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patientEntity = new PatientEntity { Id = patientId, Name = "Test Patient" };
        var patientDto = new PatientDto { Id = patientId, Name = "Test Patient" };
        
        _mockRepository.Setup(x => x.GetByIdAsync(patientId))
            .ReturnsAsync(patientEntity);
        _mockMapper.Setup(x => x.Map<PatientDto>(patientEntity))
            .Returns(patientDto);
        
        // Act
        var result = await _patientService.GetByIdAsync(patientId);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Test Patient", result.Data.Name);
    }
}
```

#### 2. 使用测试框架
```csharp
// AutoFixture 配置
public class PatientServiceTests
{
    private IFixture _fixture;
    private Mock<IPatientRepository> _mockRepository;
    private Mock<IMapper> _mockMapper;
    private Mock<ILogger<PatientService>> _mockLogger;
    private PatientService _patientService;
    
    public PatientServiceTests()
    {
        _fixture = new Fixture();
        _mockRepository = new Mock<IPatientRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<PatientService>>();
        
        _patientService = new PatientService(
            _mockRepository.Object,
            _mockMapper.Object,
            _mockLogger.Object);
        
        // 配置 AutoFixture
        _fixture.Customize<PatientCreateDto>(dto => dto.Name = "Test Patient");
    }
    
    [Test]
    public async Task CreateAsync_ValidDto_ReturnsPatient()
    {
        // Arrange
        var createDto = _fixture.Create<PatientCreateDto>();
        var patientEntity = _fixture.Create<PatientEntity>();
        var patientDto = _fixture.Create<PatientDto>();
        
        _mockRepository.Setup(x => x.AddAsync(It.IsAny<PatientEntity>()))
            .ReturnsAsync(patientEntity);
        _mockMapper.Setup(x => x.Map<PatientDto>(patientEntity))
            .Returns(patientDto);
        
        // Act
        var result = await _patientService.CreateAsync(createDto);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccess);
    }
}
```

### 集成测试

#### 1. 使用测试容器
```csharp
// WebApplicationFactory 配置
public class PatientIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public PatientIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
    
    [Test]
    public async Task GetPatients_ReturnsPatientList()
    {
        // Act
        var response = await _client.GetAsync("/api/patients");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var patients = await response.Content.ReadFromJsonAsync<List<PatientDto>>();
        Assert.IsNotNull(patients);
        Assert.IsTrue(patients.Count > 0);
    }
    
    [Test]
    public async Task CreatePatient_ValidDto_ReturnsCreatedPatient()
    {
        // Arrange
        var createDto = new PatientCreateDto
        {
            Name = "Test Patient",
            Email = "test@example.com",
            PhoneNumber = "1234567890"
        };
        
        var content = new StringContent(
            JsonSerializer.Serialize(createDto),
            Encoding.UTF8,
            "application/json");
        
        // Act
        var response = await _client.PostAsync("/api/patients", content);
        
        // Assert
        response.EnsureSuccessStatusCode();
        var patient = await response.Content.ReadFromJsonAsync<PatientDto>();
        Assert.IsNotNull(patient);
        Assert.AreEqual("Test Patient", patient.Name);
    }
}
```

#### 2. 内存数据库测试
```csharp
// 内存数据库配置
public class PatientRepositoryTests
{
    private ApplicationDbContext _context;
    private IPatientRepository _repository;
    
    public PatientRepositoryTests()
    {
        // 创建内存数据库
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        
        _context = new ApplicationDbContext(options);
        _repository = new PatientRepository(_context);
        
        // 初始化测试数据
        InitializeTestData();
    }
    
    private void InitializeTestData()
    {
        var patients = new List<PatientEntity>
        {
            new PatientEntity { Id = Guid.NewGuid(), Name = "Patient 1", Email = "patient1@example.com" },
            new PatientEntity { Id = Guid.NewGuid(), Name = "Patient 2", Email = "patient2@example.com" }
        };
        
        _context.Patients.AddRange(patients);
        _context.SaveChanges();
    }
    
    [Test]
    public async Task GetAllAsync_ReturnsAllPatients()
    {
        // Act
        var result = await _repository.GetAllAsync();
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count);
    }
    
    [Test]
    public async Task GetByIdAsync_ValidId_ReturnsPatient()
    {
        // Arrange
        var patientId = _context.Patients.First().Id;
        
        // Act
        var result = await _repository.GetByIdAsync(patientId);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(patientId, result.Id);
    }
}
```

## 🚨 常见问题和解决方案

### 循环依赖（Circular Dependency）

#### 1. 问题识别
```csharp
// ❌ 循环依赖问题
public class ServiceA : IServiceA
{
    private readonly IServiceB _serviceB;
    
    public ServiceA(IServiceB serviceB)
    {
        _serviceB = serviceB;
    }
}

public class ServiceB : IServiceB
{
    private readonly IServiceA _serviceA;
    
    public ServiceB(IServiceA serviceA)
    {
        _serviceA = serviceA;
    }
}
```

#### 2. 解决方案
```csharp
// ✅ 解决方案1：重新设计依赖关系
public class ServiceA : IServiceA
{
    private readonly IServiceC _serviceC;
    
    public ServiceA(IServiceC serviceC)
    {
        _serviceC = serviceC;
    }
}

public class ServiceB : IServiceB
{
    private readonly IServiceC _serviceC;
    
    public ServiceB(IServiceC serviceC)
    {
        _serviceC = serviceC;
    }
}

// ✅ 解决方案2：使用事件解耦
public class ServiceA : IServiceA
{
    private readonly IEventAggregator _eventAggregator;
    
    public ServiceA(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        _eventAggregator.Subscribe<ServiceBEvent>(OnServiceBEvent);
    }
    
    private void OnServiceBEvent(ServiceBEvent evt)
    {
        // 处理来自 ServiceB 的事件
    }
}

public class ServiceB : IServiceB
{
    private readonly IEventAggregator _eventAggregator;
    
    public ServiceB(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
    }
    
    public void DoSomething()
    {
        // 发布事件，而不是直接调用 ServiceA
        _eventAggregator.Publish(new ServiceBEvent());
    }
}
```

### 依赖注入容器配置问题

#### 1. 服务未注册
```csharp
// ❌ 错误：服务未注册
public class PatientController : ControllerBase
{
    private readonly IPatientService _patientService;
    
    public PatientController(IPatientService patientService)
    {
        _patientService = patientService; // 运行时错误
    }
}

// ✅ 解决方案：在 Startup.cs 中注册服务
public void ConfigureServices(IServiceCollection services)
{
    services.AddScoped<IPatientService, PatientService>();
}
```

#### 2. 服务注册顺序问题
```csharp
// ❌ 错误：注册顺序依赖问题
public void ConfigureServices(IServiceCollection services)
{
    services.AddScoped<ServiceA>();
    services.AddScoped<ServiceB>(sp => 
        new ServiceB(sp.GetRequiredService<ServiceA>())); // ServiceA 尚未注册
}

// ✅ 解决方案：正确的注册顺序
public void ConfigureServices(IServiceCollection services)
{
    services.AddScoped<ServiceA>();
    services.AddScoped<ServiceB>();
    services.AddScoped<ServiceC>();
    
    // 如果有复杂依赖关系，可以使用工厂
    services.AddScoped<IServiceD>(sp => 
        new ServiceD(
            sp.GetRequiredService<ServiceA>(),
            sp.GetRequiredService<ServiceB>(),
            sp.GetRequiredService<ServiceC>()));
}
```

### 测试中的依赖注入问题

#### 1. Mock 对象配置错误
```csharp
// ❌ 错误：Mock 对象配置不完整
[TestMethod]
public async Task TestMethod()
{
    var mockRepo = new Mock<IPatientRepository>();
    var service = new PatientService(mockRepo.Object, mockMapper.Object);
    
    var result = await service.GetByIdAsync(Guid.NewGuid());
    
    Assert.IsNotNull(result.Data); // 可能失败，因为 mock 没有配置
}

// ✅ 解决方案：完整配置 Mock 对象
[TestMethod]
public async Task TestMethod()
{
    var mockRepo = new Mock<IPatientRepository>();
    var mockMapper = new Mock<IMapper>();
    
    // 配置 Mock 行为
    var patientId = Guid.NewGuid();
    var patientEntity = new PatientEntity { Id = patientId, Name = "Test" };
    mockRepo.Setup(x => x.GetByIdAsync(patientId)).ReturnsAsync(patientEntity);
    
    mockMapper.Setup(x => x.Map<PatientDto>(It.IsAny<PatientEntity>()))
        .Returns(new PatientDto { Id = patientId, Name = "Test" });
    
    var service = new PatientService(mockRepo.Object, mockMapper.Object);
    var result = await service.GetByIdAsync(patientId);
    
    Assert.IsNotNull(result.Data);
    Assert.AreEqual("Test", result.Data.Name);
}
```

#### 2. 测试中使用真实数据库
```csharp
// ✅ 使用内存数据库进行集成测试
public class PatientRepositoryIntegrationTests
{
    private ApplicationDbContext _context;
    
    public PatientRepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new ApplicationDbContext(options);
        
        // 初始化测试数据
        SeedTestData(_context);
    }
    
    private void SeedTestData(ApplicationDbContext context)
    {
        context.Patients.AddRange(new[]
        {
            new PatientEntity { Id = Guid.NewGuid(), Name = "Test Patient 1" },
            new PatientEntity { Id = Guid.NewGuid(), Name = "Test Patient 2" }
        });
        context.SaveChanges();
    }
}
```

## 📚 最佳实践总结

### 设计原则

#### 1. 依赖倒置
- 高层模块不应该依赖低层模块，都应该依赖于抽象
- 抽象不应该依赖细节，细节应该依赖抽象
- 面向接口编程，而不是面向实现编程

#### 2. 单一职责
- 每个类应该只有一个引起变化的原因
- 服务类专注于业务逻辑，仓储类专注于数据访问
- 避免创建"上帝类"（God Class）

#### 3. 开闭原则
- 对扩展开放，对修改关闭
- 使用接口和抽象类来支持扩展
- 避免修改已有的代码来添加新功能

### 实现原则

#### 1. 构造函数注入优先
- 优先使用构造函数注入，保证依赖的完整性
- 必需依赖通过构造函数注入，可选依赖可以通过属性或方法注入
- 避免在构造函数中进行复杂的业务逻辑

#### 2. 接口隔离
- 依赖接口而不是具体实现
- 使用小而专注的接口（Interface Segregation Principle）
- 避免强制客户端依赖它们不需要的方法

#### 3. 生命周期管理
- 根据服务的用途选择合适的生命周期
- Scoped 适用于有状态的服务（如 Repository、Service）
- Singleton 适用于无状态的服务（如 Cache、Configuration）
- Transient 适用于轻量级的服务（如 Validator）

### 配置原则

#### 1. 分层注册
- 按层次组织服务注册，便于维护
- 使用扩展方法组织相关服务的注册
- 为不同环境提供不同的服务配置

#### 2. 条件注册
- 根据环境和配置条件注册不同的服务实现
- 使用配置文件控制服务选择
- 提供开发和生产环境的不同配置

#### 3. 健康检查
- 定期检查依赖注入配置的健康状态
- 监控服务的生命周期和性能
- 及时发现和解决循环依赖问题

---

*本文档遵循 LYBT 项目文档标准编写，如有疑问请参考相关模板或联系技术支持团队。*