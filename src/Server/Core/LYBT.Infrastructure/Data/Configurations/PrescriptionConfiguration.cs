using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Data.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations;

/// <summary>
/// Prescription 实体 EF Core 配置
/// 继承 BaseEntityConfiguration 统一审计字段和并发控制
/// </summary>
public class PrescriptionConfiguration : BaseEntityConfiguration<Prescription>
{
    public override void Configure(EntityTypeBuilder<Prescription> builder)
    {
        base.Configure(builder);

        builder.ToTable("Prescriptions");

        // 折扣精度为(3,2)，例如0.80表示八折
        builder.Property(p => p.Discount).HasPrecision(3, 2);

        // 一病案至多一处方 - 唯一索引
        builder.HasIndex(p => p.MedicalCaseId)
              .HasDatabaseName("UX_Prescriptions_MedicalCaseId")
              .IsUnique();

        // CreatedBy 必填（覆盖基类的可空配置）
        builder.Property(p => p.CreatedBy).IsRequired();

        // 配置与MedicalCase的一对一关系
        builder.HasOne(p => p.MedicalCase)
              .WithOne(m => m.Prescription)
              .HasForeignKey<Prescription>(p => p.MedicalCaseId)
              .IsRequired()
              .OnDelete(DeleteBehavior.Cascade);
    }
}
