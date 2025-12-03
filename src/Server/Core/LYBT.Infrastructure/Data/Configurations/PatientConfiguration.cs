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

        // 字符串长度由 Entity 的 [StringLength] 定义，遵循 DRY 原则
        // 枚举转换（Fluent API 专属功能）
        builder.Property(p => p.Status).HasConversion<int>();
    }
}
