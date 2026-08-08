using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Patients;
using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.MedicalCases.Infrastructure;

/// <summary>
/// 医案模块数据库上下文。使用共享连接、同库不同 DbContext 的方式实现模块数据隔离。
/// 物理表由 AppDbContext 单一迁移链管理（ADR-0017 方案 A），本上下文仅做逻辑隔离。
/// </summary>
public class MedicalCaseDbContext : DbContext
{
    /// <summary>医案集</summary>
    public DbSet<MedicalCase> MedicalCases { get; set; } = null!;

    /// <summary>诊断集</summary>
    public DbSet<Consultation> Consultations { get; set; } = null!;

    /// <summary>处方集</summary>
    public DbSet<Prescription> Prescriptions { get; set; } = null!;

    /// <summary>处方项集</summary>
    public DbSet<PrescriptionItem> PrescriptionItems { get; set; } = null!;

    /// <summary>医案打印日志集</summary>
    public DbSet<MedicalCasePrintLog> MedicalCasePrintLogs { get; set; } = null!;

    /// <summary>医案审计日志集</summary>
    public DbSet<MedicalCaseAuditLog> MedicalCaseAuditLogs { get; set; } = null!;

    public MedicalCaseDbContext(DbContextOptions<MedicalCaseDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 复用 Infrastructure 的实体配置类（与 AppDbContext 保持一致）
        modelBuilder.ApplyConfiguration(new MedicalCaseConfiguration());
        modelBuilder.ApplyConfiguration(new ConsultationConfiguration());
        modelBuilder.ApplyConfiguration(new PrescriptionConfiguration());
        modelBuilder.ApplyConfiguration(new PrescriptionItemConfiguration());
        modelBuilder.ApplyConfiguration(new MedicalCaseAuditLogConfiguration());

        // 跨模块 FK 引用实体：MedicalCaseConfiguration 定义了到 Patient/ApplicationUser 的外键，
        // 需注册正确表名（Patients/Users）以便待看诊列表 Join 与模型一致
        modelBuilder.ApplyConfiguration(new PatientConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());

        // 软删除全局查询过滤器（与 AppDbContext ApplyOptimizations 保持一致）
        modelBuilder.Entity<MedicalCase>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Consultation>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Prescription>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MedicalCasePrintLog>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MedicalCaseAuditLog>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Patient>().HasQueryFilter(e => !e.IsDeleted);
    }
}
