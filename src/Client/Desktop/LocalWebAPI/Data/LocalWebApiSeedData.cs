using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LYBT.Entities.Auth;
using LYBT.Entities.Common;
using LYBT.Entities.Users;
using LYBT.Entities.Patients;
using LYBT.Entities.Herbs;
using LYBT.Entities.Formulas;
using LYBT.Shared.Utilities.Security;
using LYBT.Shared.Models.Enums;

namespace LYBT.LocalWebAPI.Data;

/// <summary>
/// Seed data for the embedded SQLite LocalWebAPI database.
/// </summary>
public static class LocalWebApiSeedData
{
    public static async Task SeedAsync(LocalWebApiDbContext context)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Seed default admin user if none exist
        if (!await context.Users.AnyAsync())
        {
            var passwordHash = PasswordHelper.HashPassword("admin", UserRole.Admin);
            var admin = new User
            {
                UserName = "admin",
                RealName = "Admin",
                PasswordHash = passwordHash,
                Role = UserRole.Admin,
                Status = CommonStatus.Enabled,
            };
            context.Users.Add(admin);
        }

        // Seed sample Herbs if none exist
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

        // Seed sample Formulas if none exist
        if (!await context.Formulas.AnyAsync())
        {
            context.Formulas.Add(new Formula
            {
                Name = "Sample Formula",
                Status = CommonStatus.Enabled,
                FormulaType = FormulaType.Experience,
            });
        }

        // Seed a sample Patient if none exist
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
