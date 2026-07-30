using LYBT.Module.Herbs.Domain;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Herbs.Infrastructure;

/// <summary>
/// 药材模块数据库上下文。使用共享连接、独立Schema的方式实现模块数据隔离。
/// </summary>
public class HerbsDbContext : DbContext
{
    /// <summary>药材集</summary>
    public DbSet<Herb> Herbs { get; set; } = null!;

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
    }
}


