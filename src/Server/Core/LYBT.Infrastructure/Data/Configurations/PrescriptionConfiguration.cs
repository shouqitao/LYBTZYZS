using LYBT.Entities.Prescriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Prescription 实体 EF Core 配置
    /// </summary>
    public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
    {
        public void Configure(EntityTypeBuilder<Prescription> entity)
        {
            entity.ToTable("Prescriptions");
            entity.HasKey(p => p.Id);

            // 根据文档要求：折扣精度为(3,2)，例如0.80表示八折
            entity.Property(p => p.Discount).HasPrecision(3, 2);

            // 根据文档要求：一病案至多一处方 - 唯一索引
            entity.HasIndex(p => p.MedicalCaseId)
                             .HasDatabaseName("UX_Prescriptions_MedicalCaseId")
                             .IsUnique();

            // 添加审计字段
            entity.Property(p => p.CreatedBy).IsRequired();

            // 配置并发控制字段
            entity.Property(p => p.RowVersion).IsRowVersion().IsConcurrencyToken();

            // 配置与MedicalCase的一对一关系
            entity.HasOne(p => p.MedicalCase)
                             .WithOne(m => m.Prescription)
                             .HasForeignKey<Prescription>(p => p.MedicalCaseId)
                             .IsRequired()
                             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
