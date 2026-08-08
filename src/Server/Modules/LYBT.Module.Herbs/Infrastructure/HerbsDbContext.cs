using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Patients;
using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Herbs.Infrastructure;

/// <summary>
/// 药材模块数据库上下文。使用共享连接、同库不同 DbContext 的方式实现模块数据隔离。
/// ADR-0017: 本模块 DbSet 为 Herb；另注册引用检查所需实体（处方项/处方/医案/患者/验方项），
/// 供 HerbReferenceRepository 检查药材被哪些处方/验方引用（物理表由 AppDbContext 单一迁移链管理）。
/// </summary>
public class HerbsDbContext : DbContext
{
    /// <summary>药材集</summary>
    public DbSet<Herb> Herbs { get; set; } = null!;

    // 引用检查实体（只读查询用，非本模块业务实体）
    /// <summary>处方项集（引用检查）</summary>
    public DbSet<PrescriptionItem> PrescriptionItems { get; set; } = null!;

    /// <summary>处方集（引用检查）</summary>
    public DbSet<Prescription> Prescriptions { get; set; } = null!;

    /// <summary>医案集（引用检查）</summary>
    public DbSet<MedicalCase> MedicalCases { get; set; } = null!;

    /// <summary>患者集（引用检查）</summary>
    public DbSet<Patient> Patients { get; set; } = null!;

    /// <summary>验方项集（引用检查）</summary>
    public DbSet<FormulaHerbItem> FormulaHerbItems { get; set; } = null!;

    public HerbsDbContext(DbContextOptions<HerbsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Herb>(entity =>
        {
            entity.ToTable("Herbs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PinYinCode).HasMaxLength(50);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Properties).HasMaxLength(100);
            entity.Property(e => e.Origin).HasMaxLength(100);
            entity.Property(e => e.Spec).HasMaxLength(100);
            entity.Property(e => e.Unit).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CostPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Effect).HasMaxLength(500);
            entity.Property(e => e.Usage).HasMaxLength(500);
            entity.Property(e => e.Remark).HasMaxLength(500);

            // 枚举转换（与 AppDbContext 保持一致）
            entity.Property(e => e.Status).HasConversion<int>();

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.PinYinCode);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IsDeleted);

            // 软删除全局查询过滤器（与 AppDbContext 保持一致）
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // 复用 Infrastructure 的引用实体配置类（与 AppDbContext 保持一致，仅逻辑隔离）
        modelBuilder.ApplyConfiguration(new PrescriptionItemConfiguration());
        modelBuilder.ApplyConfiguration(new PrescriptionConfiguration());
        modelBuilder.ApplyConfiguration(new MedicalCaseConfiguration());
        modelBuilder.ApplyConfiguration(new PatientConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new FormulaConfiguration());
        modelBuilder.ApplyConfiguration(new FormulaHerbItemConfiguration());

        // 软删除全局查询过滤器（与 AppDbContext ApplyOptimizations 保持一致）
        modelBuilder.Entity<Prescription>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MedicalCase>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Patient>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Formula>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<FormulaHerbItem>().HasQueryFilter(fh => fh.Formula == null || !fh.Formula.IsDeleted);
    }
}
