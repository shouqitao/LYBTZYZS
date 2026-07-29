using LYBT.Module.Formulas.Domain;
using Microsoft.EntityFrameworkCore;
using FormulaEntity = LYBT.Entities.Formulas.Formula;

namespace LYBT.Module.Formulas.Infrastructure;

/// <summary>
/// 验方模块数据库上下文。使用共享连接、独立Schema的方式实现模块数据隔离。
/// </summary>
public class FormulaDbContext : DbContext
{
    /// <summary>验方集</summary>
    public DbSet<FormulaEntity> Formulas { get; set; } = null!;

    // FormulaHerbItems 已在 AppDbContext 中管理，此处不再注册

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

        modelBuilder.Entity<FormulaEntity>(entity =>
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

            // 枚举转换（与 AppDbContext 保持一致）
            entity.Property(e => e.Status).HasConversion<int>();



            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IsDeleted);

            entity.HasMany(e => e.Herbs)
                  .WithOne(e => e.Formula)
                  .HasForeignKey(e => e.FormulaId)
                  .OnDelete(DeleteBehavior.Cascade);
        });


    }
}


