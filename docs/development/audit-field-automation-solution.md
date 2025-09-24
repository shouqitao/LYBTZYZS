# 审计字段自动化方案

## 现状分析

当前系统中审计字段（CreatedAt、UpdatedAt、CreatedBy、UpdatedBy）存在于多个实体中，但没有统一的自动化处理机制。

### 现有审计字段分布
- **BaseEntity**: 包含标准审计字段（CreatedAt、UpdatedAt、CreatedBy、UpdatedBy）
- **Patient、User、MedicalCase、Consultation、Prescription**: 各自定义审计字段，格式不统一

## 推荐方案：EF Core SaveChanges拦截

### 方案一：重写SaveChangesAsync（推荐）

```csharp
public class AppDbContext : DbContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public AppDbContext(DbContextOptions<AppDbContext> options, 
        IHttpContextAccessor httpContextAccessor) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void SetAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        var userId = GetCurrentUserId();
        var timestamp = DateTime.Now;

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = timestamp;
                entity.CreatedBy = userId;
            }

            entity.UpdatedAt = timestamp;
            entity.UpdatedBy = userId;
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
```

### 方案二：使用EF Core拦截器

```csharp
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(DbContext? context)
    {
        if (context == null) return;

        var entries = context.ChangeTracker.Entries<BaseEntity>();
        var userId = GetCurrentUserId();
        var timestamp = DateTime.Now;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = timestamp;
                entry.Entity.CreatedBy = userId;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = timestamp;
                entry.Entity.UpdatedBy = userId;
            }
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

// 注册拦截器
services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(connectionString)
           .AddInterceptors(serviceProvider.GetRequiredService<AuditInterceptor>());
});
```

### 方案三：使用Shadow Properties（轻量级方案）

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
        {
            modelBuilder.Entity(entityType.ClrType)
                .Property<DateTime>("CreatedAt")
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity(entityType.ClrType)
                .Property<DateTime?>("UpdatedAt")
                .ValueGeneratedOnUpdate();

            modelBuilder.Entity(entityType.ClrType)
                .Property<Guid?>("CreatedBy");

            modelBuilder.Entity(entityType.ClrType)
                .Property<Guid?>("UpdatedBy");
        }
    }
}
```

## 实施建议

### 第一阶段：统一实体基类
1. 让所有需要审计的实体继承自`BaseEntity`
2. 移除各实体中重复定义的审计字段
3. 统一字段命名（CreatedAt vs CreatedTime）

### 第二阶段：实现自动化
1. 在`AppDbContext`中实现方案一（重写SaveChangesAsync）
2. 注入`IHttpContextAccessor`获取当前用户
3. 配置JWT认证确保用户ID可获取

### 第三阶段：数据迁移
1. 创建迁移脚本统一现有数据的审计字段
2. 更新所有Repository移除手动设置审计字段的代码
3. 测试验证自动化效果

## 额外优化建议

### 1. 接口抽象
```csharp
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    Guid? CreatedBy { get; set; }
    Guid? UpdatedBy { get; set; }
}
```

### 2. 扩展方法
```csharp
public static class AuditExtensions
{
    public static void SetAuditFields(this IAuditable entity, Guid? userId, bool isNew)
    {
        var now = DateTime.Now;
        if (isNew)
        {
            entity.CreatedAt = now;
            entity.CreatedBy = userId;
        }
        entity.UpdatedAt = now;
        entity.UpdatedBy = userId;
    }
}
```

### 3. 单元测试
```csharp
[Fact]
public async Task SaveChanges_Should_Set_Audit_Fields()
{
    // Arrange
    var userId = Guid.NewGuid();
    var patient = new Patient { Name = "Test" };
    
    // Act
    _context.Patients.Add(patient);
    await _context.SaveChangesAsync();
    
    // Assert
    Assert.NotNull(patient.CreatedAt);
    Assert.Equal(userId, patient.CreatedBy);
}
```

## 性能考虑

1. **批量操作优化**: 使用`BulkInsert`时可能需要单独处理审计字段
2. **索引策略**: 为`CreatedAt`、`UpdatedAt`添加非聚集索引提升查询性能
3. **缓存策略**: 缓存当前用户ID减少Token解析开销

## 安全性考虑

1. **审计字段保护**: 防止客户端直接修改审计字段
2. **时区处理**: 统一使用UTC时间或服务器本地时间
3. **审计日志**: 考虑将关键操作记录到独立审计表

## 总结

推荐采用**方案一（重写SaveChangesAsync）**，因为：
- 实现简单，维护成本低
- 性能影响最小
- 与现有代码兼容性好
- 易于测试和调试

实施时应分阶段进行，先统一实体结构，再实现自动化，最后进行数据迁移，确保平滑过渡。