using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Data.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations;

/// <summary>
/// Consultation 实体 EF Core 配置
/// 继承 BaseEntityConfiguration 统一审计字段和并发控制
/// OpenSpec: refactor-server-ddd-aggregates - 移除反向导航配置
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

        // 与MedicalCase的一对一关系（共享主键）
        // 使用泛型HasOne<T>()，不指定Consultation端的导航属性
        // 关系从MedicalCase端维护（MedicalCase.Consultation）
        builder.HasOne<MedicalCase>()
              .WithOne(m => m.Consultation)
              .HasForeignKey<Consultation>(c => c.Id)
              .IsRequired()
              .OnDelete(DeleteBehavior.Cascade);
    }
}
