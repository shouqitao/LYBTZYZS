# P1 Batch1 - EF Decimal 精度统一分析报告

**生成时间**: 2025-09-18  
**任务**: P1 Batch1 第四项 - EF decimal 精度统一

## 🎯 任务目标

审计所有 decimal 映射，统一使用 HasPrecision 方法配置，解决精度不一致问题。

## 📊 精度现状分析

### 发现的 Decimal 字段

通过扫描代码库，发现以下 decimal 字段及其当前精度设置：

| 实体 | 字段 | 当前精度 | 用途 | 建议精度 |
|------|------|----------|------|----------|
| Herb | Price | decimal(18,2) | 药材单价 | ✅ 保持 |
| Herb | CostPrice | decimal(18,2) | 药材成本价 | ✅ 保持 |
| PrescriptionItem | UnitPrice | decimal(18,2) | 处方药材单价 | ✅ 保持 |
| PrescriptionItem | Quantity | decimal(10,3) | 药材用量 | ❌ 修改为 decimal(10,2) |
| Prescription | Discount | decimal(3,2) | 折扣(0-1) | ❌ 修改为 decimal(5,4) |
| User | RegistrationFee | decimal(18,2) | 挂号费 | ✅ 保持 |

### 问题识别

1. **药材用量精度不统一**: `Quantity` 使用 `decimal(10,3)`，但医疗用量通常以小数点后两位足够
2. **折扣精度不够**: `Discount` 使用 `decimal(3,2)`，但折扣值可能需要更高精度（如95.25%）
3. **配置方式不统一**: 部分使用 `HasColumnType("decimal(18,2)")`，部分使用实体注解

## 🔧 修复方案

### 1. 统一精度标准

- **金融字段**: `decimal(18,2)` - 价格、费用、金额
- **数量字段**: `decimal(10,2)` - 重量、用量、数量  
- **比率字段**: `decimal(5,4)` - 折扣、比例（支持99.9999%）

### 2. 配置方式统一

统一使用 EF Core 的 `HasPrecision()` 方法替代 `HasColumnType()`:

```csharp
// 替换前
entity.Property(h => h.Price).HasColumnType("decimal(18,2)");

// 替换后  
entity.Property(h => h.Price).HasPrecision(18, 2);
```

### 3. 需要修改的字段

1. **PrescriptionItem.Quantity**: `decimal(10,3)` → `decimal(10,2)`
2. **Prescription.Discount**: `decimal(3,2)` → `decimal(5,4)`

## 📋 实施计划

1. **阶段1**: 更新 AppDbContext 配置方法，统一使用 HasPrecision
2. **阶段2**: 调整精度不合理的字段精度
3. **阶段3**: 生成和应用EF迁移
4. **阶段4**: 更新实体注解保持一致性

## ⚠️ 影响评估

### 数据库变更影响
- **低风险**: 金融字段精度保持不变
- **中风险**: Quantity精度从3位降为2位（需要数据验证）
- **低风险**: Discount精度提升，向下兼容

### 代码影响
- **最小影响**: 仅配置层变更，业务逻辑不变
- **向后兼容**: 现有计算逻辑保持有效

## 🎯 验收标准

- [ ] 所有 decimal 字段使用 HasPrecision() 配置
- [ ] 金融字段统一为 decimal(18,2)
- [ ] 数量字段统一为 decimal(10,2) 
- [ ] 比率字段统一为 decimal(5,4)
- [ ] EF 迁移生成并成功应用
- [ ] 编译零警告零错误

## 📝 备注

此修复属于工程一致性调整，不改变业务逻辑，符合P1 Batch1"不改业务逻辑，仅做工程与映射一致性调整"的要求。