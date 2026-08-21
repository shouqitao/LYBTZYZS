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

        // 索引: 按患者查询挂号记录
        builder.HasIndex(r => r.PatientId);

        // 索引: 按医生查询挂号队列
        builder.HasIndex(r => r.DoctorId);

        // P1-23: 报表行级过滤（Doctor 仅本人 + 时间范围）复合索引
        builder.HasIndex(r => new { r.DoctorId, r.CreatedAt }).HasDatabaseName("IX_Registrations_DoctorId_CreatedAt");

        // P2-11-1 报表时间范围索引：无 Doctor 过滤时按 CreatedAt 扫描（按日/周/月聚合）
        builder.HasIndex(r => r.CreatedAt).HasDatabaseName("IX_Registrations_CreatedAt");

        // 索引: 按状态筛选 (Waiting 队列查询高频)
        builder.HasIndex(r => r.Status);

        // 索引: MedicalCaseId (可空，接诊后填入)
        builder.HasIndex(r => r.MedicalCaseId);

        // QueueNumber: 当日顺序号
        builder.Property(r => r.QueueNumber).IsRequired();

        // RegistrationFee: 挂号费
        builder.Property(r => r.RegistrationFee).HasColumnType("decimal(10,2)").IsRequired();
    }
}


