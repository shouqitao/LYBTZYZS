using LYBT.Entities.Consultations;
using LYBT.Infrastructure.Data.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations;

/// <summary>
/// Consultation 实体 EF Core 配置
/// 继承 BaseEntityConfiguration 统一审计字段和并发控制
/// </summary>
public class ConsultationConfiguration : BaseEntityConfiguration<Consultation>
{
    public override void Configure(EntityTypeBuilder<Consultation> builder)
    {
        base.Configure(builder);

        builder.ToTable("Consultations");
        builder.Property(c => c.ChiefComplaint).HasMaxLength(500);
        builder.Property(c => c.PresentIllness).HasMaxLength(1000);
        builder.Property(c => c.Inspection).HasMaxLength(500);
        builder.Property(c => c.AuscultationOlfaction).HasMaxLength(500);
        builder.Property(c => c.Inquiry).HasMaxLength(1000);
        builder.Property(c => c.Palpation).HasMaxLength(500);
        builder.Property(c => c.TCMDiagnosis).HasMaxLength(500);
        builder.Property(c => c.TreatmentPrinciple).HasMaxLength(500);
        builder.Property(c => c.MedicalAdvice).HasMaxLength(500);
        builder.Property(c => c.Remark).HasMaxLength(1000);

        // CreatedBy 必填（覆盖基类的可空配置）
        builder.Property(c => c.CreatedBy).IsRequired();

        // 配置与MedicalCase的一对一关系（共享主键）
        builder.HasOne(c => c.MedicalCase)
              .WithOne(m => m.Consultation)
              .HasForeignKey<Consultation>(c => c.Id)
              .IsRequired()
              .OnDelete(DeleteBehavior.Cascade);
    }
}
