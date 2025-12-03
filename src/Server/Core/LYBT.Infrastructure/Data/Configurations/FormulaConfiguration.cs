using LYBT.Entities.Formulas;
using LYBT.Infrastructure.Data.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations;

/// <summary>
/// Formula 实体 EF Core 配置
/// 继承 BaseEntityConfiguration 统一审计字段和并发控制
/// </summary>
public class FormulaConfiguration : BaseEntityConfiguration<Formula>
{
    public override void Configure(EntityTypeBuilder<Formula> builder)
    {
        base.Configure(builder);

        builder.ToTable("Formulas");

        // 字符串长度由 Entity 的 [StringLength] 定义，遵循 DRY 原则
        // 枚举转换（Fluent API 专属功能）
        builder.Property(f => f.Status).HasConversion<int>();
        builder.Property(f => f.IsShared).HasDefaultValue(false);

        // 配置与FormulaHerbItem的一对多关系
        builder.HasMany(f => f.Herbs)
            .WithOne(f => f.Formula)
            .HasForeignKey(f => f.FormulaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
