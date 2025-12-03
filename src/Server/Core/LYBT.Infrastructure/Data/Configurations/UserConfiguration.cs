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

        // 索引配置（Fluent API 专属功能）
        builder.HasIndex(u => u.UserName).IsUnique();

        // 字符串长度由 Entity 的 [StringLength] 定义，遵循 DRY 原则
        // 枚举转换（Fluent API 专属功能）
        builder.Property(u => u.Status).HasConversion<int>();
        builder.Property(u => u.Role).HasConversion<int>();

        // Issue #1909: 三角色体系 - SuperAdmin/Admin/Doctor统一存储在Users表
    }
}
