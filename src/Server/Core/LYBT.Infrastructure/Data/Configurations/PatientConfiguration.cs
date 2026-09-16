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

        // R-6：HMAC 盲索引列（IdNumber 非确定性加密无法 SQL 等值；本列可索引）
        builder.Property(p => p.IdCardHash).HasMaxLength(64);
        builder.HasIndex(p => p.IdCardHash).IsUnique().HasFilter("[IsDeleted] = 0 AND [IdCardHash] IS NOT NULL").HasDatabaseName("IX_Patients_IdCardHash");

        // P1-9：敏感字段透明加密（AES-GCM，落库密文，日志仍脱敏双层）
        // 加密后 Base64 长度显著大于明文（Phone 11→64，IdNumber 18→62，50 字符明文→104），列需扩容至 200 以容纳密文
        builder.Property(p => p.IdNumber).HasConversion(new AesGcmValueConverter()).HasMaxLength(200);
        builder.Property(p => p.PhoneNumber).HasConversion(new AesGcmValueConverter()).HasMaxLength(200);

        // 字符串长度由 Entity 的 [StringLength] 定义，遵循 DRY 原则
    }
}


