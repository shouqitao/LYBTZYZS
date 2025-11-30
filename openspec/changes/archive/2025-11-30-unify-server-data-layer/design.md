# Design: unify-server-data-layer

## Architecture Overview

### 当前架构

```
LYBT.Entities (领域实体)
├── Common/
│   └── BaseEntity.cs          # 基类（审计字段）
├── MedicalCase/
├── Patients/
├── Prescriptions/
├── Consultation/
├── Users/
├── Herbs/
├── Formula/
└── Auth/

LYBT.Infrastructure (EF配置)
├── Data/Configurations/
│   ├── MedicalCaseConfiguration.cs
│   ├── PatientConfiguration.cs
│   └── ... (15个配置类)
└── Migrations/
```

### 目标架构

```
LYBT.Entities
├── Common/
│   ├── BaseEntity.cs              # 增强版基类
│   ├── IAuditableEntity.cs        # 审计接口
│   └── ISoftDeletable.cs          # 软删除接口
└── [业务实体目录不变]

LYBT.Infrastructure
├── Data/
│   ├── Configurations/
│   │   ├── Base/
│   │   │   └── BaseEntityConfiguration.cs  # 配置基类
│   │   └── [业务配置类]
│   └── Seeding/
│       └── SeedDataService.cs     # Seed Data服务
└── Migrations/
```

---

## 设计决策

### DD-001: DateTime 处理策略

**决策**: 采用 UTC 标准化

```csharp
// Before
public DateTime CreatedAt { get; set; } = DateTime.Now;

// After
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
```

**理由**:
- 消除时区歧义
- 便于跨时区数据同步
- 符合行业最佳实践

**迁移策略**:
- 现有数据假定为本地时间（Asia/Shanghai, UTC+8）
- Migration 脚本转换为 UTC

**Migration 脚本示例**:
```sql
-- 步骤1: 备份原始数据（建议在生产环境执行前备份整个数据库）

-- 步骤2: 转换 CreatedAt（非空字段）
UPDATE Patients SET CreatedAt = DATEADD(HOUR, -8, CreatedAt);
UPDATE Users SET CreatedAt = DATEADD(HOUR, -8, CreatedAt);
UPDATE MedicalCases SET CreatedAt = DATEADD(HOUR, -8, CreatedAt);
-- ... 其他表

-- 步骤3: 转换 UpdatedAt（可空字段，需要条件处理）
UPDATE Patients SET UpdatedAt = DATEADD(HOUR, -8, UpdatedAt) WHERE UpdatedAt IS NOT NULL;
UPDATE Users SET UpdatedAt = DATEADD(HOUR, -8, UpdatedAt) WHERE UpdatedAt IS NOT NULL;
UPDATE MedicalCases SET UpdatedAt = DATEADD(HOUR, -8, UpdatedAt) WHERE UpdatedAt IS NOT NULL;
-- ... 其他表

-- 步骤4: 验证转换结果
SELECT 'Patients' AS TableName, COUNT(*) AS TotalRows,
       SUM(CASE WHEN CreatedAt > GETUTCDATE() THEN 1 ELSE 0 END) AS FutureCreatedAt
FROM Patients;
```

> **注意**: `GETUTCDATE()` 是 SQL Server 特定语法。如需支持其他数据库，需要调整。

---

### DD-002: Status 枚举体系

**决策**: 保留现有枚举，统一命名规范

| 枚举 | 适用范围 | 说明 |
|------|----------|------|
| `CommonStatus` | 通用实体 | Enabled/Disabled/Deleted |
| `MedicalCaseStatus` | MedicalCase | Draft/Active/Completed |
| `PrescriptionStatus` | Prescription | Draft/Confirmed/Printed |

**MedicalCase 双状态字段处理**:
- `CaseStatus`: 保留（业务流程状态）
- `Status`: 废弃，使用 `IsDeleted` 替代

```csharp
// Before
public MedicalCaseStatus CaseStatus { get; set; }
public CommonStatus Status { get; set; }  // 冗余

// After
public MedicalCaseStatus CaseStatus { get; set; }
// Status 字段废弃，软删除使用 IsDeleted
```

---

### DD-003: EF Configuration 标准化

**决策**: Fluent API 优先 + 配置基类

```csharp
/// <summary>
/// Entity Configuration 基类
/// 统一配置 BaseEntity 字段
/// </summary>
public abstract class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T>
    where T : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        // 统一审计字段配置
        // 注意: GETUTCDATE() 是 SQL Server 特定语法
        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.UpdatedAt)
            .IsRequired(false);

        // 统一并发控制
        builder.Property(e => e.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // 软删除全局过滤
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

// 业务配置继承
public class PatientConfiguration : BaseEntityConfiguration<Patient>
{
    public override void Configure(EntityTypeBuilder<Patient> builder)
    {
        base.Configure(builder);  // 调用基类配置

        // 业务特定配置
        builder.ToTable("Patients");
        builder.Property(p => p.Name).HasMaxLength(100);
    }
}
```

---

### DD-004: StringLength 标准表

| 字段类型 | 长度 | 适用场景 |
|----------|------|----------|
| `Name` | 100 | 所有名称字段 |
| `PinYinCode` | 50 | 拼音码 |
| `PhoneNumber` | 20 | 电话号码 |
| `Email` | 100 | 邮箱 |
| `Remark` | 500 | 备注 |
| `Address` | 256 | 地址 |
| `Description` | 1000 | 长文本描述 |

**Entity 与 Configuration 保持一致**:
- Entity 使用 `[StringLength(100)]`
- Configuration 使用 `HasMaxLength(100)`
- 两者必须相同，Configuration 为准

---

### DD-005: 导航属性规范

**决策**: 统一使用 `virtual` + 集合初始化

```csharp
// 单一导航（可选）
public virtual Prescription? Prescription { get; set; }

// 单一导航（必需）
public virtual MedicalCase MedicalCase { get; set; } = null!;

// 集合导航
public virtual ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
```

**变更说明**:
- 所有导航属性添加 `virtual`（支持 Lazy Loading）
- 集合改用 `ICollection<T>` 接口
- 必需导航使用 `= null!` 初始化

---

### DD-006: Seed Data 策略

**决策**: 预处理器指令 + 配置分离

> **注意**: `OnModelCreating` 中无法访问 `IHostEnvironment`，因此使用预处理器指令实现环境分离。

```csharp
public static class SeedDataService
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        // 生产环境：仅 SuperAdmin（始终执行）
        SeedSuperAdmin(modelBuilder);

#if DEBUG
        // 开发环境：测试数据（仅 Debug 构建）
        SeedTestPatients(modelBuilder);
        SeedTestHerbs(modelBuilder);
#endif
    }

    private static void SeedSuperAdmin(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            UserName = "admin",
            // ...
        });
    }

#if DEBUG
    private static void SeedTestPatients(ModelBuilder modelBuilder)
    {
        // 测试患者数据
    }

    private static void SeedTestHerbs(ModelBuilder modelBuilder)
    {
        // 测试药材数据
    }
#endif
}
```

**替代方案**: 如需运行时控制，可使用 `DbContext.Database.EnsureCreated()` 后的独立 Seed 服务。

---

### DD-007: 命名规范统一

**决策**: 统一单复数形式规范

| 元素 | 规范 | 示例 |
|------|------|------|
| Entity 类名 | 单数 | `Patient`, `MedicalCase` |
| Entity 文件名 | 单数+Model | `PatientModel.cs`, `MedicalCaseModel.cs` |
| 命名空间/目录 | 复数 | `LYBT.Entities.Patients`, `LYBT.Entities.MedicalCases` |
| 数据库表名 | 复数 | `Patients`, `MedicalCases` |
| 集合属性名 | 复数 | `public ICollection<Prescription> Prescriptions { get; set; }` |
| 单一导航属性 | 单数 | `public Patient Patient { get; set; }` |

**当前不一致**:

| 目录 | 当前 | 目标 |
|------|------|------|
| `MedicalCase` | 单数 | `MedicalCases` |
| `Consultation` | 单数 | `Consultations` |
| `Formula` | 单数 | `Formulas` |
| `Patients` | 复数 | 保持 |
| `Prescriptions` | 复数 | 保持 |
| `Users` | 复数 | 保持 |
| `Herbs` | 复数 | 保持 |
| `Auth` | 特殊 | 保持（非业务实体） |

**迁移策略**:
- 重命名目录需要更新所有 using 语句
- 使用 IDE 重构功能批量更新
- 保持 Git 历史追踪（`git mv`）

---

## Migration 策略

### 阶段划分

1. **Migration 1**: BaseEntity 字段重命名（无数据变更）
2. **Migration 2**: DateTime UTC 转换
3. **Migration 3**: RowVersion 启用
4. **Migration 4**: MedicalCase.Status 废弃
5. **Migration 5**: 索引优化

### 回滚计划

每个 Migration 都包含完整的 Down() 方法，支持回滚到任意版本。

---

## 风险缓解

| 风险 | 缓解措施 |
|------|----------|
| DateTime 转换数据丢失 | 先备份数据库，分批迁移，验证转换结果 |
| RowVersion 并发冲突 | 详见下方并发处理策略 |
| 枚举变更 API 不兼容 | 保持枚举值不变，仅改名称 |
| Status 字段废弃 | 分阶段处理，v1.x 标记 Obsolete，v2.0 删除 |

### RowVersion 并发冲突处理策略

启用 RowVersion 后，并发更新会抛出 `DbUpdateConcurrencyException`。

**后端处理**:
```csharp
try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    // 返回 409 Conflict 状态码
    throw new ConflictException("数据已被其他用户修改，请刷新后重试", ex);
}
```

**前端处理**:
1. 捕获 HTTP 409 响应
2. 显示确认对话框："数据已被修改，是否重新加载？"
3. 用户选择：
   - **重新加载**: 获取最新数据，用户重新编辑
   - **强制覆盖**: 重新获取 RowVersion 后再次提交（需谨慎）

**WPF 实现示例**:
```csharp
catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
{
    var result = MessageBox.Show(
        "数据已被其他用户修改，是否重新加载最新数据？",
        "并发冲突",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning);

    if (result == MessageBoxResult.Yes)
    {
        await RefreshDataAsync();
    }
}
```
