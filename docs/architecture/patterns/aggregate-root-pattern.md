# Aggregate Root模式（聚合根）

**创建日期**: 2025-10-25
**适用范围**: Server端
**复杂度**: ⭐⭐⭐（高）

---

## 📋 模式概述

Aggregate Root（聚合根）是DDD（领域驱动设计）中的核心概念，定义了对象图的边界和一致性规则。

**核心价值**：
- ✅ **保证一致性**：聚合内数据的一致性由聚合根保证
- ✅ **明确边界**：聚合根定义了事务边界和持久化边界
- ✅ **封装业务规则**：业务规则集中在聚合根中
- ✅ **简化复杂性**：外部只需与聚合根交互，不直接操作子实体

---

## 🎯 LYBTZYZS项目的聚合根

### MedicalCase聚合根

```
MedicalCase（聚合根）
  ├─ Consultation（子实体）- 诊疗记录
  ├─ Prescription（子实体）- 处方
  └─ Diagnosis（子实体）- 诊断记录

规则：
- ✅ 所有子实体的创建/更新/删除必须通过MedicalCase聚合根
- ❌ 禁止直接操作Consultation/Prescription/Diagnosis
```

---

## 💻 代码示例

### 聚合根实体定义

```csharp
// LYBT.Server.MedicalCase.Domain/Entities/MedicalCaseEntity.cs
public class MedicalCaseEntity
{
    public int Id { get; private set; }
    public string PatientName { get; private set; }
    public MedicalCaseStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // 子实体集合（⭐ 关键：通过聚合根管理）
    private readonly List<Consultation> _consultations = new();
    private readonly List<Prescription> _prescriptions = new();
    private readonly List<Diagnosis> _diagnoses = new();

    public IReadOnlyCollection<Consultation> Consultations => _consultations.AsReadOnly();
    public IReadOnlyCollection<Prescription> Prescriptions => _prescriptions.AsReadOnly();
    public IReadOnlyCollection<Diagnosis> Diagnoses => _diagnoses.AsReadOnly();

    // ===== 聚合根业务方法（⭐ 核心） =====

    /// <summary>
    /// 创建处方（通过聚合根）
    /// </summary>
    public Prescription CreatePrescription(CreatePrescriptionDto dto)
    {
        // 业务规则验证
        if (Status == MedicalCaseStatus.Archived)
            throw new InvalidOperationException("已归档的医案不能创建处方");

        if (string.IsNullOrWhiteSpace(dto.Content))
            throw new ValidationException("处方内容不能为空");

        // 创建子实体
        var prescription = new Prescription
        {
            MedicalCaseId = Id,
            Content = dto.Content,
            CreatedAt = DateTime.Now,
            CreatedBy = dto.CreatedBy
        };

        _prescriptions.Add(prescription);
        return prescription;
    }

    /// <summary>
    /// 更新处方（通过聚合根）
    /// </summary>
    public void UpdatePrescription(int prescriptionId, UpdatePrescriptionDto dto)
    {
        var prescription = _prescriptions.FirstOrDefault(p => p.Id == prescriptionId);
        if (prescription == null)
            throw new NotFoundException($"处方不存在: {prescriptionId}");

        // 业务规则验证
        if (Status == MedicalCaseStatus.Archived)
            throw new InvalidOperationException("已归档的医案不能修改处方");

        // 更新子实体
        prescription.Content = dto.Content;
        prescription.UpdatedAt = DateTime.Now;
        prescription.UpdatedBy = dto.UpdatedBy;
    }

    /// <summary>
    /// 删除处方（通过聚合根）
    /// </summary>
    public void DeletePrescription(int prescriptionId)
    {
        var prescription = _prescriptions.FirstOrDefault(p => p.Id == prescriptionId);
        if (prescription == null)
            throw new NotFoundException($"处方不存在: {prescriptionId}");

        // 业务规则验证
        if (Status == MedicalCaseStatus.Archived)
            throw new InvalidOperationException("已归档的医案不能删除处方");

        _prescriptions.Remove(prescription);
    }
}
```

### Repository实现（通过聚合根）

```csharp
// LYBT.Server.MedicalCase.Infrastructure/Repositories/MedicalCaseRepository.cs
public async Task<Prescription> CreatePrescriptionAsync(
    int medicalCaseId,
    CreatePrescriptionDto dto,
    CancellationToken cancellationToken = default)
{
    // 1. 加载聚合根
    var medicalCase = await GetByIdAsync(medicalCaseId, cancellationToken);
    if (medicalCase == null)
        throw new NotFoundException($"医案不存在: {medicalCaseId}");

    // 2. ✅ 通过聚合根方法创建处方（业务规则验证在聚合根内）
    var prescription = medicalCase.CreatePrescription(dto);

    // 3. 保存变更
    await _context.SaveChangesAsync(cancellationToken);

    return prescription;
}

// ❌ 错误示例：直接操作子实体
public async Task<Prescription> CreatePrescriptionAsync_WRONG(CreatePrescriptionDto dto)
{
    // ❌ 绕过聚合根，直接创建子实体
    var prescription = new Prescription
    {
        MedicalCaseId = dto.MedicalCaseId,
        Content = dto.Content,
        // ❌ 缺少业务规则验证（如医案是否已归档）
    };

    _context.Prescriptions.Add(prescription);
    await _context.SaveChangesAsync();

    return prescription;
}
```

---

## ✅ 最佳实践

### 1. 子实体集合使用ReadOnly

```csharp
// ✅ 正确：ReadOnly集合
private readonly List<Prescription> _prescriptions = new();
public IReadOnlyCollection<Prescription> Prescriptions => _prescriptions.AsReadOnly();

// ❌ 错误：直接暴露可修改集合
public List<Prescription> Prescriptions { get; set; } = new();
```

### 2. 业务规则集中在聚合根

```csharp
// ✅ 正确：业务规则在聚合根
public Prescription CreatePrescription(CreatePrescriptionDto dto)
{
    if (Status == MedicalCaseStatus.Archived)
        throw new InvalidOperationException("已归档的医案不能创建处方");

    // 创建逻辑...
}

// ❌ 错误：业务规则在Service
public async Task<Prescription> CreatePrescriptionAsync(CreatePrescriptionDto dto)
{
    if (medicalCase.Status == MedicalCaseStatus.Archived) // 业务规则泄漏
        throw new InvalidOperationException("...");

    var prescription = new Prescription { ... };
    _context.Prescriptions.Add(prescription);
}
```

### 3. Repository方法通过聚合根

```csharp
// ✅ 正确：Repository方法通过聚合根
await _medicalCaseRepository.CreatePrescriptionAsync(medicalCaseId, dto);

// ❌ 错误：直接操作子实体Repository
await _prescriptionRepository.CreateAsync(dto); // 绕过聚合根
```

---

## 🔗 相关资源

- **架构原则**: [principles.md](../principles.md) - P0-3（聚合根边界不可跨越）
- **ADR-003**: [Repository层简化](../decisions/ADR-003-repository-simplification.md)
- **Repository模式**: [repository-pattern.md](./repository-pattern.md)
- **业务规则**: [docs/business-rules.md](../../business-rules.md) - 规则#3

---

**最后更新**: 2025-10-25
