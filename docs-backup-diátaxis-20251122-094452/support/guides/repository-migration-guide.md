# Repository迁移指南

> **Epic #2016 - Repository泛型接口统一重构**
>
> **文档版本**: v1.0  
> **创建日期**: 2025-11-11  
> **适用范围**: 从旧Repository接口迁移到三层接口架构

---

## 📋 目录

1. [迁移概览](#迁移概览)
2. [场景1: 聚合根 - 从IBaseRepository迁移](#场景1-聚合根---从ibaserepository迁移)
3. [场景2: 聚合根 - 从旧IRepository迁移](#场景2-聚合根---从旧irepository迁移)
4. [场景3: 从属实体 - 创建只读Repository](#场景3-从属实体---创建只读repository)
5. [迁移检查清单](#迁移检查清单)
6. [常见问题FAQ](#常见问题faq)

---

## 迁移概览

### 三层接口架构

```
层级1: IReadRepository<T>     ← 5个标准只读方法（Shared.Models）
       ↓ 继承
层级2: IRepository<T>         ← +9个写入/辅助方法（Shared.Models）
       ↓ 继承
层级3: IXxxRepository         ← +模块特定业务方法（Module层）
```

### 模块分类

| 实体类型 | 模块 | 使用接口 | 使用基类 | 写操作方式 |
|---------|------|---------|---------|-----------|
| **聚合根** | Patient, MedicalCase, Herb, Formula | `IRepository<T>` | `BaseRepository<T>` | 直接写入 |
| **从属实体** | Prescription, Consultation | `IReadRepository<T>` | `BaseReadRepository<T>` | 通过聚合根 |

---

## 场景1: 聚合根 - 从IBaseRepository迁移

### 适用模块
- **Patient** (患者)
- **Herb** (中药)
- **Formula** (方剂)
- **MedicalCase** (病案)

### Before（旧代码）

```csharp
// ❌ 旧接口定义
using LYBT.Shared.Models.Interfaces;

namespace LYBT.Module.Patients.Interfaces
{
    public interface IPatientRepository : IBaseRepository<Patient>
    {
        // 模块特定方法
        Task<Patient?> GetByPhoneAsync(string phone);
        Task<PagedResult<Patient>> GetPagedAsync(int pageNumber, int pageSize, string? keyword);
    }
}

// ❌ 旧实现
using LYBT.Infrastructure.Repositories;

namespace LYBT.Module.Patients.Repositories
{
    internal class PatientRepository : BaseRepository<Patient>, IPatientRepository
    {
        public PatientRepository(AppDbContext context) : base(context) { }
        
        // 模块特定方法实现
        public async Task<Patient?> GetByPhoneAsync(string phone)
        {
            return await DbSet
                .Where(p => p.Phone == phone && !p.IsDeleted)
                .FirstOrDefaultAsync();
        }
        
        // ... 其他方法
    }
}
```

### After（新代码）

```csharp
// ✅ 新接口定义
using LYBT.Shared.Models.Interfaces;

namespace LYBT.Module.Patients.Interfaces
{
    /// <summary>
    /// 患者仓储接口 - 继承IRepository标准接口（Epic #2016 Phase 3）
    /// </summary>
    /// <remarks>
    /// 设计原则：
    /// - ⭐ 统一共性：继承IRepository&lt;Patient&gt;获得14个标准方法
    /// - ⭐ 保持特性：保留患者模块特定业务方法
    /// - 聚合根模式：完整CRUD能力，支持直接写入
    /// </remarks>
    public interface IPatientRepository : IRepository<Patient>
    {
        // 模块特定方法（保持不变）
        Task<Patient?> GetByPhoneAsync(string phone);
        Task<PagedResult<Patient>> GetPagedAsync(int pageNumber, int pageSize, string? keyword);
    }
}

// ✅ 新实现（无需修改，继承BaseRepository自动获得14个方法）
using LYBT.Infrastructure.Repositories;

namespace LYBT.Module.Patients.Repositories
{
    /// <summary>
    /// 患者仓储 - 继承BaseRepository标准实现（Epic #2016 Phase 3）
    /// </summary>
    internal class PatientRepository : BaseRepository<Patient>, IPatientRepository
    {
        public PatientRepository(AppDbContext context) : base(context) { }
        
        // 模块特定方法实现（保持不变）
        public async Task<Patient?> GetByPhoneAsync(string phone)
        {
            return await DbSet
                .Where(p => p.Phone == phone && !p.IsDeleted)
                .FirstOrDefaultAsync();
        }
        
        // ... 其他方法
    }
}
```

### 迁移步骤

1. **更新接口继承**
   ```diff
   - public interface IPatientRepository : IBaseRepository<Patient>
   + public interface IPatientRepository : IRepository<Patient>
   ```

2. **添加文档注释**（推荐）
   ```csharp
   /// <summary>
   /// 患者仓储接口 - 继承IRepository标准接口（Epic #2016 Phase 3）
   /// </summary>
   /// <remarks>
   /// 设计原则：
   /// - ⭐ 统一共性：继承IRepository&lt;Patient&gt;获得14个标准方法
   /// - ⭐ 保持特性：保留患者模块特定业务方法
   /// </remarks>
   ```

3. **实现类无需修改**（继承BaseRepository已实现所有标准方法）

4. **更新依赖注入**（如果接口名有变化）
   ```csharp
   // Program.cs 或 ServiceCollectionExtensions.cs
   services.AddScoped<IPatientRepository, PatientRepository>();
   ```

5. **更新单元测试**（接口名变化需同步）

---

## 场景2: 聚合根 - 从旧IRepository迁移

### 适用场景
如果之前已经使用 `IRepository<T>` 接口，但未继承新的三层架构。

### Before（旧代码）

```csharp
// ❌ 旧接口定义（自定义IRepository）
namespace LYBT.Module.Patients.Interfaces
{
    public interface IPatientRepository : IRepository<Patient>
    {
        // 可能包含自定义的CRUD方法
        Task<Patient?> GetByIdAsync(Guid id);
        Task AddAsync(Patient patient);
        Task UpdateAsync(Patient patient);
        Task DeleteAsync(Guid id);
        
        // 模块特定方法
        Task<Patient?> GetByPhoneAsync(string phone);
    }
}
```

### After（新代码）

```csharp
// ✅ 新接口定义（继承Shared层的IRepository<T>）
using LYBT.Shared.Models.Interfaces;

namespace LYBT.Module.Patients.Interfaces
{
    /// <summary>
    /// 患者仓储接口 - 继承IRepository标准接口（Epic #2016 Phase 3）
    /// </summary>
    public interface IPatientRepository : IRepository<Patient>
    {
        // ⚠️ 移除重复的CRUD方法定义（已在IRepository<T>中）
        // - Task<Patient?> GetByIdAsync(Guid id);        ← 删除
        // - Task AddAsync(Patient patient);              ← 删除
        // - Task UpdateAsync(Patient patient);           ← 删除
        // - Task DeleteAsync(Guid id);                   ← 删除
        
        // 仅保留模块特定方法
        Task<Patient?> GetByPhoneAsync(string phone);
    }
}
```

### 迁移步骤

1. **添加using语句**
   ```csharp
   using LYBT.Shared.Models.Interfaces;
   ```

2. **移除重复的CRUD方法定义**
   ```diff
   public interface IPatientRepository : IRepository<Patient>
   {
   -   Task<Patient?> GetByIdAsync(Guid id);
   -   Task AddAsync(Patient patient);
   -   Task UpdateAsync(Patient patient);
   -   Task DeleteAsync(Guid id);
   +   // 已继承IRepository<T>的14个标准方法
       
       // 仅保留模块特定方法
       Task<Patient?> GetByPhoneAsync(string phone);
   }
   ```

3. **更新实现类**
   ```csharp
   using LYBT.Infrastructure.Repositories;
   
   internal class PatientRepository : BaseRepository<Patient>, IPatientRepository
   {
       public PatientRepository(AppDbContext context) : base(context) { }
       
       // ⚠️ 移除标准CRUD方法实现（已在BaseRepository<T>中）
       // 仅保留模块特定方法实现
       
       public async Task<Patient?> GetByPhoneAsync(string phone)
       {
           return await DbSet
               .Where(p => p.Phone == phone && !p.IsDeleted)
               .FirstOrDefaultAsync();
       }
   }
   ```

4. **编译验证**
   ```bash
   dotnet build LYBT.All.sln -c Release
   ```

---

## 场景3: 从属实体 - 创建只读Repository

### 适用模块
- **Prescription** (处方 - 从属于MedicalCase)
- **Consultation** (诊疗记录 - 从属于MedicalCase)

### 设计原则

**从属实体采用只读Repository模式**：
- ✅ **读操作**: 通过自己的Repository
- ✅ **写操作**: 必须通过聚合根（MedicalCase）

### Before（旧代码 - 可能有写方法）

```csharp
// ❌ 旧接口（可能包含写方法）
namespace LYBT.Module.Prescriptions.Interfaces
{
    public interface IPrescriptionRepository : IBaseRepository<Prescription>
    {
        Task<Prescription?> GetByIdWithItemsAsync(Guid id);
        Task AddAsync(Prescription prescription);         // ❌ 不应直接写入
        Task UpdateAsync(Prescription prescription);      // ❌ 不应直接更新
    }
}
```

### After（新代码 - 只读模式）

```csharp
// ✅ 新接口定义（只读）
using LYBT.Shared.Models.Interfaces;

namespace LYBT.Module.Prescriptions.Interfaces
{
    /// <summary>
    /// 处方仓储接口 - 继承IReadRepository标准接口（Epic #2016 Phase 3）
    /// </summary>
    /// <remarks>
    /// 设计原则：
    /// - ⭐ 统一共性：继承IReadRepository&lt;Prescription&gt;获得5个标准只读方法
    /// - ⭐ 保持特性：保留处方模块特定业务方法
    /// - Read-only模式：所有写操作必须通过MedicalCase聚合根
    /// </remarks>
    public interface IPrescriptionRepository : IReadRepository<Prescription>
    {
        // 模块特定查询方法
        Task<Prescription?> GetByIdWithItemsAsync(Guid id);
        Task<PagedResult<Prescription>> GetPagedWithDetailsAsync(int pageNumber, int pageSize, string? keyword);
        Task<List<Prescription>> GetByPatientIdAsync(Guid patientId);
        Task<List<Prescription>> GetByMedicalCaseIdAsync(Guid medicalCaseId);
    }
}

// ✅ 新实现（只读）
using LYBT.Infrastructure.Repositories;

namespace LYBT.Module.Prescriptions.Repositories
{
    /// <summary>
    /// 处方仓储 - 继承BaseReadRepository标准实现（Epic #2016 Phase 3）
    /// </summary>
    internal class PrescriptionRepository : BaseReadRepository<Prescription>, IPrescriptionRepository
    {
        public PrescriptionRepository(AppDbContext context) : base(context) { }
        
        // 模块特定查询方法实现
        public async Task<Prescription?> GetByIdWithItemsAsync(Guid id)
        {
            return await DbSet
                .AsNoTracking()
                .Include(p => p.Items)
                .Where(p => p.Id == id && !p.IsDeleted)
                .SingleOrDefaultAsync();
        }
        
        // ... 其他查询方法
    }
}
```

### 写操作通过聚合根

```csharp
// ✅ 正确方式：通过MedicalCase聚合根操作处方
public class MedicalCaseService
{
    private readonly IMedicalCaseRepository _medicalCaseRepository;
    
    public async Task AddPrescriptionAsync(Guid medicalCaseId, PrescriptionDto dto)
    {
        // 1. 加载聚合根
        var medicalCase = await _medicalCaseRepository.GetByIdAsync(medicalCaseId);
        if (medicalCase == null) throw new NotFoundException("病案不存在");
        
        // 2. 通过聚合根添加处方
        var prescription = new Prescription
        {
            MedicalCaseId = medicalCaseId,
            PatientId = medicalCase.PatientId,
            // ... 其他属性
        };
        medicalCase.Prescriptions.Add(prescription);
        
        // 3. 保存聚合根
        await _medicalCaseRepository.UpdateAsync(medicalCase);
        await _medicalCaseRepository.SaveChangesAsync();
    }
}
```

### 迁移步骤

1. **更新接口继承**
   ```diff
   - public interface IPrescriptionRepository : IBaseRepository<Prescription>
   + public interface IPrescriptionRepository : IReadRepository<Prescription>
   ```

2. **移除所有写方法**
   ```diff
   public interface IPrescriptionRepository : IReadRepository<Prescription>
   {
   -   Task AddAsync(Prescription prescription);
   -   Task UpdateAsync(Prescription prescription);
   -   Task DeleteAsync(Guid id);
       
       // 仅保留查询方法
       Task<Prescription?> GetByIdWithItemsAsync(Guid id);
   }
   ```

3. **更新实现类**
   ```diff
   - internal class PrescriptionRepository : BaseRepository<Prescription>, IPrescriptionRepository
   + internal class PrescriptionRepository : BaseReadRepository<Prescription>, IPrescriptionRepository
   ```

4. **迁移写操作到聚合根Service**
   - 将所有创建/更新/删除处方的逻辑迁移到 `MedicalCaseService`
   - 通过 `MedicalCase.Prescriptions` 集合操作

---

## 迁移检查清单

### ✅ 代码层面

- [ ] **接口继承关系检查**
  - [ ] 聚合根接口继承 `IRepository<T>`
  - [ ] 从属实体接口继承 `IReadRepository<T>`
  - [ ] 添加 `using LYBT.Shared.Models.Interfaces;`

- [ ] **接口方法清理**
  - [ ] 移除与基础接口重复的方法定义
  - [ ] 从属实体接口移除所有写方法
  - [ ] 保留模块特定业务方法

- [ ] **实现类更新**
  - [ ] 聚合根实现继承 `BaseRepository<T>`
  - [ ] 从属实体实现继承 `BaseReadRepository<T>`
  - [ ] 移除重复的标准方法实现
  - [ ] 保留模块特定方法实现

- [ ] **Service层调整**
  - [ ] 从属实体的写操作迁移到聚合根Service
  - [ ] 验证聚合根边界正确性

### ✅ 配置层面

- [ ] **依赖注入配置**
  - [ ] 验证 `services.AddScoped<IXxxRepository, XxxRepository>();` 正确
  - [ ] 检查接口名是否有变化

### ✅ 测试层面

- [ ] **单元测试更新**
  - [ ] 更新Mock接口类型（如有变化）
  - [ ] 补充标准方法测试用例
  - [ ] 验证聚合根写操作测试
  - [ ] 验证从属实体只读测试

- [ ] **集成测试验证**
  - [ ] 三步看诊流程完整性测试
  - [ ] 聚合根写操作端到端测试

### ✅ 编译与运行验证

- [ ] **编译通过**
  ```bash
  dotnet build LYBT.All.sln -c Release
  # 预期：0 errors, ≤5 warnings
  ```

- [ ] **单元测试通过**
  ```bash
  dotnet test LYBT.All.sln -c Release --settings tests/.runsettings
  # 预期：100%通过率
  ```

- [ ] **运行时验证**
  - [ ] 启动Server端（API正常响应）
  - [ ] 启动Client端（登录、患者管理、三步看诊流程）
  - [ ] 验证数据库记录正确

---

## 常见问题FAQ

### Q1: 为什么Prescription使用只读Repository？

**A**: Prescription是**从属实体**，依附于MedicalCase聚合根。根据DDD聚合根模式（AR-001）：
- ✅ **读操作**: 可以独立查询（性能优化）
- ✅ **写操作**: 必须通过聚合根保证一致性（业务规则约束）

**示例场景**：
- 创建处方时，需要验证病案状态、患者信息、医生权限 → 通过MedicalCase聚合根
- 查询历史处方列表时，只需读取数据 → 通过Prescription只读Repository（性能更好）

### Q2: 如果我需要直接更新Prescription怎么办？

**A**: 不应该直接更新Prescription。正确做法：

```csharp
// ❌ 错误方式（破坏聚合根边界）
await _prescriptionRepository.UpdateAsync(prescription);

// ✅ 正确方式（通过聚合根）
var medicalCase = await _medicalCaseRepository.GetByIdAsync(medicalCaseId);
var prescription = medicalCase.Prescriptions.First(p => p.Id == prescriptionId);
prescription.Indication = "新的主治功能";
await _medicalCaseRepository.UpdateAsync(medicalCase);
```

### Q3: BaseRepository和BaseReadRepository的区别？

**A**: 

| 特性 | BaseReadRepository\<T\> | BaseRepository\<T\> |
|-----|------------------------|---------------------|
| **实现接口** | `IReadRepository<T>` (5个方法) | `IRepository<T>` (14个方法) |
| **用途** | 从属实体只读 | 聚合根完整CRUD |
| **方法** | GetById, GetAll, Find, GetSingle, Count | +Add, Update, Delete, AddRange, DeleteRange, Exists, SaveChanges |
| **适用场景** | Prescription, Consultation | Patient, MedicalCase, Herb, Formula |

### Q4: 迁移后性能会受影响吗？

**A**: 不会，反而可能提升：
- ✅ **软删除过滤自动化**: 所有查询自动过滤 `IsDeleted`，避免遗漏
- ✅ **代码复用**: 减少重复代码，降低维护成本
- ✅ **EF Core优化**: 统一的DbSet访问模式，更易优化
- ✅ **只读优化**: 从属实体使用 `AsNoTracking()` 提升查询性能

### Q5: 如何验证迁移成功？

**A**: 按以下步骤验证：

1. **编译验证**
   ```bash
   dotnet build LYBT.All.sln -c Release
   # 0 errors, ≤5 warnings
   ```

2. **单元测试验证**
   ```bash
   dotnet test LYBT.All.sln -c Release --settings tests/.runsettings
   # 100%通过率
   ```

3. **运行时验证**
   - 启动Server + Client
   - 执行三步看诊流程（辨证→开方标记→处方）
   - 检查数据库记录正确性

4. **代码审查**
   - 检查接口继承关系
   - 验证从属实体无写方法
   - 确认聚合根边界正确

### Q6: 旧的IBaseRepository接口会删除吗？

**A**: 是的，在Phase 6最终清理时：
- 删除 `IRepositoryLegacy<T>` 接口（如果存在v1.1版本）
- 清理过时的代码注释
- 移除 `#pragma warning` 临时指令

### Q7: 如何处理复杂查询？

**A**: 模块特定的复杂查询方法保留在各自的Repository接口中：

```csharp
public interface IPrescriptionRepository : IReadRepository<Prescription>
{
    // ✅ 复杂查询：预加载关联数据
    Task<Prescription?> GetByIdWithItemsAsync(Guid id);
    
    // ✅ 复杂查询：分页+关键字搜索
    Task<PagedResult<Prescription>> GetPagedWithDetailsAsync(
        int pageNumber, int pageSize, string? keyword);
    
    // ✅ 复杂查询：按关联实体过滤
    Task<List<Prescription>> GetByPatientIdAsync(Guid patientId);
}
```

---

## 相关文档

- **架构文档**: [repository-pattern.md](../explanation/architecture/patterns/repository-pattern.md)
- **Server端架构**: [server/README.md](../explanation/architecture/server/README.md)
- **Shared层架构**: [shared/README.md](../explanation/architecture/shared/README.md)
- **CLAUDE.md**: [Section 2.4 Repository架构规范](../../CLAUDE.md#24-repository架构规范)

---

**文档维护**:
- **创建**: 2025-11-11 - Epic #2016 Phase 6
- **最后更新**: 2025-11-11
