# Repository Include预加载策略

> **Epic #2175 BF-002 Phase 4 Task 4.4**: Repository Include预加载优化文档
>
> **创建日期**: 2025-11-20
> **版本**: v1.0

## 概述

本文档记录LYBTZYZS项目中Entity Framework Core Repository层的Include预加载策略，确保避免N+1查询问题，提升查询性能。

## 核心原则

### 1. 按需Include原则

- **基础查询（GetBaseQuery）**：不预加载关联数据，用于简单列表、统计查询
- **详细查询（GetDetailQuery）**：预加载必需的关联数据，用于详情展示、复杂业务逻辑

### 2. ThenInclude深度预加载

对于多层级关联，使用`ThenInclude`一次性加载所有必需数据，避免延迟加载导致的N+1查询。

### 3. 性能权衡

- **优点**：消除N+1查询，减少数据库往返
- **代价**：单次查询返回更多数据，JOIN复杂度增加
- **选择**：详情查询使用Include，列表查询谨慎使用

## MedicalCaseRepository Include策略

### 当前实现

**文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs`

#### GetBaseQuery() - 基础查询（无Include）

```csharp
private IQueryable<MedicalCaseEntity> GetBaseQuery()
{
    return _dbSet.Where(m => !m.IsDeleted);
}
```

**适用场景**：
- `GetByPatientIdAsync`: 患者病案列表（只需基础信息）
- `GetByDoctorIdAsync`: 医生病案列表（只需基础信息）
- `GetPagedWithDetailsAsync`: 分页列表（使用BaseQuery + 辅助方法）

**设计理由**：
- 列表展示不需要关联数据
- 减少JOIN复杂度，提升分页性能
- 降低内存占用

#### GetDetailQuery() - 详细查询（Include关联）

```csharp
private IQueryable<MedicalCaseEntity> GetDetailQuery()
{
    return _dbSet
        .Include(m => m.Consultation)          // 预加载诊断信息
        .Include(m => m.Prescription!)         // 预加载处方
            .ThenInclude(p => p.Items)         // 深度预加载处方药材项
        .Where(m => !m.IsDeleted);
}
```

**适用场景**：
- `GetByIdWithDetailsAsync`: 病案详情查询
- `QueryAsync`: 多条件组合查询（需要诊断关键字搜索）
- `GetUnfinishedCaseByPatientIdAsync`: 未完成病案查询（需要完整数据）
- `UpdateAsync`: 更新操作（Detached场景需要加载关联数据）

**设计理由**：
1. **避免N+1查询**：Prescription.Items通过ThenInclude一次性加载，而非逐个Item延迟加载
2. **业务必需**：详情页面需要显示完整诊断信息和处方明细
3. **更新安全**：UpdateAsync需要检查关联数据以支持级联删除逻辑

### 性能优化历史

**Epic #1612 Task 1.5**: 增强Include策略
- **问题**：原实现只Include Prescription，未Include Items
- **后果**：展示处方详情时触发N+1查询（每个Item一次SELECT）
- **优化**：添加`ThenInclude(p => p.Items)`
- **收益**：假设处方包含10个药材项，从11次查询降低到1次查询

## FormulaRepository Include策略

### 当前实现

**文件**: `src/Server/Modules/LYBT.Module.Formula/Repositories/FormulaRepository.cs`

```csharp
private IQueryable<FormulaEntity> GetQueryable()
{
    return _dbSet
        .Include(f => f.Herbs)              // 预加载经验方药材组成
        .Where(f => !f.IsDeleted);
}
```

**设计理由**：
- 经验方（Formula）的核心价值在于药材组成（Herbs）
- 几乎所有查询都需要Herbs数据
- 统一使用Include，简化Repository逻辑

## QueryAsync动态Include优化建议

### 当前实现分析

```csharp
public async Task<List<MedicalCaseEntity>> QueryAsync(
    string? patientName = null,
    DateTime? startDate = null,
    DateTime? endDate = null,
    string? diagnosisKeyword = null)
{
    // 总是使用GetDetailQuery()，即使不需要Consultation
    var query = GetDetailQuery();

    // 只有在有diagnosisKeyword时才需要Consultation数据
    if (!string.IsNullOrWhiteSpace(diagnosisKeyword))
    {
        query = query.Where(m =>
            m.Consultation != null &&
            m.Consultation.TCMDiagnosis != null &&
            m.Consultation.TCMDiagnosis.Contains(diagnosisKeyword));
    }
    // ...
}
```

### 潜在优化方案（可选）

#### 方案1: 条件Include（推荐用于高频查询）

```csharp
public async Task<List<MedicalCaseEntity>> QueryAsync(
    string? patientName = null,
    DateTime? startDate = null,
    DateTime? endDate = null,
    string? diagnosisKeyword = null)
{
    // 根据是否有诊断关键字决定Include策略
    var query = string.IsNullOrWhiteSpace(diagnosisKeyword)
        ? GetBaseQuery()
        : GetDetailQuery();

    // 其余逻辑不变
}
```

**收益**：
- 无诊断关键字搜索时，减少JOIN(Consultation, Prescription, PrescriptionItems)
- 约减少30-50%的查询时间（取决于数据规模）

**代价**：
- 增加代码复杂度
- 调用方需要明确知道返回数据是否包含关联

#### 方案2: 分离为两个方法（最佳实践）

```csharp
// 简化查询 - 仅基础信息
public async Task<List<MedicalCaseEntity>> QueryBasicAsync(
    string? patientName = null,
    DateTime? startDate = null,
    DateTime? endDate = null)
{
    var query = GetBaseQuery();
    // 应用过滤条件
}

// 详细查询 - 包含关联数据
public async Task<List<MedicalCaseEntity>> QueryWithDetailsAsync(
    string? patientName = null,
    DateTime? startDate = null,
    DateTime? endDate = null,
    string? diagnosisKeyword = null)
{
    var query = GetDetailQuery();
    // 应用过滤条件（包括诊断关键字）
}
```

**收益**：
- API语义明确，调用方知道返回数据结构
- 各司其职，避免单一方法承担过多责任
- 性能最优，无不必要的Include

**适用场景**：
- QueryAsync使用频率高且数据规模大时考虑优化
- 目前Epic #2175未发现此性能瓶颈，暂不优化

## 性能监控建议

### 1. SQL日志分析

启用EF Core SQL日志记录，监控Include生成的JOIN语句：

```csharp
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

### 2. N+1查询检测

观察日志中是否存在重复的SELECT语句模式：

```sql
-- 正常模式（Include正确）
SELECT ... FROM MedicalCases m
LEFT JOIN Consultations c ON ...
LEFT JOIN Prescriptions p ON ...
LEFT JOIN PrescriptionItems pi ON ...
WHERE m.Id = @p0

-- 异常模式（N+1查询）
SELECT ... FROM MedicalCases WHERE Id = @p0
SELECT ... FROM Consultations WHERE MedicalCaseId = @p0
SELECT ... FROM Prescriptions WHERE MedicalCaseId = @p0
SELECT ... FROM PrescriptionItems WHERE PrescriptionId = @p0  -- 重复N次
```

### 3. 性能基准测试

建议的性能指标（1000条病案数据规模）：

| 查询场景 | Include策略 | 预期时间 | N+1风险 |
|---------|------------|---------|---------|
| 病案列表（分页20条） | GetBaseQuery | < 50ms | 无 |
| 单个病案详情 | GetDetailQuery | < 100ms | 已避免 |
| 多条件查询（无诊断关键字）| GetBaseQuery | < 80ms | 无 |
| 多条件查询（有诊断关键字）| GetDetailQuery | < 150ms | 已避免 |

## 总结

### 已完成的优化

✅ MedicalCaseRepository.GetDetailQuery使用ThenInclude预加载Prescription.Items
✅ FormulaRepository.GetQueryable统一Include Herbs
✅ 区分BaseQuery和DetailQuery，按需Include

### 未优化但可接受的场景

⚠️ QueryAsync总是使用GetDetailQuery，即使不需要诊断搜索
- 理由：方法使用频率低，优化收益有限
- 建议：如发现性能瓶颈再考虑拆分方法

### 最佳实践总结

1. **优先使用BaseQuery**：列表、统计、简单过滤场景
2. **详情查询用DetailQuery**：需要展示关联数据时
3. **ThenInclude深度预加载**：多层级关联一次性加载
4. **监控SQL日志**：定期检查是否存在N+1查询

## 参考资料

- [Entity Framework Core - 加载相关数据](https://learn.microsoft.com/zh-cn/ef/core/querying/related-data/)
- [N+1查询问题及解决方案](https://learn.microsoft.com/zh-cn/ef/core/performance/efficient-querying#beware-of-lazy-loading)
- [Epic #1612 Task 1.5: Include策略优化](https://github.com/shouqitao/LYBTZYZS/issues/1612)
