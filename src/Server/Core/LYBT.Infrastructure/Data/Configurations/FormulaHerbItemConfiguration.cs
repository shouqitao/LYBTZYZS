using LYBT.Entities.Formula;
using LYBT.Entities.Herbs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations
{
    /// <summary>
    /// FormulaHerbItem 实体 EF Core 配置
    /// </summary>
    public class FormulaHerbItemConfiguration : IEntityTypeConfiguration<FormulaHerbItem>
    {
        public void Configure(EntityTypeBuilder<FormulaHerbItem> entity)
        {
            entity.ToTable("FormulaHerbItems");
            entity.HasKey(f => f.Id);

            entity.Property(f => f.HerbName).IsRequired().HasMaxLength(100);
            entity.Property(f => f.Quantity).HasDefaultValue(1);
            entity.Property(f => f.Unit).HasMaxLength(16).HasDefaultValue("g");
            entity.Property(f => f.Usage).HasMaxLength(200);
            entity.Property(f => f.Remark).HasMaxLength(200);
            entity.Property(f => f.ProcessingMethod).HasMaxLength(100);

            // 配置与Herb的关系
            entity.HasOne<Herb>()
                .WithMany()
                .HasForeignKey(f => f.HerbId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
