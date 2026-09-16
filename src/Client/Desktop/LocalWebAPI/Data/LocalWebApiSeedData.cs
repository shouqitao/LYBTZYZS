using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LYBT.Entities.Common;
using LYBT.Entities.Patients;
using LYBT.Entities.Herbs;
using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Enums;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Serialization;

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
                Name = "人参",
                Category = "补气药",
                Unit = "克",
                Price = 9.99m,
                Status = CommonStatus.Enabled
            });
        }

        if (!await context.Formulas.AnyAsync())
        {
            context.Formulas.Add(new Formula
            {
                Name = "示例验方",
                Status = CommonStatus.Enabled,
                FormulaType = FormulaType.Experience,
            });
        }

        if (!await context.Patients.AnyAsync())
        {
            context.Patients.Add(new Patient
            {
                Name = "示例患者",
                BirthDate = DateTime.UtcNow.AddYears(-30),
                Gender = LYBT.Shared.Models.Enums.Gender.Unknown,
                Status = CommonStatus.Enabled
            });
        }

        await context.SaveChangesAsync();

        // R-6: 存量患者 IdCardHash 回填（迁移后一次性；新数据经 PatientRepository 写入时已带 hash）
        await BackfillPatientIdCardHashesAsync(context);
    }

    private static async Task BackfillPatientIdCardHashesAsync(AppDbContext context)
    {
        var candidates = await context.Patients
            .Where(p => p.IdCardHash == null && p.IdNumber != null)
            .ToListAsync();
        if (candidates.Count == 0)
            return;

        foreach (var patient in candidates)
        {
            var hash = SensitiveDataHashHelper.ComputeHmacSha256Hex(patient.IdNumber);
            if (hash != null)
                patient.IdCardHash = hash;
        }

        await context.SaveChangesAsync();
    }
}
