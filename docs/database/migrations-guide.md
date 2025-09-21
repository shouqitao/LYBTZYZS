# 数据库迁移指南

**Issue**: #628
**日期**: 2025-09-21
**版本**: v1.0.0

## 1. 迁移概述

本指南提供 LYBT 系统数据库迁移的最佳实践和标准流程。

## 2. 迁移命令

### 2.1 创建迁移

```bash
# 标准迁移命令
dotnet ef migrations add [MigrationName] \
    --project src/Server/Core/LYBT.Infrastructure \
    --startup-project src/Server/Services/LYBT.WebAPI

# 示例
dotnet ef migrations add UnifyStatusFieldsToInt \
    --project src/Server/Core/LYBT.Infrastructure \
    --startup-project src/Server/Services/LYBT.WebAPI
```

### 2.2 应用迁移

```bash
# 更新数据库
dotnet ef database update \
    --project src/Server/Core/LYBT.Infrastructure \
    --startup-project src/Server/Services/LYBT.WebAPI

# 更新到特定迁移
dotnet ef database update [MigrationName] \
    --project src/Server/Core/LYBT.Infrastructure \
    --startup-project src/Server/Services/LYBT.WebAPI
```

### 2.3 回滚迁移

```bash
# 回滚最后一个迁移
dotnet ef database update [PreviousMigrationName] \
    --project src/Server/Core/LYBT.Infrastructure \
    --startup-project src/Server/Services/LYBT.WebAPI

# 生成回滚脚本
dotnet ef migrations script [FromMigration] [ToMigration] \
    --project src/Server/Core/LYBT.Infrastructure \
    --startup-project src/Server/Services/LYBT.WebAPI \
    --output rollback.sql
```

## 3. 迁移类型

### 3.1 结构迁移

更改表结构、字段类型、约束等。

#### 示例：状态字段类型更改
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // 1. 添加临时列
    migrationBuilder.AddColumn<int>(
        name: "StatusTemp",
        table: "MedicalCases",
        nullable: false,
        defaultValue: 1);

    // 2. 数据迁移
    migrationBuilder.Sql(@"
        UPDATE MedicalCases SET StatusTemp =
            CASE Status
                WHEN 'Active' THEN 1
                WHEN 'Inactive' THEN 0
                WHEN 'Completed' THEN 2
                ELSE 1
            END");

    // 3. 删除旧列
    migrationBuilder.DropColumn(
        name: "Status",
        table: "MedicalCases");

    // 4. 重命名新列
    migrationBuilder.RenameColumn(
        name: "StatusTemp",
        table: "MedicalCases",
        newName: "Status");
}
```

### 3.2 数据迁移

仅修改数据，不改变结构。

#### 示例：审计字段初始化
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(@"
        UPDATE Patients
        SET CreatedAt = ISNULL(CreatedAt, GETDATE()),
            CreatedBy = ISNULL(CreatedBy, '00000000-0000-0000-0000-000000000000')
        WHERE CreatedAt IS NULL");
}
```

### 3.3 索引迁移

添加或修改索引以优化性能。

#### 示例：复合索引创建
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateIndex(
        name: "IX_MedicalCases_PatientId_CreatedAt",
        table: "MedicalCases",
        columns: new[] { "PatientId", "CreatedAt" },
        descending: new[] { false, true });

    // 带过滤条件的索引
    migrationBuilder.Sql(@"
        CREATE INDEX IX_MedicalCases_DoctorId_CreatedAt_Active
        ON MedicalCases (DoctorId, CreatedAt DESC)
        WHERE Status = 1");
}
```

## 4. 迁移最佳实践

### 4.1 命名规范

```
[Action][Target][Purpose]

示例：
- AddAuditFieldsToEntities
- UnifyStatusFieldsToInt
- CreateIndexForPatientSearch
- FixDuplicateMedicalCases
```

### 4.2 迁移原则

1. **原子性**: 每个迁移应该是一个完整的、可独立回滚的单元
2. **幂等性**: 迁移可以安全地重复执行
3. **向后兼容**: 考虑应用程序的滚动更新
4. **数据保护**: 永远不要在迁移中删除数据，除非有明确的备份策略

### 4.3 测试策略

```csharp
[Fact]
public void Migration_Should_PreserveData()
{
    // Arrange
    var options = CreateInMemoryDatabaseOptions();
    using var context = new AppDbContext(options);

    // 添加测试数据
    var patient = new Patient { Name = "Test", Status = "Active" };
    context.Patients.Add(patient);
    context.SaveChanges();

    // Act
    // 应用迁移
    ApplyMigration(context);

    // Assert
    var migratedPatient = context.Patients.First();
    migratedPatient.Status.Should().Be(PatientStatus.Active);
}
```

## 5. 生产环境迁移流程

### 5.1 准备阶段

- [ ] 1. 完整备份生产数据库
- [ ] 2. 在测试环境验证迁移
- [ ] 3. 准备回滚脚本
- [ ] 4. 评估迁移时间和影响

### 5.2 执行阶段

```bash
# 1. 生成迁移脚本
dotnet ef migrations script \
    --project src/Server/Core/LYBT.Infrastructure \
    --startup-project src/Server/Services/LYBT.WebAPI \
    --output migration_prod.sql

# 2. 在维护窗口执行
sqlcmd -S production_server -d LYBTDB -i migration_prod.sql

# 3. 验证迁移结果
SELECT * FROM __EFMigrationsHistory
ORDER BY MigrationId DESC
```

### 5.3 验证阶段

```sql
-- 检查迁移历史
SELECT * FROM __EFMigrationsHistory
WHERE MigrationId = '20250921_UnifyStatusFieldsToInt'

-- 验证数据完整性
SELECT COUNT(*) FROM MedicalCases WHERE Status NOT IN (0, 1, 2, 3)

-- 检查索引创建
SELECT * FROM sys.indexes
WHERE name LIKE 'IX_MedicalCases_%'
```

## 6. 常见问题处理

### 6.1 迁移失败

#### 问题：迁移中途失败
```bash
# 回滚到上一个稳定版本
dotnet ef database update [LastStableMigration]

# 修复问题后重新应用
dotnet ef database update
```

#### 问题：索引创建超时
```sql
-- 分批创建索引
CREATE INDEX IX_Large_Table ON LargeTable (Column1)
WITH (ONLINE = ON, MAXDOP = 4)
```

### 6.2 性能问题

#### 大表迁移优化
```csharp
// 分批处理
migrationBuilder.Sql(@"
    DECLARE @BatchSize INT = 10000
    DECLARE @Offset INT = 0

    WHILE EXISTS (
        SELECT 1 FROM LargeTable
        WHERE ProcessedFlag = 0
        ORDER BY Id
        OFFSET @Offset ROWS
        FETCH NEXT @BatchSize ROWS ONLY
    )
    BEGIN
        UPDATE LargeTable
        SET ProcessedFlag = 1
        WHERE Id IN (
            SELECT Id FROM LargeTable
            WHERE ProcessedFlag = 0
            ORDER BY Id
            OFFSET @Offset ROWS
            FETCH NEXT @BatchSize ROWS ONLY
        )

        SET @Offset = @Offset + @BatchSize

        -- 避免锁定过久
        WAITFOR DELAY '00:00:01'
    END
");
```

## 7. 迁移监控

### 7.1 迁移日志

```sql
-- 创建迁移日志表
CREATE TABLE MigrationLogs (
    Id INT IDENTITY PRIMARY KEY,
    MigrationName NVARCHAR(200),
    StartTime DATETIME2,
    EndTime DATETIME2,
    Status NVARCHAR(50),
    ErrorMessage NVARCHAR(MAX),
    AffectedRows INT
)
```

### 7.2 性能基准

```sql
-- 记录迁移前后的性能指标
-- 迁移前
SELECT
    OBJECT_NAME(object_id) AS TableName,
    rows AS RowCount,
    reserved_page_count * 8 AS ReservedKB
FROM sys.dm_db_partition_stats
WHERE index_id IN (0, 1)

-- 迁移后对比
```

## 8. 迁移清单模板

### 迁移前
- [ ] 备份数据库
- [ ] 记录当前性能基准
- [ ] 准备回滚脚本
- [ ] 通知相关团队

### 迁移中
- [ ] 设置维护模式
- [ ] 执行迁移脚本
- [ ] 监控执行进度
- [ ] 记录异常日志

### 迁移后
- [ ] 验证数据完整性
- [ ] 性能测试
- [ ] 更新文档
- [ ] 监控系统24小时

## 9. 参考资源

- [Entity Framework Core 迁移文档](https://docs.microsoft.com/ef/core/managing-schemas/migrations/)
- [SQL Server 索引设计指南](https://docs.microsoft.com/sql/relational-databases/indexes/)
- [数据库版本控制最佳实践](https://www.red-gate.com/simple-talk/sql/database-administration/database-version-control/)

## 10. 版本历史

- v1.0.0 (2025-09-21): 初始版本，包含基础迁移指南