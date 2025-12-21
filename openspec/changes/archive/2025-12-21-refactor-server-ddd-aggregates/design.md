# Design: refactor-server-ddd-aggregates

## 技术设计

### Phase 1: 删除反向导航属性

#### 1.1 Consultation实体修改

**修改前**:
```csharp
// LYBT.Entities/Consultations/ConsultationModel.cs
public class Consultation : BaseEntity
{
    // ... 其他属性

    [Required]
    public virtual MedicalCases.MedicalCase MedicalCase { get; set; } = null!;  // 删除
}
```

**修改后**:
```csharp
public class Consultation : BaseEntity
{
    // Id与MedicalCase共享主键，通过EF配置关联
    // 无导航属性，需要MedicalCase时通过Repository单独查询

    public string? PresentIllness { get; private set; }
    public string? TCMDiagnosis { get; private set; }
    // ... 其他诊断字段
}
```

#### 1.2 Prescription实体修改

**修改前**:
```csharp
// LYBT.Entities/Prescriptions/PrescriptionModel.cs
public class Prescription : BaseEntity
{
    public Guid MedicalCaseId { get; set; }

    public virtual MedicalCases.MedicalCase? MedicalCase { get; set; }  // 删除
}
```

**修改后**:
```csharp
public class Prescription : BaseEntity
{
    public Guid MedicalCaseId { get; private set; }
    // 无MedicalCase导航属性

    private readonly List<PrescriptionItem> _items = [];
    public IReadOnlyCollection<PrescriptionItem> Items => _items.AsReadOnly();
}
```

### Phase 2: 修改EF Core配置

#### 2.1 MedicalCase配置

```csharp
// LYBT.Infrastructure/Data/Configurations/MedicalCaseConfiguration.cs
public class MedicalCaseConfiguration : IEntityTypeConfiguration<MedicalCase>
{
    public void Configure(EntityTypeBuilder<MedicalCase> builder)
    {
        builder.ToTable("MedicalCases");
        builder.HasKey(x => x.Id);

        // 跨聚合引用：只配置外键，无导航属性
        builder.HasOne<Patient>()  // 泛型，无导航属性
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // 聚合内关系：单向导航（MedicalCase -> Consultation）
        builder.HasOne(x => x.Consultation)
            .WithOne()  // 无反向导航
            .HasForeignKey<Consultation>(x => x.Id)
            .OnDelete(DeleteBehavior.Cascade);

        // 聚合内关系：单向导航（MedicalCase -> Prescription）
        builder.HasOne(x => x.Prescription)
            .WithOne()
            .HasForeignKey<Prescription>(x => x.MedicalCaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

#### 2.2 Consultation配置

```csharp
// LYBT.Infrastructure/Data/Configurations/ConsultationConfiguration.cs
public class ConsultationConfiguration : IEntityTypeConfiguration<Consultation>
{
    public void Configure(EntityTypeBuilder<Consultation> builder)
    {
        builder.ToTable("Consultations");
        builder.HasKey(x => x.Id);

        // 无反向导航配置
        // 关系由MedicalCaseConfiguration中定义

        builder.Property(x => x.PresentIllness).HasMaxLength(2000);
        builder.Property(x => x.TCMDiagnosis).HasMaxLength(500);
    }
}
```

#### 2.3 Prescription配置

```csharp
// LYBT.Infrastructure/Data/Configurations/PrescriptionConfiguration.cs
public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("Prescriptions");
        builder.HasKey(x => x.Id);

        // 外键索引
        builder.HasIndex(x => x.MedicalCaseId).IsUnique();

        // 无反向导航配置

        // 使用backing field访问私有集合
        var navigation = builder.Metadata.FindNavigation(nameof(Prescription.Items));
        navigation?.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### Phase 3: 创建Query Service

#### 3.1 Query Model定义

```csharp
// LYBT.Module.MedicalCase/Queries/Models/MedicalCaseDetailQueryModel.cs
namespace LYBT.Module.MedicalCase.Queries.Models;

/// <summary>
/// 医案详情查询模型（读模型，非DDD实体）
/// </summary>
public class MedicalCaseDetailQueryModel
{
    public Guid Id { get; set; }
    public string? CaseNumber { get; set; }
    public MedicalCaseStatus CaseStatus { get; set; }

    // 关联数据（展平视图）
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;

    // 诊断信息
    public string? PresentIllness { get; set; }
    public string? TCMDiagnosis { get; set; }

    // 处方摘要
    public bool HasPrescription { get; set; }
    public int PrescriptionItemCount { get; set; }
    public decimal TotalPrice { get; set; }

    // 时间信息
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
```

#### 3.2 Query Service实现

```csharp
// LYBT.Module.MedicalCase/Queries/MedicalCaseQueryService.cs
namespace LYBT.Module.MedicalCase.Queries;

public interface IMedicalCaseQueryService
{
    Task<MedicalCaseDetailQueryModel?> GetDetailAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<MedicalCaseListQueryModel>> GetListAsync(MedicalCaseQueryFilter filter, CancellationToken ct = default);
}

public class MedicalCaseQueryService : IMedicalCaseQueryService
{
    private readonly LybtDbContext _context;

    public MedicalCaseQueryService(LybtDbContext context)
    {
        _context = context;
    }

    public async Task<MedicalCaseDetailQueryModel?> GetDetailAsync(Guid id, CancellationToken ct = default)
    {
        // 使用Join替代Include（无导航属性）
        var query = from mc in _context.MedicalCases
                    where mc.Id == id && !mc.IsDeleted
                    select new MedicalCaseDetailQueryModel
                    {
                        Id = mc.Id,
                        CaseNumber = mc.CaseNumber,
                        CaseStatus = mc.CaseStatus,
                        PatientName = mc.PatientName,  // 冗余字段
                        DoctorName = mc.DoctorName,    // 冗余字段
                        CreatedAt = mc.CreatedAt,
                        CompletedAt = mc.CompletedAt,

                        // 子查询获取诊断信息
                        PresentIllness = _context.Consultations
                            .Where(c => c.Id == mc.Id)
                            .Select(c => c.PresentIllness)
                            .FirstOrDefault(),
                        TCMDiagnosis = _context.Consultations
                            .Where(c => c.Id == mc.Id)
                            .Select(c => c.TCMDiagnosis)
                            .FirstOrDefault(),

                        // 子查询获取处方摘要
                        HasPrescription = _context.Prescriptions
                            .Any(p => p.MedicalCaseId == mc.Id),
                        PrescriptionItemCount = _context.Prescriptions
                            .Where(p => p.MedicalCaseId == mc.Id)
                            .SelectMany(p => p.Items)
                            .Count(),
                        TotalPrice = _context.Prescriptions
                            .Where(p => p.MedicalCaseId == mc.Id)
                            .SelectMany(p => p.Items)
                            .Sum(i => i.Dosage * i.Price)
                    };

        return await query.FirstOrDefaultAsync(ct);
    }
}
```

### Phase 4: 领域事件（可选增强）

#### 4.1 事件定义

```csharp
// LYBT.Domain.Abstractions/Events/IDomainEvent.cs
public interface IDomainEvent : INotification
{
    DateTime OccurredAt { get; }
}

// LYBT.Entities/MedicalCases/Events/MedicalCaseCompletedEvent.cs
public record MedicalCaseCompletedEvent(
    Guid MedicalCaseId,
    Guid PatientId,
    DateTime OccurredAt
) : IDomainEvent;
```

#### 4.2 事件处理器

```csharp
// LYBT.Module.Patients/EventHandlers/UpdatePatientLastVisitHandler.cs
public class UpdatePatientLastVisitHandler : INotificationHandler<MedicalCaseCompletedEvent>
{
    private readonly IRepository<Patient> _patientRepository;

    public UpdatePatientLastVisitHandler(IRepository<Patient> patientRepository)
    {
        _patientRepository = patientRepository;
    }

    public async Task Handle(MedicalCaseCompletedEvent notification, CancellationToken ct)
    {
        var patient = await _patientRepository.GetByIdAsync(notification.PatientId, ct);
        if (patient != null)
        {
            patient.UpdateLastVisitTime(notification.OccurredAt);
            patient.IncrementVisitCount();
            await _patientRepository.UpdateAsync(patient, ct);
        }
    }
}
```

#### 4.3 事件发布（SaveChanges拦截）

```csharp
// LYBT.Infrastructure/Data/LybtDbContext.cs
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    // 收集领域事件
    var domainEvents = ChangeTracker.Entries<IAggregateRoot>()
        .SelectMany(x => x.Entity.DomainEvents)
        .ToList();

    // 清除已收集的事件
    foreach (var entry in ChangeTracker.Entries<IAggregateRoot>())
    {
        entry.Entity.ClearDomainEvents();
    }

    // 保存变更
    var result = await base.SaveChangesAsync(cancellationToken);

    // 发布事件
    foreach (var domainEvent in domainEvents)
    {
        await _mediator.Publish(domainEvent, cancellationToken);
    }

    return result;
}
```

## 迁移策略

### 渐进式迁移

1. **阶段一**：删除反向导航属性（Breaking Change）
   - 修改实体定义
   - 修改EF配置
   - 编译验证

2. **阶段二**：修复受影响的查询
   - 识别所有使用`Include(x => x.MedicalCase)`的代码
   - 改用Query Service或单独查询

3. **阶段三**：添加Query Service
   - 创建专用查询模型
   - 实现Query Service

4. **阶段四**：可选领域事件
   - 添加事件基础设施
   - 实现跨聚合协调

## 受影响代码定位

```bash
# 查找所有使用反向导航的代码
rg "\.MedicalCase" src/Server --type cs
rg "Include.*MedicalCase" src/Server --type cs

# 查找Consultation相关Include
rg "Include.*Consultation.*MedicalCase" src/Server --type cs

# 查找Prescription相关Include
rg "Include.*Prescription.*MedicalCase" src/Server --type cs
```

## 测试策略

1. **单元测试**：验证实体行为不变
2. **集成测试**：验证EF配置正确
3. **API测试**：验证现有端点功能正常
