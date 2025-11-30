using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Data.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations;

/// <summary>
/// MedicalCase 实体 EF Core 配置
/// 继承 BaseEntityConfiguration 统一审计字段和并发控制
/// </summary>
public class MedicalCaseConfiguration : BaseEntityConfiguration<MedicalCase>
{
    public override void Configure(EntityTypeBuilder<MedicalCase> builder)
    {
        base.Configure(builder);

        builder.ToTable("MedicalCases");
        builder.Property(m => m.Remark).HasMaxLength(500);

        // Epic #2175 BF-002: NeedsPrescription字段配置（nullable支持三态）
        builder.Property(m => m.NeedsPrescription).IsRequired(false);

        // CreatedBy 必填（覆盖基类的可空配置）
        builder.Property(m => m.CreatedBy).IsRequired();

        // 根据文档要求：单患者仅一条未完成病案 - 过滤唯一索引
        builder.HasIndex(m => m.PatientId)
              .HasDatabaseName("UX_MedicalCases_Patient_ActiveOnly")
              .IsUnique()
              .HasFilter("[CaseStatus] = 1 AND [IsDeleted] = 0");
    }
}
