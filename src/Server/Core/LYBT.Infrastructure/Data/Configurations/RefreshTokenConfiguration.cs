using LYBT.Entities.Auth;
using LYBT.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations
{
    /// <summary>
    /// RefreshToken 实体 EF Core 配置
    /// </summary>
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> entity)
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(e => e.Id);

            // 字符串长度由 Entity 的 [StringLength] 定义，遵循 DRY 原则
            entity.Property(e => e.Token).IsRequired();
            entity.Property(e => e.Jti).IsRequired();

            // Issue #1765: Token唯一索引（JWT验证必需）
            entity.HasIndex(e => e.Token).IsUnique();

            // Issue #1868: Token撤销优化索引（覆盖索引减少IO）
            entity.HasIndex(e => new { e.IsRevoked, e.Token })
                .HasDatabaseName("IX_RefreshTokens_IsRevoked_Token")
                .IncludeProperties(e => new { e.UserId, e.UserType, e.ExpiresAt });

            // Issue #1864 AUTH-007: Token重放攻击检测索引
            // 用于快速查找同一Family下的所有Token
            entity.HasIndex(e => e.FamilyId)
                .HasDatabaseName("IX_RefreshTokens_FamilyId");

            // 与用户的关系
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
