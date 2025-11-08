using LYBT.Entities.Users;
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

            // Issue #1262: 数据库列名升级为 UserName（与实体属性一致）
            entity.HasIndex(u => u.UserName).IsUnique();
            entity.Property(u => u.UserName).HasMaxLength(50);
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

            // Issue #1909: 三角色体系 - SuperAdmin/Admin/Doctor统一存储在Users表
            // SuperAdmin通过Role=100标识，初始化时通过迁移脚本创建
            // 所有用户（包括SuperAdmin）统一使用Users表，简化认证流程
        }
    }
}
