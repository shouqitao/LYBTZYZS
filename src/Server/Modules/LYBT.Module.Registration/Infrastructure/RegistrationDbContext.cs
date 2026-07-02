using Microsoft.EntityFrameworkCore;
using RegistrationEntity = LYBT.Module.Registration.Domain.Registration;

namespace LYBT.Module.Registration.Infrastructure;

/// <summary>
/// 挂号模块数据库上下文。使用共享连接、独立Schema的方式实现模块数据隔离。
/// </summary>
public class RegistrationDbContext : DbContext
{
    /// <summary>挂号记录集</summary>
    public DbSet<RegistrationEntity> Registrations { get; set; } = null!;

    public RegistrationDbContext(DbContextOptions<RegistrationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RegistrationEntity>(entity =>
        {
            entity.ToTable("Registrations");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PatientId).IsRequired();
            entity.Property(e => e.PatientName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DoctorId).IsRequired();
            entity.Property(e => e.DoctorName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.MedicalCaseId);
            entity.Property(e => e.Source).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.QueueNumber).IsRequired();
            entity.Property(e => e.RegistrationFee).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Remark).HasMaxLength(500);
            entity.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.CreatedBy);
            entity.Property(e => e.UpdatedBy);
            entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();

            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.DoctorId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsDeleted);
        });
    }
}


