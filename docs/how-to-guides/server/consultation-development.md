# Server端诊疗管理开发指南

> **文档版本**: v1.0.0  
> **最后更新**: 2025-10-30  
> **适用模块**: `LYBT.Module.Consultation`  
> **依赖架构**: [Server端诊疗管理架构设计](../../explanation/architecture/server/consultation-design.md)

---

## 📚 目录

1. [快速开始](#1-快速开始)
2. [Read-only Service实现](#2-read-only-service实现)
3. [Repository层开发](#3-repository层开发)
4. [AutoMapper配置](#4-automapper配置)
5. [FluentValidation验证](#5-fluentvalidation验证)
6. [依赖注入配置](#6-依赖注入配置)
7. [聚合根集成](#7-聚合根集成)
8. [性能优化实践](#8-性能优化实践)
9. [最佳实践](#9-最佳实践)
10. [常见问题](#10-常见问题)
11. [测试指南](#11-测试指南)
12. [调试技巧](#12-调试技巧)

---

## 1. 快速开始

### 1.1 模块定位

**Consultation模块**是MedicalCase聚合根的一部分，负责诊疗记录的**只读查询**功能。

**核心设计原则**（Issue #1600 Phase 3）：
```
✅ Read操作：ConsultationService提供只读查询
❌ Write操作：所有写操作必须通过MedicalCaseService聚合根
```

**共享主键设计**：
```csharp
// Consultation与MedicalCase是一对一关系，共享主键
Consultation.Id == MedicalCase.Id
```

### 1.2 环境准备

**NuGet包依赖**：
```xml
<ItemGroup>
  <!-- EF Core -->
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
  
  <!-- AutoMapper -->
  <PackageReference Include="AutoMapper" Version="12.0.1" />
  <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />
  
  <!-- FluentValidation -->
  <PackageReference Include="FluentValidation" Version="11.9.0" />
  <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.0" />
  
  <!-- Logging -->
  <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
</ItemGroup>
```

**项目引用**：
```xml
<ItemGroup>
  <ProjectReference Include="..\..\Core\LYBT.Core.Domain\LYBT.Core.Domain.csproj" />
  <ProjectReference Include="..\..\Core\LYBT.Core.Shared\LYBT.Core.Shared.csproj" />
  <ProjectReference Include="..\LYBT.Module.MedicalCase\LYBT.Module.MedicalCase.csproj" />
</ItemGroup>
```

### 1.3 基本代码示例

**Service层使用示例**：
```csharp
public class ConsultationController : ControllerBase
{
    private readonly IConsultationService _consultationService;
    
    public ConsultationController(IConsultationService consultationService)
    {
        _consultationService = consultationService;
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<ConsultationDto>> GetById(Guid id)
    {
        var result = await _consultationService.GetByIdAsync(id);
        
        if (!result.Success)
        {
            return NotFound(result.ErrorMessage);
        }
        
        return Ok(result.Data);
    }
}
```

---

## 2. Read-only Service实现

### 2.1 IConsultationService接口定义

**接口位置**：`LYBT.Module.Consultation/Services/IConsultationService.cs`

```csharp
using LYBT.Core.Shared.Results;
using LYBT.Module.Consultation.Contracts.DTOs;

namespace LYBT.Module.Consultation.Services;

/// <summary>
/// 诊疗服务接口 - Read Layer（Issue #1600 Phase 3）
/// 职责：提供诊疗记录的只读查询功能
/// 所有Write操作必须通过MedicalCaseService聚合根进行
/// </summary>
public interface IConsultationService
{
    /// <summary>
    /// 根据ID获取诊疗记录详情（包含病案信息）
    /// </summary>
    /// <param name="id">诊疗记录ID（等于病案ID）</param>
    /// <returns>诊疗记录DTO，包含PatientName和DoctorName</returns>
    Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id);
    
    /// <summary>
    /// 根据病案ID获取诊疗记录（共享主键设计）
    /// </summary>
    /// <param name="medicalCaseId">病案ID</param>
    /// <returns>诊疗记录DTO列表（通常只有1个）</returns>
    Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId);
}
```

**⚠️ 重要说明**（Issue #1600 Phase 3）：
```
❌ 以下方法已从接口移除：
- CreateAsync
- UpdateAsync
- DeleteAsync
- CompleteStep1Async

✅ 所有写操作必须通过 IMedicalCaseService 聚合根：
- medicalCaseService.UpdateConsultationAsync(medicalCaseId, consultationDto)
- medicalCaseService.CompleteStep1Async(medicalCaseId)
```

### 2.2 ConsultationService实现

**实现位置**：`LYBT.Module.Consultation/Services/ConsultationService.cs`

```csharp
using AutoMapper;
using LYBT.Core.Shared.Results;
using LYBT.Module.Consultation.Contracts.DTOs;
using LYBT.Module.Consultation.Repositories;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Consultation.Services;

/// <summary>
/// 诊疗服务实现 - Read Layer（Issue #1600 Phase 3）
/// </summary>
public class ConsultationService : IConsultationService
{
    private readonly IConsultationRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<ConsultationService> _logger;

    public ConsultationService(
        IConsultationRepository repository,
        IMapper mapper,
        ILogger<ConsultationService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 根据ID获取诊疗记录详情
    /// </summary>
    public async Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id)
    {
        try
        {
            // ⚡ 使用优化后的查询方法，包含所有关联数据
            var entity = await _repository.GetByIdWithDetailsAsync(id);
            if (entity == null)
            {
                return ServiceResult<ConsultationDto>.Failure("诊疗记录不存在");
            }

            var dto = _mapper.Map<ConsultationDto>(entity);
            
            // ⚡ 确保PatientName和DoctorName从预加载的导航属性获取
            dto.PatientName = entity.MedicalCase?.PatientName ?? string.Empty;
            dto.DoctorName = entity.MedicalCase?.DoctorName ?? string.Empty;

            return ServiceResult<ConsultationDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取诊疗记录详情失败，ID: {ConsultationId}", id);
            return ServiceResult<ConsultationDto>.Failure("获取诊疗记录详情失败");
        }
    }

    /// <summary>
    /// 根据病案ID获取诊疗记录（共享主键设计）
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
    {
        try
        {
            // 共享主键设计：Consultation.Id == MedicalCase.Id
            var consultation = await _repository.GetByMedicalCaseIdAsync(medicalCaseId);
            
            if (consultation == null)
            {
                // 返回空列表而非错误（病案可能还没有诊疗记录）
                return ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto>());
            }

            var dto = _mapper.Map<ConsultationDto>(consultation);
            dto.PatientName = consultation.MedicalCase?.PatientName ?? string.Empty;
            dto.DoctorName = consultation.MedicalCase?.DoctorName ?? string.Empty;

            return ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto> { dto });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据病案ID获取诊疗记录失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
            return ServiceResult<List<ConsultationDto>>.Failure("获取诊疗记录失败");
        }
    }
}
```

**关键实现要点**：

1. **PatientName/DoctorName获取策略**：
   ```csharp
   // ✅ 正确：从预加载的导航属性获取
   dto.PatientName = entity.MedicalCase?.PatientName ?? string.Empty;
   dto.DoctorName = entity.MedicalCase?.DoctorName ?? string.Empty;
   
   // ❌ 错误：不要再次查询数据库
   // var medicalCase = await _medicalCaseRepository.GetByIdAsync(entity.Id);
   ```

2. **共享主键查询**：
   ```csharp
   // GetByMedicalCaseIdAsync实际上查询 Consultation.Id == medicalCaseId
   var consultation = await _repository.GetByMedicalCaseIdAsync(medicalCaseId);
   ```

3. **ServiceResult<T>模式**：
   ```csharp
   // 成功
   return ServiceResult<ConsultationDto>.Success(dto);
   
   // 失败
   return ServiceResult<ConsultationDto>.Failure("错误消息");
   ```

---

## 3. Repository层开发

### 3.1 IConsultationRepository接口定义

**接口位置**：`LYBT.Module.Consultation/Repositories/IConsultationRepository.cs`

```csharp
using LYBT.Core.Domain.Repositories;
using LYBT.Core.Shared.Pagination;
using LYBT.Module.Consultation.Entities;

namespace LYBT.Module.Consultation.Repositories;

/// <summary>
/// 诊疗记录仓储接口
/// </summary>
public interface IConsultationRepository : IRepository<ConsultationEntity>
{
    /// <summary>
    /// 根据患者ID获取诊疗记录列表
    /// </summary>
    Task<List<ConsultationEntity>> GetByPatientIdAsync(Guid patientId);
    
    /// <summary>
    /// 分页获取诊疗记录（包含病案信息）
    /// </summary>
    Task<PagedResult<ConsultationEntity>> GetPagedWithDetailsAsync(
        int pageNumber,
        int pageSize,
        string? keyword = null);
    
    /// <summary>
    /// 根据ID获取诊疗记录详情（包含病案信息）
    /// ⚡ 预加载MedicalCase导航属性，避免N+1查询
    /// </summary>
    Task<ConsultationEntity> GetByIdWithDetailsAsync(Guid id);
    
    /// <summary>
    /// 根据病案ID获取诊疗记录（共享主键设计）
    /// </summary>
    Task<ConsultationEntity?> GetByMedicalCaseIdAsync(Guid medicalCaseId);
}
```

### 3.2 ConsultationRepository实现

**实现位置**：`LYBT.Module.Consultation/Repositories/ConsultationRepository.cs`

```csharp
using LYBT.Core.Domain.Repositories;
using LYBT.Core.Shared.Pagination;
using LYBT.Module.Consultation.Entities;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Consultation.Repositories;

/// <summary>
/// 诊疗记录仓储实现
/// 核心优化：所有*WithDetailsAsync方法使用.Include(c => c.MedicalCase)预加载
/// </summary>
internal class ConsultationRepository : BaseRepository<ConsultationEntity>, IConsultationRepository
{
    public ConsultationRepository(DbContext context) : base(context)
    {
    }

    /// <summary>
    /// 根据患者ID获取诊疗记录列表
    /// </summary>
    public async Task<List<ConsultationEntity>> GetByPatientIdAsync(Guid patientId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(c => c.MedicalCase)  // ⚡预加载病案信息
            .Where(c => c.MedicalCase.PatientId == patientId && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 分页获取诊疗记录（包含病案信息）
    /// ⚡ 预加载MedicalCase，支持关键字搜索（主诉、诊断、患者名、医生名）
    /// </summary>
    public async Task<PagedResult<ConsultationEntity>> GetPagedWithDetailsAsync(
        int pageNumber,
        int pageSize,
        string? keyword = null)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(c => c.MedicalCase)  // ⚡预加载病案信息（包含患者和医生信息）
            .Where(c => !c.IsDeleted);

        // 关键字搜索（主诉、诊断、患者名、医生名）
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(c =>
                (c.ChiefComplaint != null && c.ChiefComplaint.Contains(keyword)) ||
                (c.TCMDiagnosis != null && c.TCMDiagnosis.Contains(keyword)) ||
                c.MedicalCase.PatientName.Contains(keyword) ||
                c.MedicalCase.DoctorName.Contains(keyword));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ConsultationEntity>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = pageNumber,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 根据ID获取诊疗记录详情（包含病案信息）
    /// ⚡ 预加载MedicalCase导航属性，避免N+1查询
    /// </summary>
    public async Task<ConsultationEntity> GetByIdWithDetailsAsync(Guid id)
    {
        return (await _dbSet
            .AsNoTracking()
            .Include(c => c.MedicalCase)  // ⚡预加载MedicalCase
            .Where(c => c.Id == id && !c.IsDeleted)
            .FirstOrDefaultAsync())!;
    }

    /// <summary>
    /// 根据病案ID获取诊疗记录（共享主键设计）
    /// 因为 Consultation.Id == MedicalCase.Id，所以直接查询 c.Id == medicalCaseId
    /// </summary>
    public async Task<ConsultationEntity?> GetByMedicalCaseIdAsync(Guid medicalCaseId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(c => c.MedicalCase)  // ⚡预加载MedicalCase
            .Where(c => c.Id == medicalCaseId && !c.IsDeleted)  // 共享主键查询
            .FirstOrDefaultAsync();
    }

    // ========== BaseRepository方法显式实现 ==========
    // 以下方法为BaseRepository的显式实现，确保返回类型匹配
    
    async Task<IEnumerable<ConsultationEntity>> IRepository<ConsultationEntity>.GetAllAsync()
    {
        return await base.GetAllAsync();
    }

    async Task<IEnumerable<ConsultationEntity>> IRepository<ConsultationEntity>.FindAsync(
        System.Linq.Expressions.Expression<Func<ConsultationEntity, bool>> predicate)
    {
        return await base.FindAsync(predicate);
    }
}
```

**关键实现要点**：

1. **Include策略（解决N+1查询问题）**：
   ```csharp
   // ✅ 正确：所有*WithDetailsAsync方法使用Include预加载
   var entity = await _dbSet
       .AsNoTracking()
       .Include(c => c.MedicalCase)  // 预加载导航属性
       .FirstOrDefaultAsync();
   
   // ❌ 错误：不使用Include会导致N+1查询
   var entity = await _dbSet.FirstOrDefaultAsync();
   var medicalCase = entity.MedicalCase;  // 触发额外查询！
   ```

2. **AsNoTracking优化**：
   ```csharp
   // ✅ 所有只读查询使用AsNoTracking
   var entities = await _dbSet
       .AsNoTracking()  // 减少内存占用，提升性能
       .Include(c => c.MedicalCase)
       .ToListAsync();
   
   // ❌ 只读查询不要追踪实体
   var entities = await _dbSet.Include(c => c.MedicalCase).ToListAsync();
   ```

3. **共享主键查询**：
   ```csharp
   // 因为 Consultation.Id == MedicalCase.Id（共享主键）
   // 所以直接用 c.Id == medicalCaseId 查询
   var consultation = await _dbSet
       .Where(c => c.Id == medicalCaseId)
       .FirstOrDefaultAsync();
   ```

4. **关键字搜索优化**：
   ```csharp
   // 支持在主诉、诊断、患者名、医生名中搜索
   if (!string.IsNullOrWhiteSpace(keyword))
   {
       query = query.Where(c =>
           (c.ChiefComplaint != null && c.ChiefComplaint.Contains(keyword)) ||
           (c.TCMDiagnosis != null && c.TCMDiagnosis.Contains(keyword)) ||
           c.MedicalCase.PatientName.Contains(keyword) ||
           c.MedicalCase.DoctorName.Contains(keyword));
   }
   ```

### 3.3 Repository性能对比

**N+1查询问题示例**：
```csharp
// ❌ 不使用Include的N+1查询问题
var consultations = await _dbSet.Take(10).ToListAsync();  // 1次查询
foreach (var c in consultations)
{
    var patientName = c.MedicalCase.PatientName;  // 10次额外查询！
    var doctorName = c.MedicalCase.DoctorName;    // 已在上一行查询中获取
}
// 总计：1 + 10 = 11次数据库查询

// ✅ 使用Include的优化查询
var consultations = await _dbSet
    .Include(c => c.MedicalCase)  // 预加载
    .Take(10)
    .ToListAsync();  // 1次查询（使用JOIN）
foreach (var c in consultations)
{
    var patientName = c.MedicalCase.PatientName;  // 0次额外查询
    var doctorName = c.MedicalCase.DoctorName;    // 0次额外查询
}
// 总计：1次数据库查询
```

**生成的SQL对比**：
```sql
-- ❌ 不使用Include（N+1查询）
SELECT TOP(10) * FROM Consultations WHERE IsDeleted = 0;
-- 然后对每条记录：
SELECT * FROM MedicalCases WHERE Id = @p0;  -- 执行10次

-- ✅ 使用Include（JOIN查询）
SELECT TOP(10) 
    c.*, 
    mc.*
FROM Consultations c
LEFT JOIN MedicalCases mc ON c.Id = mc.Id
WHERE c.IsDeleted = 0;
-- 只执行1次
```

---

## 4. AutoMapper配置

### 4.1 ConsultationMappingProfile实现

**Profile位置**：`LYBT.Module.Consultation/Mappings/ConsultationMappingProfile.cs`

```csharp
using AutoMapper;
using LYBT.Module.Consultation.Contracts.DTOs;
using LYBT.Module.Consultation.Entities;

namespace LYBT.Module.Consultation.Mappings;

/// <summary>
/// 诊疗记录AutoMapper映射配置
/// </summary>
public class ConsultationMappingProfile : Profile
{
    public ConsultationMappingProfile()
    {
        // ========== Entity → DTO ==========
        CreateMap<ConsultationEntity, ConsultationDto>()
            .ForMember(dest => dest.PatientName, opt => opt.Ignore())  // Service层手动设置
            .ForMember(dest => dest.DoctorName, opt => opt.Ignore());  // Service层手动设置

        // ========== CreateDto → Entity ==========
        CreateMap<ConsultationCreateDto, ConsultationEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())  // 由MedicalCase聚合根设置
            .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

        // ========== UpdateDto → Entity（Partial Update支持）==========
        CreateMap<ConsultationUpdateDto, ConsultationEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            // ⚡ Partial Update：只映射非null值
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
```

**关键映射要点**：

1. **PatientName/DoctorName手动设置**：
   ```csharp
   // DTO中有PatientName/DoctorName，但Entity中没有
   // 这些字段从MedicalCase导航属性获取，不需要AutoMapper映射
   .ForMember(dest => dest.PatientName, opt => opt.Ignore())
   .ForMember(dest => dest.DoctorName, opt => opt.Ignore())
   ```

2. **Partial Update支持**：
   ```csharp
   // UpdateDto → Entity时，只映射非null值
   // 这样前端只发送变更的字段，未变更的字段保持原值
   .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
   ```

3. **Entity字段忽略**：
   ```csharp
   // Id由MedicalCase聚合根设置（共享主键）
   .ForMember(dest => dest.Id, opt => opt.Ignore())
   
   // 导航属性不需要映射
   .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
   
   // 审计字段由BaseEntity自动处理
   .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
   .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
   .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
   ```

### 4.2 Mapper使用示例

**Service层使用示例**：
```csharp
public async Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id)
{
    var entity = await _repository.GetByIdWithDetailsAsync(id);
    if (entity == null)
    {
        return ServiceResult<ConsultationDto>.Failure("诊疗记录不存在");
    }

    // ⚡ AutoMapper映射
    var dto = _mapper.Map<ConsultationDto>(entity);
    
    // ⚡ 手动设置PatientName/DoctorName（从预加载的导航属性获取）
    dto.PatientName = entity.MedicalCase?.PatientName ?? string.Empty;
    dto.DoctorName = entity.MedicalCase?.DoctorName ?? string.Empty;

    return ServiceResult<ConsultationDto>.Success(dto);
}
```

**Partial Update示例**（在MedicalCaseService中）：
```csharp
public async Task<ServiceResult<MedicalCaseDto>> UpdateConsultationAsync(
    Guid medicalCaseId,
    ConsultationUpdateDto consultationDto)
{
    var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
    if (medicalCase == null)
    {
        return ServiceResult<MedicalCaseDto>.Failure("病案不存在");
    }

    // ⚡ Partial Update：只更新非null字段
    _mapper.Map(consultationDto, medicalCase.Consultation);
    
    medicalCase.Consultation.UpdatedAt = DateTime.UtcNow;
    await _repository.UpdateAsync(medicalCase);

    var dto = _mapper.Map<MedicalCaseDto>(medicalCase);
    return ServiceResult<MedicalCaseDto>.Success(dto);
}
```

---

## 5. FluentValidation验证

### 5.1 ConsultationCreateDtoValidator实现

**Validator位置**：`LYBT.Module.Consultation/Validators/ConsultationCreateDtoValidator.cs`

```csharp
using FluentValidation;
using LYBT.Module.Consultation.Contracts.DTOs;

namespace LYBT.Module.Consultation.Validators;

/// <summary>
/// 诊疗记录创建DTO验证器
/// </summary>
public class ConsultationCreateDtoValidator : AbstractValidator<ConsultationCreateDto>
{
    public ConsultationCreateDtoValidator()
    {
        // ========== 必填字段验证 ==========
        
        // 主诉（必填，1-500字符）
        RuleFor(x => x.ChiefComplaint)
            .NotEmpty().WithMessage("主诉不能为空")
            .MaximumLength(500).WithMessage("主诉不能超过500个字符");

        // 中医诊断（必填，1-500字符）
        RuleFor(x => x.TCMDiagnosis)
            .NotEmpty().WithMessage("中医诊断不能为空")
            .MaximumLength(500).WithMessage("中医诊断不能超过500个字符");

        // ========== 可选字段验证 ==========
        
        // 现病史（可选，最长2000字符）
        RuleFor(x => x.PresentIllness)
            .MaximumLength(2000).WithMessage("现病史不能超过2000个字符")
            .When(x => !string.IsNullOrWhiteSpace(x.PresentIllness));

        // 望诊（可选，最长1000字符）
        RuleFor(x => x.Inspection)
            .MaximumLength(1000).WithMessage("望诊记录不能超过1000个字符")
            .When(x => !string.IsNullOrWhiteSpace(x.Inspection));

        // 闻诊（可选，最长1000字符）
        RuleFor(x => x.AuscultationOlfaction)
            .MaximumLength(1000).WithMessage("闻诊记录不能超过1000个字符")
            .When(x => !string.IsNullOrWhiteSpace(x.AuscultationOlfaction));

        // 问诊（可选，最长1000字符）
        RuleFor(x => x.Inquiry)
            .MaximumLength(1000).WithMessage("问诊记录不能超过1000个字符")
            .When(x => !string.IsNullOrWhiteSpace(x.Inquiry));

        // 切诊（可选，最长1000字符）
        RuleFor(x => x.Palpation)
            .MaximumLength(1000).WithMessage("切诊记录不能超过1000个字符")
            .When(x => !string.IsNullOrWhiteSpace(x.Palpation));

        // 治则治法（可选，最长500字符）
        RuleFor(x => x.TreatmentPrinciple)
            .MaximumLength(500).WithMessage("治则治法不能超过500个字符")
            .When(x => !string.IsNullOrWhiteSpace(x.TreatmentPrinciple));

        // 医嘱（可选，最长1000字符）
        RuleFor(x => x.MedicalAdvice)
            .MaximumLength(1000).WithMessage("医嘱不能超过1000个字符")
            .When(x => !string.IsNullOrWhiteSpace(x.MedicalAdvice));
    }
}
```

### 5.2 ConsultationUpdateDtoValidator实现

**Validator位置**：`LYBT.Module.Consultation/Validators/ConsultationUpdateDtoValidator.cs`

```csharp
using FluentValidation;
using LYBT.Module.Consultation.Contracts.DTOs;

namespace LYBT.Module.Consultation.Validators;

/// <summary>
/// 诊疗记录更新DTO验证器
/// 注意：UpdateDto支持Partial Update，所有字段都是可选的
/// </summary>
public class ConsultationUpdateDtoValidator : AbstractValidator<ConsultationUpdateDto>
{
    public ConsultationUpdateDtoValidator()
    {
        // ========== 条件验证（只验证非null字段）==========
        
        // 主诉（如果提供，1-500字符）
        RuleFor(x => x.ChiefComplaint)
            .NotEmpty().WithMessage("主诉不能为空")
            .MaximumLength(500).WithMessage("主诉不能超过500个字符")
            .When(x => x.ChiefComplaint != null);

        // 现病史（如果提供，最长2000字符）
        RuleFor(x => x.PresentIllness)
            .MaximumLength(2000).WithMessage("现病史不能超过2000个字符")
            .When(x => x.PresentIllness != null);

        // 望诊（如果提供，最长1000字符）
        RuleFor(x => x.Inspection)
            .MaximumLength(1000).WithMessage("望诊记录不能超过1000个字符")
            .When(x => x.Inspection != null);

        // 闻诊（如果提供，最长1000字符）
        RuleFor(x => x.AuscultationOlfaction)
            .MaximumLength(1000).WithMessage("闻诊记录不能超过1000个字符")
            .When(x => x.AuscultationOlfaction != null);

        // 问诊（如果提供，最长1000字符）
        RuleFor(x => x.Inquiry)
            .MaximumLength(1000).WithMessage("问诊记录不能超过1000个字符")
            .When(x => x.Inquiry != null);

        // 切诊（如果提供，最长1000字符）
        RuleFor(x => x.Palpation)
            .MaximumLength(1000).WithMessage("切诊记录不能超过1000个字符")
            .When(x => x.Palpation != null);

        // 中医诊断（如果提供，1-500字符）
        RuleFor(x => x.TCMDiagnosis)
            .NotEmpty().WithMessage("中医诊断不能为空")
            .MaximumLength(500).WithMessage("中医诊断不能超过500个字符")
            .When(x => x.TCMDiagnosis != null);

        // 治则治法（如果提供，最长500字符）
        RuleFor(x => x.TreatmentPrinciple)
            .MaximumLength(500).WithMessage("治则治法不能超过500个字符")
            .When(x => x.TreatmentPrinciple != null);

        // 医嘱（如果提供，最长1000字符）
        RuleFor(x => x.MedicalAdvice)
            .MaximumLength(1000).WithMessage("医嘱不能超过1000个字符")
            .When(x => x.MedicalAdvice != null);
    }
}
```

**关键验证要点**：

1. **必填字段验证**（CreateDto）：
   ```csharp
   // ChiefComplaint和TCMDiagnosis必填
   RuleFor(x => x.ChiefComplaint)
       .NotEmpty().WithMessage("主诉不能为空")
       .MaximumLength(500).WithMessage("主诉不能超过500个字符");
   ```

2. **条件验证**（UpdateDto）：
   ```csharp
   // UpdateDto支持Partial Update，只验证提供的字段
   RuleFor(x => x.ChiefComplaint)
       .NotEmpty().WithMessage("主诉不能为空")
       .MaximumLength(500).WithMessage("主诉不能超过500个字符")
       .When(x => x.ChiefComplaint != null);  // ⚡ 只验证非null字段
   ```

3. **字段长度限制**：
   ```csharp
   // 主诉、诊断：500字符
   // 现病史：2000字符
   // 四诊记录（望闻问切）：1000字符
   // 治则治法：500字符
   // 医嘱：1000字符
   ```

### 5.3 Validator使用示例

**在MedicalCaseService中使用Validator**：
```csharp
using FluentValidation;

public class MedicalCaseService : IMedicalCaseService
{
    private readonly IValidator<ConsultationUpdateDto> _consultationUpdateValidator;
    
    public MedicalCaseService(
        IValidator<ConsultationUpdateDto> consultationUpdateValidator)
    {
        _consultationUpdateValidator = consultationUpdateValidator;
    }
    
    public async Task<ServiceResult<MedicalCaseDto>> UpdateConsultationAsync(
        Guid medicalCaseId,
        ConsultationUpdateDto consultationDto)
    {
        // ⚡ FluentValidation验证
        var validationResult = await _consultationUpdateValidator.ValidateAsync(consultationDto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("；", validationResult.Errors.Select(e => e.ErrorMessage));
            return ServiceResult<MedicalCaseDto>.Failure(errors);
        }

        // 继续业务逻辑...
        var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
        // ...
    }
}
```

**在Controller中使用Validator**（自动验证）：
```csharp
[ApiController]
[Route("api/v1/medical-cases")]
public class MedicalCaseController : ControllerBase
{
    // ⚠️ 使用[FromBody]时，ASP.NET Core会自动调用FluentValidation验证器
    [HttpPut("{id}/consultation")]
    public async Task<ActionResult<MedicalCaseDto>> UpdateConsultation(
        Guid id,
        [FromBody] ConsultationUpdateDto consultationDto)  // ⚡ 自动验证
    {
        // 如果验证失败，返回400 BadRequest + ValidationProblemDetails
        // 如果验证成功，继续执行
        
        var result = await _medicalCaseService.UpdateConsultationAsync(id, consultationDto);
        
        if (!result.Success)
        {
            return BadRequest(result.ErrorMessage);
        }
        
        return Ok(result.Data);
    }
}
```

---

## 6. 依赖注入配置

### 6.1 ConsultationModule实现

**Module位置**：`LYBT.Module.Consultation/ConsultationModule.cs`

```csharp
using FluentValidation;
using LYBT.Module.Consultation.Repositories;
using LYBT.Module.Consultation.Services;
using LYBT.Module.Consultation.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Consultation;

/// <summary>
/// 诊疗模块依赖注入配置
/// </summary>
public static class ConsultationModule
{
    /// <summary>
    /// 注册诊疗模块服务
    /// </summary>
    public static IServiceCollection AddConsultationModule(this IServiceCollection services)
    {
        // ========== Service层 ==========
        services.AddScoped<IConsultationService, ConsultationService>();

        // ========== Repository层 ==========
        services.AddScoped<IConsultationRepository, ConsultationRepository>();

        // ========== AutoMapper ==========
        // 自动扫描并注册本程序集中的所有Profile
        services.AddAutoMapper(typeof(ConsultationModule).Assembly);

        // ========== FluentValidation ==========
        // 自动扫描并注册本程序集中的所有Validator
        services.AddValidatorsFromAssembly(typeof(ConsultationModule).Assembly);

        return services;
    }
}
```

**关键配置要点**：

1. **Service层注册**：
   ```csharp
   // Scoped生命周期：每次HTTP请求一个实例
   services.AddScoped<IConsultationService, ConsultationService>();
   ```

2. **Repository层注册**：
   ```csharp
   // Scoped生命周期：与DbContext生命周期一致
   services.AddScoped<IConsultationRepository, ConsultationRepository>();
   ```

3. **AutoMapper自动扫描**：
   ```csharp
   // 自动注册本程序集中的所有Profile
   services.AddAutoMapper(typeof(ConsultationModule).Assembly);
   ```

4. **FluentValidation自动扫描**：
   ```csharp
   // 自动注册本程序集中的所有IValidator<T>
   services.AddValidatorsFromAssembly(typeof(ConsultationModule).Assembly);
   ```

### 6.2 在WebAPI中注册Module

**Startup.cs配置**：
```csharp
using LYBT.Module.Consultation;
using LYBT.Module.MedicalCase;
using LYBT.Module.Patients;
// ... 其他模块

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // ========== 注册所有业务模块 ==========
        services.AddAuthModule();          // 认证模块
        services.AddUsersModule();         // 用户模块
        services.AddPatientsModule();      // 患者模块
        services.AddMedicalCaseModule();   // 病案模块（聚合根）
        services.AddConsultationModule();  // ⚡ 诊疗模块
        services.AddPrescriptionsModule(); // 处方模块
        services.AddHerbsModule();         // 药材模块
        services.AddFormulaModule();       // 验方模块

        // ========== EF Core配置 ==========
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                Configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.EnableRetryOnFailure()));

        // ========== 其他配置 ==========
        services.AddControllers()
            .AddFluentValidation();  // ⚡ 启用FluentValidation自动验证

        services.AddSwaggerGen();
        // ...
    }
}
```

**Program.cs配置**（.NET 8 Minimal API）：
```csharp
using LYBT.Module.Consultation;
using LYBT.Module.MedicalCase;

var builder = WebApplication.CreateBuilder(args);

// ========== 注册所有业务模块 ==========
builder.Services.AddAuthModule();
builder.Services.AddUsersModule();
builder.Services.AddPatientsModule();
builder.Services.AddMedicalCaseModule();
builder.Services.AddConsultationModule();  // ⚡ 诊疗模块
builder.Services.AddPrescriptionsModule();
builder.Services.AddHerbsModule();
builder.Services.AddFormulaModule();

// ========== EF Core配置 ==========
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

// ========== 其他配置 ==========
builder.Services.AddControllers()
    .AddFluentValidation();  // ⚡ 启用FluentValidation

var app = builder.Build();

app.MapControllers();
app.Run();
```

---

## 7. 聚合根集成

### 7.1 MedicalCase聚合根设计

**核心原则**（Issue #1600 Phase 3）：
```
✅ Consultation的所有Write操作必须通过MedicalCase聚合根
❌ 不允许直接调用ConsultationService的Write方法（已移除）
```

**聚合根边界**：
```
MedicalCaseEntity（聚合根）
├── ConsultationEntity（子实体，共享主键）
└── PrescriptionEntity（子实体，共享主键）
```

### 7.2 IMedicalCaseService接口定义

**接口位置**：`LYBT.Module.MedicalCase/Services/IMedicalCaseService.cs`

```csharp
using LYBT.Core.Shared.Results;
using LYBT.Module.Consultation.Contracts.DTOs;
using LYBT.Module.MedicalCase.Contracts.DTOs;

namespace LYBT.Module.MedicalCase.Services;

/// <summary>
/// 病案服务接口 - 聚合根
/// </summary>
public interface IMedicalCaseService
{
    // ========== Consultation Write操作（通过聚合根）==========
    
    /// <summary>
    /// 更新诊疗记录（通过病案聚合根）
    /// </summary>
    Task<ServiceResult<MedicalCaseDto>> UpdateConsultationAsync(
        Guid medicalCaseId,
        ConsultationUpdateDto consultationDto);
    
    /// <summary>
    /// 完成Step1（辨证阶段）
    /// </summary>
    Task<ServiceResult<MedicalCaseDto>> CompleteStep1Async(Guid medicalCaseId);
    
    // ========== 其他聚合根方法 ==========
    Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto);
    Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
    // ...
}
```

### 7.3 MedicalCaseService实现（Consultation相关方法）

**Service位置**：`LYBT.Module.MedicalCase/Services/MedicalCaseService.cs`

```csharp
using AutoMapper;
using FluentValidation;
using LYBT.Core.Shared.Results;
using LYBT.Module.Consultation.Contracts.DTOs;
using LYBT.Module.MedicalCase.Contracts.DTOs;
using LYBT.Module.MedicalCase.Repositories;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCase.Services;

/// <summary>
/// 病案服务实现 - 聚合根
/// </summary>
public class MedicalCaseService : IMedicalCaseService
{
    private readonly IMedicalCaseRepository _repository;
    private readonly IMapper _mapper;
    private readonly IValidator<ConsultationUpdateDto> _consultationUpdateValidator;
    private readonly ILogger<MedicalCaseService> _logger;

    public MedicalCaseService(
        IMedicalCaseRepository repository,
        IMapper mapper,
        IValidator<ConsultationUpdateDto> consultationUpdateValidator,
        ILogger<MedicalCaseService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _consultationUpdateValidator = consultationUpdateValidator;
        _logger = logger;
    }

    /// <summary>
    /// 更新诊疗记录（通过病案聚合根）
    /// </summary>
    public async Task<ServiceResult<MedicalCaseDto>> UpdateConsultationAsync(
        Guid medicalCaseId,
        ConsultationUpdateDto consultationDto)
    {
        try
        {
            // 1️⃣ FluentValidation验证
            var validationResult = await _consultationUpdateValidator.ValidateAsync(consultationDto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("；", validationResult.Errors.Select(e => e.ErrorMessage));
                return ServiceResult<MedicalCaseDto>.Failure(errors);
            }

            // 2️⃣ 获取病案聚合根（包含Consultation）
            var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
            if (medicalCase == null)
            {
                return ServiceResult<MedicalCaseDto>.Failure("病案不存在");
            }

            if (medicalCase.Consultation == null)
            {
                return ServiceResult<MedicalCaseDto>.Failure("诊疗记录不存在");
            }

            // 3️⃣ Partial Update：只更新非null字段
            _mapper.Map(consultationDto, medicalCase.Consultation);
            medicalCase.Consultation.UpdatedAt = DateTime.UtcNow;

            // 4️⃣ 更新聚合根（EF Core会自动追踪Consultation的变更）
            await _repository.UpdateAsync(medicalCase);

            _logger.LogInformation("诊疗记录更新成功，病案ID: {MedicalCaseId}", medicalCaseId);

            // 5️⃣ 返回更新后的病案
            var dto = _mapper.Map<MedicalCaseDto>(medicalCase);
            return ServiceResult<MedicalCaseDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新诊疗记录失败，病案ID: {MedicalCaseId}", medicalCaseId);
            return ServiceResult<MedicalCaseDto>.Failure("更新诊疗记录失败");
        }
    }

    /// <summary>
    /// 完成Step1（辨证阶段）
    /// </summary>
    public async Task<ServiceResult<MedicalCaseDto>> CompleteStep1Async(Guid medicalCaseId)
    {
        try
        {
            // 1️⃣ 获取病案聚合根
            var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
            if (medicalCase == null)
            {
                return ServiceResult<MedicalCaseDto>.Failure("病案不存在");
            }

            if (medicalCase.Consultation == null)
            {
                return ServiceResult<MedicalCaseDto>.Failure("诊疗记录不存在");
            }

            // 2️⃣ 验证必填字段
            if (string.IsNullOrWhiteSpace(medicalCase.Consultation.ChiefComplaint))
            {
                return ServiceResult<MedicalCaseDto>.Failure("主诉不能为空");
            }

            if (string.IsNullOrWhiteSpace(medicalCase.Consultation.TCMDiagnosis))
            {
                return ServiceResult<MedicalCaseDto>.Failure("中医诊断不能为空");
            }

            // 3️⃣ 标记Step1完成
            if (medicalCase.Consultation.Step1CompletedAt.HasValue)
            {
                return ServiceResult<MedicalCaseDto>.Failure("Step1已完成，不能重复操作");
            }

            medicalCase.Consultation.Step1CompletedAt = DateTime.UtcNow;
            medicalCase.Consultation.PrescriptionEnabled = true;  // 启用处方功能
            medicalCase.Consultation.UpdatedAt = DateTime.UtcNow;

            // 4️⃣ 更新聚合根
            await _repository.UpdateAsync(medicalCase);

            _logger.LogInformation("Step1完成，病案ID: {MedicalCaseId}", medicalCaseId);

            // 5️⃣ 返回更新后的病案
            var dto = _mapper.Map<MedicalCaseDto>(medicalCase);
            return ServiceResult<MedicalCaseDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完成Step1失败，病案ID: {MedicalCaseId}", medicalCaseId);
            return ServiceResult<MedicalCaseDto>.Failure("完成Step1失败");
        }
    }

    // ========== 其他聚合根方法 ==========
    // CreateAsync, UpdateAsync, DeleteAsync...
}
```

**关键实现要点**：

1. **通过聚合根更新Consultation**：
   ```csharp
   // ✅ 正确：通过MedicalCase聚合根更新Consultation
   var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
   _mapper.Map(consultationDto, medicalCase.Consultation);
   await _repository.UpdateAsync(medicalCase);  // 更新聚合根
   
   // ❌ 错误：直接更新Consultation（违反聚合根原则）
   // var consultation = await _consultationRepository.GetByIdAsync(id);
   // await _consultationRepository.UpdateAsync(consultation);
   ```

2. **EF Core自动追踪子实体变更**：
   ```csharp
   // EF Core会自动追踪Consultation的变更，无需显式更新Consultation
   medicalCase.Consultation.ChiefComplaint = "新主诉";
   await _repository.UpdateAsync(medicalCase);  // 自动保存Consultation的变更
   ```

3. **业务规则在聚合根中验证**：
   ```csharp
   // Step1完成时的业务规则验证
   if (string.IsNullOrWhiteSpace(medicalCase.Consultation.ChiefComplaint))
   {
       return ServiceResult<MedicalCaseDto>.Failure("主诉不能为空");
   }
   
   if (medicalCase.Consultation.Step1CompletedAt.HasValue)
   {
       return ServiceResult<MedicalCaseDto>.Failure("Step1已完成，不能重复操作");
   }
   ```

### 7.4 Controller调用示例

**MedicalCaseController使用聚合根方法**：
```csharp
using LYBT.Module.Consultation.Contracts.DTOs;
using LYBT.Module.MedicalCase.Services;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers;

[ApiController]
[Route("api/v1/medical-cases")]
public class MedicalCaseController : ControllerBase
{
    private readonly IMedicalCaseService _medicalCaseService;
    
    public MedicalCaseController(IMedicalCaseService medicalCaseService)
    {
        _medicalCaseService = medicalCaseService;
    }

    /// <summary>
    /// 更新诊疗记录（通过病案聚合根）
    /// </summary>
    [HttpPut("{id}/consultation")]
    public async Task<ActionResult<MedicalCaseDto>> UpdateConsultation(
        Guid id,
        [FromBody] ConsultationUpdateDto consultationDto)
    {
        var result = await _medicalCaseService.UpdateConsultationAsync(id, consultationDto);
        
        if (!result.Success)
        {
            return BadRequest(result.ErrorMessage);
        }
        
        return Ok(result.Data);
    }

    /// <summary>
    /// 完成Step1（辨证阶段）
    /// </summary>
    [HttpPost("{id}/complete-step1")]
    public async Task<ActionResult<MedicalCaseDto>> CompleteStep1(Guid id)
    {
        var result = await _medicalCaseService.CompleteStep1Async(id);
        
        if (!result.Success)
        {
            return BadRequest(result.ErrorMessage);
        }
        
        return Ok(result.Data);
    }
}
```

---

## 8. 性能优化实践

### 8.1 N+1查询问题解决

**问题示例**：
```csharp
// ❌ N+1查询问题（执行11次数据库查询）
var consultations = await _dbSet.Take(10).ToListAsync();  // 1次查询
foreach (var c in consultations)
{
    var patientName = c.MedicalCase.PatientName;  // 10次额外查询
}
```

**解决方案**：
```csharp
// ✅ 使用Include预加载（只执行1次数据库查询）
var consultations = await _dbSet
    .Include(c => c.MedicalCase)  // 预加载导航属性
    .Take(10)
    .ToListAsync();  // 1次查询（使用LEFT JOIN）

foreach (var c in consultations)
{
    var patientName = c.MedicalCase.PatientName;  // 0次额外查询
}
```

**生成的SQL对比**：
```sql
-- ❌ N+1查询（11次）
SELECT TOP(10) * FROM Consultations;
SELECT * FROM MedicalCases WHERE Id = @p0;  -- 重复10次

-- ✅ Include查询（1次）
SELECT TOP(10) 
    c.*, 
    mc.*
FROM Consultations c
LEFT JOIN MedicalCases mc ON c.Id = mc.Id;
```

### 8.2 AsNoTracking优化

**问题示例**：
```csharp
// ❌ 只读查询使用Change Tracker（浪费内存）
var consultations = await _dbSet
    .Include(c => c.MedicalCase)
    .ToListAsync();  // EF Core追踪所有实体
```

**解决方案**：
```csharp
// ✅ 只读查询使用AsNoTracking（节省内存）
var consultations = await _dbSet
    .AsNoTracking()  // 不追踪实体变更
    .Include(c => c.MedicalCase)
    .ToListAsync();
```

**性能对比**：
```
场景：查询100条Consultation记录

追踪模式（不使用AsNoTracking）：
- 内存占用：~2.5 MB
- 查询时间：~150ms

非追踪模式（使用AsNoTracking）：
- 内存占用：~1.2 MB（节省52%）
- 查询时间：~100ms（提升33%）
```

### 8.3 分页查询优化

**问题示例**：
```csharp
// ❌ 先加载所有数据再分页（性能极差）
var allConsultations = await _dbSet.ToListAsync();  // 加载所有记录
var pagedConsultations = allConsultations
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToList();
```

**解决方案**：
```csharp
// ✅ 在数据库层面分页（性能最优）
var pagedConsultations = await _dbSet
    .AsNoTracking()
    .Include(c => c.MedicalCase)
    .OrderByDescending(c => c.CreatedAt)
    .Skip((pageNumber - 1) * pageSize)  // 在SQL中Skip
    .Take(pageSize)                      // 在SQL中Take
    .ToListAsync();
```

**生成的SQL对比**：
```sql
-- ❌ 先加载所有数据（极差）
SELECT * FROM Consultations;  -- 加载所有记录，例如10,000条
-- 然后在内存中Skip和Take

-- ✅ 数据库层面分页（最优）
SELECT * 
FROM Consultations
ORDER BY CreatedAt DESC
OFFSET @skip ROWS         -- Skip在SQL中执行
FETCH NEXT @take ROWS ONLY;  -- Take在SQL中执行
```

### 8.4 关键字搜索优化

**问题示例**：
```csharp
// ❌ 多次查询数据库（效率低）
var byChiefComplaint = await _dbSet.Where(c => c.ChiefComplaint.Contains(keyword)).ToListAsync();
var byTCMDiagnosis = await _dbSet.Where(c => c.TCMDiagnosis.Contains(keyword)).ToListAsync();
var byPatientName = await _dbSet.Include(c => c.MedicalCase).Where(c => c.MedicalCase.PatientName.Contains(keyword)).ToListAsync();
var results = byChiefComplaint.Concat(byTCMDiagnosis).Concat(byPatientName).Distinct().ToList();
```

**解决方案**：
```csharp
// ✅ 单次查询，使用OR条件（效率高）
var results = await _dbSet
    .AsNoTracking()
    .Include(c => c.MedicalCase)
    .Where(c =>
        (c.ChiefComplaint != null && c.ChiefComplaint.Contains(keyword)) ||
        (c.TCMDiagnosis != null && c.TCMDiagnosis.Contains(keyword)) ||
        c.MedicalCase.PatientName.Contains(keyword) ||
        c.MedicalCase.DoctorName.Contains(keyword))
    .ToListAsync();
```

**生成的SQL对比**：
```sql
-- ❌ 多次查询（效率低）
SELECT * FROM Consultations WHERE ChiefComplaint LIKE @p0;
SELECT * FROM Consultations WHERE TCMDiagnosis LIKE @p0;
SELECT c.*, mc.* FROM Consultations c JOIN MedicalCases mc ON c.Id = mc.Id WHERE mc.PatientName LIKE @p0;

-- ✅ 单次查询，使用OR（效率高）
SELECT c.*, mc.*
FROM Consultations c
LEFT JOIN MedicalCases mc ON c.Id = mc.Id
WHERE 
    (c.ChiefComplaint LIKE @p0 OR 
     c.TCMDiagnosis LIKE @p0 OR 
     mc.PatientName LIKE @p0 OR 
     mc.DoctorName LIKE @p0);
```

### 8.5 索引优化建议

**数据库索引**：
```sql
-- ConsultationEntity推荐索引

-- 1. 主键索引（自动创建）
CREATE UNIQUE CLUSTERED INDEX [PK_Consultations] 
ON [Consultations]([Id]);

-- 2. 外键索引（共享主键，自动创建）
-- Consultation.Id == MedicalCase.Id，不需要额外索引

-- 3. 软删除过滤索引
CREATE NONCLUSTERED INDEX [IX_Consultations_IsDeleted_CreatedAt] 
ON [Consultations]([IsDeleted], [CreatedAt] DESC)
WHERE [IsDeleted] = 0;

-- 4. 关键字搜索索引
CREATE NONCLUSTERED INDEX [IX_Consultations_ChiefComplaint] 
ON [Consultations]([ChiefComplaint])
WHERE [IsDeleted] = 0;

CREATE NONCLUSTERED INDEX [IX_Consultations_TCMDiagnosis] 
ON [Consultations]([TCMDiagnosis])
WHERE [IsDeleted] = 0;

-- 5. 患者查询索引（通过MedicalCase）
-- 已在MedicalCases表上创建 IX_MedicalCases_PatientId
```

---

## 9. 最佳实践

### 9.1 异步优先（Async-First）

**原则**：所有涉及I/O操作的方法都使用async/await。

**正确示例**：
```csharp
// ✅ 异步查询
public async Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id)
{
    var entity = await _repository.GetByIdWithDetailsAsync(id);
    // ...
}

// ✅ 异步验证
var validationResult = await _consultationUpdateValidator.ValidateAsync(consultationDto);

// ✅ 异步更新
await _repository.UpdateAsync(medicalCase);
```

**错误示例**：
```csharp
// ❌ 同步查询（阻塞线程）
public ServiceResult<ConsultationDto> GetById(Guid id)
{
    var entity = _repository.GetByIdWithDetailsAsync(id).Result;  // 阻塞！
    // ...
}

// ❌ 混合同步/异步（性能问题）
public async Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id)
{
    var entity = _repository.GetByIdWithDetailsAsync(id).Result;  // 阻塞！
    return ServiceResult<ConsultationDto>.Success(_mapper.Map<ConsultationDto>(entity));
}
```

### 9.2 ServiceResult<T>统一返回

**原则**：Service层方法统一返回ServiceResult<T>，封装成功/失败状态和错误消息。

**正确示例**：
```csharp
// ✅ 统一返回ServiceResult<T>
public async Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id)
{
    try
    {
        var entity = await _repository.GetByIdWithDetailsAsync(id);
        if (entity == null)
        {
            return ServiceResult<ConsultationDto>.Failure("诊疗记录不存在");
        }

        var dto = _mapper.Map<ConsultationDto>(entity);
        return ServiceResult<ConsultationDto>.Success(dto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取诊疗记录详情失败");
        return ServiceResult<ConsultationDto>.Failure("获取诊疗记录详情失败");
    }
}
```

**Controller调用示例**：
```csharp
public async Task<ActionResult<ConsultationDto>> GetById(Guid id)
{
    var result = await _consultationService.GetByIdAsync(id);
    
    if (!result.Success)
    {
        return NotFound(result.ErrorMessage);  // 统一错误处理
    }
    
    return Ok(result.Data);
}
```

### 9.3 日志记录最佳实践

**原则**：关键操作记录日志，包含上下文信息（如ID、操作类型）。

**正确示例**：
```csharp
public async Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id)
{
    try
    {
        _logger.LogDebug("开始获取诊疗记录，ID: {ConsultationId}", id);
        
        var entity = await _repository.GetByIdWithDetailsAsync(id);
        if (entity == null)
        {
            _logger.LogWarning("诊疗记录不存在，ID: {ConsultationId}", id);
            return ServiceResult<ConsultationDto>.Failure("诊疗记录不存在");
        }

        var dto = _mapper.Map<ConsultationDto>(entity);
        _logger.LogInformation("成功获取诊疗记录，ID: {ConsultationId}", id);
        return ServiceResult<ConsultationDto>.Success(dto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取诊疗记录详情失败，ID: {ConsultationId}", id);
        return ServiceResult<ConsultationDto>.Failure("获取诊疗记录详情失败");
    }
}
```

**日志级别使用**：
```csharp
_logger.LogDebug("调试信息（开发环境）");        // Debug
_logger.LogInformation("正常业务流程");         // Information
_logger.LogWarning("可恢复的异常或警告");       // Warning
_logger.LogError(ex, "业务异常或数据库错误");   // Error
_logger.LogCritical(ex, "系统崩溃级别的错误");  // Critical
```

### 9.4 异常处理最佳实践

**原则**：捕获具体异常，记录详细日志，返回用户友好的错误消息。

**正确示例**：
```csharp
public async Task<ServiceResult<MedicalCaseDto>> UpdateConsultationAsync(
    Guid medicalCaseId,
    ConsultationUpdateDto consultationDto)
{
    try
    {
        // 业务逻辑...
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "数据库更新失败，病案ID: {MedicalCaseId}", medicalCaseId);
        return ServiceResult<MedicalCaseDto>.Failure("数据库更新失败，请稍后重试");
    }
    catch (ValidationException ex)
    {
        _logger.LogWarning(ex, "验证失败，病案ID: {MedicalCaseId}", medicalCaseId);
        return ServiceResult<MedicalCaseDto>.Failure($"验证失败：{ex.Message}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "更新诊疗记录失败，病案ID: {MedicalCaseId}", medicalCaseId);
        return ServiceResult<MedicalCaseDto>.Failure("更新诊疗记录失败");
    }
}
```

**错误示例**：
```csharp
// ❌ 不捕获异常（应用崩溃）
public async Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id)
{
    var entity = await _repository.GetByIdWithDetailsAsync(id);  // 可能抛出异常
    return ServiceResult<ConsultationDto>.Success(_mapper.Map<ConsultationDto>(entity));
}

// ❌ 捕获所有异常但不记录日志
public async Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id)
{
    try
    {
        // ...
    }
    catch (Exception)
    {
        return ServiceResult<ConsultationDto>.Failure("操作失败");  // 没有日志！
    }
}

// ❌ 捕获异常后再次抛出（破坏调用链）
public async Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id)
{
    try
    {
        // ...
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "错误");
        throw;  // 破坏ServiceResult<T>模式
    }
}
```

### 9.5 依赖注入最佳实践

**原则**：使用构造函数注入，避免Service Locator模式。

**正确示例**：
```csharp
public class ConsultationService : IConsultationService
{
    private readonly IConsultationRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<ConsultationService> _logger;

    // ✅ 构造函数注入
    public ConsultationService(
        IConsultationRepository repository,
        IMapper mapper,
        ILogger<ConsultationService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }
}
```

**错误示例**：
```csharp
public class ConsultationService : IConsultationService
{
    private readonly IServiceProvider _serviceProvider;

    // ❌ 使用Service Locator模式
    public ConsultationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id)
    {
        // ❌ 运行时解析依赖（反模式）
        var repository = _serviceProvider.GetRequiredService<IConsultationRepository>();
        var mapper = _serviceProvider.GetRequiredService<IMapper>();
        // ...
    }
}
```

### 9.6 聚合根边界最佳实践

**原则**：所有Write操作必须通过聚合根，保持数据一致性。

**正确示例**：
```csharp
// ✅ 通过MedicalCase聚合根更新Consultation
var medicalCase = await _medicalCaseRepository.GetByIdWithDetailsAsync(medicalCaseId);
medicalCase.Consultation.ChiefComplaint = "新主诉";
await _medicalCaseRepository.UpdateAsync(medicalCase);
```

**错误示例**：
```csharp
// ❌ 直接更新Consultation（违反聚合根原则）
var consultation = await _consultationRepository.GetByIdAsync(id);
consultation.ChiefComplaint = "新主诉";
await _consultationRepository.UpdateAsync(consultation);
```

---

## 10. 常见问题

### 10.1 Q: 为什么ConsultationService只有Read方法？

**A:** 这是Issue #1600 Phase 3的架构调整结果。原因如下：

1. **聚合根原则**：Consultation是MedicalCase聚合根的一部分，所有Write操作必须通过聚合根保证数据一致性。

2. **共享主键设计**：Consultation.Id == MedicalCase.Id，它们是紧密耦合的一对一关系。

3. **业务规则集中管理**：在MedicalCaseService中统一管理业务规则（如Step1完成前不能创建处方）。

**正确的Write操作流程**：
```csharp
// ✅ 通过MedicalCaseService聚合根
await _medicalCaseService.UpdateConsultationAsync(medicalCaseId, consultationDto);
await _medicalCaseService.CompleteStep1Async(medicalCaseId);

// ❌ 直接调用ConsultationService（这些方法已移除）
// await _consultationService.CreateAsync(consultationDto);  // 已移除
// await _consultationService.UpdateAsync(id, consultationDto);  // 已移除
```

### 10.2 Q: 如何解决N+1查询问题？

**A:** 使用Include策略预加载导航属性。

**问题场景**：
```csharp
// ❌ 不使用Include会导致N+1查询
var consultations = await _dbSet.Take(10).ToListAsync();  // 1次查询
foreach (var c in consultations)
{
    var patientName = c.MedicalCase.PatientName;  // 10次额外查询
}
```

**解决方案**：
```csharp
// ✅ 使用Include预加载
var consultations = await _dbSet
    .Include(c => c.MedicalCase)  // 预加载MedicalCase
    .Take(10)
    .ToListAsync();  // 只执行1次查询（使用JOIN）

foreach (var c in consultations)
{
    var patientName = c.MedicalCase.PatientName;  // 0次额外查询
}
```

### 10.3 Q: 为什么PatientName和DoctorName需要手动设置？

**A:** 因为这些字段不存储在ConsultationEntity中，而是从MedicalCase导航属性获取。

**数据模型**：
```csharp
public class ConsultationEntity
{
    public Guid Id { get; set; }  // 共享主键（== MedicalCase.Id）
    public string ChiefComplaint { get; set; }
    // ... 其他诊疗字段

    // ⚡ 导航属性
    public MedicalCaseEntity MedicalCase { get; set; }  // 包含PatientName和DoctorName
}

public class MedicalCaseEntity
{
    public Guid Id { get; set; }
    public string PatientName { get; set; }  // 存储在这里
    public string DoctorName { get; set; }   // 存储在这里
    
    public ConsultationEntity Consultation { get; set; }
}
```

**Service层处理**：
```csharp
public async Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id)
{
    // 1️⃣ 使用Include预加载MedicalCase
    var entity = await _repository.GetByIdWithDetailsAsync(id);
    
    // 2️⃣ AutoMapper映射（PatientName和DoctorName被忽略）
    var dto = _mapper.Map<ConsultationDto>(entity);
    
    // 3️⃣ 手动设置PatientName和DoctorName（从预加载的导航属性）
    dto.PatientName = entity.MedicalCase?.PatientName ?? string.Empty;
    dto.DoctorName = entity.MedicalCase?.DoctorName ?? string.Empty;
    
    return ServiceResult<ConsultationDto>.Success(dto);
}
```

### 10.4 Q: 如何实现Partial Update？

**A:** 使用AutoMapper的条件映射 + FluentValidation的条件验证。

**AutoMapper配置**：
```csharp
CreateMap<ConsultationUpdateDto, ConsultationEntity>()
    // ⚡ 只映射非null字段
    .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
```

**FluentValidation配置**：
```csharp
public class ConsultationUpdateDtoValidator : AbstractValidator<ConsultationUpdateDto>
{
    public ConsultationUpdateDtoValidator()
    {
        // ⚡ 只验证非null字段
        RuleFor(x => x.ChiefComplaint)
            .NotEmpty().WithMessage("主诉不能为空")
            .MaximumLength(500).WithMessage("主诉不能超过500个字符")
            .When(x => x.ChiefComplaint != null);  // 条件验证
    }
}
```

**前端发送示例**：
```json
// 只发送变更的字段
{
  "chiefComplaint": "新主诉",
  "tcmDiagnosis": "新诊断"
  // 其他字段为null，不更新
}
```

**Service层处理**：
```csharp
public async Task<ServiceResult<MedicalCaseDto>> UpdateConsultationAsync(
    Guid medicalCaseId,
    ConsultationUpdateDto consultationDto)
{
    var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
    
    // ⚡ Partial Update：只更新非null字段
    _mapper.Map(consultationDto, medicalCase.Consultation);
    
    // 原有字段保持不变
    await _repository.UpdateAsync(medicalCase);
    
    return ServiceResult<MedicalCaseDto>.Success(_mapper.Map<MedicalCaseDto>(medicalCase));
}
```

### 10.5 Q: 如何确保Step1完成后才能创建处方？

**A:** 通过MedicalCase聚合根的业务规则验证。

**MedicalCaseService中的业务规则**：
```csharp
public async Task<ServiceResult<MedicalCaseDto>> CreatePrescriptionAsync(
    Guid medicalCaseId,
    PrescriptionCreateDto prescriptionDto)
{
    var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
    
    // 1️⃣ 验证Step1是否完成
    if (medicalCase.Consultation == null || 
        !medicalCase.Consultation.Step1CompletedAt.HasValue)
    {
        return ServiceResult<MedicalCaseDto>.Failure("请先完成辨证阶段（Step1）");
    }
    
    // 2️⃣ 验证处方功能是否启用
    if (!medicalCase.Consultation.PrescriptionEnabled)
    {
        return ServiceResult<MedicalCaseDto>.Failure("处方功能未启用");
    }
    
    // 3️⃣ 创建处方
    // ...
}
```

**CompleteStep1方法中启用处方功能**：
```csharp
public async Task<ServiceResult<MedicalCaseDto>> CompleteStep1Async(Guid medicalCaseId)
{
    var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
    
    // 标记Step1完成
    medicalCase.Consultation.Step1CompletedAt = DateTime.UtcNow;
    
    // ⚡ 启用处方功能
    medicalCase.Consultation.PrescriptionEnabled = true;
    
    await _repository.UpdateAsync(medicalCase);
    
    return ServiceResult<MedicalCaseDto>.Success(_mapper.Map<MedicalCaseDto>(medicalCase));
}
```

---

## 11. 测试指南

### 11.1 单元测试（Service层）

**测试类**：`LYBT.Module.Consultation.Tests/Services/ConsultationServiceTests.cs`

```csharp
using AutoMapper;
using FluentAssertions;
using LYBT.Module.Consultation.Entities;
using LYBT.Module.Consultation.Repositories;
using LYBT.Module.Consultation.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Module.Consultation.Tests.Services;

public class ConsultationServiceTests
{
    private readonly IConsultationRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<ConsultationService> _logger;
    private readonly ConsultationService _service;

    public ConsultationServiceTests()
    {
        _repository = Substitute.For<IConsultationRepository>();
        _mapper = Substitute.For<IMapper>();
        _logger = Substitute.For<ILogger<ConsultationService>>();
        _service = new ConsultationService(_repository, _mapper, _logger);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnSuccess_WhenEntityExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity = new ConsultationEntity
        {
            Id = id,
            ChiefComplaint = "头痛",
            TCMDiagnosis = "风寒感冒",
            MedicalCase = new MedicalCaseEntity
            {
                Id = id,
                PatientName = "张三",
                DoctorName = "李医生"
            }
        };

        var dto = new ConsultationDto
        {
            Id = id,
            ChiefComplaint = "头痛",
            TCMDiagnosis = "风寒感冒"
        };

        _repository.GetByIdWithDetailsAsync(id).Returns(entity);
        _mapper.Map<ConsultationDto>(entity).Returns(dto);

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Id.Should().Be(id);
        result.Data.PatientName.Should().Be("张三");
        result.Data.DoctorName.Should().Be("李医生");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFailure_WhenEntityNotExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repository.GetByIdWithDetailsAsync(id).Returns((ConsultationEntity)null);

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("诊疗记录不存在");
    }

    [Fact]
    public async Task GetByMedicalCaseIdAsync_ShouldReturnEmptyList_WhenConsultationNotExists()
    {
        // Arrange
        var medicalCaseId = Guid.NewGuid();
        _repository.GetByMedicalCaseIdAsync(medicalCaseId).Returns((ConsultationEntity)null);

        // Act
        var result = await _service.GetByMedicalCaseIdAsync(medicalCaseId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }
}
```

### 11.2 集成测试（Repository层）

**测试类**：`LYBT.Module.Consultation.Tests/Repositories/ConsultationRepositoryTests.cs`

```csharp
using FluentAssertions;
using LYBT.Module.Consultation.Entities;
using LYBT.Module.Consultation.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LYBT.Module.Consultation.Tests.Repositories;

public class ConsultationRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ConsultationRepository _repository;

    public ConsultationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new ConsultationRepository(_context);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_ShouldIncludeMedicalCase()
    {
        // Arrange
        var medicalCaseId = Guid.NewGuid();
        var medicalCase = new MedicalCaseEntity
        {
            Id = medicalCaseId,
            PatientId = Guid.NewGuid(),
            PatientName = "张三",
            DoctorName = "李医生",
            Status = MedicalCaseStatus.Draft
        };

        var consultation = new ConsultationEntity
        {
            Id = medicalCaseId,  // 共享主键
            ChiefComplaint = "头痛",
            TCMDiagnosis = "风寒感冒",
            MedicalCase = medicalCase
        };

        _context.MedicalCases.Add(medicalCase);
        _context.Consultations.Add(consultation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdWithDetailsAsync(medicalCaseId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(medicalCaseId);
        result.MedicalCase.Should().NotBeNull();
        result.MedicalCase.PatientName.Should().Be("张三");
        result.MedicalCase.DoctorName.Should().Be("李医生");
    }

    [Fact]
    public async Task GetPagedWithDetailsAsync_ShouldSupportKeywordSearch()
    {
        // Arrange
        var medicalCaseId1 = Guid.NewGuid();
        var medicalCase1 = new MedicalCaseEntity
        {
            Id = medicalCaseId1,
            PatientId = Guid.NewGuid(),
            PatientName = "张三",
            DoctorName = "李医生",
            Status = MedicalCaseStatus.Draft
        };

        var consultation1 = new ConsultationEntity
        {
            Id = medicalCaseId1,
            ChiefComplaint = "头痛发热",
            TCMDiagnosis = "风寒感冒",
            MedicalCase = medicalCase1
        };

        _context.MedicalCases.Add(medicalCase1);
        _context.Consultations.Add(consultation1);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPagedWithDetailsAsync(1, 10, "头痛");

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].ChiefComplaint.Should().Contain("头痛");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

### 11.3 API测试（Controller层）

**测试类**：`LYBT.WebAPI.Tests/Controllers/ConsultationControllerTests.cs`

```csharp
using FluentAssertions;
using LYBT.Core.Shared.Results;
using LYBT.Module.Consultation.Contracts.DTOs;
using LYBT.Module.Consultation.Services;
using LYBT.WebAPI.Controllers;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace LYBT.WebAPI.Tests.Controllers;

public class ConsultationControllerTests
{
    private readonly IConsultationService _consultationService;
    private readonly ConsultationController _controller;

    public ConsultationControllerTests()
    {
        _consultationService = Substitute.For<IConsultationService>();
        _controller = new ConsultationController(_consultationService);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenConsultationExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new ConsultationDto
        {
            Id = id,
            ChiefComplaint = "头痛",
            TCMDiagnosis = "风寒感冒",
            PatientName = "张三",
            DoctorName = "李医生"
        };

        _consultationService.GetByIdAsync(id)
            .Returns(ServiceResult<ConsultationDto>.Success(dto));

        // Act
        var result = await _controller.GetById(id);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenConsultationNotExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        _consultationService.GetByIdAsync(id)
            .Returns(ServiceResult<ConsultationDto>.Failure("诊疗记录不存在"));

        // Act
        var result = await _controller.GetById(id);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }
}
```

---

## 12. 调试技巧

### 12.1 SQL日志查看

**启用EF Core SQL日志**（appsettings.Development.json）：
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

**查看生成的SQL**：
```csharp
// 启用日志后，控制台会输出SQL
var consultations = await _dbSet
    .Include(c => c.MedicalCase)
    .ToListAsync();

// 控制台输出：
// Executed DbCommand (10ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
// SELECT c.*, mc.*
// FROM Consultations c
// LEFT JOIN MedicalCases mc ON c.Id = mc.Id
// WHERE c.IsDeleted = 0
```

### 12.2 断点调试技巧

**关键断点位置**：
```csharp
public async Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id)
{
    try
    {
        // 断点1：检查输入参数
        var entity = await _repository.GetByIdWithDetailsAsync(id);
        
        // 断点2：检查查询结果
        if (entity == null)
        {
            return ServiceResult<ConsultationDto>.Failure("诊疗记录不存在");
        }

        var dto = _mapper.Map<ConsultationDto>(entity);
        
        // 断点3：检查映射结果
        dto.PatientName = entity.MedicalCase?.PatientName ?? string.Empty;
        
        // 断点4：检查最终结果
        return ServiceResult<ConsultationDto>.Success(dto);
    }
    catch (Exception ex)
    {
        // 断点5：检查异常信息
        _logger.LogError(ex, "获取诊疗记录详情失败");
        return ServiceResult<ConsultationDto>.Failure("获取诊疗记录详情失败");
    }
}
```

### 12.3 性能分析

**使用MiniProfiler分析SQL查询**：
```csharp
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddMiniProfiler(options =>
    {
        options.RouteBasePath = "/profiler";
    }).AddEntityFramework();
}

public void Configure(IApplicationBuilder app)
{
    app.UseMiniProfiler();
}
```

**访问性能分析页面**：`https://localhost:5001/profiler/results`

### 12.4 常见问题排查

**问题1：PatientName/DoctorName为空**

**排查步骤**：
```csharp
// 1. 检查Repository是否使用Include
var entity = await _repository.GetByIdWithDetailsAsync(id);

// 2. 断点检查导航属性是否已加载
if (entity.MedicalCase == null)
{
    // 问题：没有使用Include预加载
    // 解决：确保Repository方法使用.Include(c => c.MedicalCase)
}

// 3. 检查Service层是否手动设置
dto.PatientName = entity.MedicalCase?.PatientName ?? string.Empty;
```

**问题2：N+1查询问题**

**排查步骤**：
```csharp
// 1. 启用SQL日志（参见12.1）
// 2. 查看控制台输出的SQL数量
// 3. 如果看到多次SELECT语句，说明有N+1查询

// 解决方案：确保使用Include预加载
var consultations = await _dbSet
    .Include(c => c.MedicalCase)  // 添加Include
    .ToListAsync();
```

**问题3：Partial Update不生效**

**排查步骤**：
```csharp
// 1. 检查AutoMapper配置
CreateMap<ConsultationUpdateDto, ConsultationEntity>()
    .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

// 2. 检查前端发送的DTO
// 确保未变更的字段为null，而非空字符串或默认值

// 3. 断点检查映射结果
_mapper.Map(consultationDto, medicalCase.Consultation);
// 查看medicalCase.Consultation的字段是否正确更新
```

---

## 附录

### A. 相关文档

- **架构设计**：[Server端诊疗管理架构设计](../../explanation/architecture/server/consultation-design.md)
- **Client端开发指南**：[Client端诊疗管理开发指南](../client/consultation-development.md)
- **API文档**：[诊疗管理API文档](../../reference/api/consultation-api.md)
- **DTO契约**：`LYBT.Module.Consultation.Contracts/DTOs/`

### B. 关键类型参考

**ConsultationEntity**：
```csharp
public class ConsultationEntity : BaseEntity
{
    public Guid Id { get; set; }  // 共享主键（== MedicalCase.Id）
    public string ChiefComplaint { get; set; }  // 主诉（必填）
    public string? PresentIllness { get; set; }  // 现病史
    public string? Inspection { get; set; }  // 望诊
    public string? AuscultationOlfaction { get; set; }  // 闻诊
    public string? Inquiry { get; set; }  // 问诊
    public string? Palpation { get; set; }  // 切诊
    public string TCMDiagnosis { get; set; }  // 中医诊断（必填）
    public string? TreatmentPrinciple { get; set; }  // 治则治法
    public string? MedicalAdvice { get; set; }  // 医嘱
    
    // 工作流字段
    public DateTime? Step1CompletedAt { get; set; }  // Step1完成时间
    public DateTime? Step2CompletedAt { get; set; }  // Step2完成时间
    public DateTime? Step3CompletedAt { get; set; }  // Step3完成时间
    public bool PrescriptionEnabled { get; set; }  // 处方功能启用
    
    // 导航属性
    public MedicalCaseEntity MedicalCase { get; set; }
}
```

**ConsultationDto**：
```csharp
public class ConsultationDto
{
    public Guid Id { get; set; }
    public string ChiefComplaint { get; set; }
    public string? PresentIllness { get; set; }
    public string? Inspection { get; set; }
    public string? AuscultationOlfaction { get; set; }
    public string? Inquiry { get; set; }
    public string? Palpation { get; set; }
    public string TCMDiagnosis { get; set; }
    public string? TreatmentPrinciple { get; set; }
    public string? MedicalAdvice { get; set; }
    
    // 工作流字段
    public DateTime? Step1CompletedAt { get; set; }
    public DateTime? Step2CompletedAt { get; set; }
    public DateTime? Step3CompletedAt { get; set; }
    public bool PrescriptionEnabled { get; set; }
    
    // 关联信息（从MedicalCase获取）
    public string PatientName { get; set; }  // 患者姓名
    public string DoctorName { get; set; }   // 医生姓名
    
    // 审计字段
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

---

**文档结束** | 如有问题，请参考[架构设计文档](../../explanation/architecture/server/consultation-design.md)或联系Server端开发组
