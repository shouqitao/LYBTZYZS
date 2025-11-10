# Repository泛型接口统一重构需求讨论

**版本**: v1.0  
**创建日期**: 2025-11-10  
**状态**: 📝 需求讨论  
**相关Epic**: [#2016 - Repository泛型接口统一重构](https://github.com/shouqitao/LYBTZYZS/issues/2016)  
**相关文档**: [Repository模式](../patterns/repository-pattern.md) | [ADR-007: Repository简化](../decisions/ADR-007-repository-service-simplification.md)

---

## 📋 需求概述

### 业务目标
统一项目中Repository泛型接口体系，消除接口重复和命名混淆，建立清晰的三层接口架构（IReadRepository<T> → IRepository<T> → IXxxRepository），提升代码一致性和可维护性。

### 目标用户
- 开发者（减少重复代码，提升开发效率）
- 架构师（统一架构规范，降低技术债务）

### 核心场景

1. **开发者创建新模块Repository时**：
   - 当前：不知道应该继承IBaseRepository还是IRepository，命名混乱
   - 期望：明确继承IRepository<T>（聚合根）或IReadRepository<T>（从属实体）

2. **开发者需要批量操作时**：
   - 当前：IBaseRepository<T>缺少AddRangeAsync/DeleteRangeAsync，需要手动循环
   - 期望：直接调用批量操作方法，提升性能

3. **开发者维护Formula/MedicalCase模块时**：
   - 当前：使用旧IRepository<T>（Infrastructure层），与其他模块（IBaseRepository<T>，Shared层）不一致
   - 期望：所有模块使用统一接口，降低认知负担

4. **开发者为Consultation/Prescription模块添加查询方法时**：
   - 当前：直接定义在具体Repository接口，缺少标准CRUD方法
   - 期望：继承IReadRepository<T>获得标准查询方法，仅扩展业务特定方法

---

## ✨ 功能性需求

### FR-001: 创建IReadRepository<T>接口
**User Story**:
```
作为 开发者
我想要 创建只读Repository泛型接口
以便 为从属实体模块（Consultation/Prescription）提供标准查询方法
```

**验收标准**:
- [x] 定义在Infrastructure层（`src/Server/Core/LYBT.Infrastructure/Interfaces/IReadRepository.cs`）
- [x] 包含5个核心查询方法：
  - `Task<T?> GetByIdAsync(Guid id)`
  - `Task<IEnumerable<T>> GetAllAsync()`
  - `Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)`
  - `Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate)`
  - `Task<long> CountAsync()`
- [x] 支持泛型约束 `where T : class`
- [x] 所有方法支持异步操作

### FR-002: 重命名IBaseRepository为IRepository
**User Story**:
```
作为 开发者
我想要 统一接口命名（IBaseRepository → IRepository）
以便 消除与Infrastructure层旧IRepository<T>的命名混淆
```

**验收标准**:
- [x] Shared层的IBaseRepository<T>重命名为IRepository<T>
- [x] Infrastructure层的旧IRepository<T>重命名为IRepositoryLegacy<T>（临时保留，标记@Obsolete）
- [x] 更新所有引用（Users/Patients/Herbs的Repository接口）
- [x] 更新Service层依赖注入配置

### FR-003: 补全IRepository<T>缺失方法
**User Story**:
```
作为 开发者
我想要 在IRepository<T>中添加批量操作和高级分页方法
以便 支持批量导入、高级查询等MVP场景（如Epic #1934）
```

**验收标准**:
- [x] 添加批量插入：`Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)`
- [x] 添加批量删除：`Task<int> DeleteRangeAsync(IEnumerable<T> entities)`
- [x] 添加高级分页（支持排序、过滤）：
  ```csharp
  Task<PagedResult<T>> GetPagedAsync(
      Expression<Func<T, bool>>? predicate,
      int pageNumber, int pageSize,
      Expression<Func<T, object>>? orderBy = null,
      bool ascending = true)
  ```
- [x] 所有新方法包含XML注释和使用示例

### FR-004: 迁移Consultation/Prescription到IReadRepository
**User Story**:
```
作为 开发者
我想要 为Consultation/Prescription引入只读Repository接口
以便 规范化从属实体的数据访问（写操作通过MedicalCase聚合根）
```

**验收标准**:
- [x] IConsultationRepository继承IReadRepository<ConsultationEntity>
- [x] IPrescriptionRepository继承IReadRepository<PrescriptionEntity>
- [x] 保留特定查询方法：
  - `IConsultationRepository.GetByMedicalCaseIdAsync(Guid medicalCaseId)`
  - `IPrescriptionRepository.GetByMedicalCaseIdAsync(Guid medicalCaseId)`
- [x] 移除写操作方法（Create/Update/Delete）- 写操作通过IMedicalCaseRepository

### FR-005: 迁移Formula/MedicalCase到新IRepository
**User Story**:
```
作为 开发者
我想要 将Formula/MedicalCase迁移到新的IRepository<T>接口
以便 与Users/Patients/Herbs模块保持一致
```

**验收标准**:
- [x] IFormulaRepository继承新IRepository<Formula>（Shared层）
- [x] IMedicalCaseRepository继承新IRepository<MedicalCase>（Shared层）
- [x] 移除与泛型接口重复的方法（GetByIdAsync, GetAllAsync等）
- [x] 保留特定业务方法（≤5个）
- [x] 更新实现类（FormulaRepository, MedicalCaseRepository）

### FR-006: 更新BaseRepository实现类
**User Story**:
```
作为 开发者
我想要 更新Infrastructure层的BaseRepository<T>实现类
以便 提供IRepository<T>和IReadRepository<T>的默认实现
```

**验收标准**:
- [x] BaseRepository<T>实现IRepository<T>（20个方法）
- [x] 新增BaseReadRepository<T>实现IReadRepository<T>（5个方法）
- [x] 所有Repository实现类继承正确的基类
- [x] 所有方法包含完整单元测试

### FR-007: 更新文档和测试
**User Story**:
```
作为 开发者
我想要 更新架构文档和测试用例
以便 确保重构后的接口体系有完整文档和测试覆盖
```

**验收标准**:
- [x] 更新CLAUDE.md第2.4节（Repository架构规范）
- [x] 更新docs/explanation/architecture/patterns/repository-pattern.md
- [x] 更新docs/explanation/architecture/server/README.md
- [x] 为新接口和方法添加单元测试（覆盖率≥90%）
- [x] 更新API文档（XML注释）

---

## 🔒 非功能性需求

### NFR-001: 性能要求
- 泛型方法内联优化：编译器自动优化，性能与手写代码相当
- 批量操作性能：使用EF Core批量API，性能优于循环单条插入（约5-10倍）
- 查询性能：GetPagedAsync支持数据库层排序和分页（避免内存分页）

### NFR-002: 向后兼容性
- 旧接口临时保留：IRepositoryLegacy<T>标记@Obsolete("请使用新IRepository<T>")
- 分阶段迁移：每个Phase独立编译、测试、验证
- API签名不变：Formula/MedicalCase迁移时保持Service层调用代码不变

### NFR-003: 代码质量
- 接口方法必须有XML注释（/// <summary>）
- 实现类必须有单元测试覆盖（AAA模式）
- 重构后测试覆盖率≥90%
- 编译通过（0 errors, ≤5个非关键warnings）

### NFR-004: 可维护性
- 接口定义集中：IReadRepository<T>和IRepository<T>定义在Infrastructure层
- 特定方法数量≤5个：避免接口膨胀
- 命名遵循统一规范：GetByXxxAsync/SearchXxxAsync/ExistsByXxxAsync

---

## 📐 业务规则

### BR-001: 统一共性，保持特性
- **规则**: 所有Repository继承泛型接口获得标准CRUD（90%共性），同时保留3-5个模块特定方法（10%特性）
- **理由**: 减少代码重复，同时体现领域语义
- **实现**: `IUserRepository : IRepository<User> + GetByUsernameAsync + IsUsernameExistsAsync`

### BR-002: 避免过度抽象
- **规则**: 接口继承不超过3层
- **理由**: 符合MVP约束，避免过度设计
- **实现**: `IReadRepository<T>` → `IRepository<T>` → `IXxxRepository`（最多3层）

### BR-003: 每个聚合根一个Repository
- **规则**: 每个聚合根实体对应一个Repository接口（DDD原则）
- **理由**: 明确聚合根边界，维护事务一致性
- **实现**: User → IUserRepository, Patient → IPatientRepository, Formula → IFormulaRepository

### BR-004: 从属实体使用只读Repository
- **规则**: 从属实体（Consultation/Prescription）使用IReadRepository<T>，写操作通过聚合根
- **理由**: 保护聚合根边界，避免绕过业务规则
- **实现**: `IConsultationRepository : IReadRepository<ConsultationEntity>`（无Create/Update/Delete方法）

### BR-005: 特定方法命名规范
- **规则**: 使用标准命名模式（GetByXxxAsync/SearchXxxAsync/ExistsByXxxAsync/BatchXxxAsync）
- **理由**: 保持代码风格一致性，降低学习成本
- **实现**:
  - ✅ `GetByUsernameAsync`（而非FindByUsername）
  - ✅ `SearchPatientsAsync`（而非QueryPatients）
  - ✅ `ExistsByNameAsync`（而非CheckNameExists）
  - ✅ `BatchCreateAsync`（而非AddMultiple）

---

## 🗃️ 数据模型草案

### 三层接口体系

```csharp
// ===== 层级1: 只读Repository（5个方法）=====
// 位置: src/Server/Core/LYBT.Infrastructure/Interfaces/IReadRepository.cs

namespace LYBT.Infrastructure.Interfaces;

/// <summary>
/// 只读Repository泛型接口 - 用于从属实体模块
/// </summary>
/// <typeparam name="T">实体类型</typeparam>
public interface IReadRepository<T> where T : class
{
    /// <summary>根据ID获取实体</summary>
    Task<T?> GetByIdAsync(Guid id);

    /// <summary>获取所有实体</summary>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>根据条件查询实体</summary>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

    /// <summary>根据条件获取单个实体</summary>
    Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate);

    /// <summary>统计实体数量</summary>
    Task<long> CountAsync();
}

// ===== 层级2: 完整CRUD Repository（20个方法）=====
// 位置: src/Shared/LYBT.Shared.Models/Interfaces/IRepository.cs（原IBaseRepository.cs）

namespace LYBT.Shared.Models.Interfaces;

/// <summary>
/// 完整CRUD Repository泛型接口 - 用于聚合根模块
/// 继承IReadRepository<T>获得5个查询方法，扩展15个写操作和高级查询方法
/// </summary>
/// <typeparam name="T">聚合根实体类型</typeparam>
public interface IRepository<T> : IReadRepository<T> where T : class
{
    // ===== 基础分页 =====
    Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, string? keyword = null);

    // ===== 高级分页（新增）=====
    Task<PagedResult<T>> GetPagedAsync(
        Expression<Func<T, bool>>? predicate,
        int pageNumber, int pageSize,
        Expression<Func<T, object>>? orderBy = null,
        bool ascending = true);

    // ===== 条件查询扩展 =====
    Task<bool> ExistsAsync(Guid id);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    Task<long> CountAsync(Expression<Func<T, bool>> predicate);

    // ===== 写操作 =====
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(Guid id);

    // ===== 批量操作（新增）=====
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
    Task<int> DeleteRangeAsync(IEnumerable<T> entities);
    Task<int> DeleteRangeAsync(IEnumerable<Guid> ids);

    // ===== 事务 =====
    Task<int> SaveChangesAsync();
}

// ===== 层级3: 模块特定Repository（3-5个特定方法）=====

// 示例1：Users模块（聚合根）
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> IsUsernameExistsAsync(string username);
}

// 示例2：Patients模块（聚合根）
public interface IPatientRepository : IRepository<Patient>
{
    Task<PaginatedList<Patient>> SearchPatientsAsync(string? searchTerm, int pageIndex, int pageSize);
    Task<Patient?> GetByPhoneNumberAsync(string phoneNumber);
}

// 示例3：Consultation模块（从属实体）
public interface IConsultationRepository : IReadRepository<ConsultationEntity>
{
    // 仅扩展只读查询方法
    Task<List<ConsultationEntity>> GetByMedicalCaseIdAsync(Guid medicalCaseId);
    Task<ConsultationEntity> GetByIdWithDetailsAsync(Guid id);
}

// 示例4：Prescription模块（从属实体）
public interface IPrescriptionRepository : IReadRepository<PrescriptionEntity>
{
    // 仅扩展只读查询方法
    Task<List<PrescriptionEntity>> GetByMedicalCaseIdAsync(Guid medicalCaseId);
    Task<PagedResult<PrescriptionEntity>> GetPagedWithDetailsAsync(int pageNumber, int pageSize);
}
```

### 模块分类表

| 模块 | 类型 | 接口选择 | 特征 | 特定方法示例 |
|------|------|---------|------|-------------|
| **Users** | 聚合根 | IRepository<User> | 完整生命周期，独立聚合 | GetByUsernameAsync |
| **Patients** | 聚合根 | IRepository<Patient> | 完整生命周期，独立聚合 | SearchPatientsAsync |
| **Herbs** | 聚合根 | IRepository<Herb> | 完整生命周期，独立聚合 | GetByNameAsync |
| **Formula** | 聚合根 | IRepository<Formula> | 完整生命周期，独立聚合 | GetByCategoryAsync |
| **MedicalCase** | 聚合根 | IRepository<MedicalCase> | 完整生命周期，独立聚合 | GetByPatientIdAsync |
| **Consultation** | 从属实体 | IReadRepository<ConsultationEntity> | 写操作通过MedicalCase | GetByMedicalCaseIdAsync |
| **Prescription** | 从属实体 | IReadRepository<PrescriptionEntity> | 写操作通过MedicalCase | GetByMedicalCaseIdAsync |
| **Auth** | 特殊 | 不使用泛型 | Token/Session管理 | N/A |

---

## 🏗️ 架构约束

### 技术栈限制（基于MVP Constitution）

**✅ 允许技术**:
- EF Core 8.0 - 允许（项目标准ORM）
- SQL Server 2022 - 允许（项目标准数据库）
- 泛型接口 - 允许（减少代码重复，符合DRY原则）
- 依赖注入（Scoped） - 允许（ASP.NET Core标准）

**❌ 禁止技术**:
- Repository工厂模式 - 禁止（过度抽象，违反MVP原则）
- 通用Repository（单一IRepository适配所有实体） - 禁止（缺乏类型安全）
- UnitOfWork模式 - 禁止（DbContext已是UoW，无需额外抽象）
- 动态查询构建器 - 禁止（Expression Tree复杂度高，维护成本大）

### 架构层分配

**Server端**（本次重构核心）:
- Infrastructure层：IReadRepository<T>、IRepositoryLegacy<T>、BaseRepository<T>、BaseReadRepository<T>
- Shared层：IRepository<T>（原IBaseRepository<T>）
- 模块Repository接口：各模块的IXxxRepository（继承IRepository<T>或IReadRepository<T>）
- 模块Repository实现：各模块的XxxRepository（继承BaseRepository<T>或BaseReadRepository<T>）

**Client端**（无需变更）:
- Repository仅Server使用，Client端通过API访问数据
- 无需修改Client端代码

### 模块定位

**核心改动模块**:
1. Infrastructure层（新增IReadRepository<T>，重命名接口）
2. Shared层（IBaseRepository → IRepository）
3. Users/Patients/Herbs模块（更新接口引用）
4. Formula/MedicalCase模块（迁移到新接口）
5. Consultation/Prescription模块（引入IReadRepository<T>）

**影响范围**:
- 7个模块Repository接口
- 7个模块Repository实现类
- Infrastructure层BaseRepository基类
- Service层依赖注入配置

---

## ❓ 开放问题

### Q1: IBaseRepository接口位置（Shared vs Infrastructure）

**问题**: 重命名后的IRepository<T>应该放在Shared层还是Infrastructure层？

**选项**:
- **A. 保持Shared层**（推荐）
  - 理由：Client端未来可能需要本地Repository（如LiteDB）
  - 优点：跨端复用，架构灵活
  - 缺点：Shared层增加依赖

- B. 移到Infrastructure层
  - 理由：当前仅Server端使用
  - 优点：职责更清晰
  - 缺点：未来Client端需要时需要重新迁移

**建议**: 选A（保持Shared层，为未来扩展预留空间）

### Q2: 旧IRepository接口删除时机

**问题**: Infrastructure层的IRepositoryLegacy<T>何时删除？

**选项**:
- A. 重构完成后立即删除
  - 理由：避免代码冗余
  - 风险：可能影响未知依赖

- **B. 保留1-2个版本后删除**（推荐）
  - 理由：给缓冲期，确保稳定
  - 方案：标记@Obsolete，v1.1版本删除
  - 优点：安全稳妥

**建议**: 选B（标记@Obsolete，给1个版本缓冲期）

### Q3: IReadRepository是否需要SaveChangesAsync？

**问题**: 只读Repository是否应该有SaveChanges方法？

**选项**:
- **A. 不包含**（推荐）
  - 理由：从属实体写操作通过聚合根，IReadRepository应纯只读
  - 优点：职责清晰，防止误用
  - 实现：IReadRepository<T>不包含SaveChangesAsync

- B. 包含
  - 理由：某些从属实体可能有状态变更
  - 缺点：违反DDD聚合根原则

**建议**: 选A（IReadRepository<T>不包含SaveChangesAsync）

### Q4: 批量操作的性能优化策略

**问题**: AddRangeAsync/DeleteRangeAsync如何实现高性能？

**选项**:
- **A. 使用EF Core原生批量API**（推荐）
  - 实现：`_context.Entities.AddRange(entities); await _context.SaveChangesAsync();`
  - 优点：简单直接，性能提升5-10倍
  - 缺点：大批量（>1000条）仍可能慢

- B. 使用EF Core扩展库（如EFCore.BulkExtensions）
  - 优点：性能更好（>100倍）
  - 缺点：引入第三方依赖，违反MVP原则

**建议**: 选A（MVP阶段使用EF Core原生API，性能足够）

---

## 📊 风险分析

### 风险1: Formula/MedicalCase迁移影响现有功能（高）

**影响**:
- Formula/MedicalCase是核心业务模块，功能复杂
- 迁移可能引入bug，影响处方开具、病案管理等关键流程

**缓解措施**:
- Phase拆分：每个模块独立Phase，单独测试验证
- 回归测试：执行完整业务流程测试（登录 → 开处方 → 保存病案）
- 运行时验证：启动Client+Server，手动验证关键功能

**验收标准**:
- [ ] 编译通过（0 errors, ≤5 warnings）
- [ ] 单元测试通过（覆盖率≥90%）
- [ ] 运行时验证通过（完整业务流程）

### 风险2: 批量操作性能未达预期（中）

**影响**:
- 批量导入药材（>1000条）性能可能不理想
- Epic #1934可能无法按期完成

**缓解措施**:
- 性能测试：批量插入1000条记录，测试耗时（目标<5秒）
- 性能监控：添加日志记录批量操作耗时
- 降级方案：如性能不足，使用EFCore.BulkExtensions（需评估MVP约束）

**触发条件**:
- 批量操作耗时>5秒（1000条记录）
- 用户反馈导入慢

### 风险3: 接口命名冲突（低）

**影响**:
- IRepository<T>在Shared和Infrastructure层可能冲突
- 编译错误或运行时错误

**缓解措施**:
- 命名空间隔离：Shared.IRepository vs Infrastructure.IReadRepository
- 代码审查：检查using语句，确保引用正确接口
- 编译验证：每个Phase编译验证

---

## 📎 参考资料

**项目文档**:
- [CLAUDE.md - 2.4节 Repository架构规范](../../../../CLAUDE.md#24-repository架构规范-新增)
- [Repository模式文档](../patterns/repository-pattern.md)
- [ADR-007: Repository和Service层简化重构](../decisions/ADR-007-repository-service-simplification.md)
- [Server端架构指南](../server/README.md)

**技术文档**:
- [Microsoft Docs: Repository Pattern with EF Core](https://learn.microsoft.com/en-us/ef/core/testing/testing-without-the-database#repository-pattern)
- [Microsoft Docs: DDD Infrastructure Persistence Layer](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design#the-repository-pattern)

**业务规则**:
- [三层架构规范](../server/README.md) - P0-2（依赖方向）、P0-3（聚合根边界）
- [MVP Constitution](.spec-workflow/steering/constitution.md) - 技术黑名单

**相关Issue**:
- [Epic #2016 - Repository泛型接口统一重构](https://github.com/shouqitao/LYBTZYZS/issues/2016)
- [Epic #1934 - 批量导入药材功能](https://github.com/shouqitao/LYBTZYZS/issues/1934)（需要批量操作方法）

---

## 📅 下一步

1. ✅ **需求确认**（人工确认点1）
   - 用户审查本需求文档
   - 确认开放问题的选项
   - 批准进入设计阶段

2. ⏳ **设计生成**（调用lybtzyzs-design-generator）
   - 生成详细设计文档
   - 包含Phase拆分（预计5-6个Phase）
   - 包含API设计、数据库迁移、测试策略

3. ⏳ **任务分解**（调用lybtzyzs-task-breakdown）
   - 根据设计文档拆分任务清单
   - 估算工作量（预计8-12天）
   - 分析依赖关系

4. ⏳ **Issue创建**（调用lybtzyzs-issue-template）
   - 批量创建GitHub Issues
   - 关联Epic #2016
   - 标注依赖关系

---

**创建者**: Claude Code (lybtzyzs-requirements-generator)  
**审核者**: 待确认  
**最后更新**: 2025-11-10
