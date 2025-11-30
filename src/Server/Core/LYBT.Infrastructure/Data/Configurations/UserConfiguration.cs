using LYBT.Entities.Users;
using LYBT.Infrastructure.Data.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations;

/// <summary>
/// User 实体 EF Core 配置
/// 继承 BaseEntityConfiguration 统一审计字段和并发控制
/// </summary>
public class UserConfiguration : BaseEntityConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);

        builder.ToTable("Users");
        builder.HasIndex(u => u.UserName).IsUnique();
        builder.Property(u => u.UserName).HasMaxLength(50);
        builder.Property(u => u.RealName).HasMaxLength(50);
        builder.Property(u => u.PasswordHash).HasMaxLength(256);
        builder.Property(u => u.PinYinCode).HasMaxLength(50);
        builder.Property(u => u.PhoneNumber).HasMaxLength(20);

        // 配置枚举字段
        builder.Property(u => u.Status).HasConversion<int>();
        builder.Property(u => u.Role).HasConversion<int>();

        // Issue #1909: 三角色体系 - SuperAdmin/Admin/Doctor统一存储在Users表
    }
}
