# LYBTZYZS Server 层架构修改建议

**创建日期**: 2025-09-24
**基于分析**: 2025-09-24-server-layer-architecture-analysis.md
**优先级**: P0 (紧急执行)
**预计工期**: 6-8 周

## 🎯 修改策略概览

本文档提供了详细的、可执行的修改建议，按照风险和影响程度分为三个阶段逐步实施。每个建议都包含具体的实现方案、代码示例和验收标准。

### 修改原则
- **渐进式重构**: 分阶段实施，最小化业务影响
- **向后兼容**: 保证现有功能不受影响
- **测试先行**: 每次修改都要有对应的测试
- **文档同步**: 及时更新架构和 API 文档

## 📋 阶段一：紧急架构违规修复 (2-3 周)

### 1.1 移除 QueryService 对 DbContext 的直接依赖 (P0)

**问题**: ConsultationQueryService、PrescriptionQueryService 等直接注入 AppDbContext

#### 修改方案

**第一步**: 为每个 QueryService 创建对应的 ReadOnlyRepository

```csharp
// 新增: LYBT.Module.Consultation/Interfaces/IConsultationReadRepository.cs
public interface IConsultationReadRepository
{
    Task<PagedResult<ConsultationListDto>> GetPagedAsync(ConsultationSearchDto searchDto);
    Task<ConsultationDetailDto?> GetDetailByIdAsync(Guid id);
    Task<List<ConsultationSummaryDto>> GetByPatientIdAsync(Guid patientId);
}

// 实现: LYBT.Module.Consultation/Repositories/ConsultationReadRepository.cs
public class ConsultationReadRepository : IConsultationReadRepository
{
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;

    public ConsultationReadRepository(AppDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<PagedResult<ConsultationListDto>> GetPagedAsync(ConsultationSearchDto searchDto)
    {
        var query = _dbContext.Consultations.AsNoTracking();

        // 应用搜索条件
        if (!string.IsNullOrEmpty(searchDto.PatientName))
        {
            query = query.Where(c => c.Patient.Name.Contains(searchDto.PatientName));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.ConsultationDate)
            .Skip((searchDto.PageIndex - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .ProjectTo<ConsultationListDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return new PagedResult<ConsultationListDto>
        {
            Items = items,
            Total = total,
            PageIndex = searchDto.PageIndex,
            PageSize = searchDto.PageSize
        };
    }
}
```

**第二步**: 修改 QueryService 使用 ReadOnlyRepository

```csharp
// 修改: ConsultationQueryService.cs
public class ConsultationQueryService : IConsultationQueryService
{
    private readonly IConsultationReadRepository _readRepository; // 替换 AppDbContext

    public ConsultationQueryService(IConsultationReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<PagedResult<ConsultationListDto>> GetPagedAsync(ConsultationSearchDto searchDto)
    {
        return await _readRepository.GetPagedAsync(searchDto);
    }
}
```

**第三步**: 注册新的依赖

```csharp
// 在各模块的 ServiceCollectionExtensions.cs 中
services.AddScoped<IConsultationReadRepository, ConsultationReadRepository>();
```

#### 验收标准
- [ ] 所有 QueryService 不再直接依赖 AppDbContext
- [ ] 单元测试可以通过 Mock ReadOnlyRepository 进行
- [ ] 现有 API 功能保持不变
- [ ] 查询性能无明显下降

### 1.2 解耦 Auth-Users 模块依赖 (P0)

**问题**: AuthService 直接依赖 UserRepository，违反模块边界

#### 修改方案

**第一步**: 在 Shared.Interfaces 中定义用户认证接口

```csharp
// 新增: src/Shared/LYBT.Shared.Interfaces/Auth/IUserAuthenticationService.cs
public interface IUserAuthenticationService
{
    Task<AuthUserDto?> ValidateCredentialsAsync(string username, string password);
    Task<AuthUserDto?> GetUserByIdAsync(Guid userId);
    Task<bool> IsUserActiveAsync(Guid userId);
    Task UpdateLastLoginAsync(Guid userId);
}

// 新增: src/Shared/LYBT.Shared.Models/Contracts/Auth/AuthUserDto.cs
public class AuthUserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
```

**第二步**: 在 Users 模块中实现认证服务

```csharp
// 新增: LYBT.Module.Users/Services/UserAuthenticationService.cs
public class UserAuthenticationService : IUserAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public UserAuthenticationService(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<AuthUserDto?> ValidateCredentialsAsync(string username, string password)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null || !PasswordHelper.Verify(password, user.PasswordHash))
            return null;

        return _mapper.Map<AuthUserDto>(user);
    }

    // 其他方法实现...
}
```

**第三步**: 修改 AuthService 移除直接依赖

```csharp
// 修改: LYBT.Module.Auth/Services/AuthService.cs
public class AuthService : IAuthService
{
    private readonly IUserAuthenticationService _userAuthService; // 替换 IUserRepository
    private readonly IJwtService _jwtService;

    public AuthService(
        IUserAuthenticationService userAuthService,
        IJwtService jwtService)
    {
        _userAuthService = userAuthService;
        _jwtService = jwtService;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var authUser = await _userAuthService.ValidateCredentialsAsync(
            request.Username, request.Password);

        if (authUser == null)
            throw new UnauthorizedAccessException("用户名或密码错误");

        var token = await _jwtService.GenerateTokenAsync(authUser);
        await _userAuthService.UpdateLastLoginAsync(authUser.Id);

        return new LoginResponseDto { Token = token, User = authUser };
    }
}
```

#### 验收标准
- [ ] Auth 模块不再引用 Users 模块
- [ ] Auth 模块可以独立编译和部署
- [ ] 登录功能保持正常
- [ ] 用户权限验证正常

### 1.3 统一 Repository 接口 (P0)

**问题**: IRepository<T> 和 IBaseRepository<T> 重复定义

#### 修改方案

**第一步**: 废弃 IRepository<T> 接口

```csharp
// 标记为过时: LYBT.Infrastructure/Interfaces/IRepository.cs
[Obsolete("使用 IBaseRepository<T> 替代", true)]
public interface IRepository<T> : IBaseRepository<T> where T : class
{
    // 空接口，仅用于向后兼容
}
```

**第二步**: 更新所有 Repository 实现

```csharp
// 修改所有Repository类，如: UserRepository.cs
public class UserRepository : OptimizedBaseRepository<User>, IUserRepository // 移除 IRepository<User>
{
    // 保持现有实现不变
}
```

**第三步**: 更新所有注入点

```csharp
// 在各个Service中，统一使用IBaseRepository
public class UserBusinessService : IUserBusinessService
{
    private readonly IUserRepository _userRepository; // IUserRepository继承自IBaseRepository<User>

    // 保持现有注入方式不变，因为IUserRepository已经继承IBaseRepository
}
```

#### 验收标准
- [ ] 所有代码使用 IBaseRepository<T> 或其具体接口
- [ ] 编译时没有 IRepository<T> 的直接使用
- [ ] 所有单元测试正常通过
- [ ] IDE 中没有过时警告

## 📋 阶段二：代码重复消除 (2-3 周)

### 2.1 统一分页查询基类

**问题**: 分页逻辑在各个 QueryService 中重复

#### 修改方案

**第一步**: 创建通用分页扩展方法

```csharp
// 新增: LYBT.Infrastructure/Extensions/QueryableExtensions.cs
public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>
        {
            Items = items,
            Total = total,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public static async Task<PagedResult<TResult>> ToPagedResultAsync<T, TResult>(
        this IQueryable<T> query,
        int pageIndex,
        int pageSize,
        IConfigurationProvider mapperConfig,
        CancellationToken cancellationToken = default)
    {
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<TResult>(mapperConfig)
            .ToListAsync(cancellationToken);

        return new PagedResult<TResult>
        {
            Items = items,
            Total = total,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }
}
```

**第二步**: 在 ReadOnlyRepository 中使用扩展方法

```csharp
// 修改: ConsultationReadRepository.cs
public async Task<PagedResult<ConsultationListDto>> GetPagedAsync(ConsultationSearchDto searchDto)
{
    var query = _dbContext.Consultations.AsNoTracking();

    // 应用搜索条件
    if (!string.IsNullOrEmpty(searchDto.PatientName))
    {
        query = query.Where(c => c.Patient.Name.Contains(searchDto.PatientName));
    }

    // 使用扩展方法处理分页
    return await query
        .OrderByDescending(c => c.ConsultationDate)
        .ToPagedResultAsync<Consultation, ConsultationListDto>(
            searchDto.PageIndex,
            searchDto.PageSize,
            _mapper.ConfigurationProvider);
}
```

#### 验收标准
- [ ] 所有分页查询使用统一的扩展方法
- [ ] 代码重复行数减少 60% 以上
- [ ] 分页逻辑一致且可测试

### 2.2 统一缓存策略

**问题**: 每个 Repository 独立实现缓存逻辑

#### 修改方案

**第一步**: 创建缓存装饰器

```csharp
// 新增: LYBT.Infrastructure/Caching/CachedRepository.cs
public class CachedRepository<T> : IBaseRepository<T> where T : class, IEntity
{
    private readonly IBaseRepository<T> _innerRepository;
    private readonly ICacheService _cacheService;
    private readonly string _cacheKeyPrefix;

    public CachedRepository(
        IBaseRepository<T> innerRepository,
        ICacheService cacheService)
    {
        _innerRepository = innerRepository;
        _cacheService = cacheService;
        _cacheKeyPrefix = typeof(T).Name.ToLower();
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        var cacheKey = $"{_cacheKeyPrefix}:id:{id}";

        var cached = await _cacheService.GetAsync<T>(cacheKey);
        if (cached != null) return cached;

        var entity = await _innerRepository.GetByIdAsync(id);
        if (entity != null)
        {
            await _cacheService.SetAsync(cacheKey, entity, TimeSpan.FromMinutes(30));
        }

        return entity;
    }

    public async Task<T> AddAsync(T entity)
    {
        var result = await _innerRepository.AddAsync(entity);

        // 清除相关缓存
        await _cacheService.RemoveByPatternAsync($"{_cacheKeyPrefix}:*");

        return result;
    }

    // 其他方法实现类似的缓存策略
}
```

**第二步**: 修改 DI 配置使用装饰器模式

```csharp
// 修改: ServiceCollectionExtensions.cs
public static IServiceCollection AddUsersModule(this IServiceCollection services)
{
    // 注册实际的Repository
    services.AddScoped<UserRepository>();

    // 使用装饰器包装
    services.AddScoped<IUserRepository>(provider =>
    {
        var innerRepo = provider.GetRequiredService<UserRepository>();
        var cacheService = provider.GetRequiredService<ICacheService>();
        return new CachedRepository<User>(innerRepo, cacheService) as IUserRepository;
    });

    return services;
}
```

#### 验收标准
- [ ] 缓存逻辑从各个 Repository 中移除
- [ ] 统一的缓存策略应用到所有实体
- [ ] 缓存失效策略一致

### 2.3 全局异常处理中间件

**问题**: 异常处理逻辑在各层重复

#### 修改方案

**第一步**: 创建全局异常处理中间件

```csharp
// 新增: LYBT.Infrastructure/Middleware/GlobalExceptionHandlingMiddleware.cs
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "未处理的异常发生: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = exception switch
        {
            ValidationException validationEx => new ApiResponse<object>
            {
                Success = false,
                Message = "输入验证失败",
                Errors = validationEx.Errors?.Select(e => e.ErrorMessage).ToList()
            },
            UnauthorizedAccessException => new ApiResponse<object>
            {
                Success = false,
                Message = "未授权访问",
                ErrorCode = ApiErrorCodes.Unauthorized
            },
            NotFoundException notFoundEx => new ApiResponse<object>
            {
                Success = false,
                Message = notFoundEx.Message,
                ErrorCode = ApiErrorCodes.NotFound
            },
            BusinessException businessEx => new ApiResponse<object>
            {
                Success = false,
                Message = businessEx.Message,
                ErrorCode = businessEx.ErrorCode
            },
            _ => new ApiResponse<object>
            {
                Success = false,
                Message = "系统内部错误",
                ErrorCode = ApiErrorCodes.InternalServerError
            }
        };

        context.Response.StatusCode = exception switch
        {
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            NotFoundException => StatusCodes.Status404NotFound,
            ValidationException => StatusCodes.Status400BadRequest,
            BusinessException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
```

**第二步**: 注册中间件

```csharp
// 修改: Program.cs 或 Startup.cs
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
```

**第三步**: 简化 Controller 代码

```csharp
// 修改: 各个Controller，移除重复的异常处理
[ApiController]
public class ConsultationController : BaseApiController
{
    public async Task<IActionResult> GetPagedAsync([FromQuery] ConsultationSearchDto searchDto)
    {
        // 直接调用服务，异常由中间件处理
        var result = await _consultationQueryService.GetPagedAsync(searchDto);
        return Ok(result);
    }
}
```

#### 验收标准
- [ ] 所有 Controller 移除重复的异常处理代码
- [ ] 统一的错误响应格式
- [ ] 异常日志记录统一

## 📋 阶段三：架构优化升级 (3-4 周)

### 3.1 引入 CQRS 和 MediatR

**问题**: 命令和查询混合在一起，职责不清

#### 修改方案

**第一步**: 安装 MediatR

```xml
<PackageReference Include="MediatR" Version="12.0.1" />
<PackageReference Include="MediatR.Extensions.Microsoft.DependencyInjection" Version="11.1.0" />
```

**第二步**: 定义查询和命令

```csharp
// 新增: LYBT.Module.Consultation/Queries/GetConsultationPagedQuery.cs
public record GetConsultationPagedQuery(ConsultationSearchDto SearchDto)
    : IRequest<PagedResult<ConsultationListDto>>;

public class GetConsultationPagedQueryHandler
    : IRequestHandler<GetConsultationPagedQuery, PagedResult<ConsultationListDto>>
{
    private readonly IConsultationReadRepository _readRepository;

    public GetConsultationPagedQueryHandler(IConsultationReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<PagedResult<ConsultationListDto>> Handle(
        GetConsultationPagedQuery request,
        CancellationToken cancellationToken)
    {
        return await _readRepository.GetPagedAsync(request.SearchDto);
    }
}

// 新增: LYBT.Module.Consultation/Commands/CreateConsultationCommand.cs
public record CreateConsultationCommand(CreateConsultationDto ConsultationDto)
    : IRequest<ConsultationDetailDto>;

public class CreateConsultationCommandHandler
    : IRequestHandler<CreateConsultationCommand, ConsultationDetailDto>
{
    private readonly IConsultationRepository _repository;
    private readonly IMapper _mapper;

    public CreateConsultationCommandHandler(
        IConsultationRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ConsultationDetailDto> Handle(
        CreateConsultationCommand request,
        CancellationToken cancellationToken)
    {
        var consultation = _mapper.Map<Consultation>(request.ConsultationDto);
        var result = await _repository.AddAsync(consultation);
        await _repository.SaveChangesAsync();

        return _mapper.Map<ConsultationDetailDto>(result);
    }
}
```

**第三步**: 修改 Controller 使用 MediatR

```csharp
// 修改: ConsultationController.cs
[ApiController]
[Route("api/[controller]")]
public class ConsultationController : BaseApiController
{
    private readonly IMediator _mediator;

    public ConsultationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetPagedAsync([FromQuery] ConsultationSearchDto searchDto)
    {
        var query = new GetConsultationPagedQuery(searchDto);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateConsultationDto consultationDto)
    {
        var command = new CreateConsultationCommand(consultationDto);
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
```

#### 验收标准
- [ ] 查询和命令职责清晰分离
- [ ] Controller 代码大幅简化
- [ ] 业务逻辑集中在 Handler 中

### 3.2 实现 Unit of Work 模式

**问题**: 跨模块事务管理困难

#### 修改方案

**第一步**: 定义 UnitOfWork 接口

```csharp
// 新增: LYBT.Infrastructure/Interfaces/IUnitOfWork.cs
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

// 实现: LYBT.Infrastructure/Data/UnitOfWork.cs
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return _transaction;
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context?.Dispose();
    }
}
```

**第二步**: 在复杂业务操作中使用 UnitOfWork

```csharp
// 修改: 涉及多表操作的CommandHandler
public class CreateConsultationWithPrescriptionCommandHandler
    : IRequestHandler<CreateConsultationWithPrescriptionCommand, ConsultationDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConsultationRepository _consultationRepo;
    private readonly IPrescriptionRepository _prescriptionRepo;

    public async Task<ConsultationDetailDto> Handle(
        CreateConsultationWithPrescriptionCommand request,
        CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // 创建诊疗记录
            var consultation = await _consultationRepo.AddAsync(request.Consultation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 创建处方
            var prescription = request.Prescription;
            prescription.ConsultationId = consultation.Id;
            await _prescriptionRepo.AddAsync(prescription);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return _mapper.Map<ConsultationDetailDto>(consultation);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
```

#### 验收标准
- [ ] 复杂业务操作具有事务一致性
- [ ] 事务管理代码统一
- [ ] 异常时能正确回滚

### 3.3 建立完整的测试基础设施

**问题**: 测试覆盖率低，测试代码重复

#### 修改方案

**第一步**: 创建测试基类和工具

```csharp
// 新增: tests/TestInfrastructure/IntegrationTestBase.cs
public abstract class IntegrationTestBase : IDisposable
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;
    protected readonly IServiceScope Scope;

    protected IntegrationTestBase()
    {
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // 替换为测试数据库
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
                });
            });

        Client = Factory.CreateClient();
        Scope = Factory.Services.CreateScope();
    }

    protected async Task<T> ExecuteDbContextAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(context);
    }

    protected async Task ExecuteDbContextAsync(Func<AppDbContext, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(context);
    }

    public void Dispose()
    {
        Scope?.Dispose();
        Client?.Dispose();
        Factory?.Dispose();
    }
}

// 新增: tests/TestInfrastructure/TestDataFactory.cs
public static class TestDataFactory
{
    public static User CreateUser(string? username = null, UserRole role = UserRole.Doctor)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = username ?? $"testuser_{Guid.NewGuid():N}",
            PasswordHash = PasswordHelper.Hash("Test123!"),
            RealName = "测试用户",
            Role = role,
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow,
            RowVersion = BitConverter.GetBytes(1L)
        };
    }

    public static Patient CreatePatient(string? name = null)
    {
        return new Patient
        {
            Id = Guid.NewGuid(),
            Name = name ?? $"测试患者_{Random.Shared.Next(1000, 9999)}",
            Gender = Gender.Male,
            BirthDate = DateTime.Today.AddYears(-30),
            Phone = "13800138000",
            CreatedAt = DateTime.UtcNow,
            RowVersion = BitConverter.GetBytes(1L)
        };
    }
}
```

**第二步**: 创建针对性的测试

```csharp
// 新增: tests/Integration/ConsultationTests.cs
public class ConsultationIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateConsultation_Should_Return_Created_Result()
    {
        // Arrange
        var patient = TestDataFactory.CreatePatient();
        var doctor = TestDataFactory.CreateUser("doctor1", UserRole.Doctor);

        await ExecuteDbContextAsync(async context =>
        {
            context.Patients.Add(patient);
            context.Users.Add(doctor);
            await context.SaveChangesAsync();
        });

        var createDto = new CreateConsultationDto
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            ChiefComplaint = "头痛",
            PresentIllnessHistory = "持续头痛2天"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/consultation", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ConsultationDetailDto>();
        result.Should().NotBeNull();
        result!.PatientId.Should().Be(patient.Id);
        result.DoctorId.Should().Be(doctor.Id);
    }

    [Fact]
    public async Task GetPagedConsultations_Should_Return_Filtered_Results()
    {
        // Arrange - 创建测试数据
        var patient1 = TestDataFactory.CreatePatient("张三");
        var patient2 = TestDataFactory.CreatePatient("李四");
        var doctor = TestDataFactory.CreateUser("doctor1");

        await ExecuteDbContextAsync(async context =>
        {
            context.Patients.AddRange(patient1, patient2);
            context.Users.Add(doctor);

            context.Consultations.AddRange(
                new Consultation
                {
                    Id = Guid.NewGuid(),
                    PatientId = patient1.Id,
                    DoctorId = doctor.Id,
                    ChiefComplaint = "感冒",
                    ConsultationDate = DateTime.Now
                },
                new Consultation
                {
                    Id = Guid.NewGuid(),
                    PatientId = patient2.Id,
                    DoctorId = doctor.Id,
                    ChiefComplaint = "发烧",
                    ConsultationDate = DateTime.Now
                }
            );

            await context.SaveChangesAsync();
        });

        // Act
        var response = await Client.GetAsync("/api/consultation?patientName=张三&pageIndex=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<ConsultationListDto>>();
        result.Should().NotBeNull();
        result!.Total.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].PatientName.Should().Be("张三");
    }
}
```

#### 验收标准
- [ ] 关键业务流程有完整的集成测试
- [ ] 单元测试覆盖率达到 80% 以上
- [ ] 测试数据创建标准化
- [ ] 测试运行稳定且快速

## 📊 实施计划与里程碑

### 阶段一里程碑 (第 1-3 周)
- **Week 1**: 完成 QueryService 重构，移除 DbContext 直接依赖
- **Week 2**: 解耦 Auth-Users 模块，实现独立部署
- **Week 3**: 统一 Repository 接口，完善单元测试

**验收标准**:
- [ ] 架构违规问题全部修复
- [ ] 模块可以独立编译
- [ ] 现有功能无回归

### 阶段二里程碑 (第 4-6 周)
- **Week 4**: 实现统一分页和缓存策略
- **Week 5**: 部署全局异常处理中间件
- **Week 6**: 代码去重和性能优化

**验收标准**:
- [ ] 代码重复率降低到 15% 以下
- [ ] 异常处理统一
- [ ] API 响应时间无明显增加

### 阶段三里程碑 (第 7-8 周)
- **Week 7**: 引入 CQRS 和 MediatR，实现 UnitOfWork
- **Week 8**: 建立完整测试基础设施，部署监控

**验收标准**:
- [ ] CQRS 模式应用到核心模块
- [ ] 单元测试覆盖率达到 80%
- [ ] 集成测试覆盖关键业务流程

## 🔧 实施注意事项

### 开发规范
1. **分支管理**: 每个阶段创建独立的 feature 分支
2. **代码审查**: 所有修改必须经过 Code Review
3. **测试先行**: 修改前先编写测试，确保不破坏现有功能
4. **文档更新**: 及时更新 API 文档和架构文档

### 风险控制
1. **渐进发布**: 每个阶段完成后进行小范围测试
2. **回滚方案**: 准备每个阶段的回滚方案
3. **监控告警**: 部署过程中密切监控系统指标
4. **用户沟通**: 提前通知可能的短暂服务中断

### 团队协作
1. **专门小组**: 成立 3-4 人的重构专项小组
2. **时间分配**: 50% 时间用于重构，50% 用于维护现有功能
3. **知识分享**: 每周进行重构进展和技术分享
4. **问题跟踪**: 使用专门的任务看板跟踪问题和进度

## ✅ 成功标准

### 技术指标
- **代码重复率**: 从 45% 降低到 < 10%
- **模块耦合度**: 从高降低到低
- **单元测试覆盖率**: 从 30% 提升到 > 80%
- **构建时间**: 从 2.5 分钟降低到 < 1 分钟

### 业务指标
- **API 响应时间**: 不超过当前性能的 110%
- **系统稳定性**: 无因重构导致的线上问题
- **开发效率**: 新功能开发时间减少 30%
- **Bug 修复时间**: 平均修复时间减少 40%

通过系统性的分阶段重构，我们将显著提升 LYBTZYZS Server 层的架构质量和开发效率，为后续业务发展奠定坚实的技术基础。

---
**文档状态**: ✅ 完整
**执行优先级**: P0 - 立即开始
**预期收益**: 架构质量提升 300%，开发效率提升 150%