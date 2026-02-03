using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.LocalData.Context;
using LYBT.Entities.Patients;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.DataSources;

/// <summary>
/// 本地患者数据源实现 - SQLite EF Core
/// OpenSpec: implement-local-mode
/// </summary>
public class LocalPatientDataSource : IPatientDataSource
{
    private readonly LocalDbContext _context;
    private readonly ILogger<LocalPatientDataSource> _logger;

    public LocalPatientDataSource(LocalDbContext context, ILogger<LocalPatientDataSource> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] Patient.GetById - Id={Id}", id);
        return await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<(List<Patient> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] Patient.GetPaged - Page={Page}, PageSize={PageSize}, Keyword={Keyword}",
            page, pageSize, keyword);

        var query = _context.Patients.AsNoTracking();

        // 关键词搜索
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(p =>
                p.Name.Contains(keyword) ||
                (p.PhoneNumber != null && p.PhoneNumber.Contains(keyword)) ||
                (p.IdNumber != null && p.IdNumber.Contains(keyword)) ||
                (p.PinYinCode != null && p.PinYinCode.Contains(keyword)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        _logger.LogDebug("[LocalDataSource] Patient.GetPaged - Total={Total}, Items={Count}", total, items.Count);
        return (items, total);
    }

    public async Task<Patient> CreateAsync(Patient entity, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Patient.Create - Name={Name}", entity.Name);

        entity.Id = Guid.NewGuid();
        _context.Patients.Add(entity);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalDataSource] Patient.Create completed - Id={Id}", entity.Id);
        return entity;
    }

    public async Task<Patient> UpdateAsync(Patient entity, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Patient.Update - Id={Id}", entity.Id);

        var existing = await _context.Patients.FindAsync([entity.Id], ct)
            ?? throw new InvalidOperationException($"患者不存在: {entity.Id}");

        // 更新属性
        _context.Entry(existing).CurrentValues.SetValues(entity);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalDataSource] Patient.Update completed - Id={Id}", entity.Id);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Patient.Delete - Id={Id}", id);

        var entity = await _context.Patients.FindAsync([id], ct);
        if (entity == null)
        {
            _logger.LogWarning("[LocalDataSource] Patient.Delete - NotFound: {Id}", id);
            return false;
        }

        // 软删除
        entity.IsDeleted = true;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalDataSource] Patient.Delete completed - Id={Id}", id);
        return true;
    }

    public async Task<List<Patient>> SearchAsync(string keyword, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] Patient.Search - Keyword={Keyword}", keyword);

        if (string.IsNullOrWhiteSpace(keyword))
            return new List<Patient>();

        return await _context.Patients
            .AsNoTracking()
            .Where(p =>
                p.Name.Contains(keyword) ||
                (p.PhoneNumber != null && p.PhoneNumber.Contains(keyword)) ||
                (p.IdNumber != null && p.IdNumber.Contains(keyword)) ||
                (p.PinYinCode != null && p.PinYinCode.Contains(keyword)))
            .OrderByDescending(p => p.CreatedAt)
            .Take(100)
            .ToListAsync(ct);
    }

    public async Task<Patient?> GetByIdNumberAsync(string idNumber, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] Patient.GetByIdNumber - IdNumber={IdNumber}",
            idNumber.Length > 6 ? idNumber[..6] + "****" : idNumber);

        if (string.IsNullOrWhiteSpace(idNumber))
            return null;

        return await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdNumber == idNumber, ct);
    }

    public async Task<Patient?> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Patient.Restore - Id={Id}", id);

        var entity = await _context.Patients
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (entity == null)
        {
            _logger.LogWarning("[LocalDataSource] Patient.Restore - NotFound: {Id}", id);
            return null;
        }

        entity.IsDeleted = false;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalDataSource] Patient.Restore completed - Id={Id}", id);
        return entity;
    }

    public async Task<BatchOperationResultDto> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Patient.BatchDelete - Count={Count}", ids.Count);

        var result = new BatchOperationResultDto
        {
            TotalCount = ids.Count,
            IsSuccess = true
        };

        foreach (var id in ids)
        {
            var entity = await _context.Patients.FindAsync([id], ct);
            if (entity != null)
            {
                entity.IsDeleted = true;
                result.SuccessCount++;
                result.SuccessfulIds.Add(id);
            }
            else
            {
                result.FailureCount++;
                result.FailedIds.Add(id);
                result.FailedItems.Add(new BatchOperationFailureItem
                {
                    Id = id,
                    Reason = "患者不存在"
                });
            }
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalDataSource] Patient.BatchDelete completed - Success={Success}, Failure={Failure}",
            result.SuccessCount, result.FailureCount);

        return result;
    }
}
