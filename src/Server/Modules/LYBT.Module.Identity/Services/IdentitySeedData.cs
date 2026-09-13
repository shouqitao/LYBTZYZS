using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Constants;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Models.Primitives;
using LYBT.Shared.Models.Utilities.Security;

namespace LYBT.Module.Identity.Services;

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
    /// 生产环境：必须通过环境变量提供（DefaultPasswords__SysAdminPassword——配置唯一化，单一变量名），
    /// 缺失时抛异常，禁止使用配置默认/明文密码回退；
    /// 开发/本地环境：允许使用配置默认密码。
    /// </summary>
    private static string ResolveSysAdminPassword(IHostEnvironment environment, string configuredPassword)
    {
        if (!environment.IsProduction())
            return configuredPassword;

        // 配置唯一化（2026-08-12）：单一环境变量名——DefaultPasswords__SysAdminPassword（双下划线——踩坑 #1）
        var envPassword = Environment.GetEnvironmentVariable("DefaultPasswords__SysAdminPassword");

        if (string.IsNullOrWhiteSpace(envPassword))
        {
            throw new InvalidOperationException(
                "生产环境必须通过环境变量 DefaultPasswords__SysAdminPassword 提供系统管理员初始密码，禁止使用配置默认密码");
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

            var createResult = await userManager.CreateAsync(user, defaultPassword);
            if (!createResult.Succeeded)
            {
                // 建号失败必须显式抛出：Identity 的 CreateAsync(user, password) 在「密码不满足策略」时
                // **先于**写入 SecurityStamp 返回失败结果；若忽略该结果继续 AddToRoleAsync，
                // 只会得到与真实原因无关的「User security stamp cannot be null.」
                // ——掩盖「默认密码未注入/仍为占位符」等运维问题（本地模式 Shell appsettings 的 __REPLACE__ 即此场景，13c #134/#135）。
                throw new InvalidOperationException(
                    $"[SEED] 创建用户 {userName} 失败：{string.Join("; ", createResult.Errors.Select(e => e.Description))}"
                    + $"。请检查默认密码配置 DefaultPasswords:SysAdminPassword（环境变量 DefaultPasswords__SysAdminPassword）"
                    + $"是否满足密码策略（至少 {PasswordPolicyValidator.Policy.MinLength} 位，含大写/小写/数字/特殊字符）"
                    + "——占位符 __REPLACE__ 不可用，本地/测试环境同样需注入。");
            }

            // 防御：个别 Store/早退路径可能未写 stamp，而后续角色分配要求非空
            await EnsureSecurityStampAsync(userManager, user, userName);
            await userManager.AddToRoleAsync(user, role);
            return;
        }

        // 存量用户 SecurityStamp 修复（2026-08-17）：历史版本曾经裸 EF Core 建用户 → SecurityStamp 为空 →
        // 后续 GeneratePasswordResetTokenAsync → GetSecurityStampAsync 抛
        // InvalidOperationException("User security stamp cannot be null.")
        // → LocalWebAPI 种子失败 → 嵌入式服务启动失败 → 本地模式不可用。
        // 先补 stamp 再进入密码重置/角色校准流程（校验与令牌生成均依赖 stamp）。
        await EnsureSecurityStampAsync(userManager, user, userName);

        if (user.LastLoginTime == null)
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

    /// <summary>
    /// 保证用户 SecurityStamp 非空——Identity 的角色分配/令牌生成链
    /// （AddToRoleAsync → UpdateUserAsync → ValidateUserAsync → GetSecurityStampAsync）要求其非空，
    /// 否则抛「User security stamp cannot be null.」。覆盖两类来源：
    /// ① 历史版本裸 EF Core 建号（库中为 NULL）；② 建号路径早退未写 stamp。
    /// </summary>
    private static async Task EnsureSecurityStampAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        string userName)
    {
        if (!string.IsNullOrEmpty(user.SecurityStamp))
            return;

        var stampResult = await userManager.UpdateSecurityStampAsync(user);
        if (!stampResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"[SEED] 无法为用户 {userName} 修复 SecurityStamp：{string.Join("; ", stampResult.Errors.Select(e => e.Description))}");
        }
    }
}
