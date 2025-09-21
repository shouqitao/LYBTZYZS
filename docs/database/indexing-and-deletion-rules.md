# 索引与删除规则规范

**Issue**: #628
**日期**: 2025-09-21
**版本**: v1.0.0

## 1. 索引设计规范

### 1.1 索引类型与应用场景

#### 聚集索引 (Clustered Index)
- **定义**: 决定表数据物理存储顺序的索引
- **数量**: 每表只能有一个
- **最佳实践**: 使用自增 ID 作为主键聚集索引

```sql
-- 推荐做法
CREATE TABLE Patients (
    Id UNIQUEIDENTIFIER PRIMARY KEY CLUSTERED DEFAULT NEWID(),
    -- 其他字段
)
```

#### 非聚集索引 (Non-Clustered Index)
- **定义**: 独立于表数据的索引结构
- **数量**: 每表最多 999 个
- **应用**: 查询优化、约束实现

### 1.2 索引命名规范

```
IX_{TableName}_{Column1}_{Column2}..._{Purpose}

示例:
IX_MedicalCases_PatientId_CreatedAt        # 复合索引
IX_Users_Username_Unique                   # 唯一索引
IX_Patients_Status_Active                  # 过滤索引
IX_Prescriptions_MedicalCaseId_Include     # 覆盖索引
```

### 1.3 必需索引清单

#### 1.3.1 主键索引
所有表必须有主键索引（自动创建）

#### 1.3.2 外键索引
```csharp
// EF Core 自动配置
entity.HasOne<Patient>()
      .WithMany()
      .HasForeignKey(m => m.PatientId)
      .HasConstraintName("FK_MedicalCases_Patients");

// 对应索引自动创建
// IX_MedicalCases_PatientId
```

#### 1.3.3 状态字段索引
```csharp
entity.HasIndex(e => e.Status)
      .HasDatabaseName("IX_{Entity}_Status");
```

#### 1.3.4 唯一性约束索引
```csharp
// 用户名唯一性
entity.HasIndex(u => u.Username)
      .IsUnique()
      .HasDatabaseName("IX_Users_Username_Unique");

// 病案唯一性约束（带条件）
entity.HasIndex(m => new { m.PatientId, m.IsOpenComputed })
      .IsUnique()
      .HasDatabaseName("IX_MedicalCases_PatientId_IsOpenComputed")
      .HasFilter("[IsOpenComputed] IS NOT NULL");
```

## 2. 复合索引策略

### 2.1 设计原则

1. **选择性优先**: 选择性高的列放在前面
2. **查询模式**: 基于实际查询模式设计
3. **排序需求**: 考虑 ORDER BY 子句
4. **过滤条件**: 考虑 WHERE 子句

### 2.2 高频查询索引

#### 患者病案查询
```csharp
// 按患者查看历史病案（按时间倒序）
entity.HasIndex(m => new { m.PatientId, m.CreatedAt })
      .HasDatabaseName("IX_MedicalCases_PatientId_CreatedAt");

// 对应查询
// SELECT * FROM MedicalCases
// WHERE PatientId = @patientId
// ORDER BY CreatedAt DESC
```

#### 医生工作量统计
```csharp
// 医生按日期查询工作量（仅活跃病案）
entity.HasIndex(m => new { m.DoctorId, m.CreatedAt })
      .HasDatabaseName("IX_MedicalCases_DoctorId_CreatedAt")
      .HasFilter("[Status] = 1");

// 对应查询
// SELECT COUNT(*) FROM MedicalCases
// WHERE DoctorId = @doctorId
// AND Status = 1
// AND CAST(CreatedAt AS DATE) = @date
```

#### 处方药材统计
```csharp
// 药材使用频率统计
entity.HasIndex(pi => new { pi.HerbId, pi.CreatedDate })
      .HasDatabaseName("IX_PrescriptionItems_HerbId_CreatedDate");

// 对应查询
// SELECT HerbId, SUM(Quantity)
// FROM PrescriptionItems
// WHERE CreatedDate >= @startDate
// GROUP BY HerbId
```

### 2.3 覆盖索引设计

#### 患者列表查询优化
```csharp
entity.HasIndex(p => new { p.Status, p.Name })
      .HasDatabaseName("IX_Patients_Status_Name")
      .IncludeProperties(p => new { p.PhoneNumber, p.Gender, p.Age })
      .HasFilter("[Status] = 1");

// 优化查询（避免回表）
// SELECT Name, PhoneNumber, Gender, Age
// FROM Patients
// WHERE Status = 1
// ORDER BY Name
```

#### 用户登录查询优化
```csharp
entity.HasIndex(u => new { u.Username, u.Status })
      .HasDatabaseName("IX_Users_Username_Status")
      .IncludeProperties(u => new { u.PasswordHash, u.Role, u.RealName })
      .HasFilter("[Status] = 1");
```

## 3. 删除行为规范

### 3.1 删除行为类型

#### Cascade（级联删除）
**适用场景**: 紧密耦合的父子关系

```csharp
// MedicalCase -> Consultation (1:1)
entity.HasOne(c => c.MedicalCase)
      .WithOne(m => m.Consultation)
      .HasForeignKey<Consultation>(c => c.MedicalCaseId)
      .OnDelete(DeleteBehavior.Cascade);

// MedicalCase -> Prescription (1:0..1)
entity.HasOne<MedicalCase>()
      .WithOne(m => m.Prescription)
      .HasForeignKey<Prescription>(p => p.MedicalCaseId)
      .OnDelete(DeleteBehavior.Cascade);

// Prescription -> PrescriptionItem (1:N)
entity.HasOne<Prescription>()
      .WithMany(p => p.Items)
      .HasForeignKey(i => i.PrescriptionId)
      .OnDelete(DeleteBehavior.Cascade);
```

#### Restrict（限制删除）
**适用场景**: 业务独立性强的关系（默认推荐）

```csharp
// Patient -> MedicalCase (1:N)
entity.HasOne<Patient>()
      .WithMany()
      .HasForeignKey(m => m.PatientId)
      .OnDelete(DeleteBehavior.Restrict);

// User -> MedicalCase (1:N)
entity.HasOne<User>()
      .WithMany()
      .HasForeignKey(m => m.DoctorId)
      .OnDelete(DeleteBehavior.Restrict);

// Herb -> PrescriptionItem (1:N)
entity.HasOne<Herb>()
      .WithMany()
      .HasForeignKey(pi => pi.HerbId)
      .OnDelete(DeleteBehavior.Restrict);
```

#### SetNull（设为空值）
**适用场景**: 可选关联关系

```csharp
// 可选的上级用户关系
entity.HasOne<User>()
      .WithMany()
      .HasForeignKey(u => u.SupervisorId)
      .OnDelete(DeleteBehavior.SetNull)
      .IsRequired(false);
```

### 3.2 删除行为决策矩阵

| 关系类型 | 业务耦合度 | 删除行为 | 原因 |
|---------|----------|---------|------|
| MedicalCase → Consultation | 高 (1:1) | Cascade | 病案删除时诊断信息应一起删除 |
| MedicalCase → Prescription | 高 (1:0..1) | Cascade | 病案删除时处方应一起删除 |
| Prescription → PrescriptionItem | 高 (1:N) | Cascade | 处方删除时处方项应一起删除 |
| Patient → MedicalCase | 中 (1:N) | Restrict | 患者删除不应影响历史病案 |
| User → MedicalCase | 中 (1:N) | Restrict | 医生离职不应删除病案记录 |
| Herb → PrescriptionItem | 低 (1:N) | Restrict | 药材停用不应影响历史处方 |

### 3.3 软删除实现

对于重要业务数据，推荐使用软删除：

```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    Guid? DeletedBy { get; set; }
}

// 全局查询过滤器
modelBuilder.Entity<Patient>()
    .HasQueryFilter(p => !p.IsDeleted);

// 软删除实现
public async Task SoftDeleteAsync<T>(T entity) where T : ISoftDeletable
{
    entity.IsDeleted = true;
    entity.DeletedAt = DateTime.UtcNow;
    entity.DeletedBy = _currentUserService.UserId;
    await _context.SaveChangesAsync();
}
```

## 4. 索引维护策略

### 4.1 索引监控

#### 索引使用统计
```sql
SELECT
    OBJECT_NAME(s.object_id) AS TableName,
    i.name AS IndexName,
    s.user_seeks,
    s.user_scans,
    s.user_lookups,
    s.user_updates,
    CAST(s.user_seeks + s.user_scans + s.user_lookups AS float) /
        NULLIF(s.user_updates, 0) AS ReadWriteRatio
FROM sys.dm_db_index_usage_stats s
INNER JOIN sys.indexes i ON s.object_id = i.object_id AND s.index_id = i.index_id
WHERE s.database_id = DB_ID()
    AND OBJECT_NAME(s.object_id) LIKE '%MedicalCases%'
ORDER BY ReadWriteRatio DESC
```

#### 缺失索引分析
```sql
SELECT
    migs.avg_total_user_cost * migs.avg_user_impact * (migs.user_seeks + migs.user_scans) AS improvement_measure,
    'CREATE INDEX missing_index_' + CONVERT(varchar, mig.index_group_handle) + '_' +
    CONVERT(varchar, mid.index_handle) + ' ON ' + mid.statement + ' (' +
    ISNULL(mid.equality_columns,'') +
    CASE WHEN mid.equality_columns IS NOT NULL AND mid.inequality_columns IS NOT NULL
         THEN ',' ELSE '' END + ISNULL(mid.inequality_columns, '') + ')' +
    ISNULL(' INCLUDE (' + mid.included_columns + ')', '') AS create_index_statement
FROM sys.dm_db_missing_index_groups mig
INNER JOIN sys.dm_db_missing_index_group_stats migs ON migs.group_handle = mig.index_group_handle
INNER JOIN sys.dm_db_missing_index_details mid ON mig.index_handle = mid.index_handle
WHERE migs.avg_total_user_cost * migs.avg_user_impact * (migs.user_seeks + migs.user_scans) > 10
ORDER BY improvement_measure DESC
```

### 4.2 索引维护周期

#### 每日监控
- 检查索引碎片率
- 监控查询性能
- 分析慢查询日志

#### 每周维护
```sql
-- 重建高碎片率索引 (>30%)
ALTER INDEX ALL ON MedicalCases REBUILD
WITH (ONLINE = ON, MAXDOP = 4)

-- 重新组织中等碎片率索引 (10-30%)
ALTER INDEX IX_MedicalCases_PatientId_CreatedAt ON MedicalCases REORGANIZE
```

#### 每月分析
- 评估索引效益
- 清理无用索引
- 优化查询计划

### 4.3 索引性能基准

#### 建立基线
```sql
-- 查询执行时间基准
DECLARE @StartTime DATETIME2 = GETDATE()

-- 测试查询
SELECT TOP 20 * FROM MedicalCases
WHERE PatientId = '12345678-1234-1234-1234-123456789012'
ORDER BY CreatedAt DESC

DECLARE @Duration INT = DATEDIFF(MILLISECOND, @StartTime, GETDATE())
PRINT '查询耗时: ' + CAST(@Duration AS VARCHAR) + 'ms'
```

#### 性能目标
| 查询类型 | 目标响应时间 | 优化前基线 | 优化后目标 |
|----------|-------------|-----------|-----------|
| 患者病案列表 | <50ms | ~200ms | <50ms |
| 医生工作量统计 | <100ms | ~500ms | <100ms |
| 处方药材统计 | <80ms | ~300ms | <80ms |
| 用户登录验证 | <10ms | ~50ms | <10ms |

## 5. 故障处理指南

### 5.1 索引相关问题

#### 索引碎片过高
```sql
-- 检查碎片率
SELECT
    OBJECT_NAME(ips.object_id) AS TableName,
    i.name AS IndexName,
    ips.avg_fragmentation_in_percent,
    ips.page_count
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'DETAILED') ips
INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
WHERE ips.avg_fragmentation_in_percent > 10
ORDER BY ips.avg_fragmentation_in_percent DESC

-- 处理方案
-- >30%: REBUILD
-- 10-30%: REORGANIZE
-- <10%: 无需处理
```

#### 索引阻塞问题
```sql
-- 检查阻塞会话
SELECT
    blocking_session_id,
    session_id,
    wait_type,
    wait_resource,
    wait_time
FROM sys.dm_exec_requests
WHERE blocking_session_id <> 0

-- 强制终止阻塞会话（谨慎使用）
-- KILL [session_id]
```

### 5.2 删除约束冲突

#### 外键约束冲突
```sql
-- 检查引用关系
SELECT
    fk.name AS ForeignKeyName,
    tp.name AS ParentTable,
    tr.name AS ReferencedTable,
    cp.name AS ParentColumn,
    cr.name AS ReferencedColumn
FROM sys.foreign_keys fk
INNER JOIN sys.tables tp ON fk.parent_object_id = tp.object_id
INNER JOIN sys.tables tr ON fk.referenced_object_id = tr.object_id
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.columns cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
INNER JOIN sys.columns cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id
WHERE tp.name = 'MedicalCases'
```

#### 解决方案
1. **业务层处理**: 检查关联数据
2. **级联删除**: 修改约束为 CASCADE（谨慎）
3. **软删除**: 使用软删除替代物理删除

## 6. 测试与验证

### 6.1 索引效果测试

```csharp
[Fact]
public async Task PatientMedicalCasesQuery_Should_UseOptimalIndex()
{
    // Arrange
    var patientId = Guid.NewGuid();
    await SeedTestData(patientId, 1000); // 1000条病案

    // Act
    var stopwatch = Stopwatch.StartNew();
    var result = await _context.MedicalCases
        .Where(m => m.PatientId == patientId)
        .OrderByDescending(m => m.CreatedAt)
        .Take(20)
        .ToListAsync();
    stopwatch.Stop();

    // Assert
    result.Should().HaveCount(20);
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(50);
}
```

### 6.2 删除行为测试

```csharp
[Fact]
public async Task DeletePatient_Should_RestrictWhenHasMedicalCases()
{
    // Arrange
    var patient = new Patient { Name = "Test Patient" };
    var medicalCase = new MedicalCase { PatientId = patient.Id };
    _context.Patients.Add(patient);
    _context.MedicalCases.Add(medicalCase);
    await _context.SaveChangesAsync();

    // Act & Assert
    _context.Patients.Remove(patient);
    var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
        _context.SaveChangesAsync());

    exception.InnerException.Should().BeOfType<SqlException>();
}
```

## 7. 版本管理

### 当前版本
- v1.0.0 (2025-09-21): 初始规范发布

### 更新记录
- 建立索引设计规范
- 定义删除行为标准
- 创建维护监控体系

## 8. 参考文档

- [实体一致性规范](./entity-consistency-plan.md)
- [迁移指南](./migrations-guide.md)
- [SQL Server 索引设计指南](https://docs.microsoft.com/sql/relational-databases/indexes/)
- [Entity Framework Core 关系配置](https://docs.microsoft.com/ef/core/modeling/relationships/)