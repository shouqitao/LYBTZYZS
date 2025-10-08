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
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.LoginTime);
            entity.HasIndex(e => e.Status);
        }
    }
}
