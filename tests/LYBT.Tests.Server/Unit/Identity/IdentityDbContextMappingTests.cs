using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Module.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// IdentityDbContext 映射守卫（IDENTITY-DBCONTEXT-FIX 回归：
/// ApplicationUser.LastLoginTime 必须映射到 Users 表 LastLoginAt 列——
/// 漏挂 UserConfiguration 时 EF 按属性名查询 → SqlException → 登录 500）
/// </summary>
public class IdentityDbContextMappingTests
{
    [Fact]
    public void ApplicationUser_LastLoginTime_MapsToLastLoginAtColumn()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new IdentityDbContext(options);

        var entityType = context.Model.FindEntityType(typeof(ApplicationUser));
        entityType.Should().NotBeNull();

        var property = entityType!.FindProperty(nameof(ApplicationUser.LastLoginTime));
        property.Should().NotBeNull("UserConfiguration 必须应用（LastLoginTime 属性映射）");
        property!.GetColumnName().Should().Be("LastLoginAt",
            "EF 查询必须用 LastLoginAt 列（Users 表实际列名）——防登录 SqlException");
    }

    [Fact]
    public void ApplicationUser_RealName_IsMapped()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new IdentityDbContext(options);

        var property = context.Model.FindEntityType(typeof(ApplicationUser))!.FindProperty(nameof(ApplicationUser.RealName));
        property.Should().NotBeNull();
    }
}
