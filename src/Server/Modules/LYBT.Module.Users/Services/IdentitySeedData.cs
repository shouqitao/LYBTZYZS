using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Constants;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Models.Primitives;

namespace LYBT.Module.Users.Services;

public static class IdentitySeedData
{
    public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var passwordOptions = serviceProvider.GetRequiredService<IOptions<DefaultPasswordOptions>>().Value;
        var environment = serviceProvider.GetRequiredService<IHostEnvironment>();

        string[] roles = { RoleConstants.Receptionist, RoleConstants.Doctor, RoleConstants.Admin, RoleConstants.SuperAdmin };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var sysAdminPassword = ResolveSysAdminPassword(environment, passwordOptions.SysAdminPassword);
        await EnsureUserAsync(userManager, UserConstants.SysAdminUsername, "系统运维", "sysadmin@lybtzyzs.local", sysAdminPassword, RoleConstants.SuperAdmin, isSysAdmin: true);
    }

    /// <summary>
    /// 解析系统管理员初始密码（K4 安全加固）
    /// 生产环境：必须通过环境变量提供（DefaultPasswords__SysAdminPassword，兼容 ${SYSADMIN_PASSWORD} 命名），
    /// 缺失时抛异常，禁止使用配置默认/明文密码回退；
    /// 开发/本地环境：允许使用配置默认密码。
    /// </summary>
    private static string ResolveSysAdminPassword(IHostEnvironment environment, string configuredPassword)
    {
        if (!environment.IsProduction())
            return configuredPassword;

        var envPassword = Environment.GetEnvironmentVariable("DefaultPasswords__SysAdminPassword");
        if (string.IsNullOrWhiteSpace(envPassword))
        {
            // 兼容 appsettings.Production.json 的 ${SYSADMIN_PASSWORD} 占位符命名
            envPassword = Environment.GetEnvironmentVariable("SYSADMIN_PASSWORD");
        }

        if (string.IsNullOrWhiteSpace(envPassword))
        {
            throw new InvalidOperationException(
                "生产环境必须通过环境变量 DefaultPasswords__SysAdminPassword（或 SYSADMIN_PASSWORD）提供系统管理员初始密码，禁止使用配置默认密码");
        }

        return envPassword;
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string userName, string realName, string email,
        string defaultPassword, string role, bool isSysAdmin)
    {
        var user = await userManager.FindByNameAsync(userName);

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = userName,
                RealName = realName,
                Email = email,
                IsSysAdmin = isSysAdmin,
                Role = Enum.Parse<LYBT.Shared.Models.Enums.UserRole>(role)
            };
            await userManager.CreateAsync(user, defaultPassword);
            await userManager.AddToRoleAsync(user, role);
            return;
        }

        if (user.LastLoginAt == null)
        {
            try
            {
                if (!await userManager.CheckPasswordAsync(user, defaultPassword))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    await userManager.ResetPasswordAsync(user, token, defaultPassword);
                }
            }
            catch
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                await userManager.ResetPasswordAsync(user, token, defaultPassword);
            }
        }

        if (user.IsSysAdmin != isSysAdmin)
        {
            user.IsSysAdmin = isSysAdmin;
            await userManager.UpdateAsync(user);
        }

        var userRole = Enum.Parse<LYBT.Shared.Models.Enums.UserRole>(role);
        if (user.Role != userRole)
        {
            user.Role = userRole;
            await userManager.UpdateAsync(user);
        }

        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Contains(role))
            await userManager.AddToRoleAsync(user, role);
    }
}


