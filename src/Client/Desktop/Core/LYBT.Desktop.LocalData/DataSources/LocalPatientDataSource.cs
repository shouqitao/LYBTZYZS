using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
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
    private readonly LocalPatientMapper _mapper = new();

    public LocalPatientDataSource(LocalDbContext context, ILogger<LocalPatientDataSource> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PatientDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] Patient.GetById - Id={Id}", id);
        var entity = await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return entity == null ? null : _mapper.ToDetailDto(entity);
    }

    public async Task<(List<PatientDetailDto> Items, int Total)> GetPagedAsync(
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
        return (items.Select(e => _mapper.ToDetailDto(e)).ToList(), total);
    }

    public async Task<PatientDetailDto> CreateAsync(PatientInputDto input, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Patient.Create - Name={Name}", input.Name);

        var entity = _mapper.ToEntity(input);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.Now;

        _context.Patients.Add(entity);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalDataSource] Patient.Create completed - Id={Id}", entity.Id);
        return _mapper.ToDetailDto(entity);
    }

    public async Task<PatientDetailDto> UpdateAsync(PatientInputDto input, CancellationToken ct = default)
    {
        var id = input.Id ?? throw new InvalidOperationException("更新患者时必须提供ID");
        _logger.LogInformation("[LocalDataSource] Patient.Update - Id={Id}", id);

        var existing = await _context.Patients.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"患者不存在: {id}");

        // 更新可变字段
        existing.Name = input.Name;
        existing.PinYinCode = input.PinYinCode;
        existing.Gender = input.Gender;
        existing.BirthDate = input.BirthDate;
        existing.IdNumber = input.IdNumber;
        existing.PhoneNumber = input.PhoneNumber;
        existing.Address = input.Address;
        existing.AllergyHistory = input.AllergyHistory;
        existing.MedicalHistory = input.MedicalHistory;
        existing.MaritalStatus = input.MaritalStatus;
        existing.IdType = input.IdType;
        existing.BloodType = input.BloodType;
        existing.EmergencyContactName = input.EmergencyContactName;
        existing.EmergencyContactPhone = input.EmergencyContactPhone;
        existing.EmergencyContactRelation = input.EmergencyContactRelation;
        existing.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalDataSource] Patient.Update completed - Id={Id}", existing.Id);
        return _mapper.ToDetailDto(existing);
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

    public async Task<List<PatientDetailDto>> SearchAsync(string keyword, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] Patient.Search - Keyword={Keyword}", keyword);

        if (string.IsNullOrWhiteSpace(keyword))
            return new List<PatientDetailDto>();

        var entities = await _context.Patients
            .AsNoTracking()
            .Where(p =>
                p.Name.Contains(keyword) ||
                (p.PhoneNumber != null && p.PhoneNumber.Contains(keyword)) ||
                (p.IdNumber != null && p.IdNumber.Contains(keyword)) ||
                (p.PinYinCode != null && p.PinYinCode.Contains(keyword)))
            .OrderByDescending(p => p.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        return entities.Select(e => _mapper.ToDetailDto(e)).ToList();
    }

    public async Task<PatientDetailDto?> GetByIdNumberAsync(string idNumber, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] Patient.GetByIdNumber - IdNumber={IdNumber}",
            idNumber.Length > 6 ? idNumber[..6] + "****" : idNumber);

        if (string.IsNullOrWhiteSpace(idNumber))
            return null;

        var entity = await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdNumber == idNumber, ct);

        return entity == null ? null : _mapper.ToDetailDto(entity);
    }

    public async Task<PatientDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default)
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
        return _mapper.ToDetailDto(entity);
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
