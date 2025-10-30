# Server端诊疗管理模块架构设计文档

> **文档版本**: v1.0
> **最后更新**: 2025-10-30
> **作者**: LYBT开发团队
> **适用范围**: LYBT诊疗系统 Server端 - 诊疗管理模块

---

## 文档目标

本文档提供LYBT系统Server端**诊疗管理模块（Consultation Module）**的完整架构设计说明，涵盖：

- **模块定位**：作为MedicalCase聚合根的一部分，采用共享主键设计
- **Read-only架构**：所有Write操作通过MedicalCase聚合根，Service层仅提供只读查询
- **数据模型**：Consultation Entity的完整字段定义和关系映射
- **三层架构**：Service层、Repository层、Validator层的职责分离
- **性能优化**：Include策略解决N+1查询问题
- **测试与扩展**：单元测试、集成测试策略及未来扩展方向

适用于Server端开发人员理解诊疗管理模块的架构设计和实施标准。

---

## 目录

1. [模块概述](#1-模块概述)
2. [模块架构](#2-模块架构)
3. [数据模型](#3-数据模型)
4. [Service层设计（Read-only）](#4-service层设计read-only)
5. [Repository层设计](#5-repository层设计)
6. [Validator层设计](#6-validator层设计)
7. [AutoMapper配置](#7-automapper配置)
8. [依赖注入配置](#8-依赖注入配置)
9. [核心设计原则](#9-核心设计原则)
10. [模块集成与使用](#10-模块集成与使用)
11. [测试策略](#11-测试策略)
12. [性能优化](#12-性能优化)
13. [安全性考虑](#13-安全性考虑)
14. [未来扩展](#14-未来扩展)
15. [总结](#15-总结)

---

## 1. 模块概述

### 1.1 模块定位

诊疗管理模块是LYBTZYZS系统的**Server端业务模块**，负责：

- **定位**：作为MedicalCase聚合根的一部分，使用共享主键设计（Consultation.Id == MedicalCase.Id）
- **职责范围**：提供诊疗记录的只读查询功能，所有Write操作必须通过MedicalCase聚合根
- **工作流集成**：作为三步工作流的Step1（辨证），包含Step1/2/3完成时间戳和处方开关控制
- **中医特色**：完整实现中医四诊合参（望闻问切）和辨证论治流程

### 1.2 核心职责

| 职责分类 | 具体功能 | 实现方式 |
|---------|---------|---------|
| **只读查询** | 根据ID/MedicalCaseId/PatientId查询诊疗记录 | ConsultationService + ConsultationRepository |
| **分页查询** | 支持关键字搜索的分页列表 | GetPagedWithDetailsAsync + Include策略 |
| **数据映射** | Entity ↔ DTO转换 | AutoMapper (ConsultationMappingProfile) |
| **输入验证** | DTO验证（Create/Update） | FluentValidation (ConsultationCreateDtoValidator, ConsultationUpdateDtoValidator) |
| **性能优化** | 预加载导航属性，避免N+1查询 | Repository层Include策略 |
| **聚合根约束** | 所有Write操作通过MedicalCase | Service层无Create/Update/Delete方法 |

### 1.3 关键特性

#### Read-only Service设计（Issue #1600 Phase 3）

```csharp
/// <summary>
/// 诊疗服务 - Read Layer（Issue #1600 Phase 3）
/// 职责：提供诊疗记录的只读查询功能
/// 所有Write操作必须通过MedicalCaseService聚合根进行
/// </summary>
public class ConsultationService : IConsultationService
{
    // ✅ 保留Read方法
    public async Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id);
    public async Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId);

    // ❌ Write方法已全部移除
    // CreateAsync, UpdateAsync, DeleteAsync, CompleteStep1Async 已移除
    // 所有写操作必须通过MedicalCase聚合根进行
}
```

#### 共享主键设计

```csharp
public class Consultation : BaseEntity
{
    // Id字段与MedicalCase共享主键
    // 通过EF Core配置建立一对一关系

    [Required]
    public virtual MedicalCase MedicalCase { get; set; } = null!;
}

// EF Core配置（在AppDbContext中）
modelBuilder.Entity<Consultation>()
    .HasOne(c => c.MedicalCase)
    .WithOne(mc => mc.Consultation)
    .HasForeignKey<Consultation>(c => c.Id)  // 共享主键
    .OnDelete(DeleteBehavior.Cascade);
```

#### 三步工作流支持

```csharp
/// <summary>Step1完成时间（辩证）</summary>
public DateTime? Step1CompletedAt { get; set; }

/// <summary>Step2完成时间（施治）</summary>
public DateTime? Step2CompletedAt { get; set; }

/// <summary>Step3完成时间（总结）</summary>
public DateTime? Step3CompletedAt { get; set; }

/// <summary>处方开关（true=开处方，false=不开处方）</summary>
public bool PrescriptionEnabled { get; set; } = true;
```

---

## 2. 模块架构

### 2.1 架构图

```
┌────────────────────────────────────────────────────────────────┐
│                   Consultation Module Architecture              │
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐ │
│  │                   Service Layer (Read-only)               │ │
│  │  ┌───────────────────────────────────────────────────┐   │ │
│  │  │  ConsultationService (IConsultationService)       │   │ │
│  │  │  - GetByIdAsync(Guid id)                          │   │ │
│  │  │  - GetByMedicalCaseIdAsync(Guid medicalCaseId)    │   │ │
│  │  │  ❌ Create/Update/Delete已移除                     │   │ │
│  │  └───────────────────────────────────────────────────┘   │ │
│  └──────────────────────────────────────────────────────────┘ │
│                           ↓                                    │
│  ┌──────────────────────────────────────────────────────────┐ │
│  │                   Repository Layer                        │ │
│  │  ┌───────────────────────────────────────────────────┐   │ │
│  │  │  ConsultationRepository (IConsultationRepository) │   │ │
│  │  │  - GetByPatientIdAsync()                          │   │ │
│  │  │  - GetPagedWithDetailsAsync() ⚡Include策略        │   │ │
│  │  │  - GetByIdWithDetailsAsync() ⚡预加载MedicalCase   │   │ │
│  │  │  - GetByMedicalCaseIdAsync() ⚡共享主键查询        │   │ │
│  │  │  - GetByIdAsync() / GetAllAsync() / FindAsync()   │   │ │
│  │  └───────────────────────────────────────────────────┘   │ │
│  └──────────────────────────────────────────────────────────┘ │
│                           ↓                                    │
│  ┌──────────────────────────────────────────────────────────┐ │
│  │                   Data Layer (Entity)                     │ │
│  │  ┌───────────────────────────────────────────────────┐   │ │
│  │  │  Consultation : BaseEntity                        │   │ │
│  │  │  - 中医四诊：Inspection, Auscultation, Inquiry..  │   │ │
│  │  │  - 辨证论治：TCMDiagnosis, TreatmentPrinciple     │   │ │
│  │  │  - 三步工作流：Step1/2/3CompletedAt              │   │ │
│  │  │  - 共享主键：Id == MedicalCase.Id                │   │ │
│  │  └───────────────────────────────────────────────────┘   │ │
│  └──────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐ │
│  │                   Cross-Cutting Concerns                  │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │ │
│  │  │  Validators  │  │AutoMapper    │  │  DI Config   │   │ │
│  │  │  FluentVal   │  │  Mapping     │  │  Module.cs   │   │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘   │ │
│  └──────────────────────────────────────────────────────────┘ │
│                                                                 │
└────────────────────────────────────────────────────────────────┘

对外接口 (Shared Interface):
  LYBT.Server.Interfaces.Services.IConsultationService

依赖关系:
  Service → Repository → Entity → DbContext
  Service → AutoMapper (Entity ↔ DTO)
  API Controller → Service (注入IConsultationService)
```

### 2.2 代码结构

```
LYBT.Module.Consultation/
├── ConsultationModule.cs                    # 依赖注入配置
├── Services/
│   └── ConsultationService.cs               # 服务实现（Read-only）
├── Repositories/
│   └── ConsultationRepository.cs            # 仓储实现（Include策略）
├── Interfaces/
│   └── IConsultationRepository.cs           # 仓储接口（Read-only）
├── Validators/
│   ├── ConsultationCreateDtoValidator.cs    # 创建验证器
│   └── ConsultationUpdateDtoValidator.cs    # 更新验证器
└── Mapping/
    └── ConsultationMappingProfile.cs        # AutoMapper配置
```

### 2.3 依赖关系

#### 依赖的项目

| 项目名称 | 依赖原因 |
|---------|---------|
| `LYBT.Entities` | 引用Consultation Entity、MedicalCase Entity、BaseEntity |
| `LYBT.Infrastructure` | 使用AppDbContext、BaseRepository、IRepository接口 |
| `LYBT.Shared.Models` | 引用ConsultationDto、ConsultationCreateDto、ConsultationUpdateDto等DTO |
| `LYBT.Server.Interfaces` | 实现IConsultationService接口 |

#### 被依赖项目

| 项目名称 | 使用场景 |
|---------|---------|
| `LYBT.WebAPI` | API Controller注入IConsultationService，提供HTTP端点 |
| `LYBT.Module.MedicalCase` | MedicalCaseService通过Repository写入Consultation数据 |

#### NuGet包依赖

| 包名称 | 版本 | 用途 |
|-------|-----|------|
| `AutoMapper` | 13.x | Entity ↔ DTO自动映射 |
| `FluentValidation` | 11.x | DTO输入验证 |
| `Microsoft.EntityFrameworkCore` | 8.0 | EF Core ORM |
| `Microsoft.Extensions.DependencyInjection` | 8.0 | 依赖注入 |
| `Microsoft.Extensions.Logging` | 8.0 | 日志记录 |

---

## 3. 数据模型

### 3.1 Consultation Entity（完整定义）

位置：`LYBT.Entities/Consultation/ConsultationModel.cs`

```csharp
/// <summary>
/// 诊疗实体 - UltraThink v2.0架构简化版
/// 合并了原BaseConsultation和ConsultationModel
/// 专注于中医诊疗，包含中医四诊和辨证论治
/// 作为MedicalCase的一部分，使用共享主键
/// </summary>
[Table("Consultations")]
public class Consultation : BaseEntity
{
    // ========== 主键设计 ==========
    // Id字段与MedicalCase共享主键
    // 通过EF Core Fluent API配置一对一关系

    // ========== 基本信息 ==========
    // PatientId和UserId通过MedicalCase导航属性获取，不需要重复存储

    /// <summary>主诉</summary>
    [StringLength(500)]
    [DisplayName("主诉")]
    public string? ChiefComplaint { get; set; }

    /// <summary>现病史</summary>
    [StringLength(1000)]
    [DisplayName("现病史")]
    public string? PresentIllness { get; set; }

    // ========== 中医四诊 ==========

    /// <summary>望诊</summary>
    [StringLength(500)]
    [DisplayName("望诊")]
    public string? Inspection { get; set; }

    /// <summary>闻诊</summary>
    [StringLength(500)]
    [DisplayName("闻诊")]
    public string? AuscultationOlfaction { get; set; }

    /// <summary>问诊</summary>
    [StringLength(500)]
    [DisplayName("问诊")]
    public string? Inquiry { get; set; }

    /// <summary>切诊（包含脉诊、舌诊等）</summary>
    [StringLength(500)]
    [DisplayName("切诊")]
    public string? Palpation { get; set; }

    // ========== 中医诊断结果 ==========

    /// <summary>中医辨证</summary>
    [StringLength(500)]
    [DisplayName("中医辨证")]
    public string? TCMDiagnosis { get; set; }

    /// <summary>治疗原则</summary>
    [StringLength(500)]
    [DisplayName("治疗原则")]
    public string? TreatmentPrinciple { get; set; }

    /// <summary>医嘱</summary>
    [StringLength(1000)]
    [DisplayName("医嘱")]
    public string? MedicalAdvice { get; set; }

    // ========== 状态字段 ==========

    /// <summary>状态</summary>
    [DisplayName("状态")]
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;

    /// <summary>备注信息</summary>
    [StringLength(500)]
    [DisplayName("备注")]
    public string? Remark { get; set; }

    // ========== 三步工作流状态字段（Issue #1598）==========

    /// <summary>Step1完成时间（辩证）</summary>
    [DisplayName("Step1完成时间")]
    public DateTime? Step1CompletedAt { get; set; }

    /// <summary>Step2完成时间（施治）</summary>
    [DisplayName("Step2完成时间")]
    public DateTime? Step2CompletedAt { get; set; }

    /// <summary>Step3完成时间（总结）</summary>
    [DisplayName("Step3完成时间")]
    public DateTime? Step3CompletedAt { get; set; }

    /// <summary>处方开关（true=开处方，false=不开处方）</summary>
    [DisplayName("处方开关")]
    public bool PrescriptionEnabled { get; set; } = true;

    // ========== 审计字段（继承自BaseEntity）==========
    // Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, RowVersion, IsDeleted

    // ========== 导航属性 ==========

    /// <summary>
    /// 所属医疗案例（必需的，通过共享主键关联）
    /// </summary>
    [Required]
    public virtual MedicalCase.MedicalCase MedicalCase { get; set; } = null!;
}
```

### 3.2 字段说明

| 字段名 | 类型 | 长度限制 | 必填 | 说明 |
|-------|------|---------|------|------|
| `Id` | Guid | - | ✅ | 主键，与MedicalCase.Id共享 |
| `ChiefComplaint` | string? | 500 | ❌ | 主诉（患者描述的主要症状） |
| `PresentIllness` | string? | 1000 | ❌ | 现病史（患者当前疾病的详细描述） |
| **中医四诊** | - | - | - | - |
| `Inspection` | string? | 500 | ❌ | 望诊（观察面色、舌象等） |
| `AuscultationOlfaction` | string? | 500 | ❌ | 闻诊（听声音、嗅气味） |
| `Inquiry` | string? | 500 | ❌ | 问诊（询问症状、病史） |
| `Palpation` | string? | 500 | ❌ | 切诊（把脉、按腹等） |
| **辨证论治** | - | - | - | - |
| `TCMDiagnosis` | string? | 500 | ❌ | 中医辨证诊断（如"脾虚湿盛"） |
| `TreatmentPrinciple` | string? | 500 | ❌ | 治疗原则（如"健脾化湿"） |
| `MedicalAdvice` | string? | 1000 | ❌ | 医嘱（如"忌生冷、多休息"） |
| **状态字段** | - | - | - | - |
| `Status` | CommonStatus | - | ✅ | 状态（Enabled/Disabled） |
| `Remark` | string? | 500 | ❌ | 备注信息 |
| **三步工作流** | - | - | - | - |
| `Step1CompletedAt` | DateTime? | - | ❌ | Step1完成时间（辩证） |
| `Step2CompletedAt` | DateTime? | - | ❌ | Step2完成时间（施治） |
| `Step3CompletedAt` | DateTime? | - | ❌ | Step3完成时间（总结） |
| `PrescriptionEnabled` | bool | - | ✅ | 处方开关（默认true） |
| **导航属性** | - | - | - | - |
| `MedicalCase` | MedicalCase | - | ✅ | 所属医疗案例（共享主键关联） |

### 3.3 共享主键设计（EF Core配置）

位置：`LYBT.Infrastructure/Data/AppDbContext.cs`（或单独的ConsultationConfiguration）

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Consultation与MedicalCase一对一关系（共享主键）
    modelBuilder.Entity<Consultation>()
        .HasOne(c => c.MedicalCase)
        .WithOne(mc => mc.Consultation)
        .HasForeignKey<Consultation>(c => c.Id)  // 共享主键
        .OnDelete(DeleteBehavior.Cascade);

    // 索引优化
    modelBuilder.Entity<Consultation>()
        .HasIndex(c => c.CreatedAt);
}
```

**共享主键原理**：

1. **数据库层面**：`Consultations.Id` 是主键，同时也是外键指向 `MedicalCases.Id`
2. **EF Core层面**：`HasForeignKey<Consultation>(c => c.Id)` 配置共享主键
3. **查询等价**：`GetByIdAsync(id)` 和 `GetByMedicalCaseIdAsync(id)` 在功能上等价（`c.Id == id`）
4. **级联删除**：删除MedicalCase时，Consultation自动级联删除

---

## 4. Service层设计（Read-only）

### 4.1 ConsultationService完整实现

位置：`LYBT.Module.Consultation/Services/ConsultationService.cs`

```csharp
/// <summary>
/// 诊疗服务 - Read Layer（Issue #1600 Phase 3）
/// 职责：提供诊疗记录的只读查询功能
/// 所有Write操作必须通过MedicalCaseService聚合根进行
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
            // 使用优化后的查询方法，包含所有关联数据
            var entity = await _repository.GetByIdWithDetailsAsync(id);
            if (entity == null)
                return ServiceResult<ConsultationDto>.Failure("诊疗记录不存在");

            var dto = _mapper.Map<ConsultationDto>(entity);
            // 确保PatientName和DoctorName从预加载的导航属性获取
            dto.PatientName = entity.MedicalCase?.PatientName ?? string.Empty;
            dto.DoctorName = entity.MedicalCase?.DoctorName ?? string.Empty;

            return ServiceResult<ConsultationDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取诊疗记录详情失败");
            return ServiceResult<ConsultationDto>.Failure("获取诊疗记录详情失败");
        }
    }

    /// <summary>
    /// 根据医案ID获取诊疗记录
    /// </summary>
    /// <remarks>
    /// ⚠️ 由于共享主键设计（Consultation.Id == MedicalCase.Id），
    /// 此方法实际上等价于GetByIdAsync(medicalCaseId)
    /// </remarks>
    public async Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
    {
        try
        {
            // 使用优化后的查询方法，直接从数据库获取相关记录
            var consultation = await _repository.GetByMedicalCaseIdAsync(medicalCaseId);
            if (consultation == null)
            {
                return ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto>());
            }

            var dto = _mapper.Map<ConsultationDto>(consultation);
            // 确保PatientName和DoctorName从预加载的导航属性获取
            dto.PatientName = consultation.MedicalCase?.PatientName ?? string.Empty;
            dto.DoctorName = consultation.MedicalCase?.DoctorName ?? string.Empty;

            return ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto> { dto });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据医案ID获取诊疗记录失败");
            return ServiceResult<List<ConsultationDto>>.Failure("获取诊疗记录失败");
        }
    }

    // ========== Write方法已全部移除（Issue #1600 Phase 3）==========
    // CreateAsync, UpdateAsync, DeleteAsync, CompleteStep1Async 已移除
    // 所有写操作必须通过MedicalCase聚合根进行
}
```

### 4.2 Service层职责边界

| 职责类型 | ConsultationService | MedicalCaseService |
|---------|---------------------|-------------------|
| **Read操作** | ✅ GetByIdAsync, GetByMedicalCaseIdAsync | ✅ GetAsync（包含Consultation） |
| **Create操作** | ❌ 已移除 | ✅ CreateAsync（同时创建Consultation） |
| **Update操作** | ❌ 已移除 | ✅ UpdateConsultationAsync |
| **Delete操作** | ❌ 已移除 | ✅ DeleteAsync（级联删除Consultation） |
| **CompleteStep1** | ❌ 已移除 | ✅ CompleteStep1Async |

### 4.3 PatientName和DoctorName获取策略

**问题**：ConsultationDto需要PatientName和DoctorName，但Consultation Entity不直接存储这些字段。

**解决方案**：通过预加载的MedicalCase导航属性获取

```csharp
// ✅ 正确方式：从预加载的导航属性获取
dto.PatientName = entity.MedicalCase?.PatientName ?? string.Empty;
dto.DoctorName = entity.MedicalCase?.DoctorName ?? string.Empty;

// ❌ 错误方式：直接查询数据库（会导致N+1问题）
dto.PatientName = await _patientRepository.GetNameByIdAsync(entity.MedicalCase.PatientId);
```

**前提条件**：Repository层必须使用`.Include(c => c.MedicalCase)`预加载导航属性。

---

## 5. Repository层设计

### 5.1 IConsultationRepository接口（Read-only）

位置：`LYBT.Module.Consultation/Interfaces/IConsultationRepository.cs`

```csharp
/// <summary>
/// 诊疗仓储接口 - Read-only版本（Issue #1600 Phase 1）
/// 移除Write方法，所有写操作必须通过MedicalCase聚合根
/// </summary>
public interface IConsultationRepository
{
    /// <summary>根据患者ID获取诊疗记录</summary>
    Task<List<ConsultationEntity>> GetByPatientIdAsync(Guid patientId);

    /// <summary>获取分页列表（包含关联数据）</summary>
    Task<PagedResult<ConsultationEntity>> GetPagedWithDetailsAsync(
        int pageNumber,
        int pageSize,
        string? keyword = null);

    /// <summary>根据ID获取诊疗记录（包含所有关联数据）</summary>
    Task<ConsultationEntity> GetByIdWithDetailsAsync(Guid id);

    /// <summary>根据病案ID获取诊疗记录</summary>
    Task<ConsultationEntity> GetByMedicalCaseIdAsync(Guid medicalCaseId);

    // ========== 基础Read方法（Issue #1600 Phase 1）==========

    /// <summary>根据ID获取实体（基础方法）</summary>
    Task<ConsultationEntity?> GetByIdAsync(Guid id);

    /// <summary>获取所有实体（基础方法）</summary>
    Task<IEnumerable<ConsultationEntity>> GetAllAsync();

    /// <summary>根据条件查找（基础方法）</summary>
    Task<IEnumerable<ConsultationEntity>> FindAsync(
        Expression<Func<ConsultationEntity, bool>> predicate);
}
```

### 5.2 ConsultationRepository实现（Include策略）

位置：`LYBT.Module.Consultation/Repositories/ConsultationRepository.cs`

```csharp
/// <summary>
/// 诊疗仓储 - 优化版，包含Include策略以解决N+1查询问题
/// </summary>
internal class ConsultationRepository : BaseRepository<ConsultationEntity>, IConsultationRepository
{
    public ConsultationRepository(AppDbContext context) : base(context) { }

    public ConsultationRepository(AppDbContext context, ILogger<ConsultationRepository> logger)
        : base(context, logger) { }

    /// <summary>根据患者ID获取诊疗记录</summary>
    public async Task<List<ConsultationEntity>> GetByPatientIdAsync(Guid patientId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(c => c.MedicalCase)  // ⚡预加载医疗案例信息
            .Where(c => c.MedicalCase.PatientId == patientId && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 获取分页列表（包含关联数据）
    /// 优化：预加载Patient和User信息，避免N+1查询
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

        // 关键字搜索
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

    /// <summary>根据ID获取诊疗记录（包含所有关联数据）</summary>
    public async Task<ConsultationEntity> GetByIdWithDetailsAsync(Guid id)
    {
        return (await _dbSet
            .AsNoTracking()
            .Include(c => c.MedicalCase)  // ⚡预加载MedicalCase
            .Where(c => c.Id == id && !c.IsDeleted)
            .FirstOrDefaultAsync())!;
    }

    /// <summary>
    /// 根据病案ID获取诊疗记录
    /// </summary>
    /// <remarks>
    /// ⚠️ 设计说明：由于Consultation采用共享主键设计（Consultation.Id == MedicalCase.Id），
    /// 此方法与GetByIdAsync(medicalCaseId)在功能上等价，查询条件为c.Id == medicalCaseId。
    /// 保留此方法是为了语义清晰，明确表达"通过病案ID查询诊疗记录"的业务意图。
    /// 参见：ConsultationConfiguration.cs的Fluent API配置
    /// </remarks>
    public async Task<ConsultationEntity> GetByMedicalCaseIdAsync(Guid medicalCaseId)
    {
        return (await _dbSet
            .AsNoTracking()
            .Include(c => c.MedicalCase)  // ⚡预加载MedicalCase
            .Where(c => c.Id == medicalCaseId && !c.IsDeleted)  // c.Id == MedicalCase.Id（共享主键）
            .FirstOrDefaultAsync())!;
    }

    // ========== 显式接口实现（Issue #1600 Phase 1）==========
    // 由于BaseRepository返回List<T>,而IConsultationRepository定义返回IEnumerable<T>

    async Task<IEnumerable<ConsultationEntity>> IConsultationRepository.GetAllAsync()
    {
        return await GetAllAsync();
    }

    async Task<IEnumerable<ConsultationEntity>> IConsultationRepository.FindAsync(
        Expression<Func<ConsultationEntity, bool>> predicate)
    {
        return await FindAsync(predicate);
    }
}
```

### 5.3 Include策略说明

#### N+1查询问题示例

```csharp
// ❌ N+1查询问题（反例）
var consultations = await _dbSet.Where(c => !c.IsDeleted).ToListAsync();  // 1次查询
foreach (var c in consultations)
{
    // 每次循环都会触发一次数据库查询
    var patientName = c.MedicalCase.PatientName;  // N次查询
}
// 总计：1 + N 次数据库查询
```

#### 正确的Include策略

```csharp
// ✅ 使用Include预加载（正例）
var consultations = await _dbSet
    .Include(c => c.MedicalCase)  // ⚡一次性预加载所有MedicalCase
    .Where(c => !c.IsDeleted)
    .ToListAsync();  // 只有1次查询（使用SQL JOIN）

foreach (var c in consultations)
{
    var patientName = c.MedicalCase.PatientName;  // 无需额外查询
}
// 总计：1次数据库查询
```

#### Include策略总结

| 方法名 | Include策略 | 说明 |
|-------|------------|------|
| `GetByPatientIdAsync` | `.Include(c => c.MedicalCase)` | 预加载医疗案例信息 |
| `GetPagedWithDetailsAsync` | `.Include(c => c.MedicalCase)` | 预加载病案信息（包含患者和医生） |
| `GetByIdWithDetailsAsync` | `.Include(c => c.MedicalCase)` | 预加载MedicalCase |
| `GetByMedicalCaseIdAsync` | `.Include(c => c.MedicalCase)` | 预加载MedicalCase |
| `GetByIdAsync` | ❌ 无Include | 基础方法，不预加载 |
| `GetAllAsync` | ❌ 无Include | 基础方法，不预加载 |
| `FindAsync` | ❌ 无Include | 基础方法，不预加载 |

---

## 6. Validator层设计

### 6.1 ConsultationCreateDtoValidator

位置：`LYBT.Module.Consultation/Validators/ConsultationCreateDtoValidator.cs`

```csharp
/// <summary>
/// 诊疗创建DTO验证器 - 简化版，只保留必要验证
/// </summary>
public class ConsultationCreateDtoValidator : AbstractValidator<ConsultationCreateDto>
{
    public ConsultationCreateDtoValidator()
    {
        // 只验证患者ID和医生ID必填，其他字段允许为空（四诊信息可以逐步完善）
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("患者ID不能为空");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("医生ID不能为空");

        // 字符长度限制保留，但不强制必填
        RuleFor(x => x.ChiefComplaint)
            .MaximumLength(500).WithMessage("主诉长度不能超过500个字符")
            .When(x => !string.IsNullOrEmpty(x.ChiefComplaint));

        // Issue #1562 Phase 2: 已删除Diagnosis字段验证（Entity中不存在此字段）
    }
}
```

### 6.2 ConsultationUpdateDtoValidator

位置：`LYBT.Module.Consultation/Validators/ConsultationUpdateDtoValidator.cs`

```csharp
/// <summary>
/// 诊疗更新DTO验证器
/// </summary>
public class ConsultationUpdateDtoValidator : AbstractValidator<ConsultationUpdateDto>
{
    public ConsultationUpdateDtoValidator()
    {
        // 诊疗ID必填
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("诊疗ID不能为空");

        // 主诉长度限制（可选）
        RuleFor(x => x.ChiefComplaint)
            .MaximumLength(ValidationConstants.DiagnosisMaxLength)
            .WithMessage($"主诉长度不能超过{ValidationConstants.DiagnosisMaxLength}个字符")
            .When(x => !string.IsNullOrEmpty(x.ChiefComplaint));

        // 现病史长度限制（可选）
        RuleFor(x => x.PresentIllness)
            .MaximumLength(ValidationConstants.LongRemarkMaxLength)
            .WithMessage($"现病史长度不能超过{ValidationConstants.LongRemarkMaxLength}个字符")
            .When(x => !string.IsNullOrEmpty(x.PresentIllness));

        // 望诊结果长度限制（可选）
        RuleFor(x => x.Inspection)
            .MaximumLength(ValidationConstants.DiagnosisMaxLength)
            .WithMessage($"望诊结果长度不能超过{ValidationConstants.DiagnosisMaxLength}个字符")
            .When(x => !string.IsNullOrEmpty(x.Inspection));

        // 闻诊结果长度限制（可选）
        RuleFor(x => x.AuscultationOlfaction)
            .MaximumLength(ValidationConstants.DiagnosisMaxLength)
            .WithMessage($"闻诊结果长度不能超过{ValidationConstants.DiagnosisMaxLength}个字符")
            .When(x => !string.IsNullOrEmpty(x.AuscultationOlfaction));

        // 问诊结果长度限制（可选）
        RuleFor(x => x.Inquiry)
            .MaximumLength(ValidationConstants.DiagnosisMaxLength)
            .WithMessage($"问诊结果长度不能超过{ValidationConstants.DiagnosisMaxLength}个字符")
            .When(x => !string.IsNullOrEmpty(x.Inquiry));

        // 切诊结果长度限制（可选）
        RuleFor(x => x.Palpation)
            .MaximumLength(ValidationConstants.DiagnosisMaxLength)
            .WithMessage($"切诊结果长度不能超过{ValidationConstants.DiagnosisMaxLength}个字符")
            .When(x => !string.IsNullOrEmpty(x.Palpation));

        // 中医诊断长度限制（可选）
        RuleFor(x => x.TCMDiagnosis)
            .MaximumLength(ValidationConstants.DiagnosisMaxLength)
            .WithMessage($"中医诊断长度不能超过{ValidationConstants.DiagnosisMaxLength}个字符")
            .When(x => !string.IsNullOrEmpty(x.TCMDiagnosis));

        // 治疗原则长度限制（可选）
        RuleFor(x => x.TreatmentPrinciple)
            .MaximumLength(ValidationConstants.DiagnosisMaxLength)
            .WithMessage($"治疗原则长度不能超过{ValidationConstants.DiagnosisMaxLength}个字符")
            .When(x => !string.IsNullOrEmpty(x.TreatmentPrinciple));

        // 医嘱长度限制（可选）
        RuleFor(x => x.MedicalAdvice)
            .MaximumLength(ValidationConstants.LongRemarkMaxLength)
            .WithMessage($"医嘱长度不能超过{ValidationConstants.LongRemarkMaxLength}个字符")
            .When(x => !string.IsNullOrEmpty(x.MedicalAdvice));

        // 备注长度限制（可选）
        RuleFor(x => x.Remark)
            .MaximumLength(ValidationConstants.RemarkMaxLength)
            .WithMessage($"备注长度不能超过{ValidationConstants.RemarkMaxLength}个字符")
            .When(x => !string.IsNullOrEmpty(x.Remark));
    }
}
```

### 6.3 验证规则总结

| DTO类型 | 必填字段 | 可选字段长度限制 | 业务规则 |
|--------|---------|----------------|---------|
| **ConsultationCreateDto** | PatientId, UserId | ChiefComplaint ≤500 | 四诊信息可以逐步完善 |
| **ConsultationUpdateDto** | Id | 所有字段长度≤500或1000 | 所有字段都可选，但有长度限制 |

**ValidationConstants常量**：

```csharp
public static class ValidationConstants
{
    public const int DiagnosisMaxLength = 500;        // 诊断相关字段
    public const int LongRemarkMaxLength = 1000;      // 长备注字段
    public const int RemarkMaxLength = 500;           // 短备注字段
}
```

---

## 7. AutoMapper配置

### 7.1 ConsultationMappingProfile完整实现

位置：`LYBT.Module.Consultation/Mapping/ConsultationMappingProfile.cs`

```csharp
/// <summary>
/// 诊疗模块 AutoMapper 映射配置
/// </summary>
public class ConsultationMappingProfile : Profile
{
    public ConsultationMappingProfile()
    {
        // ========== Entity → ConsultationDto ==========
        CreateMap<LYBT.Entities.Consultation.Consultation, ConsultationDto>()
            // PatientId, UserId, PatientName, DoctorName需要手动设置
            .ForMember(dest => dest.PatientId, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.PatientName, opt => opt.Ignore())
            .ForMember(dest => dest.DoctorName, opt => opt.Ignore());

        // ========== ConsultationCreateDto → Entity ==========
        CreateMap<ConsultationCreateDto, LYBT.Entities.Consultation.Consultation>()
            // 设置默认状态为Enabled
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => CommonStatus.Enabled))
            // TCMDiagnosis字段显式映射
            .ForMember(dest => dest.TCMDiagnosis, opt => opt.MapFrom(src => src.TCMDiagnosis))
            // MedicalCase导航属性由EF Core管理
            .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
            // Source字段忽略验证
            .ForSourceMember(src => src.PatientName, opt => opt.DoNotValidate())
            .ForSourceMember(src => src.DoctorName, opt => opt.DoNotValidate())
            // BaseEntity审计字段忽略
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

        // ========== ConsultationUpdateDto → Entity ==========
        CreateMap<ConsultationUpdateDto, LYBT.Entities.Consultation.Consultation>()
            // TCMDiagnosis字段显式映射
            .ForMember(dest => dest.TCMDiagnosis, opt => opt.MapFrom(src => src.TCMDiagnosis))
            // MedicalCase导航属性由EF Core管理
            .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
            // BaseEntity审计字段忽略
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            // 只映射非null字段（Partial Update）
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
```

### 7.2 映射规则说明

#### Entity → ConsultationDto

| 目标字段 | 映射方式 | 说明 |
|---------|---------|------|
| **所有Entity字段** | 自动映射 | 字段名称匹配，自动映射 |
| `PatientId` | 手动设置 | 从`entity.MedicalCase.PatientId`获取 |
| `UserId` | 手动设置 | 从`entity.MedicalCase.UserId`获取 |
| `PatientName` | 手动设置 | 从`entity.MedicalCase.PatientName`获取 |
| `DoctorName` | 手动设置 | 从`entity.MedicalCase.DoctorName`获取 |

**手动设置代码示例**：

```csharp
var dto = _mapper.Map<ConsultationDto>(entity);
// 从预加载的MedicalCase导航属性获取
dto.PatientName = entity.MedicalCase?.PatientName ?? string.Empty;
dto.DoctorName = entity.MedicalCase?.DoctorName ?? string.Empty;
```

#### ConsultationCreateDto → Entity

| 目标字段 | 映射方式 | 说明 |
|---------|---------|------|
| **所有DTO字段** | 自动映射 | 字段名称匹配，自动映射 |
| `Status` | 默认值 | 设置为`CommonStatus.Enabled` |
| `MedicalCase` | 忽略 | 由EF Core管理导航属性 |
| **BaseEntity审计字段** | 忽略 | 由EF Core自动填充 |

#### ConsultationUpdateDto → Entity（Partial Update）

```csharp
.ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
```

**Partial Update行为**：

- ✅ 如果DTO字段为null，不更新Entity字段（保留原值）
- ✅ 如果DTO字段非null，更新Entity字段
- ✅ 支持部分字段更新（前端只传修改的字段）

**示例**：

```csharp
// 前端只传递了ChiefComplaint和TCMDiagnosis
var updateDto = new ConsultationUpdateDto
{
    Id = consultationId,
    ChiefComplaint = "头痛、咳嗽",
    TCMDiagnosis = "风寒感冒",
    PresentIllness = null,  // 不更新
    Inspection = null       // 不更新
};

// AutoMapper会保留PresentIllness和Inspection的原值
_mapper.Map(updateDto, entity);
```

---

## 8. 依赖注入配置

### 8.1 ConsultationModule配置

位置：`LYBT.Module.Consultation/ConsultationModule.cs`

```csharp
/// <summary>
/// 问诊模块服务注册（简化版本）
/// </summary>
public static class ConsultationModule
{
    /// <summary>
    /// 注册问诊模块服务
    /// </summary>
    public static IServiceCollection AddConsultationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ========== 注册仓储 ==========
        services.AddScoped<IConsultationRepository, ConsultationRepository>();

        // ========== 注册服务实现类（统一使用Shared接口）==========
        services.AddScoped<LYBT.Server.Interfaces.Services.IConsultationService, ConsultationService>();

        // ========== 注册验证器 - 自动注册所有Validator ==========
        services.AddValidatorsFromAssemblyContaining<ConsultationCreateDtoValidator>();

        // ========== AutoMapper配置已在UnifiedServiceRegistration中集中注册 ==========

        // ========== 模块无特殊配置需求（通用配置在appsettings.json）==========

        return services;
    }

    /// <summary>
    /// 配置问诊模块中间件（如有需要）
    /// </summary>
    public static IApplicationBuilder UseConsultationModule(this IApplicationBuilder app)
    {
        // 当前无特殊中间件需求
        return app;
    }

    /// <summary>
    /// 验证模块健康状态
    /// </summary>
    public static IHealthChecksBuilder AddConsultationModuleHealthCheck(
        this IHealthChecksBuilder builder)
    {
        // TODO: 待创建健康检查类
        return builder;
    }
}
```

### 8.2 注册流程说明

#### Step 1: 在Program.cs中注册

```csharp
// LYBT.WebAPI/Program.cs
var builder = WebApplication.CreateBuilder(args);

// 注册Consultation模块
builder.Services.AddConsultationModule(builder.Configuration);

// 注册其他模块...
```

#### Step 2: Validator自动注册

```csharp
services.AddValidatorsFromAssemblyContaining<ConsultationCreateDtoValidator>();
```

**效果**：自动注册程序集中所有继承`AbstractValidator<T>`的类。

#### Step 3: AutoMapper集中注册

```csharp
// UnifiedServiceRegistration.cs
services.AddAutoMapper(typeof(ConsultationMappingProfile).Assembly);
```

**效果**：自动扫描并注册所有继承`Profile`的映射配置类。

### 8.3 依赖注入生命周期

| 类型 | 生命周期 | 说明 |
|-----|---------|------|
| `IConsultationRepository` | Scoped | 每个HTTP请求创建一个实例 |
| `IConsultationService` | Scoped | 每个HTTP请求创建一个实例 |
| `IValidator<ConsultationCreateDto>` | Scoped | FluentValidation默认Scoped |
| `IMapper` | Singleton | AutoMapper推荐Singleton |
| `ILogger<ConsultationService>` | Singleton | 日志框架提供 |

---

## 9. 核心设计原则

### 9.1 Read-only Service原则（Aggregate Root Pattern）

#### 核心理念

> **Consultation作为MedicalCase聚合根的一部分，所有Write操作必须通过MedicalCase聚合根进行。**

#### 为什么采用Read-only Service？

**问题背景**：

- **聚合根边界混乱**：Client端可以直接调用`ConsultationRepository.UpdateAsync()`，绕过MedicalCase
- **数据一致性风险**：更新Consultation后，MedicalCase的状态可能不同步
- **业务规则分散**：Step1/2/3完成逻辑分散在多个Service中

**解决方案**（Issue #1600 Phase 3）：

1. **Service层Read-only**：移除ConsultationService的所有Write方法
2. **Write操作集中**：所有写操作通过MedicalCaseService聚合根
3. **Repository层保留Write**：MedicalCaseService通过Repository写入Consultation

#### 职责边界对比

| 操作类型 | ConsultationService | MedicalCaseService |
|---------|---------------------|-------------------|
| **GetByIdAsync** | ✅ 负责 | ✅ 也可调用（包含Consultation） |
| **GetByMedicalCaseIdAsync** | ✅ 负责 | ✅ 也可调用 |
| **CreateAsync** | ❌ 已移除 | ✅ CreateAsync（同时创建Consultation） |
| **UpdateAsync** | ❌ 已移除 | ✅ UpdateConsultationAsync |
| **CompleteStep1Async** | ❌ 已移除 | ✅ CompleteStep1Async |
| **DeleteAsync** | ❌ 已移除 | ✅ DeleteAsync（级联删除Consultation） |

#### 正确的Write操作流程

```csharp
// ❌ 错误方式：直接调用ConsultationService（方法已不存在）
await _consultationService.UpdateAsync(consultationDto);

// ✅ 正确方式：通过MedicalCaseService聚合根
await _medicalCaseService.UpdateConsultationAsync(medicalCaseId, consultationDto);
```

### 9.2 共享主键设计原则

#### 核心理念

> **Consultation与MedicalCase采用共享主键设计（Consultation.Id == MedicalCase.Id），确保一对一关系和数据一致性。**

#### 共享主键的优势

| 优势 | 说明 |
|-----|------|
| **数据一致性强** | Consultation.Id与MedicalCase.Id始终相同，无需同步 |
| **查询简化** | `GetByIdAsync(id)`和`GetByMedicalCaseIdAsync(id)`等价 |
| **级联删除自动** | 删除MedicalCase时，Consultation自动删除 |
| **外键约束强** | 数据库层面保证引用完整性 |

#### 共享主键的EF Core配置

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Consultation与MedicalCase一对一关系（共享主键）
    modelBuilder.Entity<Consultation>()
        .HasOne(c => c.MedicalCase)
        .WithOne(mc => mc.Consultation)
        .HasForeignKey<Consultation>(c => c.Id)  // ⚡共享主键
        .OnDelete(DeleteBehavior.Cascade);
}
```

#### 查询语义等价性

```csharp
// 以下两个方法在功能上等价（因为c.Id == medicalCaseId）
public async Task<ConsultationEntity> GetByIdAsync(Guid id)
{
    return await _dbSet.Where(c => c.Id == id && !c.IsDeleted).FirstOrDefaultAsync();
}

public async Task<ConsultationEntity> GetByMedicalCaseIdAsync(Guid medicalCaseId)
{
    return await _dbSet.Where(c => c.Id == medicalCaseId && !c.IsDeleted).FirstOrDefaultAsync();
    // c.Id == medicalCaseId（因为共享主键）
}
```

**保留`GetByMedicalCaseIdAsync`的原因**：

- ✅ 语义清晰：明确表达"通过病案ID查询诊疗记录"
- ✅ 业务直观：Client端调用更符合业务意图
- ✅ 未来扩展：如果未来改为非共享主键，修改此方法即可

### 9.3 Include策略解决N+1查询

#### N+1查询问题

**问题场景**：查询100条Consultation记录，需要显示PatientName和DoctorName

```csharp
// ❌ N+1查询问题（反例）
var consultations = await _dbSet.Where(c => !c.IsDeleted).ToListAsync();  // 1次查询
foreach (var c in consultations)
{
    var patient = await _patientRepository.GetByIdAsync(c.PatientId);  // N次查询
    var user = await _userRepository.GetByIdAsync(c.UserId);          // N次查询
}
// 总计：1 + N + N = 201次数据库查询
```

#### 正确的Include策略

```csharp
// ✅ 使用Include预加载（正例）
var consultations = await _dbSet
    .Include(c => c.MedicalCase)  // ⚡一次性预加载所有MedicalCase
    .Where(c => !c.IsDeleted)
    .ToListAsync();  // 只有1次查询（使用SQL JOIN）

foreach (var c in consultations)
{
    var patientName = c.MedicalCase.PatientName;  // 无需额外查询
    var doctorName = c.MedicalCase.DoctorName;    // 无需额外查询
}
// 总计：1次数据库查询
```

#### Repository层Include策略标准

**所有`*WithDetailsAsync`方法必须使用Include**：

```csharp
// ✅ GetByIdWithDetailsAsync - 预加载MedicalCase
public async Task<ConsultationEntity> GetByIdWithDetailsAsync(Guid id)
{
    return (await _dbSet
        .AsNoTracking()
        .Include(c => c.MedicalCase)  // ⚡必须Include
        .Where(c => c.Id == id && !c.IsDeleted)
        .FirstOrDefaultAsync())!;
}

// ✅ GetPagedWithDetailsAsync - 预加载MedicalCase
public async Task<PagedResult<ConsultationEntity>> GetPagedWithDetailsAsync(...)
{
    var query = _dbSet
        .AsNoTracking()
        .Include(c => c.MedicalCase)  // ⚡必须Include
        .Where(c => !c.IsDeleted);
    // ...
}
```

### 9.4 三步工作流状态管理（Issue #1598）

#### 工作流状态字段

```csharp
/// <summary>Step1完成时间（辩证）</summary>
public DateTime? Step1CompletedAt { get; set; }

/// <summary>Step2完成时间（施治）</summary>
public DateTime? Step2CompletedAt { get; set; }

/// <summary>Step3完成时间（总结）</summary>
public DateTime? Step3CompletedAt { get; set; }

/// <summary>处方开关（true=开处方，false=不开处方）</summary>
public bool PrescriptionEnabled { get; set; } = true;
```

#### 状态转换规则

**Step1（辨证）→ Step2（施治）**：

- **触发条件**：用户点击"完成辨证"按钮
- **状态变更**：`Step1CompletedAt = DateTime.Now`
- **副作用**：根据`PrescriptionEnabled`决定是否进入处方流程
- **实现位置**：`MedicalCaseService.CompleteStep1Async()`

**Step2（施治）→ Step3（总结）**：

- **触发条件**：用户完成处方录入（或跳过处方）
- **状态变更**：`Step2CompletedAt = DateTime.Now`
- **实现位置**：`MedicalCaseService.CompleteStep2Async()`

**Step3（总结）完成**：

- **触发条件**：用户点击"完成总结"按钮
- **状态变更**：`Step3CompletedAt = DateTime.Now`
- **副作用**：MedicalCase状态变更为"已完成"
- **实现位置**：`MedicalCaseService.CompleteStep3Async()`

#### 处方开关控制逻辑

**PrescriptionEnabled = true**：

- ✅ Step1完成后，进入Step2处方录入流程
- ✅ Client端启用"处方"标签页
- ✅ MedicalCaseFlowViewModel显示处方相关按钮

**PrescriptionEnabled = false**：

- ❌ Step1完成后，跳过处方流程，直接进入Step3
- ❌ Client端隐藏"处方"标签页
- ❌ MedicalCaseFlowViewModel隐藏处方相关按钮

**业务规则**：

```csharp
// 业务规则：DC-004 - 处方开关控制
if (!consultation.PrescriptionEnabled)
{
    // 如果不开处方，Step1完成后自动标记Step2完成
    consultation.Step2CompletedAt = DateTime.Now;
}
```

### 9.5 中医四诊合参数据结构

#### 四诊字段定义

```csharp
// 中医四诊
public string? Inspection { get; set; }            // 望诊
public string? AuscultationOlfaction { get; set; } // 闻诊
public string? Inquiry { get; set; }               // 问诊
public string? Palpation { get; set; }             // 切诊
```

#### 望闻问切实践指南

| 诊法 | 内容 | 示例 |
|-----|------|------|
| **望诊** | 观察面色、舌象、体态等 | "面色苍白，舌淡苔白，体瘦乏力" |
| **闻诊** | 听声音、嗅气味 | "声音低微，语声不清，口气重" |
| **问诊** | 询问症状、病史、生活习惯 | "失眠多梦，大便干燥，喜冷饮" |
| **切诊** | 把脉、按腹、触诊 | "脉细弱无力，腹部柔软，按之不痛" |

#### 辨证论治流程

```
望闻问切（四诊合参） → 中医辨证（TCMDiagnosis） → 治疗原则（TreatmentPrinciple）
```

**示例**：

- **望诊**：面色苍白，舌淡苔白
- **闻诊**：声音低微
- **问诊**：失眠多梦，大便干燥
- **切诊**：脉细弱无力
- **中医辨证**：脾虚湿盛
- **治疗原则**：健脾化湿，益气养血

---

## 10. 模块集成与使用

### 10.1 Program.cs中注册模块

```csharp
// LYBT.WebAPI/Program.cs
var builder = WebApplication.CreateBuilder(args);

// ========== 注册Consultation模块 ==========
builder.Services.AddConsultationModule(builder.Configuration);

// ========== 注册其他模块 ==========
builder.Services.AddMedicalCaseModule(builder.Configuration);
builder.Services.AddPrescriptionModule(builder.Configuration);
// ...

var app = builder.Build();

// ========== 使用Consultation模块中间件 ==========
app.UseConsultationModule();

app.Run();
```

### 10.2 API Controller使用Service

```csharp
[ApiController]
[Route("api/v1/consultations")]
public class ConsultationsController : ControllerBase
{
    private readonly IConsultationService _consultationService;
    private readonly ILogger<ConsultationsController> _logger;

    public ConsultationsController(
        IConsultationService consultationService,
        ILogger<ConsultationsController> logger)
    {
        _consultationService = consultationService;
        _logger = logger;
    }

    /// <summary>
    /// 根据ID获取诊疗记录详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _consultationService.GetByIdAsync(id);
        if (!result.Success)
            return NotFound(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// 根据医案ID获取诊疗记录
    /// </summary>
    [HttpGet("medical-case/{medicalCaseId}")]
    public async Task<IActionResult> GetByMedicalCaseId(Guid medicalCaseId)
    {
        var result = await _consultationService.GetByMedicalCaseIdAsync(medicalCaseId);
        if (!result.Success)
            return NotFound(result.Message);

        return Ok(result.Data);
    }

    // ❌ Create/Update/Delete端点已移除
    // 所有Write操作必须通过MedicalCaseController
}
```

### 10.3 MedicalCaseService调用Repository

```csharp
public class MedicalCaseService : IMedicalCaseService
{
    private readonly IMedicalCaseRepository _medicalCaseRepository;
    private readonly IConsultationRepository _consultationRepository;  // ⚡直接注入Repository

    /// <summary>
    /// 更新诊疗信息（通过聚合根）
    /// </summary>
    public async Task<ServiceResult> UpdateConsultationAsync(
        Guid medicalCaseId,
        ConsultationUpdateDto dto)
    {
        try
        {
            // Step 1: 加载MedicalCase聚合根
            var medicalCase = await _medicalCaseRepository.GetByIdWithDetailsAsync(medicalCaseId);
            if (medicalCase == null)
                return ServiceResult.Failure("医案不存在");

            // Step 2: 直接访问Consultation Repository（聚合根内部操作）
            var consultation = medicalCase.Consultation;
            _mapper.Map(dto, consultation);

            // Step 3: 保存更改
            await _consultationRepository.UpdateAsync(consultation);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新诊疗信息失败");
            return ServiceResult.Failure("更新诊疗信息失败");
        }
    }
}
```

---

## 11. 测试策略

### 11.1 单元测试（ConsultationService）

```csharp
using NSubstitute;
using Xunit;

public class ConsultationServiceTests
{
    private readonly IConsultationRepository _mockRepository;
    private readonly IMapper _mockMapper;
    private readonly ILogger<ConsultationService> _mockLogger;
    private readonly ConsultationService _service;

    public ConsultationServiceTests()
    {
        _mockRepository = Substitute.For<IConsultationRepository>();
        _mockMapper = Substitute.For<IMapper>();
        _mockLogger = Substitute.For<ILogger<ConsultationService>>();
        _service = new ConsultationService(_mockRepository, _mockMapper, _mockLogger);
    }

    [Fact]
    public async Task GetByIdAsync_应返回成功结果_当诊疗记录存在时()
    {
        // Arrange
        var consultationId = Guid.NewGuid();
        var entity = new Consultation
        {
            Id = consultationId,
            ChiefComplaint = "头痛",
            TCMDiagnosis = "风寒感冒",
            MedicalCase = new MedicalCase
            {
                PatientName = "张三",
                DoctorName = "李医生"
            }
        };
        var dto = new ConsultationDto
        {
            Id = consultationId,
            ChiefComplaint = "头痛",
            TCMDiagnosis = "风寒感冒"
        };

        _mockRepository.GetByIdWithDetailsAsync(consultationId).Returns(entity);
        _mockMapper.Map<ConsultationDto>(entity).Returns(dto);

        // Act
        var result = await _service.GetByIdAsync(consultationId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("头痛", result.Data.ChiefComplaint);
        Assert.Equal("张三", result.Data.PatientName);
    }

    [Fact]
    public async Task GetByIdAsync_应返回失败结果_当诊疗记录不存在时()
    {
        // Arrange
        var consultationId = Guid.NewGuid();
        _mockRepository.GetByIdWithDetailsAsync(consultationId).Returns((Consultation)null);

        // Act
        var result = await _service.GetByIdAsync(consultationId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("诊疗记录不存在", result.Message);
    }
}
```

### 11.2 单元测试（ConsultationRepository）

```csharp
public class ConsultationRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ConsultationRepository _repository;

    public ConsultationRepositoryTests()
    {
        // 使用In-Memory数据库
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new ConsultationRepository(_context);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_应预加载MedicalCase导航属性()
    {
        // Arrange
        var medicalCase = new MedicalCase
        {
            Id = Guid.NewGuid(),
            PatientName = "张三",
            DoctorName = "李医生"
        };
        var consultation = new Consultation
        {
            Id = medicalCase.Id,  // 共享主键
            ChiefComplaint = "头痛",
            MedicalCase = medicalCase
        };
        _context.MedicalCases.Add(medicalCase);
        _context.Consultations.Add(consultation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdWithDetailsAsync(consultation.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.MedicalCase);  // ⚡验证导航属性已预加载
        Assert.Equal("张三", result.MedicalCase.PatientName);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
```

### 11.3 集成测试（API端点）

```csharp
public class ConsultationsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ConsultationsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetById_应返回200_当诊疗记录存在时()
    {
        // Arrange
        var consultationId = await CreateTestConsultationAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/consultations/{consultationId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var dto = JsonSerializer.Deserialize<ConsultationDto>(content);
        Assert.NotNull(dto);
    }

    [Fact]
    public async Task GetById_应返回404_当诊疗记录不存在时()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/consultations/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

---

## 12. 性能优化

### 12.1 查询性能优化

#### Include策略（已在5.3章节详细说明）

```csharp
// ✅ 使用Include预加载，避免N+1查询
var consultations = await _dbSet
    .Include(c => c.MedicalCase)  // ⚡预加载MedicalCase
    .Where(c => !c.IsDeleted)
    .ToListAsync();
```

#### AsNoTracking优化

```csharp
// ✅ 只读查询使用AsNoTracking，提升性能
var consultation = await _dbSet
    .AsNoTracking()  // ⚡禁用EF Core变更追踪
    .Include(c => c.MedicalCase)
    .Where(c => c.Id == id && !c.IsDeleted)
    .FirstOrDefaultAsync();
```

**性能提升**：

- ✅ 减少内存占用（无需追踪Entity状态）
- ✅ 提升查询速度（跳过变更检测）
- ⚠️ 注意：使用AsNoTracking后无法Update Entity，必须重新Attach

#### 索引优化

```csharp
// EF Core配置（在AppDbContext或ConsultationConfiguration中）
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // 为CreatedAt创建索引（支持按时间排序）
    modelBuilder.Entity<Consultation>()
        .HasIndex(c => c.CreatedAt);

    // 为Step1CompletedAt创建索引（支持工作流查询）
    modelBuilder.Entity<Consultation>()
        .HasIndex(c => c.Step1CompletedAt);
}
```

### 12.2 分页查询优化

```csharp
/// <summary>
/// 分页查询优化示例
/// </summary>
public async Task<PagedResult<ConsultationEntity>> GetPagedWithDetailsAsync(
    int pageNumber,
    int pageSize,
    string? keyword = null)
{
    var query = _dbSet
        .AsNoTracking()  // ⚡只读查询
        .Include(c => c.MedicalCase)  // ⚡预加载导航属性
        .Where(c => !c.IsDeleted);

    // 关键字搜索
    if (!string.IsNullOrWhiteSpace(keyword))
    {
        query = query.Where(c =>
            (c.ChiefComplaint != null && c.ChiefComplaint.Contains(keyword)) ||
            (c.TCMDiagnosis != null && c.TCMDiagnosis.Contains(keyword)) ||
            c.MedicalCase.PatientName.Contains(keyword) ||
            c.MedicalCase.DoctorName.Contains(keyword));
    }

    // ⚡先计算总数（避免多次查询）
    var totalCount = await query.CountAsync();

    // ⚡分页查询
    var items = await query
        .OrderByDescending(c => c.CreatedAt)  // 索引优化
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
```

### 12.3 缓存策略（未来扩展）

#### Redis缓存（暂不实施，参见Constitution技术黑名单）

```csharp
// ⚠️ 当前禁止使用Redis（参见Constitution约束）
// 未来如果需要缓存，优先考虑IMemoryCache（ASP.NET Core内置）

// ✅ 简单内存缓存示例（可选）
public class ConsultationService : IConsultationService
{
    private readonly IMemoryCache _cache;

    public async Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id)
    {
        // 尝试从缓存获取
        if (_cache.TryGetValue($"consultation:{id}", out ConsultationDto cachedDto))
            return ServiceResult<ConsultationDto>.Success(cachedDto);

        // 从数据库查询
        var entity = await _repository.GetByIdWithDetailsAsync(id);
        var dto = _mapper.Map<ConsultationDto>(entity);

        // 缓存5分钟
        _cache.Set($"consultation:{id}", dto, TimeSpan.FromMinutes(5));

        return ServiceResult<ConsultationDto>.Success(dto);
    }
}
```

---

## 13. 安全性考虑

### 13.1 输入验证（FluentValidation）

#### 长度限制保护

```csharp
RuleFor(x => x.ChiefComplaint)
    .MaximumLength(500).WithMessage("主诉长度不能超过500个字符");

RuleFor(x => x.PresentIllness)
    .MaximumLength(1000).WithMessage("现病史长度不能超过1000个字符");
```

#### SQL注入防护

✅ **EF Core参数化查询**：自动防止SQL注入

```csharp
// ✅ 安全（EF Core自动参数化）
var consultation = await _dbSet
    .Where(c => c.ChiefComplaint.Contains(keyword))  // 参数化
    .ToListAsync();

// ❌ 不安全（字符串拼接SQL）
var sql = $"SELECT * FROM Consultations WHERE ChiefComplaint LIKE '%{keyword}%'";
```

### 13.2 权限控制（API层）

```csharp
[ApiController]
[Route("api/v1/consultations")]
[Authorize]  // ⚡所有端点需要认证
public class ConsultationsController : ControllerBase
{
    /// <summary>
    /// 根据ID获取诊疗记录详情
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "CanViewConsultation")]  // ⚡细粒度权限
    public async Task<IActionResult> GetById(Guid id)
    {
        // 验证用户是否有权访问此诊疗记录
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        // 业务逻辑：验证userId是否为此Consultation的医生或管理员

        var result = await _consultationService.GetByIdAsync(id);
        if (!result.Success)
            return NotFound(result.Message);

        return Ok(result.Data);
    }
}
```

### 13.3 敏感数据处理

#### 日志脱敏

```csharp
// ❌ 不安全（日志包含敏感信息）
_logger.LogInformation($"创建诊疗记录：{JsonSerializer.Serialize(dto)}");

// ✅ 安全（脱敏后记录）
_logger.LogInformation($"创建诊疗记录，患者ID：{dto.PatientId}，医生ID：{dto.UserId}");
```

#### 审计日志

```csharp
// BaseEntity自动记录审计信息
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }      // ⚡创建时间
    public Guid? CreatedBy { get; set; }         // ⚡创建人
    public DateTime? UpdatedAt { get; set; }     // ⚡更新时间
    public Guid? UpdatedBy { get; set; }         // ⚡更新人
    public byte[] RowVersion { get; set; }       // 并发控制
    public bool IsDeleted { get; set; }          // 软删除
}
```

---

## 14. 未来扩展

### 14.1 支持富领域模型（长期目标）

> **参考**：ADR-005 长期架构演进原则

#### 当前状态（贫血模型）

```csharp
// 当前：Entity仅包含数据，无业务逻辑
public class Consultation : BaseEntity
{
    public string? ChiefComplaint { get; set; }
    public string? TCMDiagnosis { get; set; }
    // ...
}

// 业务逻辑在Service层
public class MedicalCaseService
{
    public async Task CompleteStep1Async(Guid medicalCaseId, bool prescriptionEnabled)
    {
        var medicalCase = await _repository.GetByIdAsync(medicalCaseId);
        medicalCase.Consultation.Step1CompletedAt = DateTime.Now;
        medicalCase.Consultation.PrescriptionEnabled = prescriptionEnabled;
        await _repository.UpdateAsync(medicalCase);
    }
}
```

#### 未来演进（富领域模型）

```csharp
// 未来：Entity包含业务逻辑
public class Consultation : BaseEntity
{
    private DateTime? _step1CompletedAt;

    /// <summary>完成Step1（辩证）</summary>
    public void CompleteStep1(bool prescriptionEnabled)
    {
        // ⚡业务规则封装在Entity内部
        if (_step1CompletedAt.HasValue)
            throw new InvalidOperationException("Step1已完成，不能重复完成");

        if (string.IsNullOrWhiteSpace(ChiefComplaint) || string.IsNullOrWhiteSpace(TCMDiagnosis))
            throw new InvalidOperationException("主诉和中医诊断必填");

        _step1CompletedAt = DateTime.Now;
        PrescriptionEnabled = prescriptionEnabled;

        // 如果不开处方，自动标记Step2完成
        if (!prescriptionEnabled)
            _step2CompletedAt = DateTime.Now;
    }
}
```

**演进触发条件**（参见ADR-005）：

- ✅ 业务规则数量 >20条
- ✅ Service方法平均长度 >100行
- ✅ 聚合根内实体关系 >5个
- ✅ 状态转换复杂度 >10个状态

### 14.2 支持领域事件（长期目标）

#### 当前状态（同步调用）

```csharp
// 当前：Service层直接调用其他Service
public async Task CompleteStep1Async(Guid medicalCaseId, bool prescriptionEnabled)
{
    var medicalCase = await _repository.GetByIdAsync(medicalCaseId);
    medicalCase.Consultation.Step1CompletedAt = DateTime.Now;
    await _repository.UpdateAsync(medicalCase);

    // 同步调用其他Service
    await _notificationService.SendNotificationAsync(medicalCase.UserId, "Step1已完成");
}
```

#### 未来演进（领域事件）

```csharp
// 未来：Entity发布领域事件
public class Consultation : BaseEntity
{
    private readonly List<DomainEvent> _domainEvents = new();

    public void CompleteStep1(bool prescriptionEnabled)
    {
        _step1CompletedAt = DateTime.Now;
        PrescriptionEnabled = prescriptionEnabled;

        // ⚡发布领域事件
        _domainEvents.Add(new ConsultationStep1CompletedEvent
        {
            ConsultationId = Id,
            MedicalCaseId = MedicalCase.Id,
            PrescriptionEnabled = prescriptionEnabled
        });
    }
}

// 事件处理器
public class ConsultationStep1CompletedEventHandler : INotificationHandler<ConsultationStep1CompletedEvent>
{
    public async Task Handle(ConsultationStep1CompletedEvent notification, CancellationToken cancellationToken)
    {
        // 异步处理：发送通知
        await _notificationService.SendNotificationAsync(notification.MedicalCaseId, "Step1已完成");
    }
}
```

**演进触发条件**（参见ADR-005）：

- ✅ 跨Service调用 >5次/操作
- ✅ 需要异步解耦（如消息通知）
- ✅ 需要Event Sourcing（审计完整事件流）

### 14.3 支持诊疗模板

#### 功能描述

- ✅ 医生可以创建常用诊疗模板（如"感冒模板"、"腰痛模板"）
- ✅ 模板包含预设的四诊内容、中医诊断、治疗原则
- ✅ 创建诊疗记录时，可以选择模板快速填充

#### 数据模型扩展

```csharp
/// <summary>
/// 诊疗模板（未来扩展）
/// </summary>
public class ConsultationTemplate : BaseEntity
{
    public string Name { get; set; }                 // 模板名称
    public Guid UserId { get; set; }                 // 创建医生

    // 预设内容
    public string? ChiefComplaint { get; set; }
    public string? Inspection { get; set; }
    public string? TCMDiagnosis { get; set; }
    public string? TreatmentPrinciple { get; set; }
}
```

### 14.4 支持语音输入

#### 功能描述

- ✅ 医生使用语音录入四诊内容
- ✅ 自动转换为文本填充到对应字段
- ✅ 提升录入效率

#### 技术方案

- ✅ 使用Azure Speech Service或百度语音识别
- ✅ Client端调用语音识别API
- ✅ 识别结果填充到ConsultationFormViewModel属性

---

## 15. 总结

### 15.1 核心优势

| 优势 | 说明 |
|-----|------|
| **Read-only Service设计** | ConsultationService仅提供只读查询，所有Write操作通过MedicalCase聚合根，确保数据一致性 |
| **共享主键设计** | Consultation.Id == MedicalCase.Id，简化查询逻辑，自动级联删除 |
| **Include策略优化** | 预加载MedicalCase导航属性，避免N+1查询问题，提升性能 |
| **三步工作流支持** | Step1/2/3完成时间戳 + PrescriptionEnabled控制，完整实现中医诊疗流程 |
| **中医四诊合参** | 完整实现望闻问切数据结构，支持辨证论治 |
| **FluentValidation验证** | 输入验证与业务逻辑分离，验证规则清晰可维护 |
| **AutoMapper映射** | Entity ↔ DTO自动映射，支持Partial Update |
| **模块化依赖注入** | ConsultationModule统一管理依赖注入，易于测试和扩展 |

### 15.2 关键技术

- **ASP.NET Core 8.0** - Web框架
- **Entity Framework Core 8.0** - ORM框架
- **AutoMapper 13.x** - 对象映射
- **FluentValidation 11.x** - 输入验证
- **NSubstitute** - 单元测试Mock框架
- **Xunit** - 单元测试框架

### 15.3 文档维护

- **更新频率**：架构调整或重大功能变更时更新
- **维护责任人**：Server端开发人员
- **相关文档**：
  - `docs/architecture/server/README.md` - Server端架构总览
  - `docs/architecture/client/consultation-design.md` - Client端Consultation架构设计
  - `docs/api/consultation-api.md` - Consultation API文档
  - `docs/architecture/decisions/ADR-005-aggregate-root-long-term-architecture.md` - 聚合根长期架构演进决策

---

**文档结束** | **版本**: v1.0 | **最后更新**: 2025-10-30
