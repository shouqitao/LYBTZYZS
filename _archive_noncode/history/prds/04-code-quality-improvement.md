# 代码质量提升需求 (PRD-004)

## 📋 需求概述

| 字段 | 内容 |
|------|------|
| 需求编号 | PRD-004 |
| 需求名称 | 代码质量提升 - 重复代码消除与架构规范化 |
| 优先级 | P3 (一般) |
| 预估工期 | 15工作日 |
| 风险等级 | 🟡 → 🟢 (中风险降级) |
| 负责模块 | 所有业务模块 + 共享基础设施 |

## 🎯 需求背景

根据架构分析报告，系统存在**代码重复和质量问题**：
- 相似的验证逻辑在多个Service中重复出现
- 没有统一的验证框架，维护成本高
- 某些类职责过重(>300行)，违反单一职责原则
- 缺乏统一的异常处理和日志记录规范
- 业务规则分散在不同层级，缺乏集中管理

**问题影响**:
- 代码维护成本高，修改一处需要改多个地方
- 验证逻辑不一致，可能导致数据质量问题
- 新功能开发效率低，重复造轮子
- 代码可读性和可测试性有待提升

## 🎯 需求目标

### 主要目标
1. **消除重复代码，建立统一的基础设施**
2. **实施标准化的验证和异常处理机制**
3. **重构过大的类，遵循SOLID原则**
4. **建立代码质量检查和持续改进机制**

### 成功指标
- ✅ 代码重复率 < 3% (当前~15%)
- ✅ 平均类行数 < 200行 (当前~280行)
- ✅ 代码覆盖率 > 80% (当前~60%)
- ✅ 代码质量评分 > 85分 (当前~70分)

## 📊 代码质量现状分析

### 重复代码分析

#### 问题1: 验证逻辑重复
**重复模式识别**:
```csharp
// PatientBusinessService.cs
private bool ValidatePhone(string phone)
{
    if (string.IsNullOrEmpty(phone)) return false;
    return Regex.IsMatch(phone, @"^1[3-9]\d{9}$");
}

// UserBusinessService.cs  
private bool IsValidPhone(string phone)
{
    if (string.IsNullOrWhiteSpace(phone)) return false;
    return Regex.IsMatch(phone, @"^1[3-9]\d{9}$");
}

// ConsultationBusinessService.cs
private bool CheckPhoneFormat(string phone)
{
    return !string.IsNullOrEmpty(phone) && 
           Regex.IsMatch(phone, @"^1[3-9]\d{9}$");
}
```

**问题分析**:
- 同样的手机号验证逻辑重复3次
- 方法名不统一，维护时容易遗漏
- 正则表达式硬编码，修改规则需要改多处

#### 问题2: 分页查询重复
**重复模式**:
```csharp
// 在多个QueryService中重复出现的分页逻辑
public async Task<PagedResult<TDto>> GetPagedData<TEntity, TDto>(
    IQueryable<TEntity> query, 
    int page, 
    int pageSize)
{
    var totalCount = await query.CountAsync();
    var data = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    
    return new PagedResult<TDto>
    {
        Data = _mapper.Map<List<TDto>>(data),
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };
}
```

**问题分析**:
- 分页逻辑在8个QueryService中重复
- 总数统计和数据查询分离，性能不优
- 缺乏统一的分页参数验证

#### 问题3: 异常处理重复
**重复模式**:
```csharp
// 在多个Controller中重复的异常处理
public async Task<ActionResult<ApiResponse<T>>> SomeAction()
{
    try
    {
        // 业务逻辑
        var result = await _service.DoSomething();
        if (result.Success)
        {
            return Ok(ApiResponse<T>.Success(result.Data, "操作成功"));
        }
        return BadRequest(ApiResponse<T>.Failure(result.ErrorMessage));
    }
    catch (ValidationException ex)
    {
        _logger.LogWarning($"验证异常: {ex.Message}");
        return BadRequest(ApiResponse<T>.Failure(ex.Message));
    }
    catch (NotFoundException ex)
    {
        _logger.LogWarning($"资源未找到: {ex.Message}");
        return NotFound(ApiResponse<T>.Failure(ex.Message));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "未处理异常");
        return StatusCode(500, ApiResponse<T>.Failure("系统异常，请联系管理员"));
    }
}
```

### 类结构过大问题

#### 问题类识别
**过大的ViewModel类**:
```csharp
// PatientManagementViewModel.cs - 当前420行
public class PatientManagementViewModel : BaseViewModel
{
    // 患者列表管理 (100行)
    public ObservableCollection<PatientDto> Patients { get; set; }
    private async Task LoadPatientsAsync() { /* 40行方法 */ }
    
    // 患者搜索功能 (80行)  
    public string SearchName { get; set; }
    private async Task SearchPatientsAsync() { /* 35行方法 */ }
    
    // 患者CRUD操作 (120行)
    private async Task CreatePatientAsync() { /* 45行方法 */ }
    private async Task UpdatePatientAsync() { /* 40行方法 */ }
    
    // 导入导出功能 (90行)
    private async Task ExportPatientsAsync() { /* 50行方法 */ }
    
    // 其他辅助方法 (30行)
}
```

**问题分析**:
- 单个ViewModel承担过多职责
- 方法过长，逻辑复杂度高
- 难以进行单元测试
- 违反单一职责原则

## 🔧 解决方案设计

### 1. 统一验证框架

#### 基础验证服务设计
```csharp
public interface IValidationService
{
    ValidationResult ValidatePhone(string phone);
    ValidationResult ValidateIdCard(string idCard);  
    ValidationResult ValidateEmail(string email);
    ValidationResult ValidateAge(int age);
    ValidationResult ValidateEntity<T>(T entity) where T : class;
}

public class ValidationService : IValidationService
{
    private readonly Dictionary<string, Regex> _regexCache = new();
    private readonly IConfiguration _configuration;
    
    public ValidationResult ValidatePhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return ValidationResult.Failure("手机号不能为空");
            
        var phonePattern = GetRegexPattern("PhonePattern");
        if (!phonePattern.IsMatch(phone))
            return ValidationResult.Failure("手机号格式不正确");
            
        return ValidationResult.Success();
    }
    
    private Regex GetRegexPattern(string key)
    {
        if (!_regexCache.TryGetValue(key, out var regex))
        {
            var pattern = _configuration[$"ValidationPatterns:{key}"];
            regex = new Regex(pattern, RegexOptions.Compiled);
            _regexCache[key] = regex;
        }
        return regex;
    }
}

// 配置文件中的验证规则
"ValidationPatterns": {
    "PhonePattern": "^1[3-9]\\d{9}$",
    "IdCardPattern": "^[1-9]\\d{5}(19|20)\\d{2}((0[1-9])|(1[0-2]))(([0-2][1-9])|10|20|30|31)\\d{3}[0-9Xx]$",
    "EmailPattern": "^[\\w-]+(\\.[\\w-]+)*@[\\w-]+(\\.[\\w-]+)+$"
}
```

#### FluentValidation集成
```csharp
// 患者创建验证器
public class PatientCreateDtoValidator : AbstractValidator<PatientCreateDto>
{
    private readonly IValidationService _validationService;
    
    public PatientCreateDtoValidator(IValidationService validationService)
    {
        _validationService = validationService;
        
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("姓名不能为空")
            .Length(2, 50).WithMessage("姓名长度必须在2-50字符之间");
            
        RuleFor(x => x.Phone)
            .Must(phone => _validationService.ValidatePhone(phone).IsValid)
            .WithMessage("手机号格式不正确");
            
        RuleFor(x => x.IdCard)
            .Must(idCard => _validationService.ValidateIdCard(idCard).IsValid)
            .When(x => !string.IsNullOrEmpty(x.IdCard))
            .WithMessage("身份证号格式不正确");
            
        RuleFor(x => x.Age)
            .InclusiveBetween(0, 120).WithMessage("年龄必须在0-120之间");
    }
}

// 全局验证中间件
public class ValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method == "POST" || context.Request.Method == "PUT")
        {
            // 自动验证请求体
            await ValidateRequestAsync(context);
        }
        
        await _next(context);
    }
    
    private async Task ValidateRequestAsync(HttpContext context)
    {
        // 根据Content-Type和路由自动选择验证器
        // 验证失败时直接返回400错误
    }
}
```

### 2. 统一异常处理框架

#### 全局异常处理中间件
```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var apiResponse = exception switch
        {
            ValidationException validationEx => new ApiResponse<object>
            {
                Success = false,
                Message = validationEx.Message,
                ErrorCode = "VALIDATION_FAILED",
                Data = validationEx.Errors
            },
            
            NotFoundException notFoundEx => new ApiResponse<object>
            {
                Success = false,
                Message = notFoundEx.Message,
                ErrorCode = "RESOURCE_NOT_FOUND"
            },
            
            UnauthorizedException unauthorizedEx => new ApiResponse<object>
            {
                Success = false,
                Message = "访问被拒绝",
                ErrorCode = "UNAUTHORIZED"
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
                ErrorCode = "INTERNAL_ERROR"
            }
        };

        response.StatusCode = GetStatusCode(exception);

        // 记录异常日志
        LogException(exception, context);

        var jsonResponse = JsonSerializer.Serialize(apiResponse);
        await response.WriteAsync(jsonResponse);
    }
}

// 自定义业务异常类
public class BusinessException : Exception
{
    public string ErrorCode { get; }
    
    public BusinessException(string message, string errorCode = "BUSINESS_ERROR") 
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
```

### 3. ViewModel重构策略

#### 职责分离设计
```csharp
// 重构后的患者管理 - 分离为多个专门的ViewModel

// 1. 患者列表管理
public class PatientListViewModel : BaseCollectionViewModel<PatientDto>
{
    private readonly IPatientService _patientService;
    
    public AsyncRelayCommand LoadPatientsCommand { get; }
    public AsyncRelayCommand RefreshCommand { get; }
    
    protected override async Task LoadDataAsync()
    {
        await LoadDataAsync(new PatientSearchDto(), 
            criteria => _patientService.SearchPatientsAsync(criteria));
    }
}

// 2. 患者搜索功能  
public class PatientSearchViewModel : BaseViewModel
{
    private readonly IPatientService _patientService;
    
    public string SearchName { get; set; }
    public string SearchPhone { get; set; }
    public int? AgeMin { get; set; }
    public int? AgeMax { get; set; }
    
    public AsyncRelayCommand<PatientSearchDto> SearchCommand { get; }
    
    public event EventHandler<PatientSearchResultEventArgs> SearchCompleted;
    
    private async Task ExecuteSearchAsync()
    {
        var criteria = new PatientSearchDto
        {
            Name = SearchName,
            Phone = SearchPhone,
            AgeMin = AgeMin,
            AgeMax = AgeMax
        };
        
        var result = await _patientService.SearchPatientsAsync(criteria);
        SearchCompleted?.Invoke(this, new PatientSearchResultEventArgs(result));
    }
}

// 3. 患者编辑功能
public class PatientEditViewModel : BaseEditViewModel<PatientDto, PatientUpdateDto>
{
    private readonly IPatientService _patientService;
    private readonly IValidationService _validationService;
    
    protected override async Task<ServiceResult<PatientDto>> SaveAsync(PatientUpdateDto dto)
    {
        return await _patientService.UpdatePatientAsync(CurrentItem.Id, dto);
    }
    
    protected override ValidationResult ValidateItem(PatientUpdateDto dto)
    {
        var phoneValidation = _validationService.ValidatePhone(dto.Phone);
        if (!phoneValidation.IsValid)
            return phoneValidation;
            
        // 其他验证...
        return ValidationResult.Success();
    }
}

// 4. 主容器ViewModel - 协调各个子ViewModel
public class PatientManagementViewModel : BaseViewModel
{
    public PatientListViewModel PatientList { get; }
    public PatientSearchViewModel PatientSearch { get; }  
    public PatientEditViewModel PatientEdit { get; }
    public PatientImportExportViewModel ImportExport { get; }
    
    public PatientManagementViewModel(
        PatientListViewModel patientList,
        PatientSearchViewModel patientSearch,
        PatientEditViewModel patientEdit,
        PatientImportExportViewModel importExport)
    {
        PatientList = patientList;
        PatientSearch = patientSearch;
        PatientEdit = patientEdit;
        ImportExport = importExport;
        
        // 订阅子ViewModel事件
        PatientSearch.SearchCompleted += OnSearchCompleted;
        PatientEdit.ItemSaved += OnPatientSaved;
    }
    
    private void OnSearchCompleted(object sender, PatientSearchResultEventArgs e)
    {
        PatientList.UpdateItems(e.SearchResult.Data);
    }
    
    private void OnPatientSaved(object sender, ItemSavedEventArgs<PatientDto> e)
    {
        PatientList.RefreshItem(e.SavedItem);
    }
}
```

### 4. 通用基础设施

#### 通用分页查询基类
```csharp
public abstract class BaseQueryService<TEntity, TDto> : IBaseQueryService<TEntity, TDto>
    where TEntity : class, IBaseEntity
    where TDto : class
{
    protected readonly AppDbContext _context;
    protected readonly IMapper _mapper;
    protected readonly ICacheService _cache;
    protected readonly ILogger _logger;

    protected abstract IQueryable<TEntity> ApplyIncludes(IQueryable<TEntity> query);
    
    public async Task<PagedResult<TDto>> GetPagedAsync<TSearch>(
        TSearch criteria,
        Expression<Func<TEntity, bool>> filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null)
        where TSearch : BaseSearchCriteria
    {
        var cacheKey = GenerateCacheKey(criteria, filter);
        
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            using var activity = ActivitySource.StartActivity("QueryExecution");
            activity?.SetTag("entity", typeof(TEntity).Name);
            
            var query = _context.Set<TEntity>().AsNoTracking();
            query = ApplyIncludes(query);
            
            if (filter != null)
                query = query.Where(filter);
            
            if (orderBy != null)
                query = orderBy(query);
            else
                query = query.OrderByDescending(e => e.CreateTime);
            
            var totalCount = criteria.NeedTotalCount 
                ? await query.CountAsync() 
                : 0;

            var data = await query
                .Skip((criteria.Page - 1) * criteria.PageSize)
                .Take(criteria.PageSize)
                .ToListAsync();

            return new PagedResult<TDto>
            {
                Data = _mapper.Map<List<TDto>>(data),
                TotalCount = totalCount,
                Page = criteria.Page,
                PageSize = criteria.PageSize
            };
        }, TimeSpan.FromMinutes(5));
    }
}
```

#### 通用CRUD业务服务基类
```csharp
public abstract class BaseCrudService<TEntity, TDto, TCreateDto, TUpdateDto> 
    : IBaseCrudService<TDto, TCreateDto, TUpdateDto>
    where TEntity : class, IBaseEntity
    where TDto : class
    where TCreateDto : class  
    where TUpdateDto : class
{
    protected readonly IRepository<TEntity> _repository;
    protected readonly IMapper _mapper;
    protected readonly IValidator<TCreateDto> _createValidator;
    protected readonly IValidator<TUpdateDto> _updateValidator;
    protected readonly ILogger _logger;

    public virtual async Task<ServiceResult<TDto>> CreateAsync(TCreateDto createDto)
    {
        // 验证
        var validationResult = await _createValidator.ValidateAsync(createDto);
        if (!validationResult.IsValid)
            return ServiceResult<TDto>.ValidationFailure(validationResult.Errors);

        try
        {
            // 映射和创建
            var entity = _mapper.Map<TEntity>(createDto);
            await _repository.CreateAsync(entity);

            var dto = _mapper.Map<TDto>(entity);
            
            _logger.LogInformation($"{typeof(TEntity).Name} 创建成功: {entity.Id}");
            return ServiceResult<TDto>.Success(dto, "创建成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{typeof(TEntity).Name} 创建失败");
            return ServiceResult<TDto>.Failure("创建失败，请重试");
        }
    }

    public virtual async Task<ServiceResult<TDto>> UpdateAsync(Guid id, TUpdateDto updateDto)
    {
        // 验证
        var validationResult = await _updateValidator.ValidateAsync(updateDto);
        if (!validationResult.IsValid)
            return ServiceResult<TDto>.ValidationFailure(validationResult.Errors);

        try
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return ServiceResult<TDto>.NotFound("记录不存在");

            // 映射更新
            _mapper.Map(updateDto, entity);
            await _repository.UpdateAsync(entity);

            var dto = _mapper.Map<TDto>(entity);
            
            _logger.LogInformation($"{typeof(TEntity).Name} 更新成功: {id}");
            return ServiceResult<TDto>.Success(dto, "更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{typeof(TEntity).Name} 更新失败: {id}");
            return ServiceResult<TDto>.Failure("更新失败，请重试");
        }
    }
}
```

## 📝 详细需求规格

### 功能需求

#### FR-001: 统一验证框架
- **验证服务**: 提供常用数据验证方法(手机号、身份证、邮箱等)
- **FluentValidation集成**: DTO验证器自动化
- **验证中间件**: 全局请求验证
- **配置化验证规则**: 验证规则可配置化管理

#### FR-002: 全局异常处理
- **异常中间件**: 全局异常捕获和处理
- **自定义异常类**: 业务异常类型定义
- **异常日志**: 结构化异常日志记录
- **友好错误响应**: 统一的API错误响应格式

#### FR-003: ViewModel重构
- **职责分离**: 大型ViewModel拆分为专门职责的小ViewModel
- **基类抽象**: 通用ViewModel基类提供公共功能
- **事件协调**: ViewModel间通过事件进行协调
- **依赖注入**: 支持ViewModel的依赖注入

#### FR-004: 通用基础设施
- **查询基类**: 通用分页查询服务基类
- **CRUD基类**: 通用增删改查业务服务基类
- **扩展方法**: 常用功能的扩展方法
- **工具类库**: 统一的工具类和帮助方法

### 非功能需求

#### NFR-001: 代码质量指标
- **代码重复率**: < 3%
- **平均类行数**: < 200行
- **平均方法行数**: < 30行
- **圈复杂度**: < 10

#### NFR-002: 可维护性要求
- **单一职责**: 每个类只负责一个职责
- **开闭原则**: 对扩展开放，对修改封闭
- **依赖倒置**: 依赖抽象而非具体实现
- **接口隔离**: 接口功能单一，职责明确

#### NFR-003: 测试覆盖率要求
- **单元测试覆盖率**: > 80%
- **业务逻辑测试**: 100% (关键业务逻辑)
- **验证逻辑测试**: 100% (验证规则)
- **异常处理测试**: 覆盖所有异常场景

## 🔧 技术实现

### 开发任务分解

#### 任务1: 验证框架实现 (5天)
- [ ] 实现ValidationService统一验证逻辑
- [ ] 集成FluentValidation框架
- [ ] 创建DTO验证器
- [ ] 实现验证中间件

**交付物**:
- `ValidationService.cs` - 统一验证服务
- `ValidationMiddleware.cs` - 全局验证中间件
- 各模块DTO验证器类
- 验证规则配置文件

#### 任务2: 异常处理框架 (3天)
- [ ] 实现全局异常处理中间件
- [ ] 创建自定义异常类
- [ ] 优化异常日志记录
- [ ] 统一API错误响应格式

**交付物**:
- `GlobalExceptionMiddleware.cs` - 全局异常处理
- `BusinessException.cs` 等自定义异常类
- `ApiResponse.cs` 统一响应格式
- 异常处理配置

#### 任务3: ViewModel重构 (5天)
- [ ] 分析现有大型ViewModel
- [ ] 设计职责分离方案
- [ ] 重构患者管理等关键ViewModel
- [ ] 创建通用ViewModel基类

**交付物**:
- 重构后的各业务模块ViewModel
- `BaseViewModel.cs` 等基类
- `BaseCollectionViewModel.cs` 集合管理基类
- 事件协调机制实现

#### 任务4: 通用基础设施 (2天)
- [ ] 实现通用查询服务基类
- [ ] 实现通用CRUD服务基类
- [ ] 创建扩展方法库
- [ ] 整理工具类库

**交付物**:
- `BaseQueryService.cs` - 通用查询基类
- `BaseCrudService.cs` - 通用CRUD基类
- `Extensions/` 目录下的扩展方法
- `Utils/` 目录下的工具类

### 重构实施策略

#### 1. 渐进式重构
```csharp
// 重构策略：保持向后兼容，逐步迁移

// 第一步：引入新的验证服务，但保留原有验证逻辑
public class PatientBusinessService
{
    private readonly IValidationService _validationService; // 新增
    
    public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
    {
        // 新验证逻辑
        var validationResult = _validationService.ValidateEntity(dto);
        if (!validationResult.IsValid)
            return ServiceResult<PatientDto>.ValidationFailure(validationResult.Errors);
        
        // 保留原有业务逻辑不变
        // ...
    }
    
    // 标记为过时，但保留兼容性
    [Obsolete("请使用 IValidationService.ValidatePhone")]
    private bool ValidatePhone(string phone) => _validationService.ValidatePhone(phone).IsValid;
}

// 第二步：逐步移除过时方法，完全迁移到新框架
```

#### 2. A/B测试支持
```csharp
// 功能开关支持新旧实现共存
public class PatientManagementViewModel
{
    private readonly IFeatureToggle _featureToggle;
    
    private async Task LoadPatientsAsync()
    {
        if (_featureToggle.IsEnabled("UseOptimizedPatientLoading"))
        {
            // 新的优化实现
            await LoadPatientsOptimizedAsync();
        }
        else
        {
            // 原有实现，保证稳定性
            await LoadPatientsLegacyAsync();
        }
    }
}
```

### 代码质量检查工具集成

#### SonarQube规则配置
```xml
<!-- sonar-project.properties -->
sonar.projectKey=LYBT_System
sonar.projectName=LYBT中医诊所系统
sonar.projectVersion=1.0

# 代码质量门禁
sonar.qualitygate.wait=true

# 覆盖率要求
sonar.coverage.minimum=80
sonar.duplicated_lines_density.maximum=3

# 复杂度要求  
sonar.complexity.maximum=10
sonar.file.lines.maximum=500
```

#### EditorConfig统一代码风格
```ini
# .editorconfig
root = true

[*.cs]
indent_style = space
indent_size = 4
end_of_line = crlf
trim_trailing_whitespace = true
insert_final_newline = true

# 命名规范
dotnet_naming_rule.private_fields_start_with_underscore.severity = error
dotnet_naming_rule.private_fields_start_with_underscore.symbols = private_fields
dotnet_naming_rule.private_fields_start_with_underscore.style = underscore_prefix

# 代码质量规则
dotnet_analyzer_diagnostic.CA1822.severity = warning  # 方法可以标记为static
dotnet_analyzer_diagnostic.IDE0290.severity = suggestion # 使用主构造函数
```

## 🧪 测试策略

### 代码质量测试

#### 静态代码分析测试
```csharp
[Test]
public void CodeQuality_ShouldMeetStandards()
{
    // 检查代码重复率
    var duplicateAnalysis = CodeAnalyzer.AnalyzeDuplicates("src/");
    duplicateAnalysis.DuplicationRate.Should().BeLessThan(0.03);
    
    // 检查方法复杂度
    var complexityAnalysis = CodeAnalyzer.AnalyzeComplexity("src/");
    complexityAnalysis.AverageComplexity.Should().BeLessThan(10);
    
    // 检查类大小
    var sizeAnalysis = CodeAnalyzer.AnalyzeSize("src/");
    sizeAnalysis.AverageLinesPerClass.Should().BeLessThan(200);
}
```

#### 验证框架测试
```csharp
[TestFixture]
public class ValidationServiceTests
{
    private ValidationService _validationService;
    
    [Test]
    public void ValidatePhone_WithValidPhone_ShouldReturnSuccess()
    {
        var result = _validationService.ValidatePhone("13812345678");
        result.IsValid.Should().BeTrue();
    }
    
    [Test]
    public void ValidatePhone_WithInvalidPhone_ShouldReturnFailure()
    {
        var result = _validationService.ValidatePhone("12345");
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("格式不正确");
    }
}
```

### 重构安全性测试

#### 功能回归测试
```csharp
[Test]
public async Task PatientCrud_AfterRefactor_ShouldMaintainFunctionality()
{
    // 测试重构后的功能与原有功能一致
    var originalResult = await _originalPatientService.CreatePatientAsync(testDto);
    var refactoredResult = await _refactoredPatientService.CreatePatientAsync(testDto);
    
    refactoredResult.Success.Should().Be(originalResult.Success);
    refactoredResult.Data.Should().BeEquivalentTo(originalResult.Data);
}
```

## 📊 验收标准

### 代码质量验收
- [ ] **重复代码消除**: SonarQube检测重复率 < 3%
- [ ] **类大小控制**: 90%的类行数 < 200行
- [ ] **方法复杂度**: 95%的方法圈复杂度 < 10
- [ ] **测试覆盖率**: 整体覆盖率 > 80%，关键模块 > 90%

### 功能兼容性验收
- [ ] **API兼容性**: 所有现有API保持兼容
- [ ] **业务逻辑一致**: 重构后业务行为保持不变
- [ ] **性能无衰减**: 关键操作性能不低于重构前
- [ ] **错误处理改善**: 错误提示更友好，异常处理更完善

### 可维护性验收
- [ ] **新功能开发**: 基于新框架开发功能效率提升30%
- [ ] **Bug修复效率**: 验证类Bug修复时间减少50%
- [ ] **代码审查**: Code Review通过率 > 95%
- [ ] **文档完整**: 新框架使用文档和示例完整

## 🚀 部署和监控

### 部署策略
1. **Phase 1**: 基础框架部署(验证、异常处理)，不影响现有功能
2. **Phase 2**: 逐步迁移业务模块到新框架
3. **Phase 3**: ViewModel重构，支持A/B测试
4. **Phase 4**: 清理过时代码，完全切换到新实现

### 质量监控
- **代码质量指标**: 持续集成中集成SonarQube分析
- **重复代码监控**: 定期检查代码重复率变化
- **测试覆盖率**: 覆盖率趋势监控和告警
- **技术债务**: 定期评估和偿还技术债务

### 持续改进机制
- **定期Code Review**: 每月进行代码质量专项Review
- **重构规划**: 季度制定代码质量改进计划
- **最佳实践分享**: 团队内分享重构经验和最佳实践
- **工具升级**: 及时更新代码分析工具和规则配置

---

## 📞 项目信息

**需求负责人**: Senior .NET Architecture Analyst  
**开发预估**: 15工作日  
**测试预估**: 3工作日  
**发布时间**: Phase 2 实施期  
**风险等级**: 🟡 → 🟢 (代码质量显著提升，技术债务降低)

**依赖项目**: 建议在PRD-001(事务基础设施)完成后实施，可利用统一的基础设施