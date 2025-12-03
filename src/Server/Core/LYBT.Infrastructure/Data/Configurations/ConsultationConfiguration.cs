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

        // 字符串长度由 Entity 的 [StringLength] 定义，遵循 DRY 原则
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
