using System.Linq.Expressions;
using LYBT.Desktop.Contracts.Services;
using LYBT.Entities.Common;
using LYBT.Entities.Consultations;
using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Patients;
using LYBT.Entities.Prescriptions;
using LYBT.Entities.Registrations;
using LYBT.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Desktop.LocalData.Context;

/// <summary>
/// 本地数据库上下文 - SQL Server LocalDB 实现
/// OpenSpec: implement-local-mode
/// </summary>
public class LocalDbContext : DbContext
{
    private readonly ICurrentUserProvider? _currentUserProvider;

    public LocalDbContext(DbContextOptions<LocalDbContext> options) : base(options)
    {
    }

    public LocalDbContext(
        DbContextOptions<LocalDbContext> options,
        ICurrentUserProvider currentUserProvider) : base(options)
    {
        _currentUserProvider = currentUserProvider;
    }

    // ==================== DbSet 定义 ====================

    /// <summary>患者表</summary>
    public DbSet<Patient> Patients => Set<Patient>();

    /// <summary>用户表</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>药材表</summary>
    public DbSet<Herb> Herbs => Set<Herb>();

    /// <summary>验方表</summary>
    public DbSet<Formula> Formulas => Set<Formula>();

    /// <summary>验方药材关联表</summary>
    public DbSet<FormulaHerbItem> FormulaHerbItems => Set<FormulaHerbItem>();

    /// <summary>医案表</summary>
    public DbSet<MedicalCase> MedicalCases => Set<MedicalCase>();

    /// <summary>诊断表</summary>
    public DbSet<Consultation> Consultations => Set<Consultation>();

    /// <summary>处方表</summary>
    public DbSet<Prescription> Prescriptions => Set<Prescription>();

    /// <summary>处方药材关联表</summary>
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();

    /// <summary>医案打印日志表 - T4-S5-03</summary>
    public DbSet<MedicalCasePrintLog> MedicalCasePrintLogs => Set<MedicalCasePrintLog>();

    /// <summary>挂号表 - Sprint 2</summary>
    public DbSet<Registration> Registrations => Set<Registration>();

    // ==================== 模型配置 ====================

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 应用全局查询过滤器（软删除）
        ApplySoftDeleteFilter(modelBuilder);

        // 配置实体关系
        ConfigureRelationships(modelBuilder);

        // 配置索引优化查询性能
        ConfigureIndexes(modelBuilder);
    }

    /// <summary>
    /// 应用软删除全局过滤器
    /// </summary>
    private static void ApplySoftDeleteFilter(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
                var filter = Expression.Lambda(Expression.Not(property), parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }

    /// <summary>
    /// 配置实体关系
    /// </summary>
    private static void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        // MedicalCase -> Consultation (1:1，共享主键)
        // Consultation.Id = MedicalCase.Id
        modelBuilder.Entity<MedicalCase>()
            .HasOne(mc => mc.Consultation)
            .WithOne()
            .HasForeignKey<Consultation>(c => c.Id)
            .OnDelete(DeleteBehavior.Cascade);

        // MedicalCase -> Prescription (1:0..1，外键关系)
        modelBuilder.Entity<MedicalCase>()
            .HasOne(mc => mc.Prescription)
            .WithOne()
            .HasForeignKey<Prescription>(p => p.MedicalCaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Formula -> FormulaHerbItem (1:N)
        modelBuilder.Entity<Formula>()
            .HasMany(f => f.Herbs)
            .WithOne()
            .HasForeignKey(h => h.FormulaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Prescription -> PrescriptionItem (1:N)
        modelBuilder.Entity<Prescription>()
            .HasMany(p => p.Items)
            .WithOne()
            .HasForeignKey(i => i.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        // MedicalCase -> MedicalCasePrintLog (1:N) - T4-S5-03
        modelBuilder.Entity<MedicalCase>()
            .HasMany(mc => mc.PrintLogs)
            .WithOne(pl => pl.MedicalCase!)
            .HasForeignKey(pl => pl.MedicalCaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // MedicalCasePrintLog: PrintType 枚举存储为 int - T4-S5-03
        modelBuilder.Entity<MedicalCasePrintLog>()
            .Property(l => l.PrintType)
            .HasConversion<int>();
    }

    /// <summary>
    /// 配置索引优化查询性能
    /// </summary>
    private static void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Patient: 按手机号和身份证号查询
        modelBuilder.Entity<Patient>()
            .HasIndex(p => p.PhoneNumber);

        modelBuilder.Entity<Patient>()
            .HasIndex(p => p.IdCardNumber);

        // User: 按用户名查询（登录）
        modelBuilder.Entity<User>()
            .HasIndex(u => u.UserName)
            .IsUnique();

        // MedicalCase: 按患者ID和医案编号查询
        modelBuilder.Entity<MedicalCase>()
            .HasIndex(mc => mc.PatientId);

        modelBuilder.Entity<MedicalCase>()
            .HasIndex(mc => mc.CaseNumber);

        // Registration: 按患者ID查询挂号记录
        modelBuilder.Entity<Registration>()
            .HasIndex(r => r.PatientId);
    }

    // ==================== 审计字段自动化 ====================

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        SetAuditFields();
        return base.SaveChanges();
    }

    private void SetAuditFields()
    {
        var userId = _currentUserProvider?.CurrentUserId;
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    if (userId.HasValue)
                    {
                        entry.Entity.CreatedBy = userId;
                    }
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    if (userId.HasValue)
                    {
                        entry.Entity.UpdatedBy = userId;
                    }
                    break;
            }
        }
    }
}
