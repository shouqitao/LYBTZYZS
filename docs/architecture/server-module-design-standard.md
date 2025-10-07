# Server模块设计标准

> **版本**: 1.1
> **日期**: 2025-10-07
> **关联**: [ADR-003 Server模块统一设计](decisions/ADR-003-server-module-unified-design.md)
> **关联**: [ADR-004 Service接口统一设计标准](decisions/ADR-004-service-interface-unified-design-standard.md)

## 1. 概述

本文档定义LYBT中医诊所系统Server端业务模块的统一设计标准，确保所有模块遵循一致的架构模式、目录结构和服务注册规范。

## 2. 架构原则

### 2.1 三层架构（强制）

所有Server模块必须遵循以下三层架构：

```
Controller → Service → Repository
```

- **Controller层**: 负责HTTP请求处理、路由、参数验证
- **Service层**: 负责业务逻辑实现、事务控制、数据转换
- **Repository层**: 负责数据访问、查询封装、持久化操作

### 2.2 禁止CQRS模式

**严格禁止**在Server模块中使用CQRS（Command Query Responsibility Segregation）模式，包括：

- ❌ 禁止拆分 `IXxxQueryService` 和 `IXxxBusinessService`
- ❌ 禁止拆分 `XxxQueryService` 和 `XxxBusinessService`
- ✅ 必须使用单一 `IXxxService` 接口和 `XxxService` 实现

**违规示例**（禁止）：
```csharp
// ❌ 错误：双层Service接口
services.AddScoped<IConsultationQueryService, ConsultationQueryService>();
services.AddScoped<IConsultationBusinessService, ConsultationBusinessService>();
```

**正确示例**：
```csharp
// ✅ 正确：单一Service接口
services.AddScoped<LYBT.Shared.Interfaces.Services.IConsultationService, ConsultationService>();
```

**理由**: 参考 [`docs/development/standards.md`](../development/standards.md) 第32-39行：
- 小型诊所系统无需CQRS复杂性
- 三层架构已足够满足业务需求
- 避免过度工程导致的维护负担

## 3. 目录结构标准

每个Server模块必须遵循以下目录结构：

```
LYBT.Module.Xxx/
├── Controllers/          # （可选）API控制器
├── Entities/            # （已废弃）实体定义已迁移至LYBT.Entities
├── Interfaces/          # 模块内部接口（仅Repository接口）
│   └── IXxxRepository.cs
├── Mapping/             # AutoMapper映射配置
│   └── XxxMappingProfile.cs
├── Options/             # 模块配置选项
│   └── XxxModuleOptions.cs
├── Repositories/        # 仓储实现
│   └── XxxRepository.cs
├── Services/            # 业务服务实现
│   └── XxxService.cs
├── Validators/          # DTO验证器
│   ├── XxxCreateDtoValidator.cs
│   └── XxxUpdateDtoValidator.cs
└── XxxModule.cs         # 模块服务注册
```

### 3.1 目录职责说明

#### Interfaces/ 目录

**仅存放Repository接口**，Service接口已统一迁移至 `LYBT.Shared.Interfaces.Services`。

**允许的接口**：
```csharp
// ✅ 正确：Repository接口
namespace LYBT.Module.Xxx.Interfaces
{
    public interface IXxxRepository
    {
        Task<XxxEntity> GetByIdAsync(Guid id);
        // ...
    }
}
```

**禁止的接口**：
```csharp
// ❌ 错误：Service接口不应在此目录
namespace LYBT.Module.Xxx.Interfaces
{
    public interface IXxxQueryService { } // 禁止
    public interface IXxxBusinessService { } // 禁止
}
```

#### Repositories/ 目录

**必须存放所有Repository实现类**，禁止放置在其他目录（如Services/）。

**正确放置**：
```
✅ src/Server/Modules/LYBT.Module.Formula/Repositories/FormulaRepository.cs
```

**错误放置**：
```
❌ src/Server/Modules/LYBT.Module.Formula/Services/FormulaRepository.cs
```

## 4. Service 接口统一设计标准

> **参考**: [ADR-004 Service接口统一设计标准](decisions/ADR-004-service-interface-unified-design-standard.md)
> **更新日期**: 2025-10-07

### 4.1 Service接口统一位置

所有Service接口必须定义在 `LYBT.Shared.Interfaces.Services` 命名空间：

```csharp
// 文件位置: src/Shared/LYBT.Shared.Interfaces/Services/IConsultationService.cs
namespace LYBT.Shared.Interfaces.Services
{
    public interface IConsultationService
    {
        // 查询操作
        Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(...);
        Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id);

        // 业务操作
        Task<ServiceResult<ConsultationDto>> CreateAsync(ConsultationCreateDto dto);
        Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationUpdateDto dto);
        Task<ServiceResult> DeleteAsync(Guid id);
    }
}
```

**优势**：
- ✅ Desktop端和Server端共享相同接口契约
- ✅ 避免接口重复定义
- ✅ 简化依赖注入配置

### 4.2 Service 接口设计原则

#### 4.2.1 最小接口原则（ISP）

每个 Service 接口方法数控制在 **6-12 个之间**：

- **下限（6方法）**：标准CRUD（3）+ 查询（2-3）
- **上限（12方法）**：标准CRUD + 查询 + 业务操作（≤5）

**超过 12 个方法**视为过度设计，需重构拆分。

**参考案例**：
- IUserService 重构前：26 方法 ❌（过度设计）
- IUserService 重构后：11 方法 ✅（符合标准）

#### 4.2.2 单一职责原则（SRP）

每个 Service 接口只负责**一个业务实体**的核心操作：

```csharp
// ✅ 正确：用户管理职责
IUserService
{
    CreateAsync, UpdateAsync, DeleteAsync,
    DisableAsync, EnableAsync,
    ChangePasswordAsync, ChangeProfileAsync
}

// ✅ 正确：认证职责
IAuthService
{
    LoginAsync, LogoutAsync,
    ValidateTokenAsync, RefreshTokenAsync,
    ResetPasswordAsync, ChangePasswordAsync
}

// ❌ 错误：职责混合
IUserService
{
    CreateAsync, UpdateAsync, DeleteAsync,
    LoginAsync, ValidateTokenAsync,         // ❌ 应由IAuthService负责
    SaveAuthenticationAsync                  // ❌ Desktop专有方法
}
```

#### 4.2.3 YAGNI 原则（You Aren't Gonna Need It）

MVP 阶段**优先实现核心功能**，非必需功能延后：

```csharp
// ❌ 错误：批量操作在MVP阶段未使用
Task<ServiceResult> BatchEnableAsync(List<Guid> userIds);
Task<ServiceResult> BatchDisableAsync(List<Guid> userIds);

// ❌ 错误：内部逻辑不应暴露为公开方法
Task<ServiceResult<bool>> ValidateUsernameAsync(string username);
Task<ServiceResult<bool>> IsDoctorAvailableAsync(Guid userId);

// ✅ 正确：MVP核心功能
Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto);
Task<ServiceResult> DisableAsync(Guid id);
```

**判断标准**：
- 若某方法在当前MVP需求中**未被调用** → 移除或标记为"后续实现"
- 若某方法仅被**内部逻辑**使用 → 改为 private 或移至 Repository

### 4.3 标准 Service 接口结构

所有 Service 接口必须遵循以下结构（6-12 methods）：

```csharp
namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// {Entity}业务服务接口
    /// </summary>
    public interface I{Entity}Service
    {
        #region 查询操作 (2-4 methods)

        /// <summary>
        /// 分页查询{Entity}列表
        /// </summary>
        /// <param name="page">页码（从1开始）</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="keyword">关键词搜索（可选）</param>
        /// <returns>分页结果</returns>
        Task<ServiceResult<PagedResult<{Entity}Dto>>> GetPagedAsync(
            int page = 1,
            int pageSize = 20,
            string? keyword = null
        );

        /// <summary>
        /// 根据ID查询{Entity}
        /// </summary>
        Task<ServiceResult<{Entity}Dto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 关键词搜索{Entity}（可选）
        /// </summary>
        Task<ServiceResult<List<{Entity}Dto>>> SearchAsync(string keyword);

        #endregion

        #region CRUD 操作 (3 methods)

        /// <summary>
        /// 创建{Entity}
        /// </summary>
        Task<ServiceResult<{Entity}Dto>> CreateAsync(
            {Entity}CreateDto dto,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// 更新{Entity}
        /// </summary>
        Task<ServiceResult<{Entity}Dto>> UpdateAsync(
            Guid id,
            {Entity}UpdateDto dto,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// 删除{Entity}（软删除）
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);

        #endregion

        #region 业务操作 (0-5 methods)

        // Entity-specific business methods
        // 示例：
        // Task<ServiceResult> DisableAsync(Guid id);
        // Task<ServiceResult> EnableAsync(Guid id);
        // Task<ServiceResult> ChangePasswordAsync(Guid id, string oldPassword, string newPassword);

        #endregion
    }
}
```

### 4.4 命名约定

#### 4.4.1 方法命名

统一使用 **动词 + Async** 格式：

| 操作类型 | 标准命名 | 禁止命名 |
|---------|---------|---------|
| 创建 | `CreateAsync` | ❌ CreateUserAsync, AddAsync |
| 更新 | `UpdateAsync` | ❌ UpdateUserAsync, ModifyAsync |
| 删除 | `DeleteAsync` | ❌ DeleteUserAsync, RemoveAsync |
| 查询单个 | `GetByIdAsync` | ❌ FindByIdAsync, GetAsync |
| 分页查询 | `GetPagedAsync` | ❌ GetAllAsync, QueryAsync |
| 关键词搜索 | `SearchAsync` | ❌ FindAsync, QueryAsync |
| 启用/禁用 | `EnableAsync`, `DisableAsync` | ❌ ActivateAsync, DeactivateAsync |

**禁止在方法名中包含实体名称**（接口名已表明实体）：

```csharp
// ❌ 错误：重复实体名
interface IUserService {
    Task CreateUserAsync(UserCreateDto dto);
    Task UpdateUserAsync(Guid id, UserUpdateDto dto);
}

// ✅ 正确：简洁命名
interface IUserService {
    Task CreateAsync(UserCreateDto dto);
    Task UpdateAsync(Guid id, UserUpdateDto dto);
}
```

#### 4.4.2 参数命名

| 参数类型 | 标准命名 | 类型 | 示例 |
|---------|---------|------|------|
| 主键 | `id` | `Guid` | `GetByIdAsync(Guid id)` |
| 创建DTO | `dto` | `{Entity}CreateDto` | `CreateAsync(UserCreateDto dto)` |
| 更新DTO | `dto` | `{Entity}UpdateDto` | `UpdateAsync(Guid id, UserUpdateDto dto)` |
| 分页参数 | `page`, `pageSize` | `int` | `GetPagedAsync(int page, int pageSize)` |
| 关键词 | `keyword` | `string?` | `GetPagedAsync(..., string? keyword = null)` |
| 取消令牌 | `cancellationToken` | `CancellationToken` | `CreateAsync(..., CancellationToken cancellationToken = default)` |

#### 4.4.3 返回类型

| 返回场景 | 标准返回类型 | 禁止返回类型 |
|---------|------------|------------|
| 有数据返回 | `Task<ServiceResult<T>>` | ❌ `Task<T>`, `Task<bool>` |
| 无数据返回 | `Task<ServiceResult>` | ❌ `Task<ServiceResult<bool>>`, `Task` |
| 分页数据 | `Task<ServiceResult<PagedResult<T>>>` | ❌ `Task<ServiceResult<List<T>>>` |

**禁止使用裸类型返回**：

```csharp
// ❌ 错误：裸类型返回
Task<UserDto> GetByIdAsync(Guid id);
Task<bool> DeleteAsync(Guid id);

// ✅ 正确：ServiceResult包装
Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);
Task<ServiceResult> DeleteAsync(Guid id);
```

### 4.5 分页查询标准

所有分页查询必须使用以下统一签名：

```csharp
/// <summary>
/// 分页查询{Entity}列表
/// </summary>
/// <param name="page">页码（从1开始，默认1）</param>
/// <param name="pageSize">每页数量（默认20）</param>
/// <param name="keyword">关键词搜索（可选，支持名称/编号等字段）</param>
/// <returns>分页结果</returns>
Task<ServiceResult<PagedResult<{Entity}Dto>>> GetPagedAsync(
    int page = 1,
    int pageSize = 20,
    string? keyword = null
);
```

**禁止使用复杂 SearchDto** 作为参数（MVP阶段）：

```csharp
// ❌ 错误：MVP阶段过度设计
Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserSearchDto query);

public class UserSearchDto  // 12 fields - 过于复杂
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public string? Keyword { get; set; }
    public string? WuBiCode { get; set; }        // MVP未使用
    public DateTime? StartDate { get; set; }     // MVP未使用
    public DateTime? EndDate { get; set; }       // MVP未使用
    // ... 更多字段
}

// ✅ 正确：MVP阶段简化为关键词搜索
Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(
    int page = 1,
    int pageSize = 20,
    string? keyword = null  // 足够满足基础搜索需求
);
```

**简化原则**：
- MVP 阶段使用 `keyword` 参数进行基础搜索（名称、编号等常见字段）
- 高级筛选（日期范围、多字段组合）在有明确需求时再添加
- 客户端可在获取分页数据后进行二次过滤（小数据量场景）

### 4.6 软删除标准

所有 `DeleteAsync` 方法必须实现**软删除**（更新 `IsDeleted` 字段）：

```csharp
/// <summary>
/// 删除{Entity}（软删除，更新IsDeleted标志）
/// </summary>
/// <param name="id">实体ID</param>
/// <returns>删除结果</returns>
Task<ServiceResult> DeleteAsync(Guid id);
```

**实现要求**：
- 返回类型：`Task<ServiceResult>`（不是 `Task<ServiceResult<bool>>`）
- 操作类型：软删除（设置 `IsDeleted = true`，保留数据）
- 物理删除：如需物理删除，方法名必须明确标注 `DeletePermanentlyAsync`

**示例实现**：

```csharp
public async Task<ServiceResult> DeleteAsync(Guid id)
{
    var entity = await _repository.GetByIdAsync(id);
    if (entity == null)
        return ServiceResult.Failure("实体不存在");

    entity.IsDeleted = true;  // 软删除
    entity.DeletedAt = DateTime.UtcNow;
    await _repository.UpdateAsync(entity);

    return ServiceResult.Success();  // 不返回bool值
}
```

### 4.7 CancellationToken 标准

Create 和 Update 操作必须支持 `CancellationToken`（作为可选参数）：

```csharp
/// <summary>
/// 创建{Entity}
/// </summary>
/// <param name="dto">创建数据传输对象</param>
/// <param name="cancellationToken">取消令牌（可选）</param>
Task<ServiceResult<{Entity}Dto>> CreateAsync(
    {Entity}CreateDto dto,
    CancellationToken cancellationToken = default
);

/// <summary>
/// 更新{Entity}
/// </summary>
/// <param name="id">实体ID</param>
/// <param name="dto">更新数据传输对象</param>
/// <param name="cancellationToken">取消令牌（可选）</param>
Task<ServiceResult<{Entity}Dto>> UpdateAsync(
    Guid id,
    {Entity}UpdateDto dto,
    CancellationToken cancellationToken = default
);
```

**使用场景**：
- HTTP 请求被客户端取消时，终止数据库操作
- 长时间运行的创建/更新操作
- 批量操作的中途取消

**可选支持**：
- 查询操作（GetByIdAsync, GetPagedAsync）可选择性添加
- 短时间操作（DisableAsync, EnableAsync）通常不需要

### 4.8 Repository接口

Repository接口继续保留在各模块的 `Interfaces/` 目录：

```csharp
// 文件位置: src/Server/Modules/LYBT.Module.Xxx/Interfaces/IXxxRepository.cs
namespace LYBT.Module.Xxx.Interfaces
{
    public interface IXxxRepository
    {
        Task<XxxEntity> GetByIdAsync(Guid id);
        Task<List<XxxEntity>> GetAllAsync();
        Task<XxxEntity> AddAsync(XxxEntity entity);
        Task UpdateAsync(XxxEntity entity);
        Task DeleteAsync(Guid id);
    }
}
```

## 5. 服务注册模式

### 5.1 标准注册模板

每个模块的 `XxxModule.cs` 必须遵循以下注册顺序和模式：

```csharp
namespace LYBT.Module.Xxx
{
    public static class XxxModule
    {
        public static IServiceCollection AddXxxModule(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 1. 注册仓储
            services.AddScoped<IXxxRepository, XxxRepository>();

            // 2. 注册服务实现类（统一使用Shared接口）
            services.AddScoped<LYBT.Shared.Interfaces.Services.IXxxService, XxxService>();

            // 3. 注册验证器 - 自动注册所有Validator
            services.AddValidatorsFromAssemblyContaining<XxxCreateDtoValidator>();

            // 4. 注册AutoMapper配置（可选，如已在统一配置中注册则省略）
            services.AddAutoMapper(typeof(XxxMappingProfile));

            // 5. 注册模块特定配置（可选）
            services.AddOptions<XxxModuleOptions>()
                .Bind(configuration.GetSection("Modules:Xxx"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            return services;
        }

        public static IApplicationBuilder UseXxxModule(this IApplicationBuilder app)
        {
            // 配置中间件（如有需要）
            return app;
        }
    }
}
```

### 5.2 服务注册关键点

#### 禁止的注册方式
```csharp
// ❌ 错误：注册模块内部Query/Business接口
services.AddScoped<IXxxQueryService, XxxQueryService>();
services.AddScoped<IXxxBusinessService, XxxBusinessService>();
```

#### 正确的注册方式
```csharp
// ✅ 正确：注册Shared统一接口
services.AddScoped<LYBT.Shared.Interfaces.Services.IXxxService, XxxService>();
```

### 5.3 AutoMapper注册说明

**标准做法**：AutoMapper已在`UnifiedServiceRegistration.cs`中集中注册

```csharp
// UnifiedServiceRegistration.cs - 自动扫描所有LYBT.开头的程序集
var assemblies = AppDomain.CurrentDomain.GetAssemblies()
    .Where(a => a.GetName().Name?.StartsWith("LYBT.") == true)
    .ToArray();
services.AddAutoMapper(cfg => cfg.AddMaps(assemblies), assemblies);
```

**模块中的处理**：

✅ **推荐做法**：无需显式注册（已集中注册）
```csharp
public static IServiceCollection AddXxxModule(...)
{
    // 1. 注册仓储
    services.AddScoped<IXxxRepository, XxxRepository>();

    // 2. 注册服务
    services.AddScoped<LYBT.Shared.Interfaces.Services.IXxxService, XxxService>();

    // 3. AutoMapper - 无需显式注册（已集中注册）
    // services.AddAutoMapper(typeof(XxxMappingProfile)); // ❌ 冗余

    return services;
}
```

❌ **错误做法**：显式注册（造成冗余）
```csharp
// ❌ 错误：显式注册是冗余的
services.AddAutoMapper(typeof(ConsultationMappingProfile));
```

⚠️ **误导性注释**：
```csharp
// ⚠️ 误导：配置文件已存在，无需"待创建"
// 注册AutoMapper配置 - 暂时注释，待创建配置文件后启用
```

**验证方法**：
- 确认`Mapping/`目录下存在`XxxMappingProfile.cs`
- Service构造函数注入`IMapper`
- 无需在模块注册中显式调用`AddAutoMapper`

### 5.4 Validator注册说明

**标准做法**：使用`AddValidatorsFromAssemblyContaining<T>()`自动扫描

✅ **推荐做法**：自动扫描
```csharp
// ✅ 正确：自动扫描当前程序集的所有Validator
services.AddValidatorsFromAssemblyContaining<XxxCreateDtoValidator>();
```

❌ **不推荐做法**：显式注册每个Validator
```csharp
// ❌ 不推荐：显式注册（维护成本高）
services.AddScoped<IValidator<XxxCreateDto>, XxxCreateDtoValidator>();
services.AddScoped<IValidator<XxxUpdateDto>, XxxUpdateDtoValidator>();
```

**优势**：
- 自动发现所有Validator，无需手动维护
- 新增Validator时无需修改注册代码
- 符合DRY原则

### 5.5 常见注册错误与修复

| 错误类型 | 错误示例 | 正确示例 |
|---------|---------|---------|
| **CQRS拆分** | `AddScoped<IXxxQueryService, ...>` | `AddScoped<IXxxService, ...>` |
| **冗余AutoMapper** | `AddAutoMapper(typeof(XxxProfile))` | 删除（已集中注册） |
| **显式Validator** | `AddScoped<IValidator<XxxDto>, ...>` | `AddValidatorsFromAssemblyContaining<...>()` |
| **误导性注释** | "待创建配置文件后启用" | 删除注释（配置已存在） |
| **模块内Service接口** | `AddScoped<IXxxQueryService, ...>` | 删除接口，使用Shared接口 |

## 6. 已实施标准的模块

以下模块已完全符合本标准（截至2025-10-07）：

| 模块 | Service接口 | Repository接口 | 目录结构 | 服务注册 |
|------|------------|--------------|---------|---------|
| **Users** | ✅ IUserService | ✅ IUserRepository | ✅ 正确 | ✅ 标准 |
| **Consultation** | ✅ IConsultationService | ✅ IConsultationRepository | ✅ 正确 | ✅ 标准 |
| **Formula** | ✅ IFormulaService | ✅ IFormulaRepository | ✅ 正确 | ✅ 标准 |
| **Herbs** | ✅ IHerbService | ✅ IHerbRepository | ✅ 正确 | ✅ 标准 |
| **Patients** | ✅ IPatientService | ✅ IPatientRepository | ✅ 正确 | ✅ 标准 |
| **Prescriptions** | ✅ IPrescriptionService | ✅ IPrescriptionRepository | ✅ 正确 | ✅ 标准 |
| **MedicalCase** | ✅ IMedicalCaseService | ✅ IMedicalCaseRepository | ✅ 正确 | ✅ 标准 |
| **Auth** | ✅ IAuthService | ✅ IAuthRepository | ✅ 正确 | ✅ 标准 |

## 7. 验收清单

新建或修改模块时，必须通过以下验收清单：

### 7.1 架构验收
- [ ] 遵循三层架构（Controller → Service → Repository）
- [ ] 未使用CQRS模式（无Query/Business Service拆分）
- [ ] Service接口定义在 `LYBT.Shared.Interfaces.Services`

### 7.2 目录结构验收
- [ ] Repository实现类位于 `Repositories/` 目录
- [ ] Service实现类位于 `Services/` 目录
- [ ] Validator位于 `Validators/` 目录
- [ ] `Interfaces/` 目录仅包含Repository接口

### 7.3 服务注册验收
- [ ] 使用 `LYBT.Shared.Interfaces.Services.IXxxService` 注册
- [ ] 未注册模块内部Service接口（如IXxxQueryService）
- [ ] Repository使用 `IXxxRepository` 注册
- [ ] FluentValidation自动注册生效

### 7.4 编译验证
- [ ] `dotnet build LYBT.Server.sln -c Release` 0错误0警告

## 8. 迁移指南

### 8.1 从混乱到统一：分步迁移

#### Step 1: 评估现状（30分钟）
1. 检查模块目录结构是否符合标准
2. 确认Service接口位置（应在`Shared.Interfaces.Services`）
3. 检查是否存在CQRS拆分（Query/Business Service）
4. 检查AutoMapper和Validator注册方式

#### Step 2: 删除CQRS遗留（如有）（1小时）
1. 搜索`ICommandService`和`IQueryService`引用
2. 如未使用，直接删除接口文件
3. 如已使用，合并到单一Service接口
4. 更新服务注册

#### Step 3: 统一服务注册（2小时）
1. **AutoMapper**：
   - 删除模块中的`services.AddAutoMapper(typeof(XxxProfile))`
   - 确认`Mapping/XxxMappingProfile.cs`存在
   - 依赖`UnifiedServiceRegistration`的集中注册

2. **Validator**：
   - 改用`services.AddValidatorsFromAssemblyContaining<XxxCreateDtoValidator>()`
   - 删除显式的`services.AddScoped<IValidator<...>, ...>()`

3. **Service接口**：
   - 使用`services.AddScoped<LYBT.Shared.Interfaces.Services.IXxxService, XxxService>()`

#### Step 4: 修正误导性注释（30分钟）
1. 删除"UltraThink双层架构"相关注释
2. 删除"待创建配置文件"注释（如配置已存在）
3. 更新为符合三层架构的注释

#### Step 5: 验证与测试（1小时）
1. 编译验证：`dotnet build LYBT.Server.sln -c Release`
2. 测试验证：`dotnet test LYBT.Server.sln -c Release`
3. 检查验收清单（第7节）

### 8.2 迁移检查清单

**迁移前检查**：
- [ ] 已备份当前代码（Git commit）
- [ ] 已阅读server-module-design-standard.md
- [ ] 已了解UnifiedServiceRegistration的集中注册机制

**迁移中检查**：
- [ ] 删除CQRS遗留接口（如有）
- [ ] 删除冗余的AutoMapper注册
- [ ] 改用自动扫描Validator注册
- [ ] 修正误导性注释

**迁移后检查**：
- [ ] 编译通过（0错误0警告）
- [ ] 测试通过（基线一致）
- [ ] 服务注册符合5.1标准模板
- [ ] 无CQRS相关注释

### 8.3 迁移示例：Consultation模块

**迁移前**（ConsultationModule.cs）：
```csharp
public static IServiceCollection AddConsultationModule(...)
{
    services.AddScoped<IConsultationRepository, ConsultationRepository>();
    services.AddScoped<LYBT.Shared.Interfaces.Services.IConsultationService, ConsultationService>();

    // ❌ 冗余：AutoMapper已集中注册
    services.AddAutoMapper(typeof(ConsultationMappingProfile));

    // ✅ 正确：自动扫描
    services.AddValidatorsFromAssemblyContaining<ConsultationCreateDtoValidator>();

    return services;
}
```

**迁移后**：
```csharp
public static IServiceCollection AddConsultationModule(...)
{
    // 1. 注册仓储
    services.AddScoped<IConsultationRepository, ConsultationRepository>();

    // 2. 注册服务（统一使用Shared接口）
    services.AddScoped<LYBT.Shared.Interfaces.Services.IConsultationService, ConsultationService>();

    // 3. 注册验证器 - 自动扫描
    services.AddValidatorsFromAssemblyContaining<ConsultationCreateDtoValidator>();

    // 4. AutoMapper - 无需注册（已在UnifiedServiceRegistration中集中注册）

    return services;
}
```

**变更说明**：
- 删除了冗余的`AddAutoMapper`调用
- 添加了清晰的注释说明

## 9. 常见问题FAQ

### Q1: 为什么禁止CQRS模式？
**A**:
- 小型诊所系统（并发<10人）无需CQRS的读写分离复杂性
- 三层架构已足够满足业务需求
- CQRS会增加代码维护成本和学习曲线
- 参考：`docs/development/standards.md`第32-39行

### Q2: AutoMapper是否必须在模块中注册？
**A**:
- ❌ **不需要**，已在`UnifiedServiceRegistration.cs`中集中注册
- 集中注册会自动扫描所有`LYBT.`开头的程序集
- 只需确保`Mapping/XxxMappingProfile.cs`存在即可
- 显式注册会造成冗余

### Q3: Validator应该用显式注册还是自动扫描？
**A**:
- ✅ **推荐自动扫描**：`AddValidatorsFromAssemblyContaining<XxxCreateDtoValidator>()`
- ❌ **不推荐显式注册**：`AddScoped<IValidator<XxxDto>, XxxValidator>()`
- 自动扫描优势：无需维护注册列表，符合DRY原则

### Q4: Service接口应该放在哪里？
**A**:
- ✅ **必须放在**：`LYBT.Shared.Interfaces.Services`
- ❌ **禁止放在**：模块内的`Interfaces/`目录
- 原因：Desktop端和Server端共享同一接口契约

### Q5: Repository接口应该放在哪里？
**A**:
- ✅ **放在模块内**：`LYBT.Module.Xxx/Interfaces/IXxxRepository.cs`
- Repository接口是模块内部实现细节，不需要跨项目共享

### Q6: 如何验证模块是否符合标准？
**A**:
1. 使用第7节"验收清单"逐项检查
2. 参考第6节"已实施标准的模块"作为示例
3. 执行编译验证：`dotnet build LYBT.Server.sln -c Release`
4. 对照第5.1节"标准注册模板"

### Q7: 发现遗留的ICommandService/IQueryService怎么办？
**A**:
1. 搜索引用：确认是否被使用
2. 如未使用：直接删除接口文件
3. 如已使用：重构为单一Service接口（参考8.1 Step 2）
4. 更新服务注册

### Q8: "待创建配置文件"的注释应该保留吗？
**A**:
- ❌ **不应该**，如果配置文件已存在
- 检查`Mapping/`目录是否有`XxxMappingProfile.cs`
- 如存在，删除误导性注释
- 如不存在，创建MappingProfile后删除注释

### Q9: Options配置是必需的吗？
**A**:
- ⚠️ **可选**，按需配置
- 如模块需要配置项（如超时时间、页面大小），创建`Options/XxxModuleOptions.cs`
- 如无特殊配置需求，可以省略Options目录

### Q10: 如何处理"UltraThink双层架构"注释？
**A**:
- ❌ **必须删除**，严重违反禁止CQRS原则
- 改为符合三层架构的注释
- 示例：
  ```csharp
  // ❌ 错误
  /// 采用UltraThink双层架构：QueryService + BusinessService 专业分离

  // ✅ 正确
  /// 遵循三层架构标准：Controller → Service → Repository
  ```

## 10. 相关文档

- [技术标准与规范](../development/standards.md) - 架构禁令与技术决策
- [ADR-003 Server模块统一设计](ADR-003-server-module-unified-design.md) - 架构决策记录
- [功能模块设计](functional-modules-design.md) - 模块化设计详解

## 11. 变更历史

| 版本 | 日期 | 作者 | 变更说明 |
|------|------|------|---------|
| 1.0 | 2025-10-07 | Claude | 初始版本，基于Issue #1006统一设计成果 |
| 1.1 | 2025-10-07 | Claude | 新增第4节"Service接口统一设计标准"（基于Issue #1008）：<br>- 4.2 Service接口设计原则（ISP/SRP/YAGNI）<br>- 4.3 标准Service接口结构（6-12方法模板）<br>- 4.4 命名约定（方法/参数/返回类型）<br>- 4.5 分页查询标准<br>- 4.6 软删除标准<br>- 4.7 CancellationToken标准<br>关联 [ADR-004](decisions/ADR-004-service-interface-unified-design-standard.md) |
| 1.2 | 2025-10-07 | Claude | 基于Issue #1022 Phase 2补充：<br>- 5.3 AutoMapper注册说明（集中 vs 显式）<br>- 5.4 Validator注册说明（自动扫描 vs 显式）<br>- 5.5 常见注册错误与修复<br>- 第8节 迁移指南（分步迁移、检查清单、迁移示例）<br>- 第9节 常见问题FAQ（10个常见问题解答）<br>关联 [Phase 1分析报告](../reports/server-architecture-analysis-2025-10-07.md) |
