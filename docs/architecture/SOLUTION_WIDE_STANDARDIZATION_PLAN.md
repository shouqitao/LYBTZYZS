# Solution级别统一化实施计划

> 版本：1.0  
> 更新：2025-01-08  
> 目标：将所有UltraThink优化标准系统性应用到整个Solution

## 🎯 统一化目标

### 核心原则
- **统一API响应**：所有Controller使用ApiResponse<T>格式
- **统一错误处理**：所有层级使用标准化异常处理机制
- **单一职责原则**：每个类、方法、组件职责明确且唯一
- **代码一致性**：整个Solution遵循相同的编码标准和架构模式

### 质量指标
- **编译零错误**：整个Solution编译成功率100%
- **代码覆盖率**：从2.76%提升至60%
- **文件行数控制**：单个文件不超过500行
- **性能标准**：API响应时间<2秒，数据库查询<1秒

---

## 🏗️ Solution架构统一标准

### 1. API层统一化

#### 1.1 Controller基类统一
```csharp
// 所有Controller必须继承BaseApiController
public class {ModuleName}Controller : BaseApiController
{
    public {ModuleName}Controller(
        I{ModuleName}Service service,
        IMemoryCache cache,
        ILogger<{ModuleName}Controller> logger)
        : base(logger, cache)
    {
        _service = service;
    }
}
```

#### 1.2 API响应格式统一
```csharp
// 成功响应
return Success(data, "操作成功");
return Success(pagedData, "查询成功");

// 业务错误（200状态码）
return BusinessFail<T>("业务错误消息", ApiErrorCodes.BUSINESS_ERROR);

// HTTP错误响应
return ValidationFail("验证失败");                    // 400
return NotFound("资源不存在", ApiErrorCodes.NOT_FOUND);  // 404
return InternalError("服务器错误");                    // 500
```

#### 1.3 错误代码统一
```csharp
// 所有错误代码在ApiErrorCodes中定义
public static class ApiErrorCodes
{
    // 通用错误
    public const string VALIDATION_ERROR = "VALIDATION_ERROR";
    public const string DATA_NOT_FOUND = "DATA_NOT_FOUND";
    public const string DATA_SAVE_FAILED = "DATA_SAVE_FAILED";
    
    // 模块特定错误
    public const string USER_NOT_FOUND = "USER_NOT_FOUND";
    public const string USERNAME_EXISTS = "USERNAME_EXISTS";
    public const string PATIENT_NOT_FOUND = "PATIENT_NOT_FOUND";
    // ...
}
```

### 2. Service层统一化

#### 2.1 Service基类统一
```csharp
// 所有Service继承BaseService或实现IBaseService
public class {ModuleName}Service : BaseService<{Entity}, {Dto}, {CreateDto}, {UpdateDto}>
{
    public {ModuleName}Service(
        I{ModuleName}Repository repository,
        IMapper mapper,
        ILogger<{ModuleName}Service> logger,
        IUnifiedLogService logService)
        : base(repository, mapper, logger, logService)
    {
    }
    
    // 重写虚方法实现特定业务逻辑
    protected override async Task ValidateCreateAsync({CreateDto} createDto)
    {
        // 特定验证逻辑
    }
}
```

#### 2.2 异常处理统一
```csharp
// 统一异常处理模式
public async Task<TResult> MethodAsync(TInput input)
{
    try
    {
        // 参数验证
        if (input == null)
            throw new ArgumentNullException(nameof(input));
            
        // 业务逻辑
        var result = await ProcessAsync(input);
        
        // 操作日志
        await LogOperationAsync("Operation", result);
        
        return result;
    }
    catch (ArgumentException ex)
    {
        _logger.LogWarning(ex, "参数验证失败");
        throw;
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogError(ex, "业务操作失败");
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "系统错误");
        throw new SystemException("系统内部错误", ex);
    }
}
```

### 3. Repository层统一化

#### 3.1 Repository基类统一
```csharp
// 所有Repository继承BaseRepository
public class {ModuleName}Repository : BaseRepository<{Entity}>, I{ModuleName}Repository
{
    public {ModuleName}Repository(AppDbContext context) : base(context) { }
    
    // 只实现特定的业务方法
    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null)
    {
        var query = _dbSet.AsQueryable().Where(e => e.Name == name);
        if (excludeId.HasValue)
            query = query.Where(e => e.Id != excludeId.Value);
        return await query.AnyAsync();
    }
}
```

### 4. 测试层统一化

#### 4.1 测试基类统一
```csharp
// 所有测试类继承BaseTestFixture
[TestCategory(TestCategories.{Category})]
public class {ModuleName}ServiceTests : BaseTestFixture
{
    private readonly I{ModuleName}Service _service;
    private readonly Mock<I{ModuleName}Repository> _mockRepository;
    
    public {ModuleName}ServiceTests()
    {
        _mockRepository = CreateMockRepository<I{ModuleName}Repository, {Entity}>();
        _service = new {ModuleName}Service(_mockRepository.Object, Mapper, MockLogService.Object);
    }
}
```

---

## 📊 实施路线图

### 第一阶段：Controller层统一化 ⏳

#### 需要重构的Controller
```
Backend/Services/LYBT.WebAPI/Controllers/
├── AuthController.cs              ⏳ 需要重构 → BaseApiController
├── UsersController.cs             ⏳ 需要重构 → BaseApiController
├── PatientsController.cs          ⏳ 需要重构 → BaseApiController
├── HerbsController.cs             ✅ 已重构 (281行，符合标准)
├── ConsultationsController.cs     ⏳ 需要重构 → BaseApiController
├── PrescriptionsController.cs     ⏳ 需要重构 → BaseApiController
├── MedicalCasesController.cs      ⏳ 需要重构 → BaseApiController
└── FormulaController.cs           ⏳ 需要重构 → BaseApiController
```

#### 重构标准
- 继承BaseApiController
- 使用ApiResponse<T>包装所有响应
- 统一异常处理try-catch块
- 添加操作日志记录
- 应用标准化验证方法

### 第二阶段：Service层统一化 ⏳

#### 需要重构的Service
```
Backend/Services/各模块/Services/
├── AuthService.cs                 ⏳ 需要适配 → BaseService模式
├── UserService.cs                 🔄 部分适配，需完善
├── PatientService.cs              🔄 部分适配，需完善
├── HerbService.cs                 🔄 部分适配，需完善
├── ConsultationService.cs         ⏳ 需要适配 → BaseService模式
├── PrescriptionService.cs         ⏳ 需要适配 → BaseService模式
├── MedicalCaseService.cs          ⏳ 需要适配 → BaseService模式
└── FormulaService.cs              ⏳ 需要适配 → BaseService模式
```

#### 重构标准
- 继承BaseService或实现统一接口
- 统一异常处理机制
- 标准化日志记录
- 参数验证和业务规则验证分离
- 操作前后钩子方法使用

### 第三阶段：Repository层统一化 ✅

#### 已完成的Repository
```
✅ UserRepository      - 已适配BaseRepository
✅ HerbRepository      - 已适配BaseRepository (225→63行)
✅ PatientRepository   - 已适配BaseRepository架构
```

#### 待完成的Repository
```
⏳ AuthRepository
⏳ ConsultationRepository
⏳ PrescriptionRepository
⏳ MedicalCaseRepository
⏳ FormulaRepository
```

### 第四阶段：前端统一化 ⏳

#### WPF前端统一化
```
Frontend/Desktop/
├── Services/                      ⏳ 需要统一HTTP客户端和错误处理
├── ViewModels/                    🔄 部分大型ViewModel已重构
├── Views/                         ⏳ 需要统一样式和交互模式
└── Modules/                       🔄 部分模块已重构
```

#### 前端统一标准
- 统一的HTTP客户端配置和错误处理
- 标准化的ViewModel基类
- 一致的数据绑定和命令模式
- 统一的用户界面样式和交互体验

---

## 🔧 实施细节

### 1. API Controller重构模板

#### 重构前示例
```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _service.GetByIdAsync(id);
        if (user == null)
            return NotFound();
        return Ok(user);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserCreateDto dto)
    {
        var user = await _service.CreateAsync(dto);
        return Ok(user);
    }
}
```

#### 重构后示例
```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;

    public UsersController(
        IUserService userService,
        IMemoryCache cache,
        ILogger<UsersController> logger)
        : base(logger, cache)
    {
        _userService = userService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(Guid id)
    {
        try
        {
            var validation = ValidateGuid(id, "用户ID");
            if (validation != null) return validation;

            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound("用户不存在", ApiErrorCodes.USER_NOT_FOUND);

            return Success(user, "查询成功");
        }
        catch (Exception ex)
        {
            return HandleException(ex, "查询用户", id);
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] UserCreateDto dto)
    {
        try
        {
            var validation = ValidateModel();
            if (validation != null) return validation;

            var (operatorId, operatorName, _) = GetOperator();
            var user = await _userService.AddAsync(dto, operatorId, operatorName);
            
            if (user == null)
                return BusinessFail<UserDto>("创建失败", ApiErrorCodes.DATA_SAVE_FAILED);

            LogOperation("创建用户", user, user.Id);
            return Success(user, "创建成功");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("已存在"))
        {
            return BusinessFail<UserDto>(ex.Message, ApiErrorCodes.USERNAME_EXISTS);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "创建用户", dto);
        }
    }
}
```

### 2. Service层重构模板

#### 重构前示例
```csharp
public class UserService : IUserService
{
    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await _repository.GetByIdAsync(id);
        return _mapper.Map<UserDto>(user);
    }
    
    public async Task<UserDto> CreateAsync(UserCreateDto dto)
    {
        var user = _mapper.Map<UserModel>(dto);
        var result = await _repository.AddAsync(user);
        return _mapper.Map<UserDto>(result);
    }
}
```

#### 重构后示例
```csharp
public class UserService : BaseService<UserModel, UserDto, UserCreateDto, UserUpdateDto>, IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(
        IUserRepository repository,
        IMapper mapper,
        ILogger<UserService> logger,
        IUnifiedLogService logService)
        : base(repository, mapper, logger, logService)
    {
        _userRepository = repository;
    }

    // 重写基类方法实现特定业务逻辑
    protected override async Task ValidateCreateAsync(UserCreateDto createDto)
    {
        if (await _userRepository.ExistsByUsernameAsync(createDto.Username))
            throw new InvalidOperationException("用户名已存在");
    }

    protected override async Task PreCreateAsync(UserModel entity, UserCreateDto createDto)
    {
        entity.PasswordHash = HashPassword(createDto.Password);
        entity.PinYinCode = GeneratePinYin(entity.RealName);
    }

    // 特定业务方法
    public async Task<bool> ResetPasswordAsync(Guid id, Guid operatorId, string operatorName)
    {
        try
        {
            var user = await _repository.GetByIdAsync(id);
            if (user == null)
                throw new InvalidOperationException("用户不存在");

            user.PasswordHash = HashPassword(UserOptions.DefaultPassword);
            
            var result = await _repository.UpdateAsync(user);
            if (result != null)
            {
                await LogOperationAsync("重置密码", user);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重置用户密码失败，用户ID: {UserId}", id);
            throw;
        }
    }
}
```

### 3. 错误处理统一化

#### 全局异常处理中间件
```csharp
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (ArgumentException ex)
        {
            await HandleExceptionAsync(context, ex, 400, ApiErrorCodes.VALIDATION_ERROR);
        }
        catch (InvalidOperationException ex)
        {
            await HandleExceptionAsync(context, ex, 400, ApiErrorCodes.BUSINESS_ERROR);
        }
        catch (UnauthorizedAccessException ex)
        {
            await HandleExceptionAsync(context, ex, 401, ApiErrorCodes.UNAUTHORIZED);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, 500, ApiErrorCodes.INTERNAL_ERROR);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex, int statusCode, string errorCode)
    {
        var response = new ApiResponse
        {
            Success = false,
            Message = ex.Message,
            ErrorCode = errorCode,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            RequestId = context.TraceIdentifier
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

---

## 📈 质量保证措施

### 1. 编译检查
```bash
# 每次重构后必须通过的检查
dotnet build LYBT.All.sln --configuration Release --no-restore
# 目标：0个编译错误，0个编译警告
```

### 2. 测试覆盖率检查
```bash
# 运行测试并生成覆盖率报告
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
reportgenerator -reports:./TestResults/*/coverage.cobertura.xml -targetdir:./TestResults/CoverageReport
# 目标：覆盖率>60%
```

### 3. 代码质量检查
```bash
# 代码分析
dotnet format --verify-no-changes
# 目标：符合编码规范，无格式问题
```

### 4. 性能基准测试
```bash
# API响应时间测试
dotnet run --project tests/Performance/LYBT.Performance.Tests
# 目标：95%的API响应时间<2秒
```

---

## 🎯 成功验收标准

### 技术指标
- [ ] 整个Solution编译成功率100%
- [ ] 代码覆盖率达到60%
- [ ] 单个文件行数<500行
- [ ] API响应时间<2秒
- [ ] 数据库查询时间<1秒

### 架构一致性
- [ ] 所有Controller继承BaseApiController
- [ ] 所有API使用ApiResponse<T>响应格式
- [ ] 所有Service继承BaseService或实现统一接口
- [ ] 所有Repository继承BaseRepository
- [ ] 所有测试类继承BaseTestFixture

### 错误处理一致性
- [ ] 统一的异常类型和消息格式
- [ ] 标准化的错误代码使用
- [ ] 完整的日志记录覆盖
- [ ] 优雅的错误页面和用户提示

### 单一职责验证
- [ ] 每个类职责明确且唯一
- [ ] 方法功能单一且可测试
- [ ] 模块间依赖关系清晰
- [ ] 接口设计符合ISP原则

---

*"通过Solution级别的统一化，我们建立了可持续发展的企业级架构标准。"*