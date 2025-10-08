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

            // 配置字段
            entity.Property(sl => sl.Timestamp).IsRequired();
            entity.Property(sl => sl.Level).HasMaxLength(50).IsRequired();
            entity.Property(sl => sl.Message).IsRequired();
            entity.Property(sl => sl.Exception);
            entity.Property(sl => sl.LoggerName).HasMaxLength(255);
            entity.Property(sl => sl.UserId);
            entity.Property(sl => sl.RequestId).HasMaxLength(36);
            entity.Property(sl => sl.MachineName).HasMaxLength(100);
            entity.Property(sl => sl.ThreadId);
            entity.Property(sl => sl.Properties);

            // 添加索引以提高查询性能
            entity.HasIndex(sl => sl.Timestamp).HasDatabaseName("IX_SystemLogs_Timestamp");
            entity.HasIndex(sl => sl.Level).HasDatabaseName("IX_SystemLogs_Level");
            entity.HasIndex(sl => sl.LoggerName).HasDatabaseName("IX_SystemLogs_LoggerName");
            entity.HasIndex(sl => sl.UserId).HasDatabaseName("IX_SystemLogs_UserId");
            entity.HasIndex(sl => new { sl.Timestamp, sl.Level }).HasDatabaseName("IX_SystemLogs_Timestamp_Level");
        }
    }
}
