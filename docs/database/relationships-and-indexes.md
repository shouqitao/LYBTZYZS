# 数据库关系与索引优化方案

**Issue**: #627
**日期**: 2025-09-21
**Epic**: server-entity-consistency-optimization-20250921

## 1. OnDelete 规则规范

### 核心原则
- **Restrict（默认）**: 大部分关系使用，防止意外级联删除
- **Cascade**: 仅用于紧密耦合的父子关系
- **SetNull**: 用于可选关联
- **NoAction**: 避免使用，可能导致数据不一致

### 关系配置明细

#### MedicalCase 相关
```csharp
// MedicalCase -> Consultation (1:1, 紧密耦合)
entity.HasOne(c => c.MedicalCase)
      .WithOne(m => m.Consultation)
      .HasForeignKey<Consultation>(c => c.MedicalCaseId)
      .OnDelete(DeleteBehavior.Cascade);  // 保持级联

// MedicalCase -> Prescription (1:0..1, 紧密耦合)
prescriptionEntity.HasOne<MedicalCase>()
                 .WithOne(m => m.Prescription)
                 .HasForeignKey<Prescription>(p => p.MedicalCaseId)
                 .OnDelete(DeleteBehavior.Cascade);  // 保持级联

// Prescription -> PrescriptionItem (1:N, 紧密耦合)
itemEntity.HasOne<Prescription>()
         .WithMany(p => p.Items)
         .HasForeignKey(i => i.PrescriptionId)
         .OnDelete(DeleteBehavior.Cascade);  // 保持级联
```

#### 建议修改为 Restrict 的关系
```csharp
// Patient -> MedicalCase (1:N)
// 删除患者不应自动删除所有病案
entity.HasOne<Patient>()
      .WithMany()
      .HasForeignKey(m => m.PatientId)
      .OnDelete(DeleteBehavior.Restrict);

// User -> MedicalCase (1:N)
// 删除医生不应自动删除病案
entity.HasOne<User>()
      .WithMany()
      .HasForeignKey(m => m.DoctorId)
      .OnDelete(DeleteBehavior.Restrict);
```

## 2. 索引优化建议

### 高频查询索引

#### 复合索引
```sql
-- 患者病案查询（按时间排序）
CREATE INDEX IX_MedicalCases_PatientId_CreatedAt
ON MedicalCases (PatientId, CreatedAt DESC)
INCLUDE (Status, PatientName, DoctorName);

-- 医生工作量统计
CREATE INDEX IX_MedicalCases_DoctorId_CreatedAt
ON MedicalCases (DoctorId, CreatedAt DESC)
WHERE Status = 'Active';

-- 处方查询优化
CREATE INDEX IX_Prescriptions_MedicalCaseId_CreatedAt
ON Prescriptions (MedicalCaseId, CreatedAt DESC)
INCLUDE (Status);

-- 药材使用统计
CREATE INDEX IX_PrescriptionItems_HerbId_CreatedDate
ON PrescriptionItems (HerbId, CreatedDate DESC)
INCLUDE (Quantity, UnitPrice);
```

#### 覆盖索引
```sql
-- 患者列表查询
CREATE INDEX IX_Patients_Status_Name
ON Patients (Status, Name)
INCLUDE (PhoneNumber, Gender, Age)
WHERE Status = 1;  -- 只索引启用的患者

-- 用户登录查询
CREATE INDEX IX_Users_Username_Status
ON Users (Username, Status)
INCLUDE (PasswordHash, Role, RealName)
WHERE Status = 1;
```

### 现有索引评估

#### 保留的索引
- ✅ `IX_Users_Username` - 唯一性约束，必须保留
- ✅ `IX_MedicalCases_PatientId` - 高频外键查询
- ✅ `IX_MedicalCases_Status` - 状态过滤查询
- ✅ `IX_SystemLogs_Timestamp_Level` - 日志查询优化

#### 建议删除的索引
- ❌ 单列外键索引（如果有复合索引覆盖）
- ❌ 低选择性索引（如 Gender, IsDeleted）

## 3. 实施计划

### Phase 1: OnDelete 规则调整
```csharp
// AppDbContext.cs 修改
private static void ConfigureMedicalCases(ModelBuilder modelBuilder)
{
    var entity = modelBuilder.Entity<MedicalCase>();

    // 添加外键关系配置
    entity.HasOne<Patient>()
          .WithMany()
          .HasForeignKey(m => m.PatientId)
          .OnDelete(DeleteBehavior.Restrict);

    entity.HasOne<User>()
          .WithMany()
          .HasForeignKey(m => m.DoctorId)
          .OnDelete(DeleteBehavior.Restrict);
}
```

### Phase 2: 添加复合索引
```csharp
// 在各配置方法中添加
entity.HasIndex(m => new { m.PatientId, m.CreatedAt })
      .HasDatabaseName("IX_MedicalCases_PatientId_CreatedAt");

entity.HasIndex(m => new { m.DoctorId, m.CreatedAt })
      .HasDatabaseName("IX_MedicalCases_DoctorId_CreatedAt")
      .HasFilter("[Status] = 'Active'");
```

### Phase 3: 迁移脚本
```bash
# 生成迁移
dotnet ef migrations add OptimizeRelationshipsAndIndexes

# 应用迁移
dotnet ef database update
```

## 4. 性能基准测试

### 测试场景
1. **患者病案列表查询**
   - 原始: ~200ms
   - 优化后目标: <50ms

2. **医生工作量统计**
   - 原始: ~500ms
   - 优化后目标: <100ms

3. **处方药材统计**
   - 原始: ~300ms
   - 优化后目标: <80ms

### 测试SQL
```sql
-- 测试1: 患者病案查询
SELECT TOP 20 * FROM MedicalCases
WHERE PatientId = @PatientId
ORDER BY CreatedAt DESC;

-- 测试2: 医生当日工作量
SELECT COUNT(*) FROM MedicalCases
WHERE DoctorId = @DoctorId
AND CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)
AND Status = 'Active';

-- 测试3: 药材使用TOP10
SELECT TOP 10
    h.Name,
    SUM(pi.Quantity) as TotalQuantity
FROM PrescriptionItems pi
INNER JOIN Herbs h ON pi.HerbId = h.Id
WHERE pi.CreatedDate >= DATEADD(DAY, -30, GETDATE())
GROUP BY h.Name
ORDER BY TotalQuantity DESC;
```

## 5. 监控指标

### 索引使用率监控
```sql
SELECT
    OBJECT_NAME(s.object_id) AS TableName,
    i.name AS IndexName,
    s.user_seeks,
    s.user_scans,
    s.user_lookups,
    s.user_updates
FROM sys.dm_db_index_usage_stats s
INNER JOIN sys.indexes i ON s.object_id = i.object_id
    AND s.index_id = i.index_id
WHERE s.database_id = DB_ID()
ORDER BY s.user_seeks + s.user_scans + s.user_lookups DESC;
```

### 缺失索引分析
```sql
SELECT
    migs.avg_total_user_cost * (migs.avg_user_impact / 100.0) *
    (migs.user_seeks + migs.user_scans) AS improvement_measure,
    'CREATE INDEX missing_index_' +
    CONVERT(varchar, mig.index_group_handle) + '_' +
    CONVERT(varchar, mid.index_handle) + ' ON ' +
    mid.statement + ' (' + ISNULL(mid.equality_columns,'') +
    CASE WHEN mid.equality_columns IS NOT NULL
         AND mid.inequality_columns IS NOT NULL
    THEN ',' ELSE '' END + ISNULL(mid.inequality_columns, '') + ')' +
    ISNULL(' INCLUDE (' + mid.included_columns + ')', '') AS create_index_statement
FROM sys.dm_db_missing_index_groups mig
INNER JOIN sys.dm_db_missing_index_group_stats migs
    ON migs.group_handle = mig.index_group_handle
INNER JOIN sys.dm_db_missing_index_details mid
    ON mig.index_handle = mid.index_handle
WHERE migs.avg_total_user_cost * (migs.avg_user_impact / 100.0) *
      (migs.user_seeks + migs.user_scans) > 10
ORDER BY improvement_measure DESC;
```

## 6. 回滚计划

### 删除新增索引
```sql
DROP INDEX IF EXISTS IX_MedicalCases_PatientId_CreatedAt ON MedicalCases;
DROP INDEX IF EXISTS IX_MedicalCases_DoctorId_CreatedAt ON MedicalCases;
DROP INDEX IF EXISTS IX_Prescriptions_MedicalCaseId_CreatedAt ON Prescriptions;
DROP INDEX IF EXISTS IX_PrescriptionItems_HerbId_CreatedDate ON PrescriptionItems;
```

### 恢复OnDelete规则
通过 EF 迁移回滚：
```bash
dotnet ef database update [PreviousMigration]
```

## 7. 注意事项

1. **索引维护成本**: 新增索引会增加写操作开销
2. **存储空间**: 预计增加 5-10% 的存储占用
3. **重建时间**: 大表索引重建可能需要维护窗口
4. **监控周期**: 实施后持续监控 2 周
5. **备份策略**: 实施前完整备份数据库

## 8. 验收标准

- ✅ 所有 OnDelete 规则明确配置
- ✅ 高频查询响应时间降低 50% 以上
- ✅ 索引碎片率 < 30%
- ✅ 无死锁和超时问题
- ✅ 通过性能基准测试