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

            // Seed Data：创建系统唯一默认超级管理员 sysadmin
            // 注意：密码哈希为 BCrypt hash of "LybtAdmin2025@SecurePass!"
            // 使用 BCrypt.Net.BCrypt.HashPassword("LybtAdmin2025@SecurePass!") 生成
            entity.HasData(new User
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                UserName = "sysadmin",
                Email = "admin@lybt.com",
                RealName = "系统管理员",
                // BCrypt hash for "LybtAdmin2025@SecurePass!"
                // 使用 BCrypt.Net.BCrypt.HashPassword("LybtAdmin2025@SecurePass!", 11) 生成
                PasswordHash = "$2a$11$6vF3z.VwKQZLXxE9wE3D1eO5v6qU4xKQF9Qq9Ek3Z8Ky7Jq3Mq9oG",
                Role = UserRole.Admin, // 系统管理员角色
                Status = CommonStatus.Enabled, // 激活状态
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = null,
                IsDeleted = false,
                FailedLoginCount = 0,
                CreatedBy = null,
                UpdatedBy = null
            });
        }
    }
}
