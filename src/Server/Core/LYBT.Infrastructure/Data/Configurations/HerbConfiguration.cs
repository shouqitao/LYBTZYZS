using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Data.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations;

/// <summary>
/// Herb 实体 EF Core 配置
/// 继承 BaseEntityConfiguration 统一审计字段和并发控制
/// </summary>
public class HerbConfiguration : BaseEntityConfiguration<Herb>
{
    public override void Configure(EntityTypeBuilder<Herb> builder)
    {
        base.Configure(builder);

        builder.ToTable("Herbs");
        builder.Property(h => h.Name).HasMaxLength(100);
        builder.Property(h => h.PinYinCode).HasMaxLength(50);
        builder.Property(h => h.Origin).HasMaxLength(100);
        builder.Property(h => h.Spec).HasMaxLength(100);
        builder.Property(h => h.Unit).HasMaxLength(10);
        builder.Property(h => h.Effect).HasMaxLength(500);
        builder.Property(h => h.Usage).HasMaxLength(500);

        // decimal 精度配置
        builder.Property(h => h.Price).HasPrecision(18, 2);
        builder.Property(h => h.CostPrice).HasPrecision(18, 2);

        // 配置Status枚举字段
        builder.Property(h => h.Status).HasConversion<int>();
    }
}
