using LYBT.Entities.Formula;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Formula 实体 EF Core 配置
    /// </summary>
    public class FormulaConfiguration : IEntityTypeConfiguration<Formula>
    {
        public void Configure(EntityTypeBuilder<Formula> entity)
        {
            entity.ToTable("Formulas");
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Name).IsRequired().HasMaxLength(200);
            entity.Property(f => f.Effect).HasMaxLength(500);
            entity.Property(f => f.Usage).HasMaxLength(500);
            entity.Property(f => f.Property).HasMaxLength(300);
            entity.Property(f => f.Remark).HasMaxLength(500);

            // 配置Status枚举字段
            entity.Property(f => f.Status).HasConversion<int>();
            entity.Property(f => f.IsShared).HasDefaultValue(false);

            // 配置与FormulaHerbItem的一对多关系
            entity.HasMany(f => f.Herbs)
                .WithOne(f => f.Formula)
                .HasForeignKey(f => f.FormulaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}