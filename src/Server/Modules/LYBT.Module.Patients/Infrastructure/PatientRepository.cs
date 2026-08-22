using LYBT.Entities.Patients;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Patients.Interfaces;
using LYBT.Infrastructure.Extensions;
using LYBT.Infrastructure.Repositories;
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
    public async Task<PagedResult<Patient>> GetPagedAsync(
        int page, int pageSize, string? keyword, CommonStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Patients
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(kw) ||
                (p.PinYinCode != null && p.PinYinCode.ToLower().Contains(kw)) ||
                (p.PhoneNumber != null && p.PhoneNumber.Contains(kw)));
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
        return await _context.Patients
            .AsNoTracking()
            .AnyAsync(p => !p.IsDeleted && p.PhoneNumber == phoneNumber && (!excludeId.HasValue || p.Id != excludeId.Value), cancellationToken);
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
        // IdNumber 经 AesGcmValueConverter 非确定性加密（随机 nonce），SQL 层等值查询因密文不同而无法命中，需内存解密后比对
        var candidates = await _context.Patients
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .ToListAsync(cancellationToken);
        return candidates.FirstOrDefault(p => p.IdNumber == idNumber);
    }
}
