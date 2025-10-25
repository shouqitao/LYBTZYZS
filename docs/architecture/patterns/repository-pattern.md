# Repository模式

**创建日期**: 2025-10-25
**适用范围**: Server端 + Desktop端
**复杂度**: ⭐⭐（中等）

---

## 📋 模式概述

Repository模式是一种数据访问抽象层，将业务逻辑与数据访问逻辑分离，提供统一的数据访问接口。

**核心价值**：
- ✅ **抽象数据源**：业务逻辑不依赖具体的数据库技术
- ✅ **集中查询逻辑**：避免查询逻辑分散在多个Service中
- ✅ **便于测试**：通过Mock Repository接口进行单元测试
- ✅ **支持缓存**：在Repository层统一添加缓存逻辑

---

## 🎯 适用场景

### ✅ 应该使用Repository的场景

1. **Server端数据访问**：
   - 所有Entity Framework Core的数据库操作
   - 需要统一事务管理的场景
   - 需要集中查询逻辑的场景

2. **Desktop端数据访问**（⚠️ 例外情况）：
   - 需要缓存层的场景（未来）
   - 需要离线支持的场景（未来）
   - 当前MVP阶段：Read操作可以直接使用API，Write操作通过聚合根Repository

### ❌ 不应该使用Repository的场景

1. **Desktop端Read操作**（当前MVP）：
   - 简单查询可以直接调用API（`IPrescriptionApi`）
   - 避免薄封装Repository

2. **一次性查询**：
   - 仅在一个地方使用的特殊查询
   - 可以直接在Service中实现

---

## 🏗️ 模式结构

### Server端Repository标准结构

```
LYBT.Server.{Module}.Infrastructure/
  └─ Repositories/
      ├─ {Entity}Repository.cs          # 实现类
      └─ ...

LYBT.Server.{Module}.Domain/
  └─ Repositories/
      ├─ I{Entity}Repository.cs         # 接口定义
      └─ ...
```

### Desktop端Repository标准结构（⚠️ 例外）

```
LYBT.Desktop.{Module}/
  ├─ Interfaces/
  │   └─ I{Entity}Repository.cs         # 接口定义（已删除）
  └─ Repositories/
      └─ {Entity}Repository.cs          # 实现类（已删除）

⚠️ 当前状态：Prescriptions/Consultation模块已删除Repository层
详见：ADR-003（Repository层简化）
```

---

## 💻 代码示例

### Server端Repository实现（标准）

#### 接口定义（Domain层）

```csharp
// LYBT.Server.MedicalCase.Domain/Repositories/IMedicalCaseRepository.cs
using LYBT.Server.MedicalCase.Domain.Entities;

namespace LYBT.Server.MedicalCase.Domain.Repositories;

/// <summary>
/// 医案仓储接口
/// </summary>
public interface IMedicalCaseRepository
{
    // ===== CRUD操作 =====

    /// <summary>
    /// 根据ID获取医案
    /// </summary>
    Task<MedicalCaseEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页获取医案列表
    /// </summary>
    Task<PagedResult<MedicalCaseEntity>> GetPagedAsync(
        int page,
        int size,
        string? keyword = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建医案
    /// </summary>
    Task<MedicalCaseEntity> CreateAsync(
        MedicalCaseEntity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新医案
    /// </summary>
    Task UpdateAsync(
        MedicalCaseEntity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除医案
    /// </summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    // ===== 聚合根子实体操作（⭐ 关键） =====

    /// <summary>
    /// 为医案创建处方（通过聚合根）
    /// </summary>
    Task<Prescription> CreatePrescriptionAsync(
        int medicalCaseId,
        CreatePrescriptionDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新医案的处方（通过聚合根）
    /// </summary>
    Task UpdatePrescriptionAsync(
        int prescriptionId,
        UpdatePrescriptionDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除医案的处方（通过聚合根）
    /// </summary>
    Task DeletePrescriptionAsync(
        int prescriptionId,
        CancellationToken cancellationToken = default);
}
```

#### 实现类（Infrastructure层）

```csharp
// LYBT.Server.MedicalCase.Infrastructure/Repositories/MedicalCaseRepository.cs
using LYBT.Server.MedicalCase.Domain.Entities;
using LYBT.Server.MedicalCase.Domain.Repositories;
using LYBT.Server.MedicalCase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Server.MedicalCase.Infrastructure.Repositories;

/// <summary>
/// 医案仓储实现
/// </summary>
public class MedicalCaseRepository : IMedicalCaseRepository
{
    private readonly MedicalCaseDbContext _context;

    public MedicalCaseRepository(MedicalCaseDbContext context)
    {
        _context = context;
    }

    // ===== CRUD操作 =====

    public async Task<MedicalCaseEntity?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.MedicalCases
            .Include(m => m.Consultations)
            .Include(m => m.Prescriptions)
            .Include(m => m.Diagnoses)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<PagedResult<MedicalCaseEntity>> GetPagedAsync(
        int page,
        int size,
        string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.MedicalCases.AsQueryable();

        // 关键字搜索
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(m =>
                m.PatientName.Contains(keyword) ||
                m.Diagnosis.Contains(keyword));
        }

        // 分页
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return new PagedResult<MedicalCaseEntity>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = size
        };
    }

    public async Task<MedicalCaseEntity> CreateAsync(
        MedicalCaseEntity entity,
        CancellationToken cancellationToken = default)
    {
        _context.MedicalCases.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(
        MedicalCaseEntity entity,
        CancellationToken cancellationToken = default)
    {
        _context.MedicalCases.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            _context.MedicalCases.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    // ===== 聚合根子实体操作（⭐ 关键） =====

    public async Task<Prescription> CreatePrescriptionAsync(
        int medicalCaseId,
        CreatePrescriptionDto dto,
        CancellationToken cancellationToken = default)
    {
        // 1. 加载聚合根
        var medicalCase = await GetByIdAsync(medicalCaseId, cancellationToken);
        if (medicalCase == null)
            throw new NotFoundException($"医案不存在: {medicalCaseId}");

        // 2. 通过聚合根方法创建处方（业务规则验证在聚合根内）
        var prescription = medicalCase.CreatePrescription(dto);

        // 3. 保存变更
        await _context.SaveChangesAsync(cancellationToken);

        return prescription;
    }

    public async Task UpdatePrescriptionAsync(
        int prescriptionId,
        UpdatePrescriptionDto dto,
        CancellationToken cancellationToken = default)
    {
        // 1. 查找处方所属的医案
        var prescription = await _context.Prescriptions
            .Include(p => p.MedicalCase)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId, cancellationToken);

        if (prescription == null)
            throw new NotFoundException($"处方不存在: {prescriptionId}");

        // 2. 通过聚合根方法更新处方
        prescription.MedicalCase.UpdatePrescription(prescriptionId, dto);

        // 3. 保存变更
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeletePrescriptionAsync(
        int prescriptionId,
        CancellationToken cancellationToken = default)
    {
        // 1. 查找处方所属的医案
        var prescription = await _context.Prescriptions
            .Include(p => p.MedicalCase)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId, cancellationToken);

        if (prescription == null)
            throw new NotFoundException($"处方不存在: {prescriptionId}");

        // 2. 通过聚合根方法删除处方
        prescription.MedicalCase.DeletePrescription(prescriptionId);

        // 3. 保存变更
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

---

### Desktop端Repository实现（⚠️ 已删除）

#### 当前状态（ADR-003后）

```csharp
// ❌ 已删除：LYBT.Desktop.Prescriptions/Interfaces/IPrescriptionRepository.cs
// ❌ 已删除：LYBT.Desktop.Prescriptions/Repositories/PrescriptionRepository.cs

// ✅ 当前实践：ViewModel直接使用API + 聚合根Repository

public class PrescriptionManagementViewModel
{
    private readonly IPrescriptionApi _api;                    // Read操作
    private readonly IMedicalCaseRepository _repository;       // Write操作

    public PrescriptionManagementViewModel(
        IPrescriptionApi api,
        IMedicalCaseRepository repository)
    {
        _api = api;
        _repository = repository;
    }

    // Read操作：直接使用API
    private async Task LoadDataAsync()
    {
        var response = await _api.GetPrescriptionsAsync(1, 50);
        Prescriptions = new ObservableCollection<Prescription>(response.Data.Items);
    }

    // Write操作：通过聚合根Repository
    private async Task CreatePrescriptionAsync(CreatePrescriptionDto dto)
    {
        await _repository.CreatePrescriptionAsync(MedicalCaseId, dto);
    }
}
```

---

## ✅ 最佳实践

### 1. Repository仅负责数据访问

```csharp
// ✅ 正确：Repository仅负责数据访问
public async Task<MedicalCaseEntity?> GetByIdAsync(int id)
{
    return await _context.MedicalCases
        .Include(m => m.Consultations)
        .FirstOrDefaultAsync(m => m.Id == id);
}

// ❌ 错误：Repository包含业务逻辑
public async Task<MedicalCaseEntity?> GetByIdAsync(int id)
{
    var entity = await _context.MedicalCases.FindAsync(id);

    // ❌ 业务逻辑应该在Service中
    if (entity.Status == MedicalCaseStatus.Archived)
        throw new InvalidOperationException("已归档的医案不可访问");

    return entity;
}
```

### 2. 使用Include预加载关联数据

```csharp
// ✅ 正确：使用Include避免N+1查询
public async Task<MedicalCaseEntity?> GetByIdAsync(int id)
{
    return await _context.MedicalCases
        .Include(m => m.Consultations)
        .Include(m => m.Prescriptions)
        .Include(m => m.Diagnoses)
        .FirstOrDefaultAsync(m => m.Id == id);
}

// ❌ 错误：懒加载导致N+1查询
public async Task<MedicalCaseEntity?> GetByIdAsync(int id)
{
    // 后续访问Consultations/Prescriptions会产生额外查询
    return await _context.MedicalCases.FindAsync(id);
}
```

### 3. 聚合根子实体通过聚合根操作

```csharp
// ✅ 正确：通过聚合根操作子实体
public async Task<Prescription> CreatePrescriptionAsync(
    int medicalCaseId,
    CreatePrescriptionDto dto)
{
    var medicalCase = await GetByIdAsync(medicalCaseId);
    var prescription = medicalCase.CreatePrescription(dto); // 聚合根方法
    await _context.SaveChangesAsync();
    return prescription;
}

// ❌ 错误：直接操作子实体（绕过聚合根）
public async Task<Prescription> CreatePrescriptionAsync(CreatePrescriptionDto dto)
{
    var prescription = new Prescription
    {
        MedicalCaseId = dto.MedicalCaseId,
        // ... 直接创建，绕过聚合根业务规则
    };
    _context.Prescriptions.Add(prescription); // ❌ 跨聚合直接操作
    await _context.SaveChangesAsync();
    return prescription;
}
```

### 4. 分页查询返回PagedResult

```csharp
// ✅ 正确：返回PagedResult包含分页信息
public async Task<PagedResult<MedicalCaseEntity>> GetPagedAsync(
    int page,
    int size,
    string? keyword = null)
{
    var query = _context.MedicalCases.AsQueryable();

    if (!string.IsNullOrWhiteSpace(keyword))
        query = query.Where(m => m.PatientName.Contains(keyword));

    var total = await query.CountAsync();
    var items = await query
        .Skip((page - 1) * size)
        .Take(size)
        .ToListAsync();

    return new PagedResult<MedicalCaseEntity>
    {
        Items = items,
        Total = total,
        Page = page,
        PageSize = size
    };
}

// ❌ 错误：仅返回items，缺少分页信息
public async Task<List<MedicalCaseEntity>> GetPagedAsync(int page, int size)
{
    return await _context.MedicalCases
        .Skip((page - 1) * size)
        .Take(size)
        .ToListAsync(); // 缺少Total信息，前端无法计算总页数
}
```

---

## ❌ 常见错误

### 错误1: Repository包含业务逻辑

```csharp
// ❌ 错误
public async Task<MedicalCaseEntity> CreateAsync(MedicalCaseEntity entity)
{
    // ❌ 业务验证应该在Service中
    if (string.IsNullOrWhiteSpace(entity.PatientName))
        throw new ValidationException("患者姓名不能为空");

    _context.MedicalCases.Add(entity);
    await _context.SaveChangesAsync();
    return entity;
}

// ✅ 正确：验证在Service中
// MedicalCaseService.cs
public async Task<MedicalCaseEntity> CreateAsync(CreateMedicalCaseDto dto)
{
    // ✅ 业务验证在Service
    if (string.IsNullOrWhiteSpace(dto.PatientName))
        throw new ValidationException("患者姓名不能为空");

    var entity = _mapper.Map<MedicalCaseEntity>(dto);
    return await _repository.CreateAsync(entity); // Repository仅负责数据访问
}
```

### 错误2: Desktop端薄封装Repository

```csharp
// ❌ 错误：Desktop端薄封装API调用
public class PrescriptionRepository : IPrescriptionRepository
{
    private readonly IPrescriptionApi _api;

    public PrescriptionRepository(IPrescriptionApi api)
    {
        _api = api;
    }

    // ❌ 仅转发API调用，无业务价值
    public async Task<Prescription> GetByIdAsync(int id)
    {
        var response = await _api.GetPrescriptionByIdAsync(id);
        return response.Data;
    }
}

// ✅ 正确：ViewModel直接使用API
public class PrescriptionManagementViewModel
{
    private readonly IPrescriptionApi _api;

    private async Task LoadDataAsync()
    {
        // ✅ 直接调用API，避免薄封装
        var response = await _api.GetPrescriptionByIdAsync(id);
        CurrentPrescription = response.Data;
    }
}
```

### 错误3: 缺少CancellationToken支持

```csharp
// ❌ 错误：不支持取消
public async Task<MedicalCaseEntity?> GetByIdAsync(int id)
{
    return await _context.MedicalCases.FindAsync(id);
}

// ✅ 正确：支持取消
public async Task<MedicalCaseEntity?> GetByIdAsync(
    int id,
    CancellationToken cancellationToken = default)
{
    return await _context.MedicalCases
        .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
}
```

---

## 🔗 相关资源

- **架构原则**: [docs/architecture/principles.md](../principles.md) - P0-2（依赖方向）、P0-3（聚合根边界）
- **ADR-003**: [Repository层简化](../decisions/ADR-003-repository-simplification.md) - Desktop端例外
- **Server端架构**: [docs/architecture/server/README.md](../server/README.md) - 三层架构
- **聚合根模式**: [aggregate-root-pattern.md](./aggregate-root-pattern.md)
- **业务规则**: [docs/business-rules.md](../../business-rules.md) - 规则#3（聚合根边界）

---

## 📅 更新日志

| 日期 | 版本 | 变更内容 | 作者 |
|------|------|----------|------|
| 2025-10-25 | v1.0 | 初始创建 | Claude Code |

---

**最后更新**: 2025-10-25
**维护者**: 项目架构团队
