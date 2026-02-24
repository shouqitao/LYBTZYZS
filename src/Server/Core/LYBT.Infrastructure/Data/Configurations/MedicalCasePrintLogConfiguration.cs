using LYBT.Entities.MedicalCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations;

/// <summary>
/// MedicalCasePrintLog 实体 EF Core 配置
/// T2-X8-12: 打印日志从 Prescription 层级迁移到 MedicalCase 层级
/// </summary>
public class MedicalCasePrintLogConfiguration : IEntityTypeConfiguration<MedicalCasePrintLog>
{
    public void Configure(EntityTypeBuilder<MedicalCasePrintLog> entity)
    {
        entity.ToTable("MedicalCasePrintLogs");
        entity.HasKey(l => l.Id);

        // PrintType 枚举存储为 int
        entity.Property(l => l.PrintType).HasConversion<int>();

        // FK 关系在 MedicalCaseConfiguration 中配置 (聚合根拥有关系定义)

        // 配置并发控制字段
        entity.Property(l => l.RowVersion).IsRowVersion().IsConcurrencyToken();
    }
}
