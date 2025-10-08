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

            // 添加索引以优化查询性能
            entity.HasIndex(l => l.PrescriptionId)
                         .HasDatabaseName("IX_PrescriptionPrintLogs_PrescriptionId");

            entity.HasIndex(l => l.PrintedAt)
                         .HasDatabaseName("IX_PrescriptionPrintLogs_PrintedAt");

            // 配置并发控制字段
            entity.Property(l => l.RowVersion).IsRowVersion().IsConcurrencyToken();
        }
    }
}
