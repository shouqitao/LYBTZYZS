# OpenSpec Proposal: Server端代码优化重构

**提案ID**: server-code-optimization
**创建日期**: 2025-12-05
**状态**: DRAFT
**作者**: Claude Code

## 摘要

深度分析Server端代码架构，识别并修复抽象过度、继承过深、重复定义等设计问题，提升代码质量和可维护性。

## 背景与动机

### 当前问题

通过代码分析，发现以下设计问题：

#### 1. 死代码/未使用的"Optimized"接口模式
- `IUserServiceOptimized.cs` - 已删除（无实现、无引用）
- `IPatientServiceOptimized.cs` - 需要评估
- `IPrescriptionServiceOptimized.cs` - 需要评估

**问题**: 这些接口声称提供"Entity直接返回"的优化，但从未被正确实现或使用。

#### 2. 控制器基类重复方法 (BaseApiController.cs ~340行)
存在大量"兼容性Helper方法"，同一功能有泛型和非泛型两个版本：

| 方法 | 重复模式 |
|------|----------|
| `Error()` / `Error<T>()` | 泛型/非泛型重复 |
| `NotFound()` / `NotFound<T>()` | 泛型/非泛型重复 |
| `ValidationFail()` / `ValidationFail<T>()` | 泛型/非泛型重复 |
| `HandleException()` / `HandleException<T>()` | 泛型/非泛型重复 |

**问题**: 违反DRY原则，增加维护成本。

#### 3. 控制器继承链过深
```
ControllerBase (ASP.NET Core)
    └── BaseControllerCore (175行)
            └── BaseApiController (340行)
                    └── 具体Controller
```

**问题**:
- 两层自定义基类可能过度抽象
- `BaseControllerCore`和`BaseApiController`功能边界不清晰
- 继承深度增加理解和维护难度

#### 4. Service基类设计问题 (BaseService.cs ~385行)
- `BaseService` (非泛型) 和 `BaseService<T>` (泛型) 并存
- 泛型版本的 `GetEntityId`, `GetCreatedUserId`, `GetCreatedDate` 方法默认抛出 `NotImplementedException`
- 强制子类重写这些方法，违反里氏替换原则

```csharp
protected virtual Guid GetEntityId<TEntity>(TEntity entity) where TEntity : class
{
    throw new NotImplementedException($"子类 {GetType().Name} 需要重写 GetEntityId 方法以支持权限验证");
}
```

**问题**: 如果需要权限验证，应该使用接口而非虚方法+异常。

#### 5. Repository基类代码膨胀 (BaseRepository.cs ~617行)
- 过多重载方法（`GetPagedAsync`有多个变体）
- 模板方法模式使用不当
- 部分代码可以简化

#### 6. Repository层命名不一致性

通过分析各模块Repository接口，发现以下命名不一致问题：

| 模块 | 方法名 | 问题 |
|------|--------|------|
| IUserRepository | `IsUsernameExistsAsync` | 命名为`Is...Exists`，与其他模块不一致 |
| IPatientRepository | `ExistsAsync(string name, Guid? excludeId)` | 命名为`Exists`，无前缀 |
| IMedicalCaseRepository | `GetByIdWithDetailsAsync` / `GetByIdWithDetailsFreshAsync` | 使用`WithDetails`后缀 |
| IConsultationRepository | `GetByIdWithDetailsAsync` | 使用`WithDetails`后缀 |
| IPrescriptionRepository | `GetByIdWithItemsAsync` | 使用`WithItems`后缀，与其他模块的`WithDetails`不一致 |

**命名不一致分类**:

1. **存在性检查方法命名不一致**:
   - `IsUsernameExistsAsync` (User)
   - `ExistsAsync` (Patient, IRepository)
   - 建议统一为: `ExistsAsync` 或 `IsExistsAsync`

2. **详情查询方法后缀不一致**:
   - `WithDetails` (MedicalCase, Consultation)
   - `WithItems` (Prescription)
   - 建议统一为: `WithDetailsAsync` 或按业务语义命名

3. **返回类型不一致**:
   - `GetByMedicalCaseIdAsync` 在Consultation返回单个`Consultation`
   - `GetByMedicalCaseIdAsync` 在Prescription返回`List<Prescription>`
   - 建议: 单个返回用`Get`，多个返回用`GetList`或返回类型明确区分

### 最佳实践参考

根据Microsoft官方文档和DDD最佳实践：

1. **控制器设计**:
   - Web API应直接继承`ControllerBase`，而非`Controller`
   - 控制器应保持thin，仅负责请求处理和响应
   - 避免多层继承

2. **Repository模式**:
   - 每个聚合根一个Repository
   - Repository接口定义在Domain层
   - 实现在Infrastructure层

3. **Service层**:
   - Application Service协调领域对象
   - 不应包含业务逻辑
   - 遵循单一职责原则

4. **SOLID原则**:
   - 接口隔离原则：避免大而全的基类
   - 依赖倒置原则：依赖抽象而非具体实现
   - 里氏替换原则：子类应可替换基类

## 提议的解决方案

### Phase 1: 清理死代码（低风险）
1. 分析并删除未使用的"Optimized"接口
2. 移除无用的兼容性方法

### Phase 2: 简化控制器继承（中风险）
1. 合并`BaseControllerCore`和`BaseApiController`为单一基类
2. 使用泛型消除重复的Helper方法
3. 将通用功能提取到扩展方法或Filter中

### Phase 3: 重构Service基类（中风险）
1. 将`GetEntityId`等方法改为接口约束
2. 引入`IEntityIdentifiable<TKey>`接口
3. 简化权限验证逻辑

### Phase 4: 优化Repository（低风险）
1. 减少不必要的重载方法
2. 使用Specification模式替代复杂查询
3. 提取通用查询到扩展方法

## 影响范围

### 受影响的文件
- `src/Server/Core/LYBT.Infrastructure/Web/BaseControllerCore.cs`
- `src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs`
- `src/Server/Core/LYBT.Infrastructure/Services/BaseService.cs`
- `src/Server/Core/LYBT.Infrastructure/Repositories/BaseRepository.cs`
- 所有继承这些基类的模块

### 不受影响
- 数据库Schema
- API契约（外部接口保持兼容）
- 前端代码

## 风险评估

| 风险 | 级别 | 缓解措施 |
|------|------|----------|
| 破坏现有功能 | 中 | 分阶段实施，每阶段验证编译和测试 |
| 引入回归Bug | 中 | 充分的单元测试覆盖 |
| 合并冲突 | 低 | 基础设施代码变更较少 |

## 验收标准

1. 所有单元测试通过
2. 集成测试通过
3. 编译无错误无警告
4. 代码行数减少（目标: 基类总行数减少20%+）
5. 继承深度减少（目标: 最多2层自定义基类）

## 时间线

分4个Phase逐步实施，每个Phase独立可验证。

## 参考资料

- [ASP.NET Core Controller Best Practices](https://learn.microsoft.com/en-us/aspnet/core/web-api/)
- [Repository Pattern in DDD](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
- [SOLID Principles](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-application-layer-web-api-design)
