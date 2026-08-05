using LYBT.Entities.MedicalCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations;

/// <summary>
/// MedicalCaseAuditLog 实体 EF Core 配置 - 审计表复合查询索引
/// </summary>
public class MedicalCaseAuditLogConfiguration : IEntityTypeConfiguration<MedicalCaseAuditLog>
{
    public void Configure(EntityTypeBuilder<MedicalCaseAuditLog> builder)
    {
        builder.HasIndex(e => new { e.MedicalCaseId, e.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_MedicalCaseAuditLogs_MedicalCaseId_CreatedAt");

        builder.HasIndex(e => new { e.OperatorId, e.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_MedicalCaseAuditLogs_OperatorId_CreatedAt");
    }
}
