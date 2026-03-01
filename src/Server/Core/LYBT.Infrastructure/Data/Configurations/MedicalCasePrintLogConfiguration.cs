using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Data.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations;

/// <summary>
/// MedicalCasePrintLog 实体 EF Core 配置
/// T2-X8-12: 打印日志从 Prescription 层级迁移到 MedicalCase 层级
/// CODE-31: 继承 BaseEntityConfiguration 统一审计字段和并发控制
/// </summary>
public class MedicalCasePrintLogConfiguration : BaseEntityConfiguration<MedicalCasePrintLog>
{
    public override void Configure(EntityTypeBuilder<MedicalCasePrintLog> builder)
    {
        base.Configure(builder);

        builder.ToTable("MedicalCasePrintLogs");

        // PrintType 枚举存储为 int
        builder.Property(l => l.PrintType).HasConversion<int>();

        // FK 关系在 MedicalCaseConfiguration 中配置 (聚合根拥有关系定义)
    }
}
