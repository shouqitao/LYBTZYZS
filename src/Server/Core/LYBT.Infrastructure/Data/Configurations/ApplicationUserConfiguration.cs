using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LYBT.Entities.Users;

namespace LYBT.Infrastructure.Data.Configurations;

/// <summary>
/// ApplicationUser 基础配置。
/// 业务字段（枚举转换、软删除过滤、并发控制等）由 UserConfiguration 统一管理。
/// </summary>
public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // 表名映射由 UserConfiguration 统一设置（ToTable("Users")）
        // 此处仅保留 UserConfiguration 未覆盖的基础属性
        builder.Property(u => u.RealName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastLoginAt).IsRequired(false);
    }
}
