# OpenSpec Proposal: 统一RowVersion处理到BaseRepository

## 元数据

| 字段 | 值 |
|------|-----|
| **Proposal ID** | consolidate-rowversion-handling |
| **状态** | Applied |
| **创建时间** | 2025-11-29 15:35 |
| **作者** | Claude Code |
| **影响范围** | Server端 - Infrastructure/Repository层 |
| **优先级** | 低 (技术债务清理) |

---

## Why

RowVersion处理逻辑目前存在于两个位置：

1. **BaseRepository.SaveChangesAsync()** (行630-653)
   - 全局遍历所有tracked实体
   - 对所有具有RowVersion属性的实体进行OriginalValue同步
   - 这是Issue #2250修复后的正确实现

2. **MedicalCaseRepository.UpdateAsync()** (行264-300)
   - 针对MedicalCase、Consultation、Prescription三个实体
   - 硬编码的实体类型检查
   - 这是Issue #1669 Phase 7的遗留代码

**问题**：
1. 代码重复 - 同一逻辑执行两次
2. 维护成本 - 如果修改BaseRepository逻辑，需同步修改MedicalCaseRepository
3. 不一致风险 - 其他Repository可能没有类似处理，导致行为不一致

---

## What Changes

移除MedicalCaseRepository中的冗余RowVersion同步代码，统一由BaseRepository.SaveChangesAsync()处理。

### 变更范围

```
src/Server/
└── Modules/LYBT.Module.MedicalCase/
    └── Repositories/
        └── MedicalCaseRepository.cs   ← 删除264-300行的RowVersion同步代码
```

### 代码变更

**删除 MedicalCaseRepository.cs 第264-300行**:

```csharp
// 删除以下代码块 (Issue #1669 Phase 7遗留代码)

//  Issue #1669 Phase 7: InMemory数据库RowVersion同步问题
// 当entity被多次修改时，RowVersion可能不同步，导致并发异常
// 解决方案：将RowVersion的OriginalValue同步为CurrentValue，跳过并发检查
var rowVersionProperty = entry.Property("RowVersion");
if (rowVersionProperty != null)
{
    rowVersionProperty.OriginalValue = rowVersionProperty.CurrentValue;
}

// 同时同步Consultation的RowVersion（Consultation与MedicalCase使用共享主键）
if (entity.Consultation != null)
{
    var consultationEntry = _context.Entry(entity.Consultation);
    var consultationRowVersion = consultationEntry.Property("RowVersion");
    if (consultationRowVersion != null)
    {
        consultationRowVersion.OriginalValue = consultationRowVersion.CurrentValue;
        _logger?.LogDebug(" [RowVersion同步] Consultation RowVersion已同步");
    }
}

// 同步Prescription的RowVersion（如果存在）
if (entity.Prescription != null)
{
    var prescriptionEntry = _context.Entry(entity.Prescription);
    // 只对已存在的Prescription同步RowVersion（Added状态的不需要）
    if (prescriptionEntry.State != EntityState.Added && prescriptionEntry.State != EntityState.Detached)
    {
        var prescriptionRowVersion = prescriptionEntry.Property("RowVersion");
        if (prescriptionRowVersion != null)
        {
            prescriptionRowVersion.OriginalValue = prescriptionRowVersion.CurrentValue;
            _logger?.LogDebug(" [RowVersion同步] Prescription RowVersion已同步");
        }
    }
}
```

---

## 验证计划

### 测试覆盖

1. **单元测试** - MedicalCaseRepository相关测试
2. **集成测试** - MedicalCase CRUD操作
3. **并发测试** - 多次修改同一聚合根

### 验证步骤

```bash
# 1. 运行现有测试
dotnet test tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests

# 2. 验证MedicalCase相关API
# POST /api/v1/medicalcases
# PUT /api/v1/medicalcases/{id}
# POST /api/v1/medicalcases/{id}/prescriptions
```

---

## 风险评估

| 风险 | 等级 | 缓解措施 |
|------|------|----------|
| 并发异常回归 | 低 | BaseRepository已有相同逻辑 |
| 测试失败 | 低 | 全面运行现有测试 |

---

## 决策

- [x] **批准** - 执行此提案
- [ ] **拒绝** - 保持现状
- [ ] **修改** - 需要调整后再审批

---

## 相关Issue/PR

- Issue #1669: InMemory数据库RowVersion同步问题（原始问题）
- Issue #2250: RowVersion错误修复（BaseRepository全局处理）

---

## 审批

| 审批人 | 日期 | 决定 |
|--------|------|------|
| | | |
