using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.Repositories;

/// <summary>
/// 患者仓储 - 本地模式实现 (SYNC-D02)
/// 通过 EF Core + LocalDbContext 直接访问 SQL Server LocalDB。
/// DI 工厂根据 IConnectionModeProvider 在本地模式下选择此实现。
/// </summary>
public sealed class LocalPatientRepository : IPatientRepository
{
    private readonly LocalDbContext _context;
    private readonly ILogger<LocalPatientRepository> _logger;
    private readonly LocalPatientMapper _mapper = new();

    public LocalPatientRepository(
        LocalDbContext context,
        ILogger<LocalPatientRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 标准 CRUD 操作

    public async Task<PagedResult<PatientListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
    {
        try
        {
            _logger.LogDebug("[REPO:Local] Patient.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword}",
                page, pageSize, keyword);

            var query = _context.Patients.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p =>
                    p.Name.Contains(keyword) ||
                    (p.PhoneNumber != null && p.PhoneNumber.Contains(keyword)) ||
                    (p.IdNumber != null && p.IdNumber.Contains(keyword)) ||
                    (p.PinYinCode != null && p.PinYinCode.Contains(keyword)));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var listDtos = items.Select(e =>
            {
                var dto = _mapper.ToDetailDto(e);
                return new PatientListDto
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    Gender = dto.Gender,
                    Age = dto.Age,
                    PhoneNumber = dto.PhoneNumber,
                    Address = dto.Address,
                    LastVisitTime = dto.LastVisitTime,
                    VisitCount = dto.VisitCount,
                    PinYinCode = dto.PinYinCode,
                    Status = dto.Status,
                    CreatedAt = dto.CreatedAt
                };
            }).ToList();

            return new PagedResult<PatientListDto>
            {
                Items = listDtos,
                TotalCount = total,
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Patient.GetPaged failed");
            throw;
        }
    }

    public async Task<PatientDetailDto?> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("[REPO:Local] Patient.GetById - Id={Id}", id);

            var entity = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] Patient.GetById - NotFound: {Id}", id);
                return null;
            }

            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Patient.GetById failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<PatientDetailDto> CreateAsync(PatientInputDto patient)
    {
        ArgumentNullException.ThrowIfNull(patient);

        try
        {
            _logger.LogInformation("[REPO:Local] Patient.Create - Name={Name}", patient.Name);

            var entity = _mapper.ToEntity(patient);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;

            _context.Patients.Add(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] Patient.Create completed - Id={Id}", entity.Id);
            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Patient.Create failed");
            throw;
        }
    }

    public async Task<PatientDetailDto> UpdateAsync(PatientInputDto patient)
    {
        ArgumentNullException.ThrowIfNull(patient);
        var id = patient.Id ?? throw new ArgumentException("更新DTO必须包含有效的ID", nameof(patient));

        try
        {
            _logger.LogInformation("[REPO:Local] Patient.Update - Id={Id}", id);

            var existing = await _context.Patients.FindAsync(id)
                ?? throw new InvalidOperationException($"患者不存在: {id}");

            existing.Name = patient.Name;
            existing.PinYinCode = patient.PinYinCode;
            existing.Gender = patient.Gender;
            existing.BirthDate = patient.BirthDate;
            existing.IdNumber = patient.IdNumber;
            existing.PhoneNumber = patient.PhoneNumber;
            existing.Address = patient.Address;
            existing.AllergyHistory = patient.AllergyHistory;
            existing.MedicalHistory = patient.MedicalHistory;
            existing.MaritalStatus = patient.MaritalStatus;
            existing.IdType = patient.IdType;
            existing.BloodType = patient.BloodType;
            existing.EmergencyContactName = patient.EmergencyContactName;
            existing.EmergencyContactPhone = patient.EmergencyContactPhone;
            existing.EmergencyContactRelation = patient.EmergencyContactRelation;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] Patient.Update completed - Id={Id}", id);
            return _mapper.ToDetailDto(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Patient.Update failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] Patient.Delete - Id={Id}", id);

            var entity = await _context.Patients.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] Patient.Delete - NotFound: {Id}", id);
                return false;
            }

            entity.IsDeleted = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] Patient.Delete completed - Id={Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Patient.Delete failed - Id={Id}", id);
            return false;
        }
    }

    public async Task<List<PatientListDto>> SearchAsync(string keyword)
    {
        try
        {
            _logger.LogDebug("[REPO:Local] Patient.Search - Keyword={Keyword}", keyword);

            if (string.IsNullOrWhiteSpace(keyword))
                return [];

            var entities = await _context.Patients
                .AsNoTracking()
                .Where(p =>
                    p.Name.Contains(keyword) ||
                    (p.PhoneNumber != null && p.PhoneNumber.Contains(keyword)) ||
                    (p.IdNumber != null && p.IdNumber.Contains(keyword)) ||
                    (p.PinYinCode != null && p.PinYinCode.Contains(keyword)))
                .OrderByDescending(p => p.CreatedAt)
                .Take(100)
                .ToListAsync();

            return entities.Select(e =>
            {
                var dto = _mapper.ToDetailDto(e);
                return new PatientListDto
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    Gender = dto.Gender,
                    Age = dto.Age,
                    PhoneNumber = dto.PhoneNumber,
                    Address = dto.Address,
                    LastVisitTime = dto.LastVisitTime,
                    VisitCount = dto.VisitCount,
                    PinYinCode = dto.PinYinCode,
                    Status = dto.Status,
                    CreatedAt = dto.CreatedAt
                };
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Patient.Search failed");
            throw;
        }
    }

    #endregion

    #region 身份证号查询

    public async Task<PatientDetailDto?> GetByIdNumberAsync(string idNumber)
    {
        if (string.IsNullOrWhiteSpace(idNumber))
            return null;

        try
        {
            _logger.LogInformation("[REPO:Local] Patient.GetByIdNumber");

            var entity = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdNumber == idNumber);

            return entity == null ? null : _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Patient.GetByIdNumber failed");
            return null;
        }
    }

    #endregion

    #region 批量导入/导出 (本地模式: 导入支持, 导出不支持)

    public Task<PatientBatchImportResultDto?> BatchImportAsync(PatientBatchImportInputDto request)
    {
        _logger.LogWarning("[REPO:Local] Patient.BatchImport - 本地模式不支持批量导入");
        return Task.FromResult<PatientBatchImportResultDto?>(null);
    }

    public Task<byte[]?> ExportTemplateAsync()
    {
        _logger.LogWarning("[REPO:Local] Patient.ExportTemplate - 本地模式不支持导出模板");
        return Task.FromResult<byte[]?>(null);
    }

    public Task<byte[]?> ExportPatientsAsync(string? keyword = null)
    {
        _logger.LogWarning("[REPO:Local] Patient.ExportPatients - 本地模式不支持导出");
        return Task.FromResult<byte[]?>(null);
    }

    #endregion

    #region 恢复和批量操作

    public async Task<PatientDetailDto?> RestoreAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] Patient.Restore - Id={Id}", id);

            var entity = await _context.Patients
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] Patient.Restore - NotFound: {Id}", id);
                return null;
            }

            entity.IsDeleted = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] Patient.Restore completed - Id={Id}", id);
            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Patient.Restore failed - Id={Id}", id);
            return null;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] Patient.BatchDelete - Count={Count}", ids.Count);

            var result = new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                IsSuccess = true
            };

            foreach (var id in ids)
            {
                var entity = await _context.Patients.FindAsync(id);
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

            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] Patient.BatchDelete completed - Success={Success}, Failure={Failure}",
                result.SuccessCount, result.FailureCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Patient.BatchDelete failed");
            return null;
        }
    }

    #endregion
}
