using LYBT.Entities.Patients;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Patients.Infrastructure;

/// <summary>
/// 患者模块数据库上下文。
/// 注意：当前使用AppDbContext兼容模式，后续可迁移到独立Schema。
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

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("Patients");
            entity.HasKey(e => e.Id);
        });
    }
}
