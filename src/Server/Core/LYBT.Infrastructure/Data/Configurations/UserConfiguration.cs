using LYBT.Entities.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations
{
    /// <summary>
    /// User 实体 EF Core 配置
    /// </summary>
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> entity)
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);

            // 明确配置字段映射以解决命名冲突 - 统一为Username列名
            entity.HasIndex(u => u.UserName).IsUnique();
            entity.Property(u => u.UserName).HasMaxLength(50).HasColumnName("Username");
            entity.Property(u => u.RealName).HasMaxLength(50);
            entity.Property(u => u.PasswordHash).HasMaxLength(256);

            // CreateTime字段已删除（UltraThink v2.0简化）
            entity.Property(u => u.PinYinCode).HasMaxLength(50);
            entity.Property(u => u.PhoneNumber).HasMaxLength(20);

            // UltraThink v2.0: Remark字段已删除（简化用户管理）
            // 配置枚举字段
            entity.Property(u => u.Status).HasConversion<int>();
            entity.Property(u => u.Role).HasConversion<int>();

            // 配置并发控制字段
            entity.Property(u => u.RowVersion).IsRowVersion().IsConcurrencyToken();

            // Issue #1074: 移除sysadmin种子数据
            // 超级管理员通过AdminSecrets表认证，不应存在于Users表中
            // 保持Users表仅用于业务用户，确保架构边界清晰
        }
    }
}
