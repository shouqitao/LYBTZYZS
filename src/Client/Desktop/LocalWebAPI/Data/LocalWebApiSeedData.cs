using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using LYBT.Entities.Auth;
using LYBT.Entities.Common;
using LYBT.Entities.Users;
using LYBT.Entities.Patients;
using LYBT.Entities.Herbs;
using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Enums;
using LYBT.Infrastructure.Data;

namespace LYBT.LocalWebAPI.Data;

public static class LocalWebApiSeedData
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        // sysadmin = 系统运维，IsSysAdmin=true
        var sysadmin = await context.Users.FirstOrDefaultAsync(u => u.UserName == "sysadmin");
        if (sysadmin == null)
        {
            sysadmin = new ApplicationUser
            {
                UserName = "sysadmin",
                RealName = "系统运维",
                Email = "sysadmin@lybtzyzs.local",
                IsSysAdmin = true,
                Role = UserRole.SuperAdmin,
                Status = CommonStatus.Enabled,
            };
            context.Users.Add(sysadmin);
        }

        // admin = 业务管理员，IsSysAdmin=false
        var admin = await context.Users.FirstOrDefaultAsync(u => u.UserName == "admin");
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = "admin",
                RealName = "系统管理员",
                Email = "admin@lybtzyzs.local",
                IsSysAdmin = false,
                Role = UserRole.Admin,
                Status = CommonStatus.Enabled,
            };
            context.Users.Add(admin);
        }

        if (!await context.Herbs.AnyAsync())
        {
            context.Herbs.Add(new Herb
            {
                Name = "Ginseng",
                Category = "Adaptogen",
                Unit = "g",
                Price = 9.99m,
                Status = CommonStatus.Enabled
            });
        }

        if (!await context.Formulas.AnyAsync())
        {
            context.Formulas.Add(new Formula
            {
                Name = "Sample Formula",
                Status = CommonStatus.Enabled,
                FormulaType = FormulaType.Experience,
            });
        }

        if (!await context.Patients.AnyAsync())
        {
            context.Patients.Add(new Patient
            {
                Name = "Sample Patient",
                BirthDate = DateTime.UtcNow.AddYears(-30),
                Gender = LYBT.Shared.Models.Enums.Gender.Unknown,
                Status = CommonStatus.Enabled
            });
        }

        await context.SaveChangesAsync();
    }
}
