using LYBT.Entities.MedicalCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations;

/// <summary>
/// MedicalCaseAuditLog 实体 EF Core 配置
/// OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
/// </summary>
public class MedicalCaseAuditLogConfiguration : IEntityTypeConfiguration<MedicalCaseAuditLog>
{
    public void Configure(EntityTypeBuilder<MedicalCaseAuditLog> builder)
    {
        builder.ToTable("MedicalCaseAuditLogs");
        builder.HasKey(e => e.Id);

        // 必填字段配置
        builder.Property(e => e.MedicalCaseId).IsRequired();
        builder.Property(e => e.OperatorId).IsRequired();
        builder.Property(e => e.OperatorName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.OperatorRole).IsRequired();
        builder.Property(e => e.OperationType).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        // JSON字段
        builder.Property(e => e.ChangedFields);
        builder.Property(e => e.OldValues);
        builder.Property(e => e.NewValues);

        // 配置与MedicalCase的关系
        // 使用OnDelete(Restrict)防止级联删除，审计日志应永久保留
        builder.HasOne(e => e.MedicalCase)
            .WithMany()
            .HasForeignKey(e => e.MedicalCaseId)
            .OnDelete(DeleteBehavior.Restrict);

        // 添加匹配的查询过滤器，解决EF Core警告：
        // "Entity 'MedicalCase' has a global query filter defined and is the required end of a relationship"
        // 当MedicalCase被软删除时，其审计日志默认也被过滤
        // 需要访问已删除医案的审计日志时，使用 .IgnoreQueryFilters()
        builder.HasQueryFilter(log => log.MedicalCase != null && !log.MedicalCase.IsDeleted);

        // 按MedicalCaseId查询优化索引
        builder.HasIndex(e => new { e.MedicalCaseId, e.CreatedAt })
            .HasDatabaseName("IX_MedicalCaseAuditLogs_MedicalCaseId_CreatedAt")
            .IsDescending(false, true);

        // 按OperatorId查询优化索引
        builder.HasIndex(e => new { e.OperatorId, e.CreatedAt })
            .HasDatabaseName("IX_MedicalCaseAuditLogs_OperatorId_CreatedAt")
            .IsDescending(false, true);
    }
}
