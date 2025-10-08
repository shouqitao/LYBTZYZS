using LYBT.Entities.Herbs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Herb 实体 EF Core 配置
    /// </summary>
    public class HerbConfiguration : IEntityTypeConfiguration<Herb>
    {
        public void Configure(EntityTypeBuilder<Herb> entity)
        {
            entity.ToTable("Herbs");
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Name).HasMaxLength(100);
            entity.Property(h => h.PinYinCode).HasMaxLength(50);
            entity.Property(h => h.Origin).HasMaxLength(100);
            entity.Property(h => h.Spec).HasMaxLength(100);
            entity.Property(h => h.Unit).HasMaxLength(10);
            entity.Property(h => h.Effect).HasMaxLength(500);
            entity.Property(h => h.Usage).HasMaxLength(500);

            // P1 Batch1: 统一使用 HasPrecision 配置 decimal 精度
            entity.Property(h => h.Price).HasPrecision(18, 2);
            entity.Property(h => h.CostPrice).HasPrecision(18, 2);

            // 配置Status枚举字段
            entity.Property(h => h.Status).HasConversion<int>();
            entity.HasIndex(h => h.Name);
            entity.HasIndex(h => h.PinYinCode);
        }
    }
}
