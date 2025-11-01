using LYBT.Entities.Auth;
using LYBT.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations
{
    /// <summary>
    /// AuthSession 实体 EF Core 配置
    /// </summary>
    public class AuthSessionConfiguration : IEntityTypeConfiguration<AuthSession>
    {
        public void Configure(EntityTypeBuilder<AuthSession> entity)
        {
            entity.ToTable("AuthSessions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).HasMaxLength(256);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Status).HasConversion<int>();

            // Issue #1765: 删除3个多余索引
            // - UserId: EF Core外键自动创建索引
            // - LoginTime/Status: MVP阶段(<10K记录)无需额外索引
        }
    }
}
