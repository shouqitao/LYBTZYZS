using LYBT.Entities.Prescriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations
{
    /// <summary>
    /// PrescriptionPrintLog 实体 EF Core 配置
    /// </summary>
    public class PrescriptionPrintLogConfiguration : IEntityTypeConfiguration<PrescriptionPrintLog>
    {
        public void Configure(EntityTypeBuilder<PrescriptionPrintLog> entity)
        {
            entity.ToTable("PrescriptionPrintLogs");
            entity.HasKey(l => l.Id);

            // 配置与Prescription的关系
            entity.HasOne(l => l.Prescription)
                         .WithMany(p => p.PrintLogs)
                         .HasForeignKey(l => l.PrescriptionId)
                         .IsRequired()
                         .OnDelete(DeleteBehavior.Cascade);

            // Issue #1765: 删除2个多余索引
            // - PrescriptionId: EF Core外键自动创建索引
            // - PrintedAt: MVP阶段(<10K记录)无需额外索引

            // 配置并发控制字段
            entity.Property(l => l.RowVersion).IsRowVersion().IsConcurrencyToken();
        }
    }
}
