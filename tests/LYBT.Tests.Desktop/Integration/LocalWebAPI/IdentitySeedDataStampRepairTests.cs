using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.LocalWebAPI;
using LYBT.Shared.Configuration.Options.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LYBT.Tests.Desktop;

/// <summary>
/// IdentitySeedData null-SecurityStamp 修复回归测试（2026-08-17）。
/// 场景：历史版本曾裸 EF Core 建用户 → SecurityStamp 为空 → 重新启动种子时
/// GeneratePasswordResetTokenAsync → GetSecurityStampAsync 抛
/// InvalidOperationException("User security stamp cannot be null.") →
/// LocalWebApiStartupStep 失败 → 本地模式不可用。
/// 本测试经 LocalWebApiProgram 真实启动路径（MigrateAsync + 种子）验证修复。
/// </summary>
public class IdentitySeedDataStampRepairTests : IAsyncLifetime
{
    private readonly string _dbName = $"LYBTZYZS_SeedRepair_{Guid.NewGuid():N}";
    private string _connectionString = null!;
    private WebApplication? _app;

    public async Task InitializeAsync()
    {
        _connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={_dbName};Trusted_Connection=True;TrustServerCertificate=True";

        // 与 LocalWebApiProgram.CreateBuilder 相同的环境约束（内容根 = 测试输出目录）
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://127.0.0.1:0");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");

        var builder = LocalWebApiProgram.CreateBuilder(new[] { "--no-launch-settings" });
        // 种子默认密码显式注入（appsettings 的 __REPLACE__ 占位不适用于测试断言）
        builder.Configuration["DefaultPasswords:SysAdminPassword"] = "SysAdmin@2026!";
        builder.Configuration["DefaultPasswords:AdminPassword"] = "Admin@123456";
        builder.Configuration["DefaultPasswords:NewUserPassword"] = "User@123456";

        _app = LocalWebApiProgram.CreateApplication(builder, _connectionString);

        // 只验证 DI + 数据库种子，不起 Kestrel（避免端口占用/监听依赖）
        await LocalWebApiProgram.InitializeDatabaseAsync(_app);
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            using var scope = _app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureDeletedAsync();
            await _app.DisposeAsync();
        }
    }

    /// <summary>
    /// 存量用户（stamp 空 + 密码哈希不兼容 + 从未登录）在重新种子时被修复：
    /// 不抛 NullSecurityStamp、stamp 补全、密码重置为默认可验证。
    /// </summary>
    [Fact]
    public async Task Seed_Repairs_Legacy_User_With_Null_SecurityStamp()
    {
        using var scope = _app!.Services.CreateScope();
        var sp = scope.ServiceProvider;

        // 模拟历史版本裸 EF 建的存量 sysadmin：stamp 空 + 哈希不兼容 + 从未登录
        // AsNoTracking：避免本 scope 缓存旧实体导致种子修复后读到陈旧值
        var db = sp.GetRequiredService<AppDbContext>();
        var legacy = await db.Users.AsNoTracking().SingleAsync(u => u.UserName == "sysadmin");
        legacy.SecurityStamp = null;
        legacy.PasswordHash = "incompatible-legacy-hash";
        legacy.LastLoginTime = null;
        db.Update(legacy);
        await db.SaveChangesAsync();

        // 重新执行种子（模拟应用重启再次启动 LocalWebAPI）——修复前此处抛 NullSecurityStamp
        var act = async () => await LocalWebApiProgram.InitializeDatabaseAsync(_app);
        await act.Should().NotThrowAsync();

        // 修复后：stamp 已补全、密码已重置为默认并可通过 UserManager 验证（AsNoTracking 读库）
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var repaired = await db.Users.AsNoTracking().SingleAsync(u => u.UserName == "sysadmin");
        repaired.SecurityStamp.Should().NotBeNullOrEmpty();
        (await userManager.CheckPasswordAsync(repaired, "SysAdmin@2026!")).Should().BeTrue();
    }

    /// <summary>
    /// 已登录过的存量用户（stamp 空但 LastLoginTime 非空）同样修复，不影响角色/标识。
    /// </summary>
    [Fact]
    public async Task Seed_Repairs_Stamp_Even_When_User_Has_Logged_In()
    {
        using var scope = _app!.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        var legacy = await db.Users.AsNoTracking().SingleAsync(u => u.UserName == "sysadmin");
        legacy.SecurityStamp = null;
        legacy.LastLoginTime = DateTime.UtcNow.AddDays(-1);
        db.Update(legacy);
        await db.SaveChangesAsync();

        var act = async () => await LocalWebApiProgram.InitializeDatabaseAsync(_app);
        await act.Should().NotThrowAsync();

        var repaired = await db.Users.AsNoTracking().SingleAsync(u => u.UserName == "sysadmin");
        repaired.SecurityStamp.Should().NotBeNullOrEmpty();
        repaired.IsSysAdmin.Should().BeTrue();
    }
}