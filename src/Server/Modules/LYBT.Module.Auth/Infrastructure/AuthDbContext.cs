using LYBT.Module.Auth.Domain;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Auth.Infrastructure;

/// <summary>
/// 认证模块数据库上下文。使用共享连接、独立Schema的方式实现模块数据隔离。
/// </summary>
public class AuthDbContext : DbContext
{
    /// <summary>认证会话集</summary>
    public DbSet<AuthSession> AuthSessions { get; set; } = null!;

    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuthSession>(entity =>
        {
            entity.ToTable("AuthSessions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).HasMaxLength(256).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(45).IsRequired();
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Status);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TokenHash);
            entity.HasIndex(e => e.ExpiryTime);
        });
    }
}


