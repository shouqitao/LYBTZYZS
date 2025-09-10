# HerbCompatNotes 配伍禁忌记录系统 - 设计方案

**设计时间**: 2025-09-09  
**架构师**: 资深.NET架构师  
**设计原则**: MVP记录型功能，不影响处方主流程

## 📋 系统概述

### 功能定位
- **记录型功能**: 医生手动记录配伍禁忌备注，无自动校验逻辑
- **处方关联**: 可选关联到具体处方，也支持独立的配伍知识记录
- **历史查询**: 提供配伍备注的查询和管理功能
- **非阻断性**: 不干预现有处方保存流程

### 核心约束遵循
- ✅ **API路由**: 固定使用 `/api/v1`
- ✅ **命名规范**: 统一使用 `Username` 命名  
- ✅ **架构分层**: `Controller → AppService → Repository (Infra)`
- ✅ **基线遵守**: 严格遵循PRD、.editorconfig、Directory.*配置
- ✅ **存储复用**: 优先复用现有Remark字段，必要时最小新表设计

## 🎯 数据契约设计

### 核心DTO模型

```csharp
/// <summary>
/// 配伍禁忌备注DTO - MVP记录型功能
/// 遵循UltraThink架构标准，继承BaseDto提供ID字段
/// </summary>
public class HerbCompatNoteDto : BaseDto, IAuditable
{
    /// <summary>关联处方ID（可选，支持独立记录）</summary>
    [DisplayName("关联处方ID")]
    public Guid? PrescriptionId { get; set; }

    /// <summary>主药材名称</summary>
    [Required(ErrorMessage = "药材名称不能为空")]
    [StringLength(100, ErrorMessage = "药材名称长度不能超过100个字符")]
    [DisplayName("药材名称")]
    public string HerbName { get; set; } = string.Empty;

    /// <summary>配伍药材名称（可选，用于记录具体的配伍组合）</summary>
    [StringLength(100, ErrorMessage = "配伍药材名称长度不能超过100个字符")]
    [DisplayName("配伍药材")]
    public string? CounterHerbName { get; set; }

    /// <summary>配伍备注内容</summary>
    [Required(ErrorMessage = "备注内容不能为空")]
    [StringLength(1000, ErrorMessage = "备注内容长度不能超过1000个字符")]
    [DisplayName("配伍备注")]
    public string NoteText { get; set; } = string.Empty;

    /// <summary>创建者用户名</summary>
    [Required(ErrorMessage = "创建者用户名不能为空")]
    [StringLength(50, ErrorMessage = "用户名长度不能超过50个字符")]
    [DisplayName("创建者")]
    public string CreatedByUsername { get; set; } = string.Empty;

    /// <summary>创建时间</summary>
    [DisplayName("创建时间")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    /// <summary>更新时间</summary>
    [DisplayName("更新时间")]
    public DateTime? UpdateTime { get; set; }

    /// <summary>关联处方信息（查询时填充，创建时忽略）</summary>
    [DisplayName("处方信息")]
    public string? PrescriptionInfo { get; set; }
}

/// <summary>
/// 配伍备注创建DTO
/// </summary>
public class HerbCompatNoteCreateDto
{
    /// <summary>关联处方ID（可选）</summary>
    [DisplayName("关联处方ID")]
    public Guid? PrescriptionId { get; set; }

    /// <summary>主药材名称</summary>
    [Required(ErrorMessage = "药材名称不能为空")]
    [StringLength(100, ErrorMessage = "药材名称长度不能超过100个字符")]
    [DisplayName("药材名称")]
    public string HerbName { get; set; } = string.Empty;

    /// <summary>配伍药材名称（可选）</summary>
    [StringLength(100, ErrorMessage = "配伍药材名称长度不能超过100个字符")]
    [DisplayName("配伍药材")]
    public string? CounterHerbName { get; set; }

    /// <summary>配伍备注内容</summary>
    [Required(ErrorMessage = "备注内容不能为空")]
    [StringLength(1000, ErrorMessage = "备注内容长度不能超过1000个字符")]
    [DisplayName("配伍备注")]
    public string NoteText { get; set; } = string.Empty;
}

/// <summary>
/// 配伍备注查询参数DTO
/// </summary>
public class HerbCompatNoteQueryDto
{
    /// <summary>关联处方ID</summary>
    [DisplayName("关联处方ID")]
    public Guid? PrescriptionId { get; set; }

    /// <summary>药材名称（支持模糊查询）</summary>
    [DisplayName("药材名称")]
    public string? HerbName { get; set; }

    /// <summary>创建者用户名</summary>
    [DisplayName("创建者")]
    public string? CreatedByUsername { get; set; }

    /// <summary>创建时间起始</summary>
    [DisplayName("创建时间起始")]
    public DateTime? CreateTimeFrom { get; set; }

    /// <summary>创建时间结束</summary>
    [DisplayName("创建时间结束")]
    public DateTime? CreateTimeTo { get; set; }

    /// <summary>页码（默认1）</summary>
    [Range(1, int.MaxValue, ErrorMessage = "页码必须大于0")]
    [DisplayName("页码")]
    public int PageIndex { get; set; } = 1;

    /// <summary>页大小（默认20）</summary>
    [Range(1, 100, ErrorMessage = "页大小必须在1-100之间")]
    [DisplayName("页大小")]
    public int PageSize { get; set; } = 20;
}
```

## 🌐 API端点设计

### RESTful API设计

```csharp
/// <summary>
/// 配伍备注管理控制器
/// 遵循 /api/v1 路由约定，提供标准CRUD操作
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/herb-compat")]
[Authorize]
public class HerbCompatNotesController : BaseApiController
{
    /// <summary>查询配伍备注列表</summary>
    /// <param name="query">查询参数</param>
    /// <returns>分页的配伍备注列表</returns>
    [HttpGet("notes")]
    public async Task<ActionResult<ApiResponse<PagedResult<HerbCompatNoteDto>>>> GetNotesAsync(
        [FromQuery] HerbCompatNoteQueryDto query)
    {
        // 实现: 调用AppService进行分页查询
        // 支持按处方ID、药材名称、创建者等条件筛选
    }

    /// <summary>根据ID获取配伍备注详情</summary>
    /// <param name="id">备注ID</param>
    /// <returns>配伍备注详细信息</returns>
    [HttpGet("notes/{id}")]
    public async Task<ActionResult<ApiResponse<HerbCompatNoteDto>>> GetNoteByIdAsync(Guid id)
    {
        // 实现: 根据ID查询单条记录，包含关联处方信息
    }

    /// <summary>创建配伍备注</summary>
    /// <param name="dto">创建请求</param>
    /// <returns>创建成功的配伍备注</returns>
    [HttpPost("notes")]
    public async Task<ActionResult<ApiResponse<HerbCompatNoteDto>>> CreateNoteAsync(
        [FromBody] HerbCompatNoteCreateDto dto)
    {
        // 实现: 创建新的配伍备注记录
        // 自动填充创建者信息和创建时间
    }

    /// <summary>删除配伍备注</summary>
    /// <param name="id">备注ID</param>
    /// <returns>删除操作结果</returns>
    [HttpDelete("notes/{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteNoteAsync(Guid id)
    {
        // 实现: 逻辑删除或物理删除配伍备注
        // 验证删除权限（只能删除自己创建的记录）
    }

    /// <summary>获取特定处方的配伍备注</summary>
    /// <param name="prescriptionId">处方ID</param>
    /// <returns>该处方的所有配伍备注</returns>
    [HttpGet("prescriptions/{prescriptionId}/notes")]
    public async Task<ActionResult<ApiResponse<List<HerbCompatNoteDto>>>> GetNotesByPrescriptionAsync(
        Guid prescriptionId)
    {
        // 实现: 获取特定处方关联的所有配伍备注
    }
}
```

### API端点清单

| 端点 | 方法 | 描述 | 参数 |
|------|------|------|------|
| `/api/v1/herb-compat/notes` | GET | 分页查询配伍备注 | query parameters |
| `/api/v1/herb-compat/notes/{id}` | GET | 获取单条配伍备注 | id (Guid) |
| `/api/v1/herb-compat/notes` | POST | 创建配伍备注 | HerbCompatNoteCreateDto |
| `/api/v1/herb-compat/notes/{id}` | DELETE | 删除配伍备注 | id (Guid) |
| `/api/v1/herb-compat/prescriptions/{prescriptionId}/notes` | GET | 获取处方关联备注 | prescriptionId (Guid) |

## 🏗️ 架构分层设计

### 分层映射

```
┌─────────────────────────────────────────────────┐
│                Controller Layer                  │
│  HerbCompatNotesController : BaseApiController  │
│  - 参数验证、权限控制、响应格式化               │
└─────────────────┬───────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────┐
│              Application Layer                   │
│  IHerbCompatNoteAppService / HerbCompatNoteAppService │
│  - 业务流程编排、DTO转换、事务管理              │
└─────────────────┬───────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────┐
│            Infrastructure Layer                  │
│  IHerbCompatNoteRepository / HerbCompatNoteRepository │
│  - 数据访问、EF Core集成、查询优化              │
└─────────────────────────────────────────────────┘
```

### 核心服务接口

```csharp
/// <summary>
/// 配伍备注应用服务接口
/// 遵循UltraThink架构标准，提供完整的业务操作
/// </summary>
public interface IHerbCompatNoteAppService
{
    /// <summary>分页查询配伍备注</summary>
    Task<ServiceResult<PagedResult<HerbCompatNoteDto>>> GetNotesAsync(
        HerbCompatNoteQueryDto query, 
        CancellationToken cancellationToken = default);

    /// <summary>根据ID获取配伍备注</summary>
    Task<ServiceResult<HerbCompatNoteDto>> GetNoteByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    /// <summary>创建配伍备注</summary>
    Task<ServiceResult<HerbCompatNoteDto>> CreateNoteAsync(
        HerbCompatNoteCreateDto dto, 
        string currentUsername,
        CancellationToken cancellationToken = default);

    /// <summary>删除配伍备注</summary>
    Task<ServiceResult<bool>> DeleteNoteAsync(
        Guid id, 
        string currentUsername,
        CancellationToken cancellationToken = default);

    /// <summary>获取处方关联的配伍备注</summary>
    Task<ServiceResult<List<HerbCompatNoteDto>>> GetNotesByPrescriptionAsync(
        Guid prescriptionId, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 配伍备注数据访问接口
/// 提供基础的CRUD操作和查询功能
/// </summary>
public interface IHerbCompatNoteRepository
{
    /// <summary>分页查询配伍备注</summary>
    Task<PagedResult<HerbCompatNote>> GetPagedAsync(
        HerbCompatNoteQueryDto query, 
        CancellationToken cancellationToken = default);

    /// <summary>根据ID获取配伍备注</summary>
    Task<HerbCompatNote?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    /// <summary>创建配伍备注</summary>
    Task<HerbCompatNote> CreateAsync(
        HerbCompatNote entity, 
        CancellationToken cancellationToken = default);

    /// <summary>删除配伍备注</summary>
    Task<bool> DeleteAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    /// <summary>根据处方ID获取配伍备注</summary>
    Task<List<HerbCompatNote>> GetByPrescriptionIdAsync(
        Guid prescriptionId, 
        CancellationToken cancellationToken = default);

    /// <summary>检查是否存在重复的配伍备注</summary>
    Task<bool> ExistsDuplicateAsync(
        string herbName, 
        string? counterHerbName, 
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);
}
```

## 💾 数据存储策略

### 优先方案：复用现有Remark字段

**评估结果**: 现有 `Prescriptions` 表包含 `Remark` 字段 (`StringLength(500)`)，但**不适合**复用原因：

1. **语义不符**: Remark用于处方整体备注，配伍备注需要结构化存储
2. **查询复杂**: 无法有效按药材名称索引查询
3. **扩展性差**: 500字符长度限制，无法支持复杂配伍记录
4. **关联困难**: 难以建立药材级别的关联关系

### 推荐方案：最小新表设计

```sql
-- 配伍备注表设计 (最小化方案)
CREATE TABLE [dbo].[HerbCompatibilityNotes] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [PrescriptionId] UNIQUEIDENTIFIER NULL,  -- 可选关联处方
    [HerbName] NVARCHAR(100) NOT NULL,       -- 主药材名称
    [CounterHerbName] NVARCHAR(100) NULL,    -- 配伍药材名称
    [NoteText] NVARCHAR(1000) NOT NULL,      -- 配伍备注内容
    [CreatedByUsername] NVARCHAR(50) NOT NULL, -- 创建者用户名
    [CreateTime] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [UpdateTime] DATETIME2(7) NULL,
    
    -- 主键
    CONSTRAINT [PK_HerbCompatibilityNotes] PRIMARY KEY CLUSTERED ([Id] ASC),
    
    -- 外键约束
    CONSTRAINT [FK_HerbCompatibilityNotes_Prescriptions_PrescriptionId] 
        FOREIGN KEY ([PrescriptionId]) REFERENCES [dbo].[Prescriptions]([Id])
        ON DELETE SET NULL,  -- 处方删除时设置为NULL，保留配伍知识
    
    -- 索引优化
    INDEX [IX_HerbCompatibilityNotes_PrescriptionId] NONCLUSTERED ([PrescriptionId]),
    INDEX [IX_HerbCompatibilityNotes_HerbName] NONCLUSTERED ([HerbName]),
    INDEX [IX_HerbCompatibilityNotes_CreatedBy] NONCLUSTERED ([CreatedByUsername]),
    INDEX [IX_HerbCompatibilityNotes_CreateTime] NONCLUSTERED ([CreateTime] DESC),
    
    -- 复合索引支持常用查询场景
    INDEX [IX_HerbCompatibilityNotes_HerbName_CounterHerbName] 
        NONCLUSTERED ([HerbName], [CounterHerbName])
);
```

### 数据表设计说明

1. **最小字段**: 仅包含核心必要字段，避免过度设计
2. **性能优化**: 针对查询场景设计合理索引
3. **软关联**: 处方删除不影响配伍知识保留
4. **扩展友好**: 字段长度合理，支持后续功能扩展
5. **查询高效**: 支持按药材名称、创建者、时间范围等多维度查询

### Entity模型设计

```csharp
/// <summary>
/// 配伍备注实体模型
/// 对应 HerbCompatibilityNotes 表
/// </summary>
[Table("HerbCompatibilityNotes")]
public class HerbCompatNote
{
    /// <summary>备注ID</summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [DisplayName("备注ID")]
    public Guid Id { get; set; }

    /// <summary>关联处方ID</summary>
    [DisplayName("关联处方ID")]
    public Guid? PrescriptionId { get; set; }

    /// <summary>主药材名称</summary>
    [Required]
    [StringLength(100)]
    [DisplayName("药材名称")]
    public string HerbName { get; set; } = string.Empty;

    /// <summary>配伍药材名称</summary>
    [StringLength(100)]
    [DisplayName("配伍药材")]
    public string? CounterHerbName { get; set; }

    /// <summary>配伍备注内容</summary>
    [Required]
    [StringLength(1000)]
    [DisplayName("配伍备注")]
    public string NoteText { get; set; } = string.Empty;

    /// <summary>创建者用户名</summary>
    [Required]
    [StringLength(50)]
    [DisplayName("创建者")]
    public string CreatedByUsername { get; set; } = string.Empty;

    /// <summary>创建时间</summary>
    [Required]
    [DisplayName("创建时间")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    /// <summary>更新时间</summary>
    [DisplayName("更新时间")]
    public DateTime? UpdateTime { get; set; }

    // 导航属性
    /// <summary>关联的处方信息</summary>
    [ForeignKey("PrescriptionId")]
    public virtual Prescription? Prescription { get; set; }
}
```

## 🔧 技术实现要点

### AutoMapper配置

```csharp
/// <summary>
/// 配伍备注映射配置
/// </summary>
public class HerbCompatNoteMappingProfile : Profile
{
    public HerbCompatNoteMappingProfile()
    {
        // Entity -> DTO
        CreateMap<HerbCompatNote, HerbCompatNoteDto>()
            .ForMember(dest => dest.PrescriptionInfo, opt => opt.MapFrom(src => 
                src.Prescription != null ? $"{src.Prescription.Indication} ({src.Prescription.Id})" : null));

        // CreateDTO -> Entity
        CreateMap<HerbCompatNoteCreateDto, HerbCompatNote>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUsername, opt => opt.Ignore())
            .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
            .ForMember(dest => dest.Prescription, opt => opt.Ignore());
    }
}
```

### 服务注册

```csharp
// 在相应的ServiceCollectionExtensions中添加
public static class HerbCompatNoteServiceExtensions
{
    public static IServiceCollection AddHerbCompatNoteServices(this IServiceCollection services)
    {
        services.AddScoped<IHerbCompatNoteAppService, HerbCompatNoteAppService>();
        services.AddScoped<IHerbCompatNoteRepository, HerbCompatNoteRepository>();
        
        return services;
    }
}
```

## 📊 非功能性需求

### 性能要求
- **查询响应**: ≤500ms（分页查询20条记录）
- **创建响应**: ≤200ms（单条记录创建）
- **并发支持**: 支持10个用户同时操作

### 安全要求
- **权限控制**: 只能查看和删除自己创建的备注
- **输入验证**: 所有输入参数进行格式和长度验证
- **SQL注入防护**: 使用EF Core LINQ查询，避免原生SQL

### 可维护性
- **代码复用**: 遵循现有架构模式，最大化代码复用
- **扩展性**: 预留字段扩展空间，支持后续功能增强
- **测试友好**: 接口隔离，便于单元测试

---

**设计完成**: 2025-09-09  
**下一步**: 查看 `plan.md` 了解具体实施步骤