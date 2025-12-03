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

        // 字符串长度由 Entity 的 [StringLength] 定义，遵循 DRY 原则
        // decimal 精度配置（Fluent API 专属功能）
        builder.Property(h => h.Price).HasPrecision(18, 2);
        builder.Property(h => h.CostPrice).HasPrecision(18, 2);

        // 枚举转换（Fluent API 专属功能）
        builder.Property(h => h.Status).HasConversion<int>();
    }
}
