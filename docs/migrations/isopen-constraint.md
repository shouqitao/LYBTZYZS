# IsOpen 约束策略实施文档

**Issue**: #626
**日期**: 2025-09-21
**Epic**: server-entity-consistency-optimization-20250921

## 概述

为 MedicalCases 表实现 IsOpen 计算列和唯一约束，确保每个患者同时只能有一个活跃的医疗案例。

## 实现策略

### 1. 计算列设计
- **列名**: IsOpenComputed
- **类型**: bit (nullable)
- **计算逻辑**:
  ```sql
  CASE WHEN [Status] = 'Active' THEN CAST(1 AS BIT) ELSE NULL END
  ```
- **特点**:
  - Status = 'Active' 时返回 1
  - 其他状态返回 NULL
  - SQL Server 唯一约束自动忽略 NULL 值

### 2. 唯一约束设计
- **索引名**: UX_MedicalCases_Patient_OneActive
- **约束列**: (PatientId, IsOpenComputed)
- **效果**: 每个患者只能有一个 IsOpenComputed = 1 的记录

### 3. 实体模型更新
```csharp
// 计算属性（应用层使用）
public bool IsOpen => Status == MedicalCaseStatus.Active;

// 数据库计算列（用于约束）
[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
public bool? IsOpenComputed { get; set; }
```

## 数据库变更

### 添加计算列
```sql
ALTER TABLE MedicalCases
ADD IsOpenComputed AS (
    CASE WHEN [Status] = 'Active' THEN CAST(1 AS BIT) ELSE NULL END
);
```

### 创建唯一索引
```sql
CREATE UNIQUE INDEX UX_MedicalCases_Patient_OneActive
ON MedicalCases (PatientId, IsOpenComputed)
WHERE IsOpenComputed IS NOT NULL;
```

## 验证脚本

### 1. 检查约束是否生效
```sql
-- 尝试为同一患者插入两个 Active 状态的案例（应失败）
DECLARE @PatientId UNIQUEIDENTIFIER = NEWID();

-- 插入第一个活跃案例
INSERT INTO MedicalCases (Id, PatientId, PatientName, DoctorId, DoctorName, Status, CreatedBy, CreatedAt)
VALUES (NEWID(), @PatientId, '测试患者', NEWID(), '测试医生', 'Active', NEWID(), GETDATE());

-- 尝试插入第二个活跃案例（应该失败）
INSERT INTO MedicalCases (Id, PatientId, PatientName, DoctorId, DoctorName, Status, CreatedBy, CreatedAt)
VALUES (NEWID(), @PatientId, '测试患者', NEWID(), '测试医生', 'Active', NEWID(), GETDATE());
-- 预期错误: Violation of UNIQUE KEY constraint 'UX_MedicalCases_Patient_OneActive'
```

### 2. 验证多个非活跃案例可以共存
```sql
DECLARE @PatientId UNIQUEIDENTIFIER = NEWID();

-- 插入多个 Closed 状态的案例（应成功）
INSERT INTO MedicalCases (Id, PatientId, PatientName, DoctorId, DoctorName, Status, CreatedBy, CreatedAt)
VALUES (NEWID(), @PatientId, '测试患者', NEWID(), '测试医生', 'Closed', NEWID(), GETDATE());

INSERT INTO MedicalCases (Id, PatientId, PatientName, DoctorId, DoctorName, Status, CreatedBy, CreatedAt)
VALUES (NEWID(), @PatientId, '测试患者', NEWID(), '测试医生', 'Closed', NEWID(), GETDATE());

-- 验证插入成功
SELECT COUNT(*) FROM MedicalCases WHERE PatientId = @PatientId AND Status = 'Closed';
-- 预期结果: 2
```

### 3. 检查计算列值
```sql
-- 查看计算列的值分布
SELECT
    Status,
    IsOpenComputed,
    COUNT(*) as RecordCount
FROM MedicalCases
GROUP BY Status, IsOpenComputed
ORDER BY Status;
```

### 4. 验证索引信息
```sql
-- 查看索引定义
SELECT
    i.name AS IndexName,
    i.is_unique AS IsUnique,
    c.name AS ColumnName,
    i.filter_definition AS FilterDefinition
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('MedicalCases')
AND i.name = 'UX_MedicalCases_Patient_OneActive';
```

## 回滚脚本

```sql
-- 1. 删除唯一索引
DROP INDEX IF EXISTS UX_MedicalCases_Patient_OneActive ON MedicalCases;

-- 2. 删除计算列
ALTER TABLE MedicalCases DROP COLUMN IF EXISTS IsOpenComputed;

-- 3. 恢复原有索引（如果需要）
CREATE UNIQUE INDEX UX_MedicalCases_Patient_ActiveOnly
ON MedicalCases (PatientId)
WHERE [Status] = 'Active' OR [Status] = 'Draft';
```

## 性能影响评估

### 优势
1. **自动维护**: 计算列自动更新，无需应用层干预
2. **数据完整性**: 数据库级别保证约束
3. **查询优化**: 可利用索引加速查询

### 注意事项
1. **写入性能**: 插入/更新时需要重新计算列值
2. **索引维护**: Status 变更会触发索引更新
3. **空间占用**: 增加计算列和索引的存储开销

## 应用层适配

### Service 层检查
```csharp
public async Task<ServiceResult> CreateMedicalCaseAsync(MedicalCaseCreateDto dto)
{
    // 检查是否已有活跃案例
    var hasActive = await _repository.ExistsAsync(
        m => m.PatientId == dto.PatientId && m.IsOpen
    );

    if (hasActive)
    {
        return ServiceResult.Failure("该患者已有进行中的病案");
    }

    // 继续创建逻辑...
}
```

### 异常处理
```csharp
try
{
    await _repository.CreateAsync(medicalCase);
}
catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UX_MedicalCases_Patient_OneActive") == true)
{
    return ServiceResult.Failure("该患者已有进行中的病案，请先完成或取消");
}
```

## 测试覆盖

### 单元测试
- ✅ 测试 IsOpen 属性计算逻辑
- ✅ 测试唯一约束违反场景
- ✅ 测试多个非活跃案例场景

### 集成测试
- ✅ 测试并发创建活跃案例
- ✅ 测试状态转换时的约束检查
- ✅ 测试事务回滚场景

## 部署检查清单

- [ ] 备份数据库
- [ ] 执行迁移脚本
- [ ] 运行验证脚本
- [ ] 更新应用层代码
- [ ] 执行回归测试
- [ ] 监控错误日志