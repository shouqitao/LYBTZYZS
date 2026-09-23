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

        // R-6: 存量患者 HMAC 盲索引回填（迁移后一次性；新数据经 PatientRepository 写入时已带 hash）
        await BackfillPatientSearchHashesAsync(context);
    }

    /// <summary>
    /// 回填存量患者 HMAC 盲索引（R-6）：<c>IdCardHash</c>（身份证）与 <c>PhoneSearchHash</c>（手机号，2026-09-23 补）。
    /// 幂等——仅处理仍为 null 的列。
    /// </summary>
    private static async Task BackfillPatientSearchHashesAsync(AppDbContext context)
    {
        var candidates = await context.Patients
            .Where(p => (p.IdCardHash == null && p.IdNumber != null)
                        || (p.PhoneSearchHash == null && p.PhoneNumber != null))
            .ToListAsync();
        if (candidates.Count == 0)
            return;

        foreach (var patient in candidates)
        {
            if (patient.IdCardHash == null)
            {
                var idHash = SensitiveDataHashHelper.ComputeHmacSha256Hex(patient.IdNumber);
                if (idHash != null)
                    patient.IdCardHash = idHash;
            }

            if (patient.PhoneSearchHash == null)
            {
                var phoneHash = SensitiveDataHashHelper.ComputeHmacSha256Hex(patient.PhoneNumber?.Trim());
                if (phoneHash != null)
                    patient.PhoneSearchHash = phoneHash;
            }
        }

        await context.SaveChangesAsync();
    }
}
