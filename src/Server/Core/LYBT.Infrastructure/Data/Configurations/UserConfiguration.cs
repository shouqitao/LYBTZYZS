using LYBT.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations;

/// <summary>
/// ApplicationUser 实体 EF Core 配置
/// 在 Identity 默认配置之外补充业务字段（Role / Status 枚举转换、软删除过滤等）
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // 表名沿用 Users（与 Identity 约定一致）
        builder.ToTable("AspNetUsers");

        // 字符串长度由 Entity 的 [StringLength] 定义，遵循 DRY 原则
        // 枚举转换（Fluent API 专属功能）
        builder.Property(u => u.Status).HasConversion<int>();
        builder.Property(u => u.Role).HasConversion<int>();

        // 软删除全局查询过滤器
        builder.HasQueryFilter(u => !u.IsDeleted);

        // 并发控制
        builder.Property(u => u.RowVersion).IsRowVersion().IsConcurrencyToken();

        // 审计字段
        builder.Property(u => u.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
        builder.Property(u => u.UpdatedAt).IsRequired(false);
        builder.Property(u => u.CreatedBy).IsRequired(false);
        builder.Property(u => u.UpdatedBy).IsRequired(false);

        // 软删除默认值
        builder.Property(u => u.IsDeleted).IsRequired().HasDefaultValue(false);

        // Issue #1909: 三角色体系 - SuperAdmin/Admin/Doctor统一存储在Users表
    }
}


