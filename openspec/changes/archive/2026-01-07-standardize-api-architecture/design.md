# Design Document: standardize-api-architecture

## Overview

本文档详细描述Server端和Desktop端API架构标准化的**彻底重构**技术设计。

**核心原则**: 不设计兼容模式，一次性完成彻底清理。

## Architecture Diagrams

### 1. 当前架构 vs 目标架构

```
┌─────────────────────────────────────────────────────────────────┐
│                         当前架构                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Server端:                                                       │
│  ┌──────────┐    ┌─────────┐    ┌─────────────┐                │
│  │Controller│───▶│ Service │───▶│IMapper(DI)  │                │
│  └──────────┘    └─────────┘    └─────────────┘                │
│                                        │                         │
│                                        ▼                         │
│                              ┌─────────────────┐                │
│                              │Mapper(Mapperly) │                │
│                              └─────────────────┘                │
│                                                                  │
│  Desktop端:                                                      │
│  ┌──────┐    ┌─────────┐    ┌────────────────┐    ┌─────────┐ │
│  │DTO   │───▶│ Mapper  │───▶│MappingService  │───▶│ Item    │ │
│  └──────┘    │(Mapperly)│    │(手工映射+业务) │    └─────────┘ │
│              └─────────┘    └────────────────┘                  │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘

                              ▼ 重构后 ▼

┌─────────────────────────────────────────────────────────────────┐
│                         目标架构                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Server端:                                                       │
│  ┌──────────┐    ┌─────────┐    ┌─────────────────┐            │
│  │Controller│───▶│ Service │───▶│Mapper(new实例) │            │
│  └──────────┘    └─────────┘    └─────────────────┘            │
│                                                                  │
│  Desktop端:                                                      │
│  ┌──────┐    ┌─────────────────┐    ┌─────────┐                │
│  │DTO   │───▶│  Mapper(扩展)   │───▶│ Item    │                │
│  └──────┘    │(含AfterMap逻辑) │    └─────────┘                │
│              └─────────────────┘                                 │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 2. Desktop端Mapper整合详细设计

```
┌─────────────────────────────────────────────────────────────────┐
│                    MappingService职责分析                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  MappingService当前职责:                                         │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ 1. 纯映射逻辑 (DTO属性 → Item属性)                         │  │
│  │ 2. 计算属性 (如 DisplayText, StatusText)                  │  │
│  │ 3. 集合处理 (如 List<DTO> → ObservableCollection<Item>)   │  │
│  │ 4. 业务规则 (如 权限判断, 状态转换)                        │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                  │
│  职责迁移策略:                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ 1. 纯映射逻辑 ──────────────────▶ Mapper.ToItem()         │  │
│  │ 2. 计算属性   ──────────────────▶ Item属性getter          │  │
│  │ 3. 集合处理   ──────────────────▶ Mapper.ToItems()        │  │
│  │ 4. 业务规则   ──────────────────▶ Service层               │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

## Detailed Design

### 1. Server端Mapper标准化

#### 1.1 当前问题

```csharp
// 问题1: 通过DI注入IMapper接口
public class HerbService
{
    private readonly IMapper _mapper;

    public HerbService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public HerbDetailDto GetById(Guid id)
    {
        var entity = _repository.GetById(id);
        return _mapper.Map<HerbDetailDto>(entity);
    }
}

// 问题2: Module中注册AutoMapper Profile
services.AddAutoMapper(typeof(HerbMappingProfile));
```

#### 1.2 目标实现

```csharp
// 目标: 直接实例化Mapperly Mapper
public class HerbService
{
    private readonly HerbMapper _mapper = new();

    public HerbDetailDto GetById(Guid id)
    {
        var entity = _repository.GetById(id);
        return _mapper.ToDetailDto(entity);
    }
}

// 无需DI注册，Mapperly在编译时生成实现
```

#### 1.3 迁移检查清单

| 模块 | 当前IMapper引用 | Mapperly Mapper | 迁移状态 |
|------|----------------|-----------------|----------|
| Herbs | HerbService | HerbMapper | 待迁移 |
| Users | UserService | UserMapper | 待迁移 |
| Patients | PatientService | PatientMapper | 待迁移 |
| Formula | FormulaService | FormulaMapper | 待迁移 |
| MedicalCase | MedicalCaseCommandService | MedicalCaseMapper | 待迁移 |
| Consultation | ConsultationService | ConsultationMapper | 待迁移 |
| Prescriptions | PrescriptionService | PrescriptionMapper | 待迁移 |

### 2. Desktop端Mapper整合

#### 2.1 Users模块整合

**当前结构**:
```
LYBT.Desktop.Users/
├── Mappers/
│   ├── UserMapper.cs           # Mapperly (DTO↔Item基础映射)
│   └── UserMappingService.cs   # 手工 (集合处理、业务逻辑)
└── Models/Items/
    └── UserItem.cs
```

**目标结构**:
```
LYBT.Desktop.Users/
├── Mappers/
│   └── UserMapper.cs           # Mapperly扩展 (含所有映射逻辑)
└── Models/Items/
    └── UserItem.cs             # 含计算属性
```

**UserMapper扩展**:
```csharp
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class UserMapper
{
    // 基础映射 (Mapperly生成)
    public partial UserItem ToItem(UserDetailDto dto);
    public partial List<UserItem> ToItems(List<UserDetailDto> dtos);
    public partial UserInputDto ToInputDto(UserItem item);

    // AfterMap扩展 (手工实现)
    [MapperIgnoreTarget(nameof(UserItem.RoleDisplayText))]
    [MapperIgnoreTarget(nameof(UserItem.StatusText))]
    private partial UserItem ToItemCore(UserDetailDto dto);

    public UserItem ToItemWithDefaults(UserDetailDto dto)
    {
        var item = ToItemCore(dto);
        item.InitializeComputedProperties(); // 触发计算属性初始化
        return item;
    }
}
```

**UserItem计算属性**:
```csharp
public class UserItem : BindableBase
{
    // 基础属性 (Mapperly映射)
    public Guid Id { get; set; }
    public string Username { get; set; }
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }

    // 计算属性 (getter实现)
    public string RoleDisplayText => Role switch
    {
        UserRole.Admin => "管理员",
        UserRole.Doctor => "医生",
        UserRole.Nurse => "护士",
        _ => "未知"
    };

    public string StatusText => Status switch
    {
        UserStatus.Active => "正常",
        UserStatus.Disabled => "已禁用",
        UserStatus.Deleted => "已删除",
        _ => "未知"
    };

    public bool IsAdmin => Role == UserRole.Admin;
    public bool CanEdit => Status != UserStatus.Deleted;
}
```

#### 2.2 Formula模块整合

**当前结构**:
```
LYBT.Desktop.Formula/
├── Mappers/
│   ├── FormulaMapper.cs                # DTO↔FormulaItem
│   ├── FormulaHerbItemMapper.cs        # DTO↔FormulaHerbItem
│   ├── FormulaDetailModelMapper.cs     # DTO↔FormulaDetailModel
│   ├── FormulaMappingService.cs        # 手工映射
│   └── FormulaDetailModelMappingService.cs
└── Models/
    ├── Items/FormulaItem.cs
    └── FormulaDetailModel.cs           # 重复概念
```

**目标结构**:
```
LYBT.Desktop.Formula/
├── Mappers/
│   └── FormulaMapper.cs                # 统一Mapper
└── Models/Items/
    └── FormulaItem.cs                  # 含HerbItems集合
```

**统一FormulaMapper**:
```csharp
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class FormulaMapper
{
    // 列表映射
    public partial FormulaItem ToItem(FormulaListDto dto);
    public partial List<FormulaItem> ToItems(List<FormulaListDto> dtos);

    // 详情映射 (含嵌套HerbItems)
    public partial FormulaItem ToItem(FormulaDetailDto dto);

    // 子项映射
    public partial FormulaHerbItem ToHerbItem(FormulaHerbDto dto);
    public partial List<FormulaHerbItem> ToHerbItems(List<FormulaHerbDto> dtos);

    // 输入映射
    public partial FormulaInputDto ToInputDto(FormulaItem item);
}
```

#### 2.3 MedicalCase模块整合

**当前结构**:
```
LYBT.Desktop.MedicalCase/
├── Mappers/
│   ├── MedicalCaseItemMapper.cs
│   ├── MedicalCaseDetailModelMapper.cs
│   ├── ConsultationMapper.cs
│   ├── PrescriptionMapper.cs
│   ├── MedicalCaseItemMappingService.cs
│   ├── MedicalCaseDetailModelMappingService.cs
│   ├── ConsultationMappingService.cs
│   └── PrescriptionMappingService.cs
└── Models/
    ├── Items/MedicalCaseItem.cs
    └── MedicalCaseDetailModel.cs
```

**目标结构**:
```
LYBT.Desktop.MedicalCase/
├── Mappers/
│   ├── MedicalCaseMapper.cs    # 医案主体映射
│   └── PrescriptionMapper.cs   # 处方相关映射
└── Models/Items/
    ├── MedicalCaseItem.cs      # 含ConsultationItem+PrescriptionItem
    ├── ConsultationItem.cs
    └── PrescriptionItem.cs
```

### 3. Item命名统一

#### 3.1 重命名映射

| 模块 | 当前命名 | 目标命名 | 引用文件数 |
|------|----------|----------|-----------|
| Formula | FormulaDetailModel | FormulaItem | ~15 |
| MedicalCase | MedicalCaseDetailModel | MedicalCaseItem | ~20 |

#### 3.2 重命名策略

使用IDE重构工具（Rider/VS）执行安全重命名：

1. **FormulaDetailModel → FormulaItem**:
   ```
   - FormulaDetailModel.cs → FormulaItem.cs
   - FormulaDetailModelMapper.cs → (合并到FormulaMapper.cs)
   - FormulaDetailModelMappingService.cs → (删除)
   - 所有引用自动更新
   ```

2. **MedicalCaseDetailModel → MedicalCaseItem**:
   ```
   - MedicalCaseDetailModel.cs → MedicalCaseItem.cs
   - MedicalCaseDetailModelMapper.cs → (合并到MedicalCaseMapper.cs)
   - MedicalCaseDetailModelMappingService.cs → (删除)
   - 所有引用自动更新
   ```

### 4. 错误响应标准化

#### 4.1 ProblemDetails配置

```csharp
// Program.cs / Startup.cs
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Instance = context.HttpContext.Request.Path
            };
            return new BadRequestObjectResult(problemDetails);
        };
    });
```

#### 4.2 统一异常处理

```csharp
// GlobalExceptionHandler.cs
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            ValidationException ex => new ProblemDetails
            {
                Type = "validation-error",
                Title = "Validation Error",
                Status = 400,
                Detail = ex.Message
            },
            NotFoundException ex => new ProblemDetails
            {
                Type = "not-found",
                Title = "Resource Not Found",
                Status = 404,
                Detail = ex.Message
            },
            _ => new ProblemDetails
            {
                Type = "internal-error",
                Title = "Internal Server Error",
                Status = 500,
                Detail = "An unexpected error occurred."
            }
        };

        context.Response.StatusCode = problemDetails.Status ?? 500;
        await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
```

## Testing Strategy

### 1. 单元测试

- 每个Mapper方法的映射正确性
- 计算属性的逻辑正确性
- 边界条件（null值、空集合）

### 2. 集成测试

- API端点响应格式验证
- 错误响应格式验证
- 端到端数据流验证

### 3. 回归测试

- 全量编译验证
- 现有测试套件运行
- 手动功能验证（关键路径）

## Migration Path（彻底重构，无兼容期）

```
┌─────────────────────────────────────────────────────────────────┐
│                   彻底重构执行顺序（无回退）                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Phase 1: Server端彻底清理                                       │
│  ─────────────────────────                                      │
│  Day 1: 移除所有IMapper依赖 + 删除AutoMapper配置                │
│         移除所有MappingProfile类 + 清理NuGet引用                │
│         全量编译验证 + Server端测试                              │
│                                                                  │
│  Phase 2: Desktop端彻底重构                                      │
│  ─────────────────────────                                      │
│  Day 2-3: 删除所有MappingService（不保留任何一个）              │
│           合并所有Mapper为单一文件（每模块1个）                  │
│           计算属性移到Item的getter中                             │
│                                                                  │
│  Phase 3: 命名彻底统一                                           │
│  ───────────────────                                            │
│  Day 4 AM: 删除所有DetailModel类（重命名为Item）                │
│            使用IDE重构工具批量更新引用                           │
│                                                                  │
│  Phase 4: 验证和文档                                             │
│  ─────────────────                                              │
│  Day 4 PM: 全量测试 + 更新文档 + 提交                           │
│                                                                  │
│  **重要**: 每个Phase完成后立即删除旧文件，不保留备份在代码库中   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

## Rollback Plan

每个Phase完成后创建Git tag，支持快速回滚：

```bash
git tag standardize-api-phase1-complete
git tag standardize-api-phase2-complete
git tag standardize-api-phase3-complete
git tag standardize-api-phase4-complete
```

如遇严重问题，可回滚到对应Phase：

```bash
git reset --hard standardize-api-phase{N}-complete
```

---

**Author**: Claude Code
**Created**: 2026-01-07
**Status**: Draft
