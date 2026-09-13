using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Module.Identity.Services;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Models.Utilities.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// IdentitySeedData 种子测试（离线 SQLite/InMemory，无真实 SQL 依赖）。
/// <para>回归背景（13c #134 附带发现 / 本次修复）：本地模式首启失败报
/// <c>InvalidOperationException: User security stamp cannot be null</c>——
/// 根因是 <c>EnsureUserAsync</c> 建号路径**忽略 <c>CreateAsync</c> 结果**：当默认密码不满足策略
/// （本地 Shell appsettings 为占位符 <c>__REPLACE__</c>）时 <c>CreateAsync</c> 在写入 SecurityStamp 前即失败返回，
/// 代码继续调用 <c>AddToRoleAsync</c> → <c>GetSecurityStampAsync</c> 见 null → 抛出与真实原因无关的异常。</para>
/// </summary>
public class IdentitySeedDataTests
{
    private const string CompliantPassword = "SeedTest@2026!";

    /// <summary>与 Server/LocalWebAPI Program 一致的密码策略（策略值取自 PasswordPolicyValidator.Policy）</summary>
    private static ServiceProvider BuildProvider(string sysAdminPassword)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase($"IdentitySeed_{Guid.NewGuid():N}"));
        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireDigit = PasswordPolicyValidator.Policy.RequireDigit;
                options.Password.RequiredLength = PasswordPolicyValidator.Policy.MinLength;
                options.Password.RequireNonAlphanumeric = PasswordPolicyValidator.Policy.RequireSpecialChar;
                options.Password.RequireUppercase = PasswordPolicyValidator.Policy.RequireUppercase;
                options.Password.RequireLowercase = PasswordPolicyValidator.Policy.RequireLowercase;
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment(Environments.Development));
        services.Configure<DefaultPasswordOptions>(options =>
        {
            options.SysAdminPassword = sysAdminPassword;
            options.NewUserPassword = sysAdminPassword;
        });

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Seed_NonCompliantDefaultPassword_ThrowsActionableError_NotSecurityStampError()
    {
        // 本地 Shell appsettings 默认密码为占位符——真实首启场景
        await using var provider = BuildProvider("__REPLACE__");
        using var scope = provider.CreateScope();

        var act = async () => await IdentitySeedData.SeedRolesAndAdminAsync(scope.ServiceProvider);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.Message.Should().Contain("DefaultPasswords",
            "失败信息必须指向可修复的配置项（DefaultPasswords:SysAdminPassword / DefaultPasswords__SysAdminPassword）");
        ex.Which.Message.Should().NotContain("security stamp",
            "不得再以 SecurityStamp 空值掩盖真实原因（密码不满足策略）");
    }

    [Fact]
    public async Task Seed_CompliantPassword_CreatesSysAdminWithSecurityStampAndRole_And_IsIdempotent()
    {
        await using var provider = BuildProvider(CompliantPassword);
        using var scope = provider.CreateScope();

        await IdentitySeedData.SeedRolesAndAdminAsync(scope.ServiceProvider);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByNameAsync(LYBT.Shared.Models.Primitives.UserConstants.SysAdminUsername);

        user.Should().NotBeNull();
        user!.SecurityStamp.Should().NotBeNullOrEmpty("SecurityStamp 是后续令牌/角色操作的前置条件");
        user.IsSysAdmin.Should().BeTrue();
        (await userManager.GetRolesAsync(user)).Should().Contain(LYBT.Infrastructure.Constants.RoleConstants.SuperAdmin);

        // 幂等：重复种子（模拟每次启动）不得抛异常，也不得重复建号
        await IdentitySeedData.SeedRolesAndAdminAsync(scope.ServiceProvider);
        (await userManager.GetRolesAsync(user)).Should().HaveCount(1);
    }

    [Fact]
    public async Task Seed_ExistingUserWithNullSecurityStamp_IsRepairedBeforeRoleAssignment()
    {
        // 历史版本裸 EF 建号 → 库中 SecurityStamp 为 NULL 的存量用户
        await using var provider = BuildProvider(CompliantPassword);
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var legacy = new ApplicationUser
        {
            UserName = LYBT.Shared.Models.Primitives.UserConstants.SysAdminUsername,
            RealName = "系统运维",
            Email = "sysadmin@lybtzyzs.local",
        };
        db.Users.Add(legacy);
        await db.SaveChangesAsync();
        legacy.SecurityStamp.Should().BeNullOrEmpty("构造前提：存量用户无 SecurityStamp");

        await IdentitySeedData.SeedRolesAndAdminAsync(scope.ServiceProvider);

        var repaired = await userManager.FindByNameAsync(LYBT.Shared.Models.Primitives.UserConstants.SysAdminUsername);
        repaired!.SecurityStamp.Should().NotBeNullOrEmpty("种子必须先补齐 SecurityStamp 再做角色分配");
        (await userManager.GetRolesAsync(repaired)).Should().Contain(LYBT.Infrastructure.Constants.RoleConstants.SuperAdmin);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "LYBT.Tests.Server";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
