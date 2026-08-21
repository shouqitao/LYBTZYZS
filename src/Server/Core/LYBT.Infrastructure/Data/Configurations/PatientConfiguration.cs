using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data.Configurations.Base;
using LYBT.Infrastructure.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations;

/// <summary>
/// Patient 实体 EF Core 配置
/// 继承 BaseEntityConfiguration 统一审计字段和并发控制
/// </summary>
public class PatientConfiguration : BaseEntityConfiguration<Patient>
{
    public override void Configure(EntityTypeBuilder<Patient> builder)
    {
        base.Configure(builder);

        builder.ToTable("Patients");

        // P1-20：身份证号过滤唯一索引，允许软删后重建且 NULL 不参与
        builder.HasIndex(p => p.IdNumber).IsUnique().HasFilter("[IsDeleted] = 0 AND [IdNumber] IS NOT NULL").HasDatabaseName("IX_Patients_IdNumber");

        // P1-9：敏感字段透明加密（AES-GCM，落库密文，日志仍脱敏双层）
        builder.Property(p => p.IdNumber).HasConversion(new AesGcmValueConverter());
        builder.Property(p => p.PhoneNumber).HasConversion(new AesGcmValueConverter());

        // 字符串长度由 Entity 的 [StringLength] 定义，遵循 DRY 原则
    }
}


