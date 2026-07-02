using LYBT.Module.Formulas.Domain;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Formulas.Infrastructure;

/// <summary>
/// 验方模块数据库上下文。使用共享连接、独立Schema的方式实现模块数据隔离。
/// </summary>
public class FormulaDbContext : DbContext
{
    /// <summary>验方集</summary>
    public DbSet<Formula> Formulas { get; set; } = null!;

    /// <summary>验方药材明细集</summary>
    public DbSet<FormulaHerbItem> FormulaHerbItems { get; set; } = null!;

    /// <summary>
    /// 初始化数据库上下文。
    /// </summary>
    public FormulaDbContext(DbContextOptions<FormulaDbContext> options) : base(options)
    {
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("Formulas");

        modelBuilder.Entity<Formula>(entity =>
        {
            entity.ToTable("Formulas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Effect).HasMaxLength(500);
            entity.Property(e => e.Indication).HasMaxLength(1000);
            entity.Property(e => e.Usage).HasMaxLength(500);
            entity.Property(e => e.Remark).HasMaxLength(500);
            entity.Property(e => e.Property).HasMaxLength(300);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.ValidationStatus).HasConversion<string>();
            entity.Property(e => e.FormulaType).HasConversion<string>();

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IsDeleted);

            entity.HasMany(e => e.Herbs)
                  .WithOne(e => e.Formula)
                  .HasForeignKey(e => e.FormulaId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FormulaHerbItem>(entity =>
        {
            entity.ToTable("FormulaHerbItems");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.HerbName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.OriginalHerbName).HasMaxLength(100);
            entity.Property(e => e.Unit).HasMaxLength(16);
            entity.Property(e => e.Usage).HasMaxLength(200);
            entity.Property(e => e.Remark).HasMaxLength(200);
            entity.Property(e => e.ProcessingMethod).HasMaxLength(100);
            entity.Property(e => e.DecocteMethod).HasConversion<string>();
        });
    }
}


