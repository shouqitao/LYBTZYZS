using LYBT.Entities.Patients;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Patients.Interfaces;
using LYBT.Infrastructure.Extensions;
using LYBT.Infrastructure.Repositories;
using LYBT.Infrastructure.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Patients.Infrastructure;

/// <summary>
/// 患者仓储实现。封装患者数据访问逻辑。
/// ADR-0017: 注入患者模块自己的 DbContext
/// </summary>
public class PatientRepository : BaseRepository<Patient, PatientsDbContext>, IPatientRepository
{
    public PatientRepository(PatientsDbContext context, ILogger<PatientRepository> logger)
        : base(context, logger)
    {
    }

    /// <inheritdoc/>
    public override async Task<Patient> AddAsync(Patient entity, CancellationToken cancellationToken = default)
    {
        EnsureSearchHashes(entity);
        return await base.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task<Patient> UpdateAsync(Patient entity, CancellationToken cancellationToken = default)
    {
        EnsureSearchHashes(entity);
        return await base.UpdateAsync(entity, cancellationToken);
    }

    /// <summary>
    /// 计算并写入 HMAC 盲索引列（R-6）：<see cref="Patient.IdCardHash"/>（身份证）与
    /// <see cref="Patient.PhoneSearchHash"/>（手机号）。对应明文为空时清空 hash。
    /// </summary>
    private static void EnsureSearchHashes(Patient entity)
    {
        entity.IdCardHash = SensitiveDataHashHelper.ComputeHmacSha256Hex(entity.IdNumber);
        entity.PhoneSearchHash = SensitiveDataHashHelper.ComputeHmacSha256Hex(entity.PhoneNumber?.Trim());
    }

    /// <inheritdoc/>
    public async Task<PagedResult<Patient>> GetPagedAsync(
        int page, int pageSize, string? keyword, CommonStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Patients
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            // P2-7: 姓名/拼音码前缀匹配走索引；手机号走 HMAC 盲索引**精确匹配**
            // （2026-09-23 修复：原 `p.PhoneNumber.Contains(kw)` 作用在 AES-GCM 加密列上，
            //  EF 把参数也加密后生成 `LIKE @p ESCAPE N'<密文>'`，密文含非法转义字符时 SQL 报
            //  「invalid escape character」→ 患者搜索 500。加密列无法前缀/片段匹配，
            //  按 US-PAT-001「按姓名、电话、拼音首字母筛选」口径改为完整手机号精确匹配。）
            var kw = keyword.Trim();
            var phoneHash = SensitiveDataHashHelper.ComputeHmacSha256Hex(kw);

            query = query.Where(p =>
                EF.Functions.Like(p.Name, $"{kw}%") ||
                (p.PinYinCode != null && EF.Functions.Like(p.PinYinCode, $"{kw}%")) ||
                (phoneHash != null && p.PhoneSearchHash == phoneHash));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Patient>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc/>
    /// <summary>手机号查重（P2 US-PAT-003/004）</summary>
    public async Task<bool> ExistsByPhoneAsync(string phoneNumber, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // R-6（2026-09-23 修复）：PhoneNumber 为非确定性加密列，`p.PhoneNumber == phoneNumber` 会被 EF
        // 翻译为「密文 = 本次加密后的密文」，因随机 nonce 永不相等 → 电话查重恒为 false（重复患者静默放行）。
        // 改走 HMAC 盲索引精确匹配。
        var phoneHash = SensitiveDataHashHelper.ComputeHmacSha256Hex(phoneNumber.Trim());
        if (phoneHash == null)
            return false;

        return await _context.Patients
            .AsNoTracking()
            .AnyAsync(p => !p.IsDeleted && p.PhoneSearchHash == phoneHash && (!excludeId.HasValue || p.Id != excludeId.Value), cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => ExistsAsync(e => e.Name == name, excludeId, cancellationToken);

    /// <inheritdoc/>
    public async Task<Patient?> GetExactByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Patients
            .FirstOrDefaultAsync(p => p.Name == name && !p.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Patient?> GetByIdNumberAsync(string idNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idNumber))
            return null;

        // R-6: IdNumber 经 AesGcmValueConverter 非确定性加密（随机 nonce），SQL 等值无法命中。
        // 按 HMAC 盲索引精确匹配；存量 IdCardHash 为空的记录仅回退内存比对（回填完成后可移除）。
        var hash = SensitiveDataHashHelper.ComputeHmacSha256Hex(idNumber);
        if (hash == null)
            return null;

        var byHash = await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => !p.IsDeleted && p.IdCardHash == hash, cancellationToken);
        if (byHash != null)
            return byHash;

        var legacyWithoutHash = await _context.Patients
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.IdCardHash == null)
            .ToListAsync(cancellationToken);
        return legacyWithoutHash.FirstOrDefault(p => p.IdNumber == idNumber);
    }

    /// <summary>
    /// 回填存量患者的 IdCardHash（R-6 迁移后一次性执行）。
    /// 逐条解密 IdNumber 再计算 HMAC；完成前 GetByIdNumberAsync 对 null-hash 行有回退扫描。
    /// </summary>
    public async Task<int> BackfillIdCardHashesAsync(CancellationToken cancellationToken = default)
    {
        var candidates = await _context.Patients
            .Where(p => p.IdCardHash == null && p.IdNumber != null)
            .ToListAsync(cancellationToken);

        var updated = 0;
        foreach (var patient in candidates)
        {
            var hash = SensitiveDataHashHelper.ComputeHmacSha256Hex(patient.IdNumber);
            if (hash == null)
                continue;
            patient.IdCardHash = hash;
            updated++;
        }

        if (updated > 0)
            await _context.SaveChangesAsync(cancellationToken);

        return updated;
    }
}
