using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
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

        string[] roles = { RoleConstants.Receptionist, RoleConstants.Doctor, RoleConstants.Admin, RoleConstants.SuperAdmin };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        await EnsureUserAsync(userManager, UserConstants.SysAdminUsername, "系统运维", "sysadmin@lybtzyzs.local", passwordOptions.SysAdminPassword, RoleConstants.SuperAdmin, isSysAdmin: true);
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


