# Server模块设计标准

> **版本**: 1.0
> **日期**: 2025-10-07
> **关联**: [ADR-003 Server模块统一设计](ADR-003-server-module-unified-design.md)

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

## 4. 接口设计标准

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

### 4.2 Repository接口

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
