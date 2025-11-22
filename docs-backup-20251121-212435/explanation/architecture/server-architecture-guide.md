# Server端三层架构指南

**基于凌隐宝堂中医诊所实际代码架构的完整指南** - 深入理解Server端三层架构设计与实现

## 🏗️ 架构概览

### 架构层次图
```
┌─────────────────────────────────────────────────────────────┐
│                        Presentation Layer                    │
│                     (Controllers & DTOs)                     │
├─────────────────────────────────────────────────────────────┤
│                        Application Layer                     │
│                      (Services & Interfaces)                 │
├─────────────────────────────────────────────────────────────┤
│                        Infrastructure Layer                  │
│                 (Repositories & Data Access)                 │
└─────────────────────────────────────────────────────────────┘
```

### 实际项目结构
```
src/Server/
├── Services/LYBT.WebAPI/           # Presentation Layer
│   ├── Controllers/                # API控制器
│   ├── DTOs/                       # 数据传输对象
│   ├── Middleware/                 # 中间件
│   └── Configuration/              # 配置类
├── Core/                           # Application Layer (Shared)
├── Modules/                        # Application Layer (Business)
│   ├── LYBT.Module.Auth/           # 认证模块
│   ├── LYBT.Module.Users/          # 用户管理模块
│   ├── LYBT.Module.Patients/       # 患者管理模块
│   ├── LYBT.Module.MedicalCase/    # 医案管理模块
│   ├── LYBT.Module.Consultation/   # 诊疗记录模块
│   ├── LYBT.Module.Prescriptions/  # 处方管理模块
│   ├── LYBT.Module.Herbs/          # 药材管理模块
│   └── LYBT.Module.Formula/        # 验方管理模块
└── Infrastructure/LYBT.Infrastructure/  # Infrastructure Layer
    ├── Data/                       # 数据访问
    ├── Repositories/               # 仓储实现
    ├── Configuration/              # 基础设施配置
    └── Services/                   # 基础设施服务
```

## 📋 展示层 (Presentation Layer)

### 1. 控制器设计原则

#### 标准控制器模板
```csharp
/// <summary>
/// 患者管理控制器 - 展示层示例
/// 职责：处理HTTP请求/响应，参数验证，业务服务调用
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // 根据需要添加认证
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(
        IPatientService patientService,
        ILogger<PatientsController> logger)
    {
        _patientService = patientService;
        _logger = logger;
    }

    /// <summary>
    /// 分页获取患者列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<PatientDto>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null)
    {
        try
        {
            var result = await _patientService.GetPagedAsync(page, pageSize, keyword);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者列表失败");
            return StatusCode(500, new { message = "获取患者列表失败" });
        }
    }

    /// <summary>
    /// 根据ID获取患者详情
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PatientDto>> GetById(Guid id)
    {
        try
        {
            var result = await _patientService.GetByIdAsync(id);
            if (!result.IsSuccess)
                return NotFound(new { message = result.Message });
            
            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者详情失败，ID: {PatientId}", id);
            return StatusCode(500, new { message = "获取患者详情失败" });
        }
    }

    /// <summary>
    /// 创建新患者
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PatientDto>> Create([FromBody] PatientCreateDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _patientService.CreateAsync(dto);
            if (!result.IsSuccess)
                return BadRequest(new { message = result.Message });

            return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建患者失败");
            return StatusCode(500, new { message = "创建患者失败" });
        }
    }

    /// <summary>
    /// 更新患者信息
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PatientDto>> Update(Guid id, [FromBody] PatientUpdateDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _patientService.UpdateAsync(id, dto);
            if (!result.IsSuccess)
                return BadRequest(new { message = result.Message });

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新患者失败，ID: {PatientId}", id);
            return StatusCode(500, new { message = "更新患者失败" });
        }
    }

    /// <summary>
    /// 删除患者
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _patientService.DeleteAsync(id);
            if (!result.IsSuccess)
                return BadRequest(new { message = result.Message });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除患者失败，ID: {PatientId}", id);
            return StatusCode(500, new { message = "删除患者失败" });
        }
    }
}
```

#### 控制器设计规范

##### ✅ 必须遵循的原则
1. **单一职责**：只处理HTTP请求/响应
2. **参数验证**：使用ModelState验证输入
3. **错误处理**：统一异常处理和日志记录
4. **无业务逻辑**：业务逻辑委托给Service层
5. **标准响应**：使用统一的响应格式

##### ❌ 禁止的做法
1. **直接数据库操作**：禁止在Controller中直接访问数据库
2. **复杂业务逻辑**：业务逻辑必须在Service层
3. **硬编码配置**：配置信息应该外部化
4. **跨层调用**：禁止直接调用Repository层

### 2. DTO设计模式

#### 标准DTO设计
```csharp
/// <summary>
/// 患者数据传输对象
/// </summary>
public class PatientDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    
    /// <summary>
    /// 计算年龄属性（非数据库字段）
    /// </summary>
    public int Age => CalculateAge(BirthDate);
    
    public string? IdNumber { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Address { get; set; }
    public CommonStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 患者创建DTO
/// </summary>
public class PatientCreateDto
{
    /// <summary>
    /// 姓名 - 必填
    /// </summary>
    [Required(ErrorMessage = "姓名不能为空")]
    [StringLength(50, ErrorMessage = "姓名长度不能超过50个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 性别
    /// </summary>
    public Gender Gender { get; set; }

    /// <summary>
    /// 出生日期
    /// </summary>
    [DataType(DataType.Date)]
    public DateTime? BirthDate { get; set; }

    /// <summary>
    /// 身份证号
    /// </summary>
    [RegularExpression(@"^\d{17}[\dX]$", ErrorMessage = "身份证号格式错误")]
    public string? IdNumber { get; set; }

    /// <summary>
    /// 联系电话 - 必填
    /// </summary>
    [Required(ErrorMessage = "联系电话不能为空")]
    [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "手机号码格式错误")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// 地址
    /// </summary>
    [StringLength(200, ErrorMessage = "地址长度不能超过200个字符")]
    public string? Address { get; set; }
}

/// <summary>
/// 患者更新DTO
/// </summary>
public class PatientUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? IdNumber { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Address { get; set; }
    public CommonStatus Status { get; set; }
}
```

#### DTO设计规范

##### ✅ 设计原则
1. **职责单一**：每个DTO只负责特定的数据传输
2. **输入输出分离**：Create/Update/Response DTO分离
3. **验证完整**：使用数据注解进行验证
4. **命名清晰**：明确表达数据用途和范围

##### 📝 命名约定
- **查询DTO**：`{Entity}Dto`
- **创建DTO**：`{Entity}CreateDto`
- **更新DTO**：`{Entity}UpdateDto`
- **查询参数**：`{Entity}QueryDto` 或 `{Entity}SearchDto`

### 3. 统一响应格式

#### ServiceResult<T> 模式
```csharp
/// <summary>
/// 统一服务响应结果
/// </summary>
public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    public static ServiceResult<T> Success(T data, string message = "操作成功")
    {
        return new ServiceResult<T>
        {
            IsSuccess = true,
            Data = data,
            Message = message
        };
    }

    public static ServiceResult<T> Failure(string message, List<string>? errors = null)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            Message = message,
            Errors = errors
        };
    }
}

/// <summary>
/// 分页结果包装
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;
}
```

## 🎯 应用层 (Application Layer)

### 1. 服务层设计原则

#### 标准服务模板
```csharp
/// <summary>
/// 患者服务 - 应用层示例
/// 职责：业务逻辑处理、事务管理、数据协调
/// </summary>
public class PatientService : IPatientService
{
    private readonly IPatientRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<PatientService> _logger;
    private readonly IValidator<PatientCreateDto> _createValidator;
    private readonly IValidator<PatientUpdateDto> _updateValidator;

    public PatientService(
        IPatientRepository repository,
        IMapper mapper,
        ILogger<PatientService> logger,
        IValidator<PatientCreateDto> createValidator,
        IValidator<PatientUpdateDto> updateValidator)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 分页获取患者列表
    /// </summary>
    public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(
        int page = 1, 
        int pageSize = 20, 
        string? keyword = null)
    {
        try
        {
            // 参数验证
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var pagedResult = await _repository.GetPagedAsync(page, pageSize, keyword);
            var dto = new PagedResult<PatientDto>
            {
                Items = _mapper.Map<List<PatientDto>>(pagedResult.Items),
                TotalCount = pagedResult.TotalCount,
                CurrentPage = pagedResult.CurrentPage,
                PageSize = pagedResult.PageSize
            };

            return ServiceResult<PagedResult<PatientDto>>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者列表失败，参数：Page={Page}, PageSize={PageSize}, Keyword={Keyword}", 
                page, pageSize, keyword);
            return ServiceResult<PagedResult<PatientDto>>.Failure("获取患者列表失败");
        }
    }

    /// <summary>
    /// 根据ID获取患者详情
    /// </summary>
    public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
                return ServiceResult<PatientDto>.Failure("患者ID不能为空");

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return ServiceResult<PatientDto>.Failure("患者不存在");

            var dto = _mapper.Map<PatientDto>(entity);
            return ServiceResult<PatientDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者详情失败，ID: {PatientId}", id);
            return ServiceResult<PatientDto>.Failure("获取患者详情失败");
        }
    }

    /// <summary>
    /// 创建患者 - 包含业务验证
    /// </summary>
    public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
    {
        try
        {
            // 输入验证
            var validationResult = await _createValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<PatientDto>.Failure("输入验证失败", errors);
            }

            // 业务规则验证
            var existingPatient = await _repository.GetByPhoneAsync(dto.PhoneNumber);
            if (existingPatient != null)
                return ServiceResult<PatientDto>.Failure("该手机号已被使用");

            // 创建实体
            var entity = _mapper.Map<Patient>(dto);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.Now;
            entity.UpdatedAt = DateTime.Now;
            entity.Status = CommonStatus.Enabled;

            // 保存到数据库
            var result = await _repository.AddAsync(entity);
            var resultDto = _mapper.Map<PatientDto>(result);

            _logger.LogInformation("创建患者成功，ID: {PatientId}, 姓名: {Name}", result.Id, result.Name);
            return ServiceResult<PatientDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建患者失败，数据：{PatientData}", dto);
            return ServiceResult<PatientDto>.Failure("创建患者失败");
        }
    }

    /// <summary>
    /// 更新患者信息
    /// </summary>
    public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto)
    {
        try
        {
            // 输入验证
            var validationResult = await _updateValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ServiceResult<PatientDto>.Failure("输入验证失败", errors);
            }

            // 获取现有实体
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return ServiceResult<PatientDto>.Failure("患者不存在");

            // 业务规则验证
            if (dto.PhoneNumber != entity.PhoneNumber)
            {
                var existingPatient = await _repository.GetByPhoneAsync(dto.PhoneNumber);
                if (existingPatient != null && existingPatient.Id != id)
                    return ServiceResult<PatientDto>.Failure("该手机号已被使用");
            }

            // 更新实体
            _mapper.Map(dto, entity);
            entity.UpdatedAt = DateTime.Now;

            var result = await _repository.UpdateAsync(entity);
            var resultDto = _mapper.Map<PatientDto>(result);

            _logger.LogInformation("更新患者成功，ID: {PatientId}", id);
            return ServiceResult<PatientDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新患者失败，ID: {PatientId}, 数据：{PatientData}", id, dto);
            return ServiceResult<PatientDto>.Failure("更新患者失败");
        }
    }

    /// <summary>
    /// 删除患者 - 软删除
    /// </summary>
    public async Task<ServiceResult> DeleteAsync(Guid id)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return ServiceResult.Failure("患者不存在");

            // 检查是否有关联数据
            var hasMedicalCases = await _repository.HasMedicalCasesAsync(id);
            if (hasMedicalCases)
                return ServiceResult.Failure("该患者有关联的医案，无法删除");

            // 软删除
            entity.Status = CommonStatus.Disabled;
            entity.UpdatedAt = DateTime.Now;

            var result = await _repository.UpdateAsync(entity);
            
            _logger.LogInformation("删除患者成功，ID: {PatientId}", id);
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除患者失败，ID: {PatientId}", id);
            return ServiceResult.Failure("删除患者失败");
        }
    }
}
```

#### 服务层设计规范

##### ✅ 核心原则
1. **业务逻辑封装**：所有业务逻辑在Service层实现
2. **事务边界**：Service层是事务的边界
3. **数据协调**：协调多个Repository的数据操作
4. **异常处理**：统一的异常处理和日志记录
5. **接口抽象**：通过接口定义服务契约

##### 🎯 职责范围
- **业务规则验证**：检查业务规则和数据完整性
- **数据转换**：Entity与DTO之间的映射
- **流程协调**：编排多个Repository的操作
- **缓存管理**：业务数据的缓存策略
- **事件发布**：领域事件的发布和处理

### 2. 模块化设计

#### 模块结构模板
```
LYBT.Module.{ModuleName}/
├── Interfaces/                    # 接口定义
│   ├── I{ModuleName}Service.cs    # 服务接口
│   └── I{ModuleName}Repository.cs # 仓储接口
├── Services/                      # 服务实现
│   └── {ModuleName}Service.cs     # 服务实现类
├── DTOs/                          # 数据传输对象
│   ├── {ModuleName}Dto.cs         # 查询DTO
│   ├── {ModuleName}CreateDto.cs   # 创建DTO
│   ├── {ModuleName}UpdateDto.cs   # 更新DTO
│   └── Validators/                # 验证器
│       ├── {ModuleName}CreateValidator.cs
│       └── {ModuleName}UpdateValidator.cs
├── Entities/                      # 实体定义（如果模块独有）
├── Enums/                         # 枚举定义
└── {ModuleName}.Module.cs         # 模块注册类
```

#### 模块依赖原则

##### ✅ 允许的依赖关系
- **依赖Core层**：共享的基础类型和接口
- **依赖Infrastructure层**：数据访问和基础设施
- **依赖其他Module接口**：通过接口进行模块间通信

##### ❌ 禁止的依赖关系
- **禁止循环依赖**：模块之间不能形成循环引用
- **禁止直接访问实现**：跨模块必须通过接口
- **禁止数据库耦合**：模块不应该直接访问其他模块的数据库表

### 3. 业务接口设计

#### 标准接口定义
```csharp
/// <summary>
/// 患者服务接口
/// </summary>
public interface IPatientService
{
    /// <summary>
    /// 分页获取患者列表
    /// </summary>
    Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(
        int page = 1, 
        int pageSize = 20, 
        string? keyword = null);

    /// <summary>
    /// 根据ID获取患者详情
    /// </summary>
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// 创建患者
    /// </summary>
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);

    /// <summary>
    /// 更新患者信息
    /// </summary>
    Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto);

    /// <summary>
    /// 删除患者
    /// </summary>
    Task<ServiceResult> DeleteAsync(Guid id);

    /// <summary>
    /// 搜索患者
    /// </summary>
    Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword);

    /// <summary>
    /// Excel导入患者数据
    /// </summary>
    Task<ServiceResult<ImportResultDto<PatientDto>>> ImportFromExcelAsync(
        Stream stream, 
        string? fileName = null);

    /// <summary>
    /// 生成导入模板
    /// </summary>
    MemoryStream GenerateImportTemplate();
}
```

## 🗄️ 基础设施层 (Infrastructure Layer)

### 1. 仓储模式实现

#### 标准仓储接口
```csharp
/// <summary>
/// 通用仓储接口
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>> GetAllAsync();
    Task<PagedResult<T>> GetPagedAsync(int page, int pageSize, string? keyword = null);
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<int> CountAsync();
}

/// <summary>
/// 患者仓储接口 - 扩展通用接口
/// </summary>
public interface IPatientRepository : IRepository<Patient>
{
    Task<Patient?> GetByPhoneAsync(string phoneNumber);
    Task<List<Patient>> GetByNameAsync(string name);
    Task<bool> HasMedicalCasesAsync(Guid patientId);
    Task<PagedResult<Patient>> GetPagedAsync(int page, int pageSize, string? keyword = null);
}
```

#### 仓储实现模板
```csharp
/// <summary>
/// 患者仓储实现
/// 职责：数据访问抽象，EF Core操作封装
/// </summary>
public class PatientRepository : IPatientRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<PatientRepository> _logger;

    public PatientRepository(
        AppDbContext dbContext,
        ILogger<PatientRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Patient?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _dbContext.Patients
                .FirstOrDefaultAsync(p => p.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据ID获取患者失败，ID: {PatientId}", id);
            throw;
        }
    }

    public async Task<List<Patient>> GetAllAsync()
    {
        try
        {
            return await _dbContext.Patients
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有患者失败");
            throw;
        }
    }

    public async Task<PagedResult<Patient>> GetPagedAsync(int page, int pageSize, string? keyword = null)
    {
        try
        {
            var query = _dbContext.Patients.AsQueryable();

            // 关键字搜索
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p => 
                    p.Name.Contains(keyword) ||
                    p.PhoneNumber.Contains(keyword));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Patient>
            {
                Items = items,
                TotalCount = totalCount,
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分页获取患者失败，参数：Page={Page}, PageSize={PageSize}, Keyword={Keyword}", 
                page, pageSize, keyword);
            throw;
        }
    }

    public async Task<Patient> AddAsync(Patient entity)
    {
        try
        {
            var result = await _dbContext.Patients.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return result.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加患者失败，数据：{PatientData}", entity);
            throw;
        }
    }

    public async Task<Patient> UpdateAsync(Patient entity)
    {
        try
        {
            var result = _dbContext.Patients.Update(entity);
            await _dbContext.SaveChangesAsync();
            return result.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新患者失败，ID: {PatientId}", entity.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var entity = await GetByIdAsync(id);
            if (entity == null) return false;

            _dbContext.Patients.Remove(entity);
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除患者失败，ID: {PatientId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        try
        {
            return await _dbContext.Patients
                .AnyAsync(p => p.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查患者是否存在失败，ID: {PatientId}", id);
            throw;
        }
    }

    public async Task<int> CountAsync()
    {
        try
        {
            return await _dbContext.Patients.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "统计患者数量失败");
            throw;
        }
    }

    // 扩展方法
    public async Task<Patient?> GetByPhoneAsync(string phoneNumber)
    {
        try
        {
            return await _dbContext.Patients
                .FirstOrDefaultAsync(p => p.PhoneNumber == phoneNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据手机号获取患者失败，Phone: {PhoneNumber}", phoneNumber);
            throw;
        }
    }

    public async Task<List<Patient>> GetByNameAsync(string name)
    {
        try
        {
            return await _dbContext.Patients
                .Where(p => p.Name.Contains(name))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据姓名获取患者失败，Name: {Name}", name);
            throw;
        }
    }

    public async Task<bool> HasMedicalCasesAsync(Guid patientId)
    {
        try
        {
            return await _dbContext.MedicalCases
                .AnyAsync(mc => mc.PatientId == patientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查患者医案失败，PatientId: {PatientId}", patientId);
            throw;
        }
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}
```

### 2. 数据库上下文设计

#### AppDbContext 配置
```csharp
/// <summary>
/// 应用数据库上下文
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets - 按模块分组
    // 认证模块
    public DbSet<AdminSecret> AdminSecrets { get; set; }
    
    // 用户模块
    public DbSet<User> Users { get; set; }
    
    // 患者模块
    public DbSet<Patient> Patients { get; set; }
    
    // 医案模块
    public DbSet<MedicalCase> MedicalCases { get; set; }
    
    // 诊疗模块
    public DbSet<Consultation> Consultations { get; set; }
    
    // 处方模块
    public DbSet<Prescription> Prescriptions { get; set; }
    public DbSet<PrescriptionItem> PrescriptionItems { get; set; }
    
    // 药材模块
    public DbSet<Herb> Herbs { get; set; }
    
    // 验方模块
    public DbSet<Formula> Formulas { get; set; }
    public DbSet<FormulaItem> FormulaItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // 应用所有配置
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        
        // 设置软删除全局过滤器
        ConfigureSoftDelete(modelBuilder);
        
        // 设置查询过滤器
        ConfigureQueryFilters(modelBuilder);
    }

    private static void ConfigureSoftDelete(ModelBuilder modelBuilder)
    {
        // 为实现ISoftDelete的实体配置全局过滤器
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(
                    typeof(ISoftDelete).GetMethod(nameof(ISoftDelete.IsDeleted)) != null
                        ? Expression.Lambda(
                            Expression.Not(
                                Expression.Property(
                                    Expression.Parameter(entityType.ClrType, "e"),
                                    nameof(ISoftDelete.IsDeleted))),
                            Expression.Parameter(entityType.ClrType, "e"))
                        : null);
            }
        }
    }

    private static void ConfigureQueryFilters(ModelBuilder modelBuilder)
    {
        // 示例：为Patient实体添加默认状态过滤器
        modelBuilder.Entity<Patient>().HasQueryFilter(p => p.Status != CommonStatus.Deleted);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 自动设置审计字段
        SetAuditFields();
        
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void SetAuditFields()
    {
        var entries = ChangeTracker.Entries();
        
        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity is IAuditable auditableAdded)
                    {
                        auditableAdded.CreatedAt = DateTime.Now;
                        auditableAdded.UpdatedAt = DateTime.Now;
                    }
                    break;
                    
                case EntityState.Modified:
                    if (entry.Entity is IAuditable auditableModified)
                    {
                        auditableModified.UpdatedAt = DateTime.Now;
                        entry.Property(nameof(auditableModified.CreatedAt)).IsModified = false;
                    }
                    break;
            }
        }
    }
}
```

### 3. 实体配置模式

#### 实体配置模板
```csharp
/// <summary>
/// 患者实体配置
/// </summary>
public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        // 表名配置
        builder.ToTable("Patients");
        
        // 主键配置
        builder.HasKey(p => p.Id);
        
        // 索引配置
        builder.HasIndex(p => p.PhoneNumber).IsUnique().HasDatabaseName("IX_Patients_PhoneNumber");
        builder.HasIndex(p => p.Name).HasDatabaseName("IX_Patients_Name");
        builder.HasIndex(p => p.CreatedAt).HasDatabaseName("IX_Patients_CreatedAt");
        
        // 属性配置
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(p => p.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);
            
        builder.Property(p => p.IdNumber)
            .HasMaxLength(18);
            
        builder.Property(p => p.Address)
            .HasMaxLength(200);
        
        // 枚举配置
        builder.Property(p => p.Gender)
            .HasConversion<int>();
            
        builder.Property(p => p.Status)
            .HasConversion<int>();
        
        // 忽略属性
        builder.Ignore(p => p.Age); // 计算属性不映射到数据库
        
        // 关系配置
        builder.HasMany(p => p.MedicalCases)
            .WithOne(mc => mc.Patient)
            .HasForeignKey(mc => mc.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // 数据种子
        builder.HasData(
            new Patient
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Name = "测试患者",
                Gender = Gender.Male,
                PhoneNumber = "13800138000",
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
    }
}
```

## 🔄 依赖注入配置

### 1. 服务注册模式

#### 标准服务注册
```csharp
/// <summary>
/// 服务注册扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册应用服务
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // 注册AutoMapper
        services.AddAutoMapper(typeof(MappingProfile));
        
        // 注册FluentValidation
        services.AddValidatorsFromAssembly(typeof(PatientCreateValidator).Assembly);
        
        // 注册模块服务
        services.RegisterModuleServices();
        
        return services;
    }

    /// <summary>
    /// 注册模块服务 - 按模块分组注册
    /// </summary>
    private static IServiceCollection RegisterModuleServices(this IServiceCollection services)
    {
        // 认证模块
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtService, JwtService>();
        
        // 用户模块
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserRepository, UserRepository>();
        
        // 患者模块
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        
        // 医案模块
        services.AddScoped<IMedicalCaseService, MedicalCaseService>();
        services.AddScoped<IMedicalCaseRepository, MedicalCaseRepository>();
        
        // 诊疗模块
        services.AddScoped<IConsultationService, ConsultationService>();
        services.AddScoped<IConsultationRepository, ConsultationRepository>();
        
        // 处方模块
        services.AddScoped<IPrescriptionService, PrescriptionService>();
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        
        // 药材模块
        services.AddScoped<IHerbService, HerbService>();
        services.AddScoped<IHerbRepository, HerbRepository>();
        
        // 验方模块
        services.AddScoped<IFormulaService, FormulaService>();
        services.AddScoped<IFormulaRepository, FormulaRepository>();
        
        return services;
    }
}
```

### 2. 生命周期管理

#### 服务生命周期指南
```csharp
/// <summary>
/// 服务生命周期示例
/// </summary>
public class ServiceLifetimeExamples
{
    /// <summary>
    /// Scoped - 推荐：每个请求一个实例
    /// 适用于：有状态的服务、Repository、Service
    /// </summary>
    public void RegisterScopedServices(IServiceCollection services)
    {
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services scoped = provider.GetRequiredService<IPatientService>();
    }

    /// <summary>
    /// Transient - 每次注入都创建新实例
    /// 适用于：无状态的服务、轻量级服务
    /// </summary>
    public void RegisterTransientServices(IServiceCollection services)
    {
        services.AddTransient<IValidator<PatientCreateDto>, PatientCreateValidator>();
        services.AddTransient<IMapper, Mapper>();
    }

    /// <summary>
    /// Singleton - 整个应用生命周期只有一个实例
    /// 适用于：无状态的服务、缓存服务、配置服务
    /// </summary>
    public void RegisterSingletonServices(IServiceCollection services)
    {
        services.AddSingleton<IConfiguration>(provider => 
            provider.GetRequiredService<IConfiguration>());
        services.AddSingleton<ICacheService, MemoryCacheService>();
    }
}
```

## 🎯 架构合规检查

### 1. 层次依赖检查

#### 依赖方向验证
```
✅ 允许的依赖方向：
Controller → Service → Repository → DbContext
Controller → DTO
Service → Interface
Repository → Entity

❌ 禁止的依赖方向：
Controller → Repository (跨层调用)
Service → DbContext (绕过Repository)
Controller → Entity (直接访问实体)
```

### 2. 代码质量检查

#### 架构合规清单
- [ ] **Controller层检查**
  - [ ] 没有业务逻辑实现
  - [ ] 统一异常处理
  - [ ] 参数验证完整
  - [ ] 日志记录规范

- [ ] **Service层检查**
  - [ ] 接口契约清晰
  - [ ] 业务逻辑完整
  - [ ] 事务边界正确
  - [ ] 异常处理规范

- [ ] **Repository层检查**
  - [ ] 数据访问抽象
  - [ ] 查询优化合理
  - [ ] 索引使用正确
  - [ ] 并发控制考虑

### 3. 性能优化检查

#### 查询性能清单
- [ ] **N+1查询检查**
  - [ ] 使用Include预加载关联数据
  - [ ] 避免在循环中查询数据库
  - [ ] 批量操作优化

- [ ] **分页查询检查**
  - [ ] 使用Skip/Take分页
  - [ ] 索引支持排序字段
  - [ ] 结果集大小合理

- [ ] **缓存策略检查**
  - [ ] 静态数据缓存
  - [ ] 查询结果缓存
  - [ ] 缓存过期策略

---

## 📚 最佳实践总结

### ✅ 推荐做法
1. **分层明确**：严格按照三层架构组织代码
2. **依赖倒置**：高层模块不依赖低层模块，都依赖抽象
3. **单一职责**：每个类只负责一个明确的职责
4. **接口隔离**：使用小而专一的接口
5. **开闭原则**：对扩展开放，对修改关闭

### ❌ 避免做法
1. **跨层调用**：绕过中间层直接调用
2. **循环依赖**：模块或服务之间相互依赖
3. **过度设计**：不必要的抽象和复杂性
4. **硬编码**：配置信息写死在代码中
5. **忽略测试**：没有相应的单元测试和集成测试

---

*此Server端三层架构指南基于凌隐宝堂中医诊所实际代码架构编写，确保与项目实践完全一致。开发过程中应严格遵循此架构设计。*