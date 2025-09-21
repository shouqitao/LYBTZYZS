# 实体一致性规范方案

**Issue**: #628
**日期**: 2025-09-21
**Epic**: server-entity-consistency-optimization-20250921

## 1. 执行摘要

本方案定义了 LYBT 系统中所有实体的一致性规范，确保数据模型的统一性和可维护性。

## 2. 核心规范

### 2.1 状态字段规范

所有实体的状态字段必须遵循以下规则：

#### 数据库存储
- **类型**: int
- **非空**: NOT NULL
- **默认值**: 根据业务需求设置

#### C# 定义
```csharp
public enum EntityStatus
{
    Inactive = 0,    // 停用/未激活
    Active = 1,      // 启用/激活
    Suspended = 2,   // 暂停/挂起
    Deleted = 3      // 已删除（软删除）
}
```

#### EF Core 配置
```csharp
entity.Property(e => e.Status)
      .HasConversion<int>()
      .HasDefaultValue(EntityStatus.Active)
      .IsRequired();
```

### 2.2 审计字段规范

所有业务实体必须包含以下审计字段：

```csharp
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    Guid? CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    Guid? UpdatedBy { get; set; }
}
```

#### 自动维护机制
- 通过 AppDbContext.SaveChanges 重写实现
- 依赖 ICurrentUserService 获取当前用户
- 创建时设置 CreatedAt 和 CreatedBy
- 更新时设置 UpdatedAt 和 UpdatedBy

### 2.3 软删除规范

#### 实现方式
```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    Guid? DeletedBy { get; set; }
}
```

#### 全局查询过滤器
```csharp
modelBuilder.Entity<TEntity>()
    .HasQueryFilter(e => !e.IsDeleted);
```

## 3. 特殊业务约束

### 3.1 MedicalCase IsOpen 约束

#### 业务规则
- 每个患者同时只能有一个 Active 状态的病案
- 通过计算列和唯一约束实现

#### 实现方案
```sql
-- 计算列
IsOpenComputed AS CASE WHEN [Status] = 1 THEN CAST(1 AS BIT) ELSE NULL END

-- 唯一约束
CREATE UNIQUE INDEX IX_MedicalCases_PatientId_IsOpenComputed
ON MedicalCases (PatientId, IsOpenComputed)
WHERE IsOpenComputed IS NOT NULL
```

### 3.2 关系删除行为

#### 级联删除（Cascade）
仅用于紧密耦合的父子关系：
- MedicalCase → Consultation
- MedicalCase → Prescription
- Prescription → PrescriptionItem

#### 限制删除（Restrict）
用于大部分业务关系：
- Patient → MedicalCase
- User → MedicalCase
- Herb → PrescriptionItem

## 4. 索引策略

### 4.1 必需索引

#### 外键索引
所有外键字段必须创建索引

#### 状态索引
```csharp
entity.HasIndex(e => e.Status)
      .HasDatabaseName("IX_{Entity}_Status");
```

### 4.2 复合索引

#### 高频查询模式
```csharp
// 患者病案查询
entity.HasIndex(e => new { e.PatientId, e.CreatedAt })
      .HasDatabaseName("IX_MedicalCases_PatientId_CreatedAt");

// 医生工作量统计
entity.HasIndex(e => new { e.DoctorId, e.CreatedAt })
      .HasDatabaseName("IX_MedicalCases_DoctorId_CreatedAt")
      .HasFilter("[Status] = 1");
```

### 4.3 覆盖索引

```csharp
// 患者列表查询优化
entity.HasIndex(e => new { e.Status, e.Name })
      .HasDatabaseName("IX_Patients_Status_Name")
      .IncludeProperties(e => new { e.PhoneNumber, e.Gender, e.Age })
      .HasFilter("[Status] = 1");
```

## 5. 数据迁移策略

### 5.1 状态字段迁移

#### 字符串到枚举映射
```sql
UPDATE MedicalCases SET Status =
    CASE Status
        WHEN 'Active' THEN 1
        WHEN 'Completed' THEN 0
        WHEN 'Cancelled' THEN 3
        ELSE 1
    END
```

### 5.2 审计字段初始化

```sql
-- 设置默认创建时间
UPDATE {TableName}
SET CreatedAt = ISNULL(CreatedAt, GETDATE())
WHERE CreatedAt IS NULL

-- 设置默认创建人（系统用户）
UPDATE {TableName}
SET CreatedBy = '00000000-0000-0000-0000-000000000000'
WHERE CreatedBy IS NULL
```

## 6. ArchTests 约束规范

### 6.1 实体规范测试

```csharp
[Fact]
public void AllEntities_Should_HaveAuditFields()
{
    var result = Types.InAssembly(typeof(BaseEntity).Assembly)
        .That().Inherit(typeof(BaseEntity))
        .Should().ImplementInterface(typeof(IAuditable))
        .GetResult();

    result.IsSuccessful.Should().BeTrue();
}

[Fact]
public void StatusProperty_Should_UseIntConversion()
{
    var entityTypes = typeof(AppDbContext).GetProperties()
        .Where(p => p.PropertyType.Name.StartsWith("DbSet"))
        .Select(p => p.PropertyType.GenericTypeArguments[0]);

    foreach (var entityType in entityTypes)
    {
        var statusProperty = entityType.GetProperty("Status");
        if (statusProperty != null)
        {
            // Verify HasConversion<int>() is configured
        }
    }
}
```

### 6.2 关系规范测试

```csharp
[Fact]
public void ForeignKeys_Should_HaveRestrictDeleteBehavior()
{
    var context = new AppDbContext(GetOptions());
    var model = context.Model;

    var foreignKeys = model.GetEntityTypes()
        .SelectMany(e => e.GetForeignKeys())
        .Where(fk => !IsAllowedCascadeRelation(fk));

    foreach (var fk in foreignKeys)
    {
        fk.DeleteBehavior.Should().Be(DeleteBehavior.Restrict,
            $"Foreign key {fk} should use Restrict delete behavior");
    }
}
```

## 7. 监控与维护

### 7.1 索引效率监控

```sql
-- 索引使用统计
SELECT
    OBJECT_NAME(s.object_id) AS TableName,
    i.name AS IndexName,
    s.user_seeks + s.user_scans + s.user_lookups AS TotalReads,
    s.user_updates AS TotalWrites,
    CAST(s.user_seeks + s.user_scans + s.user_lookups AS float) /
        NULLIF(s.user_updates, 0) AS ReadWriteRatio
FROM sys.dm_db_index_usage_stats s
INNER JOIN sys.indexes i ON s.object_id = i.object_id
    AND s.index_id = i.index_id
WHERE s.database_id = DB_ID()
ORDER BY TotalReads DESC
```

### 7.2 数据一致性检查

```sql
-- 检查重复的 Active 病案
SELECT PatientId, COUNT(*) AS ActiveCount
FROM MedicalCases
WHERE Status = 1
GROUP BY PatientId
HAVING COUNT(*) > 1

-- 检查缺失审计字段的记录
SELECT COUNT(*) AS MissingAudit
FROM {TableName}
WHERE CreatedAt IS NULL OR CreatedBy IS NULL
```

## 8. 版本管理

### 当前版本
- v1.0.0 (2025-09-21): 初始规范发布

### 更新记录
- 统一状态字段为 int 存储
- 实现审计字段自动维护
- 添加 IsOpen 唯一性约束
- 优化查询索引策略

## 9. 参考文档

- [迁移指南](./migrations-guide.md)
- [索引与删除规则](./indexing-and-deletion-rules.md)
- [状态字段统一方案](./status-fields-unification.md)
- [关系与索引优化](./relationships-and-indexes.md)