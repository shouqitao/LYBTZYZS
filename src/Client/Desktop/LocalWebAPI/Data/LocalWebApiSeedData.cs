using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LYBT.Entities.Common;
using LYBT.Entities.Patients;
using LYBT.Entities.Herbs;
using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Enums;
using LYBT.Infrastructure.Data;

namespace LYBT.LocalWebAPI.Data;

public static class LocalWebApiSeedData
{
    public static async Task SeedAsync(AppDbContext context, IServiceProvider? serviceProvider = null)
    {
        await context.Database.EnsureCreatedAsync();

        // Users are created by IdentitySeedData via UserManager (proper Identity password hashing).
        // Do NOT create users here — raw EF Core bypasses Identity and creates incompatible password hashes.

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
