using LYBT.Entities.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations
{
    /// <summary>
    /// SystemLog 实体 EF Core 配置
    /// </summary>
    public class SystemLogConfiguration : IEntityTypeConfiguration<SystemLog>
    {
        public void Configure(EntityTypeBuilder<SystemLog> entity)
        {
            entity.ToTable("SystemLogs");
            entity.HasKey(sl => sl.Id);

            // 字符串长度由 Entity 的 [MaxLength] 定义，遵循 DRY 原则
            entity.Property(sl => sl.Timestamp).IsRequired();
            entity.Property(sl => sl.Level).IsRequired();
            entity.Property(sl => sl.Message).IsRequired();

            // V1.0.0: 生产环境索引优化
            // Timestamp索引 - 日志查询按时间范围
            entity.HasIndex(sl => sl.Timestamp)
                .HasDatabaseName("IX_SystemLogs_Timestamp");

            // Level索引 - 按级别筛选(Warning/Error)
            entity.HasIndex(sl => sl.Level)
                .HasDatabaseName("IX_SystemLogs_Level");

            // CorrelationId索引 - 端到端请求追踪
            entity.HasIndex(sl => sl.CorrelationId)
                .HasDatabaseName("IX_SystemLogs_CorrelationId");

            // UserId索引 - 按用户筛选日志
            entity.HasIndex(sl => sl.UserId)
                .HasDatabaseName("IX_SystemLogs_UserId");
        }
    }
}
