using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using LYBT.Entities.Users;

namespace LYBT.Module.Users.Services;

public static class IdentitySeedData
{
    public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = { "Receptionist", "Doctor", "Admin", "SuperAdmin" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        // sysadmin = 系统运维，IsSysAdmin=true，不可删除
        var sysadmin = await userManager.FindByNameAsync("sysadmin");
        if (sysadmin == null)
        {
            sysadmin = new ApplicationUser
            {
                UserName = "sysadmin",
                RealName = "系统运维",
                Email = "sysadmin@lybtzyzs.local",
                IsSysAdmin = true
            };
            await userManager.CreateAsync(sysadmin, "SysAdmin@2026!");
            await userManager.AddToRoleAsync(sysadmin, "SuperAdmin");
        }
        else
        {
            if (!await userManager.CheckPasswordAsync(sysadmin, "SysAdmin@2026!"))
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(sysadmin);
                await userManager.ResetPasswordAsync(sysadmin, token, "SysAdmin@2026!");
            }
            if (!sysadmin.IsSysAdmin)
            {
                sysadmin.IsSysAdmin = true;
                await userManager.UpdateAsync(sysadmin);
            }
            var sysadminRoles = await userManager.GetRolesAsync(sysadmin);
            if (!sysadminRoles.Contains("SuperAdmin"))
                await userManager.AddToRoleAsync(sysadmin, "SuperAdmin");
        }

        // admin = 业务管理员，IsSysAdmin=false，由 sysadmin 创建
        var admin = await userManager.FindByNameAsync("admin");
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = "admin",
                RealName = "系统管理员",
                Email = "admin@lybtzyzs.local",
                IsSysAdmin = false
            };
            await userManager.CreateAsync(admin, "Admin@123456");
            await userManager.AddToRoleAsync(admin, "Admin");
        }
        else
        {
            if (!await userManager.CheckPasswordAsync(admin, "Admin@123456"))
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(admin);
                await userManager.ResetPasswordAsync(admin, token, "Admin@123456");
            }
            var adminRoles = await userManager.GetRolesAsync(admin);
            if (!adminRoles.Contains("Admin"))
                await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}
