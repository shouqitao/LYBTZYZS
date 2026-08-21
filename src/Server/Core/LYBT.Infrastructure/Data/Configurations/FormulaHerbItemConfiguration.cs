using LYBT.Entities.Formulas;
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

            // 字符串长度由 Entity 的 [StringLength] 定义，遵循 DRY 原则
            entity.Property(f => f.HerbName).IsRequired();
            entity.Property(f => f.Dosage).HasDefaultValue(1);
            entity.Property(f => f.Unit).HasDefaultValue("g");

            // P2-3-5 导航 JOIN 索引：FormulaHerbItem 查询常经 Formula.IsDeleted 过滤，建复合索引加速
            entity.HasIndex(f => new { f.FormulaId, f.HerbId })
                .HasDatabaseName("IX_FormulaHerbItems_FormulaId_HerbId");

            // 配置与Herb的关系
            entity.HasOne<Herb>()
                .WithMany()
                .HasForeignKey(f => f.HerbId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}


