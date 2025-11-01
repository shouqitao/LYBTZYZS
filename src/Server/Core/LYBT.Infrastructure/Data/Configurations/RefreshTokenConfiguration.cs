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
            entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Jti).IsRequired().HasMaxLength(128);
            entity.Property(e => e.ClientIp).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.RevokedReason).HasMaxLength(200);
            entity.Property(e => e.ReplacedByToken).HasMaxLength(500);
            entity.Property(e => e.FamilyId).HasMaxLength(128);
            entity.Property(e => e.DeviceId).HasMaxLength(128);
            entity.Property(e => e.DeviceName).HasMaxLength(200);

            // Issue #1765: 仅保留Token唯一索引（JWT验证必需）
            entity.HasIndex(e => e.Token).IsUnique();

            // 删除5个多余索引：
            // - UserId: EF Core外键自动创建索引
            // - ExpiresAt/IsRevoked/Jti: MVP阶段(<10K记录)无需额外索引
            // - UserId+IsRevoked复合索引: MVP阶段无需

            // 与用户的关系
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
