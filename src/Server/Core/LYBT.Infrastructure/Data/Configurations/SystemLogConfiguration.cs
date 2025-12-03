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

            // Issue #1765: 删除5个多余索引
            // MVP阶段(<10K日志记录)无需任何索引
            // 日志查询频率极低，全表扫描足够快
            // 生产环境(>100K记录)时再考虑添加Timestamp索引
        }
    }
}
