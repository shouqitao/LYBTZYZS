using LYBT.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Users.Infrastructure;

/// <summary>
/// 用户模块数据库上下文。使用共享连接、独立Schema的方式实现模块数据隔离。
/// </summary>
public class UsersDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 设置用户模块Schema
        modelBuilder.HasDefaultSchema("Users");

        // 应用程序用户配置
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(e => e.UserName).HasMaxLength(32).IsRequired();
            entity.Property(e => e.RealName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PinYinCode).HasMaxLength(50);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Remark).HasMaxLength(500);
            entity.Property(e => e.Role).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();

            // 索引
            entity.HasIndex(e => e.UserName).IsUnique();
            entity.HasIndex(e => e.PinYinCode);
            entity.HasIndex(e => e.Role);
            entity.HasIndex(e => e.Status);
        });
    }
}


