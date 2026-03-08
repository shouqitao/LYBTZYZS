using LYBT.Entities.Registrations;
using LYBT.Infrastructure.Data.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations;

/// <summary>
/// Registration 实体 EF Core 配置
/// 继承 BaseEntityConfiguration 统一审计字段和并发控制
/// </summary>
public class RegistrationConfiguration : BaseEntityConfiguration<Registration>
{
    public override void Configure(EntityTypeBuilder<Registration> builder)
    {
        base.Configure(builder);

        builder.ToTable("Registrations");

        // 枚举转换 (Fluent API)
        builder.Property(r => r.Source).HasConversion<int>();
        builder.Property(r => r.Status).HasConversion<int>();

        // 索引: 按患者查询挂号记录
        builder.HasIndex(r => r.PatientId);

        // 索引: 按医生查询挂号队列
        builder.HasIndex(r => r.DoctorId);

        // 索引: 按状态筛选 (Waiting 队列查询高频)
        builder.HasIndex(r => r.Status);

        // 索引: MedicalCaseId (可空，接诊后填入)
        builder.HasIndex(r => r.MedicalCaseId);
    }
}
