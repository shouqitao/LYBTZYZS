using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data.Configurations.Base;
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
        builder.Property(p => p.Name).HasMaxLength(100);
        builder.Property(p => p.PinYinCode).HasMaxLength(50); // Phase 4: 统一为50
        builder.Property(p => p.PhoneNumber).HasMaxLength(20);
        builder.Property(p => p.Address).HasMaxLength(256);
        builder.Property(p => p.IdType).HasMaxLength(20);
        builder.Property(p => p.IdNumber).HasMaxLength(50);
        builder.Property(p => p.AllergyHistory).HasMaxLength(500);

        // 配置Status枚举字段
        builder.Property(p => p.Status).HasConversion<int>();
    }
}
