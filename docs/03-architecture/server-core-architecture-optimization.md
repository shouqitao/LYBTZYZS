# Server/Core 架构优化

> 日期: 2026-06-29
> 状态: 草稿
> 范围: LYBT.Entities + LYBT.Infrastructure

## [O1] 当前架构问题

### 严重问题（CRITICAL）

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| C1 | HardDeleteAsync 使用 FindAsync | BaseRepository.cs:517 | 无法硬删除已软删记录 |
| C2 | CrossModuleService 绕过 Repository | CrossModuleService.cs | 直接使用 DbContext，违反分层 |

### 高优先级（HIGH）

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| H1 | BaseService 包含业务权限逻辑 | BaseService.cs | 基础设施层不应有业务逻辑 |
| H2 | BaseApiController 包含 HTTP 层逻辑 | BaseApiController.cs | 基础设施层不应有表现层逻辑 |
| H3 | ApplicationUser 未继承 BaseEntity | ApplicationUser.cs | 手动重复审计字段，维护风险 |
| H4 | IRepository<T> 约束不匹配 | IRepository.cs vs BaseRepository.cs | 接口约束 class，实现约束 BaseEntity |

### 中优先级（MEDIUM）

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| M1 | FormulaHerbItem 全局查询过滤器脆弱 | EntityOptimizationExtensions.cs | 关系变更可能导致过滤器失效 |
| M2 | AuthSession/SystemLog 未继承 BaseEntity | 各自实体 | 缺乏审计字段一致性 |
| M3 | Registration 缩进不一致 | RegistrationModel.cs | 代码质量 |

## [O2] 实体层分析

### 实体继承树

```
BaseEntity (abstract)
├── Patient
├── Herb
├── Formula
├── Consultation (1:1 MedicalCase)
├── Prescription (1:0..1 MedicalCase)
├── MedicalCase (聚合根，唯一充血模型)
└── Registration

IdentityUser<Guid>
└── ApplicationUser (手动重复 BaseEntity 字段)

独立 POCO
├── PrescriptionItem
├── FormulaHerbItem
├── AuthSession
└── SystemLog
```

### 充血 vs 贫血模型

| 实体 | 模型类型 | 域方法 |
|------|----------|--------|
| MedicalCase | 充血 | Complete(), Suspend(), SoftDelete(), UpdateConsultation() |
| Patient | 贫血 | 无 |
| Herb | 贫血 | 无 |
| Formula | 贫血 | 无 |
| Registration | 贫血 | 无 |
| 其他 | 贫血 | 无 |

**评估**: MedicalCase 作为唯一聚合根采用充血模型是合理的。其他实体保持贫血模型，业务逻辑在 Service 层处理。

## [O3] Infrastructure 层分析

### 依赖方向

```
LYBT.Entities
└── LYBT.Shared.Models

LYBT.Infrastructure
├── LYBT.Entities
├── LYBT.Shared.Configuration
├── LYBT.Shared.Models
├── LYBT.Shared.Utilities
├── LYBT.Shared.ExceptionHandling
└── LYBT.Shared.Logging
```

**方向正确**: Entities → Shared.Models；Infrastructure → Entities + Shared libs。无循环依赖。

### 职责分析

| 组件 | 当前职责 | 问题 |
|------|----------|------|
| AppDbContext | 数据访问 + Identity | 合并 Identity 和业务 DbContext |
| BaseRepository | CRUD + 软删除 + 分页 | 712 行，职责过重 |
| BaseService | 权限验证 + 错误处理 | 业务逻辑在基础设施层 |
| BaseApiController | API 响应映射 | 表现层逻辑在基础设施层 |
| CrossModuleService | 跨模块查询 | 绕过 Repository 直接用 DbContext |

## [O4] 优化方案

### 方案 1: 修复 HardDeleteAsync（C1）

```csharp
// 当前代码（有问题）
public async Task HardDeleteAsync(TEntity entity)
{
    var existing = await _dbSet.FindAsync(entity.Id); // 受全局过滤器影响
    if (existing != null)
    {
        _dbSet.Remove(existing);
        await SaveChangesAsync();
    }
}

// 修复方案
public async Task HardDeleteAsync(Guid id)
{
    // 使用 IgnoreQueryFilters 绕过全局过滤器
    var entity = await _dbSet
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(e => e.Id == id);
    
    if (entity != null)
    {
        _dbSet.Remove(entity);
        await SaveChangesAsync();
    }
}
```

### 方案 2: 提取 BaseService 业务逻辑（H1）

```csharp
// 当前: BaseService 包含权限验证
public abstract class BaseService
{
    protected async Task<Result<T>> ExecuteAsync<TResult>(...)
    {
        // 包含业务权限逻辑
    }
}

// 优化: 分离基础设施和业务逻辑
public abstract class BaseService
{
    // 仅保留基础设施功能（错误处理、日志）
}

public abstract class BusinessService : BaseService
{
    // 业务权限逻辑移到这里
    protected async Task<Result<T>> ExecuteWithPermissionAsync<TResult>(...)
    {
        // 权限验证逻辑
    }
}
```

### 方案 3: 提取 BaseApiController（H2）

```csharp
// 当前: Infrastructure 包含 BaseApiController
// 优化: 移动到 WebAPI 项目

// LYBT.Infrastructure 中移除 BaseApiController
// LYBT.WebAPI 中创建 BaseApiController
[ApiController]
[Authorize]
public abstract class BaseApiController : ControllerBase
{
    // API 响应映射逻辑
}
```

### 方案 4: ApplicationUser 统一审计字段（H3）

```csharp
// 方案 A: 使用接口（推荐）
public interface IApplicationUserAudit
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    Guid? CreatedBy { get; set; }
    Guid? UpdatedBy { get; set; }
    bool IsDeleted { get; set; }
    byte[]? RowVersion { get; set; }
}

public class ApplicationUser : IdentityUser<Guid>, IApplicationUserAudit
{
    // 显式实现接口属性
}

// 方案 B: 创建中间基类
public abstract class AuditableIdentityUser<TUser> : IdentityUser<TUser>, IApplicationUserAudit
    where TUser : IEquatable<TUser>
{
    // 共享审计字段
}

public class ApplicationUser : AuditableIdentityUser<Guid>
{
    // 仅业务字段
}
```

### 方案 5: 统一 Repository 约束（H4）

```csharp
// 当前: 接口约束 class，实现约束 BaseEntity
public interface IRepository<T> where T : class { }
public class BaseRepository<TEntity> where TEntity : BaseEntity { }

// 优化: 统一约束
public interface IRepository<T> where T : BaseEntity { }
public class BaseRepository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity { }

// 或者为非 BaseEntity 实体提供单独的 Repository
public interface IPrescriptionItemRepository
{
    // PrescriptionItem 不继承 BaseEntity，需要单独的 Repository
}
```

## [O5] 实施优先级

| 批次 | 任务 | 工作量 | 风险 |
|------|------|--------|------|
| 1 | C1: 修复 HardDeleteAsync | 低 | 低 |
| 2 | H1: 提取 BaseService 业务逻辑 | 中 | 中 |
| 3 | H2: 移动 BaseApiController | 低 | 低 |
| 4 | H3: 统一 ApplicationUser 审计字段 | 中 | 中 |
| 5 | H4: 统一 Repository 约束 | 中 | 中 |

## [O6] 成功标准

1. **HardDeleteAsync**: 可以硬删除已软删记录
2. **BaseService**: 仅包含基础设施功能，无业务逻辑
3. **BaseApiController**: 移动到 WebAPI 项目
4. **ApplicationUser**: 审计字段与 BaseEntity 一致
5. **Repository**: 约束统一，支持所有实体类型
