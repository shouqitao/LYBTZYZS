using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Patients.Infrastructure;

/// <summary>
/// 患者模块数据库上下文。使用共享连接、同库不同 DbContext 的方式实现模块数据隔离。
/// 物理表由 AppDbContext 单一迁移链管理（ADR-0017 方案 A），本上下文仅做逻辑隔离。
/// </summary>
public class PatientsDbContext : DbContext
{
    /// <summary>患者集</summary>
    public DbSet<Patient> Patients { get; set; } = null!;

    public PatientsDbContext(DbContextOptions<PatientsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 复用 Infrastructure 的实体配置类（与 AppDbContext 保持一致）
        modelBuilder.ApplyConfiguration(new PatientConfiguration());

        // 软删除全局查询过滤器（与 AppDbContext ApplyOptimizations 保持一致）
        modelBuilder.Entity<Patient>().HasQueryFilter(e => !e.IsDeleted);
    }
}
