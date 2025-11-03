using LYBT.Entities.Prescriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations
{
    /// <summary>
    /// PrescriptionItem 实体 EF Core 配置
    /// </summary>
    public class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
    {
        public void Configure(EntityTypeBuilder<PrescriptionItem> entity)
        {
            entity.ToTable("PrescriptionItems");
            entity.HasKey(i => i.Id);

            // 根据文档要求：剂量为整数，不需要小数
            // Quantity已改为int类型，不需要HasPrecision配置

            // 单价精度配置
            entity.Property(i => i.UnitPrice).HasPrecision(18, 2);

            // 配置与Prescription的关系
            entity.HasOne<Prescription>()
                     .WithMany(p => p.Items)
                     .HasForeignKey(i => i.PrescriptionId)
                     .IsRequired()
                     .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
