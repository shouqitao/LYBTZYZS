# Server端架构指南

**🏗️ 三层架构、8个模块、服务标准** - 凌美对齐实际代码架构实现

## 🎯 Server端架构概述

凌隐宝堂中医诊所管理系统Server端采用经典的三层架构设计，确保代码的高内聚、低耦合和可扩展性。本架构指南详细阐述Server端的架构设计原理、技术选型和实现规范，与项目实际代码架构完全对齐。

## 🏗️ 三层架构设计

### 架构层次结构
```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                    │
│                (Controllers & DTOs)                     │
│              ┌─────────────┬─────────────────┐               │
│              │  Controllers   │   DTOs/Models    │               │
│              └─────────────┴─────────────────┘               │
├─────────────────────────────────────────────────────────────┤
│                    Application Layer                     │
│                  (Services & Interfaces)                 │
│              ┌─────────────────────────────────────┐           │
│              │    Business Services                    │           │
│              │    Domain Interfaces                    │           │
│              │    Application Services              │           │
│              └─────────────────────────────────────┘           │
├─────────────────────────────────────────────────────────────┤
│                  Infrastructure Layer                  │
│                (Repositories & Data Access)                 │
│              ┌─────────────────────────────────────┐           │
│              │    Entity Framework Core               │           │
│              │    Repository Implementations          │           │
│              │    Database Connections               │           │
│              └─────────────────────────────────────┘           │
└─────────────────────────────────────────────────────────────┘
```

### 实际项目结构映射

⚠️ **关键架构说明**：
- **Controllers位置**：所有API控制器统一在`LYBT.WebAPI/Controllers/`，不在各Module中
- **Module组成**：各Module仅包含Services + Repositories + Interfaces，专注业务逻辑层
- **分层职责**：Controllers（表示层）→ Services（应用层）→ Repositories（数据访问层）

```
src/Server/
├── Services/LYBT.WebAPI/           # ⭐ Presentation Layer（表示层）
│   ├── Controllers/                # ⭐ 所有API控制器统一位置（13个）
│   │   ├── 业务控制器（8个）：
│   │   ├── AuthController.cs           # 认证授权API
│   │   ├── UsersController.cs          # 用户管理API
│   │   ├── PatientsController.cs       # 患者管理API
│   │   ├── MedicalCaseController.cs    # 医案管理API
│   │   ├── ConsultationController.cs   # 诊疗记录API
│   │   ├── PrescriptionsController.cs  # 处方管理API
│   │   ├── HerbsController.cs          # 药材管理API
│   │   ├── FormulasController.cs       # 验方管理API
│   │   └── 系统控制器（5个）：
│   │       ├── HealthController.cs         # 健康检查
│   │       ├── CacheHealthController.cs    # 缓存健康检查
│   │       ├── PerformanceController.cs    # 性能监控
│   │       ├── RootHealthController.cs     # 根路径健康检查
│   │       └── BaseApiController.cs        # 基础控制器（抽象基类）
│   ├── DTOs/                       # 数据传输对象
│   ├── Middleware/                 # 中间件
│   ├── Filters/                    # 过滤器
│   └── Configuration/              # 配置类
│
├── Core/                           # Application Layer (Shared)
│   ├── LYBT.Entities/              # 实体定义
│   ├── LYBT.Infrastructure/        # 基础设施（含BaseApiController）
│   └── LYBT.Server.Interfaces/     # 服务接口定义（IPatientService等）
│
├── Modules/                        # ⭐ Application Layer (Business) - 仅包含业务逻辑
│   ├── LYBT.Module.Auth/           # 认证模块
│   │   ├── Services/               # ✅ AuthService业务服务
│   │   └── Interfaces/             # ✅ IAuthService接口
│   ├── LYBT.Module.Users/          # 用户管理模块
│   │   ├── Services/               # ✅ UserService业务服务
│   │   ├── Repositories/           # ✅ UserRepository数据访问
│   │   ├── Validators/             # ✅ FluentValidation验证器
│   │   ├── Mapping/                # ✅ AutoMapper映射配置
│   │   └── Interfaces/             # ✅ 模块接口定义
│   ├── LYBT.Module.Patients/       # 患者管理模块
│   │   ├── Services/               # ✅ PatientService
│   │   ├── Repositories/           # ✅ PatientRepository
│   │   ├── Validators/             # ✅ PatientValidator
│   │   ├── Mapping/                # ✅ PatientMappingProfile
│   │   └── Interfaces/             # ✅ IPatientService, IPatientRepository
│   ├── LYBT.Module.MedicalCase/    # 医案管理模块
│   ├── LYBT.Module.Consultation/   # 诊疗记录模块
│   ├── LYBT.Module.Prescriptions/  # 处方管理模块
│   ├── LYBT.Module.Herbs/          # 药材管理模块
│   └── LYBT.Module.Formula/        # 验方管理模块
│   ❌ 注意：Module中不包含Controllers！
│
└── Infrastructure/LYBT.Infrastructure/  # Infrastructure Layer
    ├── Data/                       # 数据访问
    │   ├── AppDbContext.cs
    │   ├── Configurations/
    │   └── Migrations/
    ├── Repositories/               # 仓储基类实现
    │   └── BaseRepository.cs
    ├── Configuration/              # 基础设施配置
    ├── Services/                   # 基础设施服务
    └── Extensions/                 # 扩展方法
```

**架构层次职责划分**：
1. **WebAPI项目（Presentation）**：
   - ✅ 包含所有Controllers（13个）
   - ✅ 处理HTTP请求/响应
   - ✅ 依赖注入Services

2. **Module项目（Application）**：
   - ✅ 包含Services（业务逻辑）
   - ✅ 包含Repositories（数据访问）
   - ✅ 包含Validators、Mapping、Interfaces
   - ❌ 不包含Controllers

3. **Infrastructure项目（Infrastructure）**：
   - ✅ DbContext和数据库配置
   - ✅ 基础仓储实现
   - ✅ 通用基础设施服务

## 📋 业务模块架构

### 8个核心业务模块

#### 1. 认证模块 (Auth Module)
**职责**：用户身份认证、授权管理、JWT令牌处理
- **服务层**：AuthService、TokenService、AuthorizationService
- **数据层**：UserRepository、AdminSecretRepository
- **核心实体**：User、AdminSecret、RefreshToken
- **关键特性**：双轨认证、令牌刷新、权限控制

#### 2. 用户管理模块 (Users Module)
**职责**：用户信息管理、角色权限分配、密码安全
- **服务层**：UserService、RoleService、PermissionService
- **数据层**：UserRepository、RoleRepository
- **核心实体**：User、Role、Permission、UserRole
- **关键特性**：RBAC权限模型、密码加密、用户状态管理

#### 3. 患者管理模块 (Patients Module)
**职责**：患者信息管理、Excel导入导出、查询统计
- **服务层**：PatientService、PatientImportService、PatientSearchService
- **数据层**：PatientRepository、PatientHistoryRepository
- **核心实体**：Patient、PatientContact、PatientHistory
- **关键特性**：批量导入、重复检查、数据统计

#### 4. 医案管理模块 (MedicalCase Module)

> **📚 权威参考**：详细实体关系定义参见 [clinical-workflow-entity-relationships.md](../shared/clinical-workflow-entity-relationships.md)（⭐⭐⭐权威文档）

**职责**：医案记录管理、状态流转、业务流程（**聚合根模式**：MedicalCase统一管理Consultation和Prescription生命周期）
- **服务层**：MedicalCaseService、CaseWorkflowService、CaseStatusService
- **数据层**：MedicalCaseRepository、MedicalCaseHistoryRepository
- **核心实体**：MedicalCase（聚合根）、Consultation、Prescription、MedicalCaseHistory、CaseStatus
- **关键特性**：状态机、工作流、审计跟踪、聚合根边界强制

**本模块重点**：从WebAPI和Service层视角实现聚合根模式，确保Consultation/Prescription只能通过MedicalCase进行创建/更新/删除操作。

#### 5. 诊疗记录模块 (Consultation Module)
**职责**：四诊信息记录、辨证论治、诊断结果
- **服务层**：ConsultationService、DiagnosisService、TreatmentService
- **数据层**：ConsultationRepository、DiagnosisRepository
- **核心实体**：Consultation、Diagnosis、Examination、Treatment
- **关键特性**：四诊合参、中医诊断、治法方案

#### 6. 处方管理模块 (Prescriptions Module)
**职责**：处方创建管理、药材配伍、价格计算、处方编号生成
- **服务层**：PrescriptionService、PrescriptionCalculationService、PrescriptionValidationService、**PrescriptionNumberService (Issue #1551)**
- **数据层**：PrescriptionRepository、PrescriptionItemRepository
- **核心实体**：Prescription、PrescriptionItem、PrescriptionStatus
- **关键特性**：四种录入方式、配伍检查、自动计价、**处方自动编号（RX-YYYYMMDD-NNNN）**

#### 7. 药材管理模块 (Herbs Module)
**职责**：药材信息管理、拼音检索、价格管理
- **服务层**：HerbService、HerbSearchService
- **数据层**：HerbRepository
- **核心实体**：Herb、HerbCategory
- **关键特性**：2000+药材、拼音码检索、价格管理

#### 8. 验方管理模块 (Formula Module)
**职责**：验方模板管理、智能推荐、统计分析
- **服务层**：FormulaService、FormulaRecommendationService、FormulaAnalysisService
- **数据层**：FormulaRepository、FormulaItemRepository
- **核心实体**：Formula、FormulaItem、FormulaCategory
- **关键特性**：模板管理、智能推荐、使用统计

## 🔧 服务层设计模式

### 标准服务模板
```csharp
/// <summary>
/// 标准业务服务基类
/// </summary>
public abstract class BaseService<T> : IBaseService<T> where T : class
{
    protected readonly IRepository<T> _repository;
    protected readonly ILogger _logger;
    protected readonly IMapper _mapper;

    protected BaseService(IRepository<T> repository, ILogger logger, IMapper mapper)
    {
        _repository = repository;
        _logger = logger;
        _mapper = mapper;
    }

    public virtual async Task<ServiceResult<T>> GetByIdAsync(Guid id)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return ServiceResult<T>.Failure("实体不存在");

            return ServiceResult<T>.Success(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取实体失败: {Id}", id);
            return ServiceResult<T>.Failure("获取实体失败");
        }
    }

    public virtual async Task<ServiceResult<IEnumerable<T>>> GetAllAsync()
    {
        try
        {
            var entities = await _repository.GetAllAsync();
            return ServiceResult<IEnumerable<T>>.Success(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取实体列表失败");
            return ServiceResult<IEnumerable<T>>.Failure("获取实体列表失败");
        }
    }

    public virtual async Task<ServiceResult<T>> CreateAsync(T entity)
    {
        try
        {
            // 业务验证
            var validationResult = await ValidateAsync(entity);
            if (!validationResult.IsValid)
                return ServiceResult<T>.Failure(validationResult.ErrorMessage);

            // 创建实体
            var createdEntity = await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("创建实体成功: {EntityType} - {Id}", 
                typeof(T).Name, createdEntity.Id);

            return ServiceResult<T>.Success(createdEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建实体失败: {EntityType}", typeof(T).Name);
            return ServiceResult<T>.Failure("创建实体失败");
        }
    }

    public virtual async Task<ServiceResult<T>> UpdateAsync(T entity)
    {
        try
        {
            // 业务验证
            var validationResult = await ValidateAsync(entity);
            if (!validationResult.IsValid)
                return ServiceResult<T>.Failure(validationResult.ErrorMessage);

            // 更新实体
            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("更新实体成功: {EntityType} - {Id}", 
                typeof(T).Name, entity.Id);

            return ServiceResult<T>.Success(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新实体失败: {EntityType} - {Id}", typeof(T).Name, entity.Id);
            return ServiceResult<T>.Failure("更新实体失败");
        }
    }

    public virtual async Task<ServiceResult> DeleteAsync(Guid id)
    {
        try
        {
            // 检查实体是否存在
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return ServiceResult.Failure("实体不存在");

            // 软删除标记
            if (entity is ISoftDeletable deletableEntity)
            {
                deletableEntity.IsDeleted = true;
                deletableEntity.DeletedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(entity);
            }
            else
            {
                await _repository.DeleteAsync(id);
            }

            await _repository.SaveChangesAsync();

            _logger.LogInformation("删除实体成功: {EntityType} - {Id}", typeof(T).Name, id);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除实体失败: {EntityType} - {Id}", typeof(T).Name, id);
            return ServiceResult.Failure("删除实体失败");
        }
    }

    protected virtual async Task<ValidationResult> ValidateAsync(T entity)
    {
        // 子类重写验证逻辑
        return ValidationResult.Success();
    }
}
```

### 仓储模式实现
```csharp
/// <summary>
/// 标准仓储基类
/// </summary>
public abstract class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    protected BaseRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public virtual async Task<PagedResult<T>> GetPagedAsync(int page, int pageSize, 
        Expression<Func<T, bool>>? predicate = null)
    {
        var query = _dbSet.AsQueryable();

        if (predicate != null)
            query = query.Where(predicate);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public virtual void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public virtual void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }

    public virtual async Task DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            Delete(entity);
        }
    }

    public virtual async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
```

## 🌐 API控制器设计

### 标准控制器模板
```csharp
/// <summary>
/// 标准API控制器基类
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public abstract class BaseController<T, TDto, TCreateDto, TUpdateDto> : ControllerBase
    where T : BaseEntity
    where TDto : BaseDto
    where TCreateDto : BaseCreateDto
    where TUpdateDto : BaseUpdateDto
{
    protected readonly IBaseService<T> _service;
    protected readonly ILogger<BaseController<T, TDto, TCreateDto, TUpdateDto>> _logger;
    protected readonly IMapper _mapper;

    protected BaseController(IBaseService<T> service, 
        ILogger<BaseController<T, TDto, TCreateDto, TUpdateDto>> logger,
        IMapper mapper)
    {
        _service = service;
        _logger = logger;
        _mapper = mapper;
    }

    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    /// <param name="id">实体ID</param>
    /// <returns>实体信息</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<TDto>>> GetByIdAsync(Guid id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            if (!result.IsSuccess)
                return NotFound(new ApiResponse<TDto>
                {
                    Success = false,
                    Message = result.Message,
                    Code = "NOT_FOUND"
                });

            var dto = _mapper.Map<TDto>(result.Data);
            return Ok(new ApiResponse<TDto>
            {
                Success = true,
                Data = dto,
                Message = "获取成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取实体失败: {Id}", id);
            return StatusCode(500, new ApiResponse<TDto>
            {
                Success = false,
                Message = "服务器内部错误",
                Code = "INTERNAL_ERROR"
            });
        }
    }

    /// <summary>
    /// 分页查询实体列表
    /// </summary>
    /// <param name="parameters">查询参数</param>
    /// <returns>分页结果</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<TDto>>> GetPagedAsync(
        [FromQuery] BaseQueryParameters parameters)
    {
        try
        {
            var result = await _service.GetPagedAsync(
                parameters.Page, 
                parameters.PageSize, 
                parameters.Keyword);

            var dtoItems = _mapper.Map<List<TDto>>(result.Data.Items);
            
            return Ok(new PagedResponse<TDto>
            {
                Success = true,
                Data = dtoItems,
                Page = result.Data.CurrentPage,
                PageSize = result.Data.PageSize,
                TotalCount = result.Data.TotalCount,
                TotalPages = result.Data.TotalPages,
                HasNextPage = result.Data.HasNextPage,
                HasPreviousPage = result.Data.HasPreviousPage,
                Message = "查询成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分页查询失败: {Parameters}", parameters);
            return StatusCode(500, new PagedResponse<TDto>
            {
                Success = false,
                Message = "服务器内部错误",
                Code = "INTERNAL_ERROR"
            });
        }
    }

    /// <summary>
    /// 创建实体
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <returns>创建结果</returns>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<TDto>>> CreateAsync([FromBody] TCreateDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            return BadRequest(new ApiResponse<TDto>
            {
                Success = false,
                Message = "请求数据无效",
                Code = "INVALID_REQUEST"
            });

            var entity = _mapper.Map<T>(request);
            var result = await _service.CreateAsync(entity);

            if (!result.IsSuccess)
                return BadRequest(new ApiResponse<TDto>
                {
                    Success = false,
                    Message = result.Message,
                    Code = "CREATE_FAILED"
                });

            var dto = _mapper.Map<TDto>(result.Data);
            return CreatedAtAction(nameof(GetByIdAsync), 
                new { id = dto.Id }, 
                new ApiResponse<TDto>
                {
                    Success = true,
                    Data = dto,
                    Message = "创建成功"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建实体失败: {Request}", request);
            return StatusCode(500, new ApiResponse<TDto>
            {
                Success = false,
                Message = "服务器内部错误",
                Code = "INTERNAL_ERROR"
            });
        }
    }

    /// <summary>
    /// 更新实体
    /// </summary>
    /// <param name="id">实体ID</param>
    /// <param name="request">更新请求</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<TDto>>> UpdateAsync(
        Guid id, [FromBody] TUpdateDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<TDto>
                {
                    Success = false,
                    Message = "请求数据无效",
                    Code = "INVALID_REQUEST"
                });

            var existingEntity = await _service.GetByIdAsync(id);
            if (!existingEntity.IsSuccess)
                return NotFound(new ApiResponse<TDto>
                {
                    Success = false,
                    Message = "实体不存在",
                    Code = "NOT_FOUND"
                });

            var entity = _mapper.Map<T>(request);
            entity.Id = id;
            
            var result = await _service.UpdateAsync(entity);
            if (!result.IsSuccess)
                return BadRequest(new ApiResponse<TDto>
                {
                    Success = false,
                    Message = result.Message,
                    Code = "UPDATE_FAILED"
                });

            var dto = _mapper.Map<TDto>(result.Data);
            return Ok(new ApiResponse<TDto>
            {
                Success = true,
                Data = dto,
                Message = "更新成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新实体失败: {Id} - {Request}", id, request);
            return StatusCode(500, new ApiResponse<TDto>
            {
                Success = false,
                Message = "服务器内部错误",
                Code = "INTERNAL_ERROR"
            });
        }
    }

    /// <summary>
    /// 删除实体
    /// </summary>
    /// <param name="id">实体ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(Guid id)
    {
        try
        {
            var result = await _service.DeleteAsync(id);
            if (!result.IsSuccess)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = result.Message,
                    Code = "NOT_FOUND"
                });

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "删除成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除实体失败: {Id}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "服务器内部错误",
                Code = "INTERNAL_ERROR"
            });
        }
    }
}
```

## 🔗 数据访问层设计

### 实体框架配置
```csharp
/// <summary>
/// 应用数据库上下文
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets for entities
    public DbSet<User> Users { get; set; }
    public DbSet<AdminSecret> AdminSecrets { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<MedicalCase> MedicalCases { get; set; }
    public DbSet<Consultation> Consultations { get; set; }
    public DbSet<Prescription> Prescriptions { get; set; }
    public DbSet<PrescriptionItem> PrescriptionItems { get; set; }
    public DbSet<Herb> Herbs { get; set; }
    public DbSet<Formula> Formulas { get; set; }
    public DbSet<FormulaItem> FormulaItems { get; set; }
    public DbSet<HerbInventory> HerbInventory { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure entity relationships
        ConfigureEntities(modelBuilder);
        
        // Configure indexes
        ConfigureIndexes(modelBuilder);
        
        // Configure query filters
        ConfigureQueryFilters(modelBuilder);
    }

    private void ConfigureEntities(ModelBuilder modelBuilder)
    {
        // Patient configuration
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.Property(p => p.PhoneNumber).HasMaxLength(20);
            entity.Property(p => p.CreatedDate).HasDefaultValueSql("GETUTDATE()");
            entity.Property(p => p.RowVersion).IsRowVersion();
            
            // Indexes
            entity.HasIndex(p => p.PhoneNumber);
            entity.HasIndex(p => p.IdentificationNumber).IsUnique();
            entity.HasIndex(p => new { p.Name, p.DateOfBirth });
            entity.HasIndex(p => p.Status, p.CreatedDate);
        });

        // MedicalCase configuration
        modelBuilder.Entity<MedicalCase>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.VisitDate).IsRequired();
            entity.Property(m => m.CreatedDate).HasDefaultValueSql("GETUTDATE()");
            entity.Property(m => m.RowVersion).IsRowVersion();
            
            // Relationships
            entity.HasOne(m => m.Patient)
                  .WithMany(p => p.MedicalCases)
                  .HasForeignKey(m => m.PatientId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Doctor)
                  .WithMany(d => d.MedicalCases)
                  .HasForeignKey(m => m.DoctorId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(m => m.PatientId, m.VisitDate);
            entity.HasIndex(m => m.DoctorId, m.VisitDate);
            entity.HasIndex(m => m.Status, m.CreatedDate);
        });

        // Prescription configuration
        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.PrescriptionDate).IsRequired();
            entity.Property(p => p.TotalAmount).HasColumnType("decimal(10,2)");
            entity.Property(p => p.CreatedDate).HasDefaultValueSql("GETUTDATE()");
            entity.Property(p => p.RowVersion).IsRowVersion();
            
            // Relationships
            entity.HasOne(p => p.MedicalCase)
                  .WithMany(m => m.Prescriptions)
                  .HasForeignKey(p => p.MedicalCaseId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Doctor)
                  .WithMany(d => d.Prescriptions)
                  .HasForeignKey(p => p.DoctorId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(p => p.MedicalCaseId, p.PrescriptionDate);
            entity.HasIndex(p => p.DoctorId, p.PrescriptionDate);
            entity.HasIndex(p => p.Status, p.CreatedDate);
        });

        // Other entities...
    }

    private void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Performance indexes
        modelBuilder.Entity<PrescriptionItem>(entity =>
        {
            entity.HasIndex(pi => pi.PrescriptionId);
            entity.HasIndex(pi => pi.HerbId);
        });

        modelBuilder.Entity<HerbInventory>(entity =>
        {
            entity.HasIndex(hi => hi.HerbId);
            entity.HasIndex(hi => hi.TransactionDate);
        });
    }

    private void ConfigureQueryFilters(ModelBuilder modelBuilder)
    {
        // Soft delete filters
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasQueryFilter(p => p.Status != "Deleted");
        });

        modelBuilder.Entity<MedicalCase>(entity =>
        {
            entity.HasQueryFilter(m => m.Status != "Deleted");
        });

        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasQueryFilter(p => p.Status != "Deleted");
        });

        modelBuilder.Entity<Herb>(entity =>
        {
            entity.HasQueryFilter(h => h.IsActive);
        });
    }
}
```

## 🔧 依赖注入配置

### 服务注册配置
```csharp
/// <summary>
/// 依赖注入扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IMedicalCaseRepository, MedicalCaseRepository>();
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        services.AddScoped<IHerbRepository, HerbRepository>();
        services.AddScoped<IFormulaRepository, FormulaRepository>();
        services.AddScoped<IAdminSecretRepository, AdminSecretRepository>();

        // Register services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IMedicalCaseService, MedicalCaseService>();
        services.AddScoped<IConsultationService, ConsultationService>();
        services.AddScoped<IPrescriptionService, PrescriptionService>();
        services.AddScoped<IHerbService, HerbService>();
        servicesocache<IFontulaService, FormulaService>();

        // Register cross-module services
        services.AddScoped<IModuleCommunicationService, ModuleCommunicationService>();
        services.AddScoped<ICrossModuleDataService, CrossModuleDataService>();

        // Register validation services
        services.AddScoped<IValidator<PatientCreateDto>, PatientCreateValidator>();
        services.AddScoped<IValidator<PatientUpdateDto>, PatientUpdateValidator>();
        services.AddScoped<IValidator<PrescriptionCreateDto>, PrescriptionCreateValidator>();

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database context
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(false);
        });

        // Health checks
        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>()
            .AddSqlServer(configuration.GetConnectionString("DefaultConnection"));

        return services;
    }

    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        // Register controllers
        services.AddControllers();

        // API documentation
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "凌隐宝堂中医诊所管理系统 API",
                Version = "v1",
                Description = "凌隐宝堂中医诊所管理系统Web API接口文档"
            });
        });

        return services;
    }
}
```

## 🎯 架构质量保证

### 代码质量标准
- ✅ **SOLID原则**：单一职责、开闭原则、里氏替换、接口隔离、依赖倒置
- ✅ **DDD领域驱动**：领域模型、领域服务、聚合根、值对象
- ✅ **Clean Code**：可读性、可维护性、命名规范、注释完整
- ✅ **Error Handling**：异常处理、错误恢复、日志记录

### 架构验证
- ✅ **分层验证**：确保代码严格遵循分层架构
- ✅ **依赖验证**：检查依赖方向和循环依赖
- ✅ **接口验证**：接口定义和实现一致性
- ✅ **性能验证**：查询性能、内存使用、并发处理

### 文档同步
- ✅ **实时同步**：代码变更后立即更新文档
- ✅ **准确一致**：文档内容与实际代码完全匹配
- ✅ **版本管理**：文档版本与代码版本对应
- ✅ **用户反馈**：收集使用反馈并持续改进

## 🔗 相关资源

### 📚 深度参考
- [深度参考文档](../../deep/README.md) - 完整技术细节
- [API设计最佳实践](../../deep/api-design-best-practices.md) - API架构规范
- [性能优化指南](../../deep/performance-optimization.md) - 性能架构优化

### 🛠️ 开发指南
- [开发指南总览](../../development/README.md) - 开发规范和流程
- [Server端开发](../../development/server/README.md) - Server开发规范
- [测试策略指南](../../deep/testing-strategies.md) - 架构测试策略

### 📊 监控和维护
- [文档使用指标](../../support/documentation-metrics.md) - 文档质量监控
- [文档维护指南](../../support/documentation-maintenance.md) - 文档维护流程

---

**Server端架构指南** - 为凌隐宝堂中医诊所提供稳定、可扩展、高性能的服务器端架构设计 🏗️

*本架构指南基于实际代码架构编写，确保架构设计与实现完全一致。如有架构问题或建议，请通过相应渠道反馈。*