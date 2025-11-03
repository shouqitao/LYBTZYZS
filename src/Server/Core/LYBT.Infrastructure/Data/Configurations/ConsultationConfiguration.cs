using LYBT.Entities.Consultation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Consultation 实体 EF Core 配置
    /// </summary>
    public class ConsultationConfiguration : IEntityTypeConfiguration<Consultation>
    {
        public void Configure(EntityTypeBuilder<Consultation> entity)
        {
            entity.ToTable("Consultations");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.ChiefComplaint).HasMaxLength(500);
            entity.Property(c => c.PresentIllness).HasMaxLength(1000);

            entity.Property(c => c.Inspection).HasMaxLength(500);
            entity.Property(c => c.AuscultationOlfaction).HasMaxLength(500);
            entity.Property(c => c.Inquiry).HasMaxLength(1000);
            entity.Property(c => c.Palpation).HasMaxLength(500);

            entity.Property(c => c.TCMDiagnosis).HasMaxLength(500);

            entity.Property(c => c.TreatmentPrinciple).HasMaxLength(500);
            entity.Property(c => c.MedicalAdvice).HasMaxLength(500);
            entity.Property(c => c.Remark).HasMaxLength(1000);

            // 添加并发控制
            entity.Property(c => c.RowVersion).IsRowVersion().IsConcurrencyToken();

            // 添加审计字段
            entity.Property(c => c.CreatedBy).IsRequired();

            // 配置与MedicalCase的一对一关系（共享主键）
            entity.HasOne(c => c.MedicalCase)
                  .WithOne(m => m.Consultation)
                  .HasForeignKey<Consultation>(c => c.Id) // 使用Id作为外键（共享主键）
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Cascade); // 级联删除
        }
    }
}
