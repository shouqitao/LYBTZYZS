using LYBT.Entities.Prescriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations
{
    /// <summary>
    /// PrescriptionPrintLog 实体 EF Core 配置
    /// T2-X8-09: Prescription.PrintLogs 导航属性已移除，关系改为无导航配置
    /// 新的打印日志通过 MedicalCasePrintLog 实体记录
    /// </summary>
    public class PrescriptionPrintLogConfiguration : IEntityTypeConfiguration<PrescriptionPrintLog>
    {
        public void Configure(EntityTypeBuilder<PrescriptionPrintLog> entity)
        {
            entity.ToTable("PrescriptionPrintLogs");
            entity.HasKey(l => l.Id);

            // T2-X8-09: Prescription.PrintLogs 导航属性已移除
            // 保留 FK 约束但不使用 WithMany 导航
            entity.HasOne(l => l.Prescription)
                         .WithMany()
                         .HasForeignKey(l => l.PrescriptionId)
                         .IsRequired()
                         .OnDelete(DeleteBehavior.Cascade);

            // 配置并发控制字段
            entity.Property(l => l.RowVersion).IsRowVersion().IsConcurrencyToken();
        }
    }
}
