# Design: refactor-service-layer

## Architecture Overview

### 目标架构

```
┌─────────────────────────────────────────────────────────────────┐
│                      Controller Layer                            │
│  - 调用Service方法                                               │
│  - 处理Result<T>返回值                                           │
│  - 转换为HTTP响应                                                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Service Layer                               │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                   BaseService<TEntity>                      ││
│  │  - ExecuteAsync<T>(): 统一错误处理包装                      ││
│  │  - ValidateAsync<TDto>(): 统一验证调用                      ││
│  │  - LogError(): 标准化日志                                   ││
│  └─────────────────────────────────────────────────────────────┘│
│       ▲           ▲           ▲           ▲                     │
│       │           │           │           │                     │
│  ┌────┴────┐ ┌────┴────┐ ┌────┴────┐ ┌────┴────┐               │
│  │Patient  │ │User     │ │Herb     │ │Medical  │  ...          │
│  │Service  │ │Service  │ │Service  │ │Case     │               │
│  │         │ │         │ │         │ │Service  │               │
│  └─────────┘ └─────────┘ └─────────┘ └─────────┘               │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Repository Layer                              │
│  BaseRepository<T> / BaseReadRepository<T>                       │
└─────────────────────────────────────────────────────────────────┘
```

## Phase 1: 统一返回值类型

### 决策：选择Result<T>而非ServiceResult<T>

| 特性 | Result<T> | ServiceResult<T> |
|------|-----------|------------------|
| 多错误支持 | Errors列表 | 仅ErrorMessage |
| 异常存储 | 无 | Exception属性 |
| 使用范围 | Herbs/Patients/Users | Auth |
| API简洁性 | 更简洁 | 略冗余 |

**结论**: 选择`Result<T>`，理由：
1. 多错误支持更适合FluentValidation场景
2. 不暴露Exception到API响应中更安全
3. 已被更多Service使用

### 迁移策略

```csharp
// Before (ServiceResult<T>)
public async Task<ServiceResult<LoginResponse>> LoginAsync(...)
{
    return ServiceResult<LoginResponse>.Failure("错误");
}

// After (Result<T>)
public async Task<Result<LoginResponse>> LoginAsync(...)
{
    return Result<LoginResponse>.Failure("错误");
}
```

### 兼容性处理
- Controller层统一处理`Result<T>.IsSuccess`
- 移除对`ServiceResult<T>.Exception`的依赖

## Phase 2: BaseService基类设计

### BaseService<TEntity>类定义

```csharp
/// <summary>
/// Service基类 - 提供统一的错误处理、日志和验证
/// </summary>
/// <typeparam name="TEntity">主要操作的实体类型</typeparam>
public abstract class BaseService<TEntity> where TEntity : class
{
    protected readonly ILogger _logger;
    protected readonly IMapper _mapper;

    protected BaseService(ILogger logger, IMapper mapper)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// 执行操作并统一处理异常
    /// </summary>
    protected async Task<Result<T>> ExecuteAsync<T>(
        Func<Task<T>> operation,
        string operationName)
    {
        try
        {
            var result = await operation();
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Operation} 失败", operationName);
            return Result<T>.Failure($"{operationName}失败");
        }
    }

    /// <summary>
    /// 执行验证
    /// </summary>
    protected async Task<Result<TDto>> ValidateAsync<TDto>(
        TDto dto,
        IValidator<TDto> validator)
    {
        var validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => e.ErrorMessage)
                .ToList();
            return Result<TDto>.Failure(errors);
        }
        return Result<TDto>.Success(dto);
    }
}
```

### 子类实现示例

```csharp
public class PatientService : BaseService<Patient>, IPatientService
{
    private readonly IPatientRepository _repository;
    private readonly IValidator<PatientInputDto> _validator;

    public PatientService(
        IPatientRepository repository,
        IMapper mapper,
        ILogger<PatientService> logger,
        IValidator<PatientInputDto> validator)
        : base(logger, mapper)
    {
        _repository = repository;
        _validator = validator;
    }

    public async Task<Result<PatientDto>> CreateAsync(PatientInputDto dto)
    {
        // 验证
        var validationResult = await ValidateAsync(dto, _validator);
        if (!validationResult.IsSuccess)
            return Result<PatientDto>.Failure(validationResult.Errors!);

        // 执行业务逻辑
        return await ExecuteAsync(async () =>
        {
            var entity = _mapper.Map<Patient>(dto);
            await _repository.AddAsync(entity);
            return _mapper.Map<PatientDto>(entity);
        }, "创建患者");
    }
}
```

## Phase 3: MedicalCaseService拆分（直接拆分，无兼容性包袱）

### 当前职责分析

MedicalCaseService包含以下职责（36个方法）：

| 职责 | 方法数 | 目标Service |
|------|-------|-------------|
| CRUD基本操作 | 6 | MedicalCaseCommandService |
| Consultation管理 | 4 | ConsultationService（已存在，扩展） |
| Prescription管理 | 6 | PrescriptionService（已存在，扩展） |
| 状态管理 | 5 | MedicalCaseStateService |
| 查询/列表 | 8 | MedicalCaseQueryService |
| 审计相关 | 4 | MedicalCaseAuditService（已存在） |

### 拆分策略：直接拆分，删除原Service

**原则**: 不保留IMedicalCaseService大接口，直接拆分为职责单一的小接口

```csharp
// 删除: IMedicalCaseService (36个方法的God Interface)
// 删除: MedicalCaseService (1465行的God Class)

// 新建: 职责单一的Service
public interface IMedicalCaseCommandService
{
    Task<Result<MedicalCaseDto>> CreateAsync(MedicalCaseCreateRequest request);
    Task<Result<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateRequest request);
    Task<Result<bool>> DeleteAsync(Guid id);
}

public interface IMedicalCaseQueryService
{
    Task<Result<MedicalCaseDto>> GetByIdAsync(Guid id);
    Task<Result<PagedResult<MedicalCaseListDto>>> GetPagedAsync(MedicalCaseQueryParams query);
    Task<Result<IEnumerable<MedicalCaseListDto>>> GetPendingAsync(Guid patientId);
}

public interface IMedicalCaseStateService
{
    Task<Result<MedicalCaseDto>> CompleteAsync(Guid id);
    Task<Result<MedicalCaseDto>> CancelAsync(Guid id);
    Task<Result<MedicalCaseDto>> SaveDraftAsync(Guid id);
}
```

### Controller层同步更新

```csharp
// Before: 注入单一大Service
public MedicalCaseController(IMedicalCaseService service) { }

// After: 注入职责明确的小Service
public MedicalCaseController(
    IMedicalCaseCommandService commandService,
    IMedicalCaseQueryService queryService,
    IMedicalCaseStateService stateService) { }
```

### 新Service职责

1. **MedicalCaseCommandService**: Create, Update, Delete（写操作）
2. **MedicalCaseQueryService**: GetById, GetPaged, GetPending, Search（读操作）
3. **MedicalCaseStateService**: Complete, Cancel, SaveDraft, UpdateStatus（状态变更）
4. **ConsultationService**: 已存在，从MedicalCaseService移出Consultation相关方法
5. **PrescriptionService**: 已存在，从MedicalCaseService移出Prescription相关方法

## Phase 4: 验证统一化

### FluentValidation覆盖范围

| Service | 当前状态 | 目标 |
|---------|---------|------|
| PatientService | 已有 | 保持 |
| UserService | 已有 | 保持 |
| HerbService | 已有 | 保持 |
| FormulaService | 无 | 添加 |
| ConsultationService | 无 | 添加 |
| PrescriptionService | 无 | 添加 |
| MedicalCaseService | 无 | 添加 |
| AuthService | 无 | 添加 |

### Validator命名规范

```
{EntityName}InputDtoValidator.cs
{EntityName}UpdateDtoValidator.cs
```

### 验证位置

- Validator类位于模块的`Validators/`目录
- DI注册在模块的`ModuleRegistration.cs`

## 文件变更清单

### Phase 1
- `src/Shared/LYBT.Shared.Models/Contracts/Common/ServiceResult.cs` - 标记废弃
- `src/Server/Modules/LYBT.Module.Auth/Services/AuthService.cs` - 返回值类型迁移
- `src/Server/Modules/LYBT.Module.Auth/Services/JwtService.cs` - 返回值类型迁移
- `src/Server/Modules/LYBT.Module.Auth/Interfaces/IAuthService.cs` - 接口更新

### Phase 2
- `src/Server/Core/LYBT.Infrastructure/Services/BaseService.cs` - 新建
- `src/Server/Modules/LYBT.Module.Patients/Services/PatientService.cs` - 继承基类
- `src/Server/Modules/LYBT.Module.Users/Services/UserService.cs` - 继承基类
- `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs` - 继承基类

### Phase 3
- `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseService.cs` - 删除
- `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs` - 删除
- `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseCommandService.cs` - 新建
- `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseQueryService.cs` - 新建
- `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseStateService.cs` - 新建
- `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs` - 新建
- `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseQueryService.cs` - 新建
- `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseStateService.cs` - 新建
- `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs` - 更新注入

### Phase 4
- `src/Server/Modules/LYBT.Module.Formula/Validators/FormulaInputDtoValidator.cs` - 新建
- `src/Server/Modules/LYBT.Module.Consultation/Validators/ConsultationInputDtoValidator.cs` - 新建
- `src/Server/Modules/LYBT.Module.MedicalCase/Validators/MedicalCaseCreateRequestValidator.cs` - 新建

## 决策记录

### DR-001: 选择Result<T>作为统一返回值类型
- **决策**: 使用`Result<T>`替代`ServiceResult<T>`
- **理由**: 支持多错误返回，不暴露Exception，已被更多Service使用
- **影响**: Auth模块需要迁移

### DR-002: BaseService使用泛型TEntity参数
- **决策**: `BaseService<TEntity>`而非无泛型基类
- **理由**: 便于子类复用实体相关逻辑，类型安全
- **影响**: 所有Service需要指定主实体类型

### DR-003: MedicalCaseService直接拆分，删除原接口
- **决策**: 删除IMedicalCaseService和MedicalCaseService，拆分为职责单一的小接口
- **理由**: 最佳实践，消除God Class/God Interface，不背兼容性包袱
- **影响**: Controller需同步更新注入，但代码更清晰、更易测试

### DR-004: Validator独立于Service
- **决策**: Validator不在Service构造函数中强制依赖
- **理由**: 部分简单Service可能不需要复杂验证
- **影响**: ValidateAsync方法接受Validator参数
