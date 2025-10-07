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

## 8. 相关文档

- [技术标准与规范](../development/standards.md) - 架构禁令与技术决策
- [ADR-003 Server模块统一设计](ADR-003-server-module-unified-design.md) - 架构决策记录
- [功能模块设计](functional-modules-design.md) - 模块化设计详解

## 9. 变更历史

| 版本 | 日期 | 作者 | 变更说明 |
|------|------|------|---------|
| 1.0 | 2025-10-07 | Claude | 初始版本，基于Issue #1006统一设计成果 |
| 1.1 | 2025-10-07 | Claude | 新增第4节"Service接口统一设计标准"（基于Issue #1008）：<br>- 4.2 Service接口设计原则（ISP/SRP/YAGNI）<br>- 4.3 标准Service接口结构（6-12方法模板）<br>- 4.4 命名约定（方法/参数/返回类型）<br>- 4.5 分页查询标准<br>- 4.6 软删除标准<br>- 4.7 CancellationToken标准<br>关联 [ADR-004](decisions/ADR-004-service-interface-unified-design-standard.md) |
