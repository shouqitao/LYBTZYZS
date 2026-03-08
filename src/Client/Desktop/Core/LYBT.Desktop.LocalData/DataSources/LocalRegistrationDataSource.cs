using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.LocalData.Context;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.LocalData.DataSources;

/// <summary>
/// 本地挂号数据源实现 - SQLite EF Core
/// PRD: registration.md
/// </summary>
public class LocalRegistrationDataSource : IRegistrationDataSource
{
    private readonly LocalDbContext _context;
    private readonly ILogger<LocalRegistrationDataSource> _logger;
    private readonly LocalRegistrationMapper _mapper = new();

    public LocalRegistrationDataSource(LocalDbContext context, ILogger<LocalRegistrationDataSource> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RegistrationDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] Registration.GetById - Id={Id}", id);
        var entity = await _context.Registrations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        return entity == null ? null : _mapper.ToDetailDto(entity);
    }

    public async Task<(List<RegistrationDetailDto> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] Registration.GetPaged - Page={Page}, Keyword={Keyword}", page, keyword);

        var query = _context.Registrations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(r =>
                r.PatientName.Contains(keyword) ||
                r.DoctorName.Contains(keyword));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items.Select(e => _mapper.ToDetailDto(e)).ToList(), total);
    }

    public async Task<RegistrationDetailDto> CreateAsync(RegistrationInputDto input, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Registration.Create - PatientName={PatientName}", input.PatientName);

        var entity = _mapper.ToEntity(input);
        entity.Id = Guid.NewGuid();
        entity.Status = RegistrationStatus.Waiting;
        entity.CreatedAt = DateTime.Now;

        _context.Registrations.Add(entity);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalDataSource] Registration.Create completed - Id={Id}", entity.Id);
        return _mapper.ToDetailDto(entity);
    }

    public Task<RegistrationDetailDto> UpdateAsync(RegistrationInputDto input, CancellationToken ct = default)
    {
        // 挂号不支持更新操作，只有状态流转 (StartVisit/Cancel)
        throw new NotSupportedException("挂号记录不支持更新操作，请使用 StartVisitAsync 或 CancelAsync");
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        // 挂号不支持删除，只支持取消
        throw new NotSupportedException("挂号记录不支持删除操作，请使用 CancelAsync");
    }

    public async Task<List<RegistrationListDto>> GetWaitingQueueAsync(Guid? doctorId = null, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] Registration.GetWaitingQueue - DoctorId={DoctorId}", doctorId);

        var query = _context.Registrations
            .AsNoTracking()
            .Where(r => r.Status == RegistrationStatus.Waiting);

        if (doctorId.HasValue)
        {
            query = query.Where(r => r.DoctorId == doctorId.Value);
        }

        var entities = await query
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(ct);

        return entities.Select(e => _mapper.ToListDto(e)).ToList();
    }

    public async Task<Guid?> StartVisitAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Registration.StartVisit - Id={Id}", id);

        var entity = await _context.Registrations.FindAsync([id], ct);
        if (entity == null)
        {
            _logger.LogWarning("[LocalDataSource] Registration.StartVisit - NotFound: {Id}", id);
            return null;
        }

        if (entity.Status != RegistrationStatus.Waiting)
        {
            _logger.LogWarning("[LocalDataSource] Registration.StartVisit - InvalidStatus: {Status}", entity.Status);
            return null;
        }

        entity.Status = RegistrationStatus.InProgress;
        entity.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalDataSource] Registration.StartVisit completed - Id={Id}", id);
        // 本地模式: MedicalCase 的创建由调用方处理，这里只返回 Registration ID 作为占位
        return entity.Id;
    }

    public async Task<bool> CancelAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Registration.Cancel - Id={Id}", id);

        var entity = await _context.Registrations.FindAsync([id], ct);
        if (entity == null)
        {
            _logger.LogWarning("[LocalDataSource] Registration.Cancel - NotFound: {Id}", id);
            return false;
        }

        if (entity.Status != RegistrationStatus.Waiting)
        {
            _logger.LogWarning("[LocalDataSource] Registration.Cancel - InvalidStatus: {Status}", entity.Status);
            return false;
        }

        entity.Status = RegistrationStatus.Cancelled;
        entity.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalDataSource] Registration.Cancel completed - Id={Id}", id);
        return true;
    }
}

/// <summary>
/// Registration Entity <-> DTO 映射器
/// </summary>
[Mapper]
internal partial class LocalRegistrationMapper
{
    [MapperIgnoreSource(nameof(Entities.Registrations.Registration.UpdatedBy))]
    [MapperIgnoreSource(nameof(Entities.Registrations.Registration.RowVersion))]
    [MapperIgnoreSource(nameof(Entities.Registrations.Registration.IsDeleted))]
    public partial RegistrationDetailDto ToDetailDto(Entities.Registrations.Registration entity);

    [MapperIgnoreTarget(nameof(Entities.Registrations.Registration.Id))]
    [MapperIgnoreTarget(nameof(Entities.Registrations.Registration.RowVersion))]
    [MapperIgnoreTarget(nameof(Entities.Registrations.Registration.IsDeleted))]
    [MapperIgnoreTarget(nameof(Entities.Registrations.Registration.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Entities.Registrations.Registration.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Entities.Registrations.Registration.CreatedAt))]
    [MapperIgnoreTarget(nameof(Entities.Registrations.Registration.CreatedBy))]
    [MapperIgnoreTarget(nameof(Entities.Registrations.Registration.MedicalCaseId))]
    [MapperIgnoreTarget(nameof(Entities.Registrations.Registration.Status))]
    public partial Entities.Registrations.Registration ToEntity(RegistrationInputDto dto);

    [MapperIgnoreSource(nameof(Entities.Registrations.Registration.Remark))]
    [MapperIgnoreSource(nameof(Entities.Registrations.Registration.UpdatedAt))]
    [MapperIgnoreSource(nameof(Entities.Registrations.Registration.CreatedBy))]
    [MapperIgnoreSource(nameof(Entities.Registrations.Registration.UpdatedBy))]
    [MapperIgnoreSource(nameof(Entities.Registrations.Registration.RowVersion))]
    [MapperIgnoreSource(nameof(Entities.Registrations.Registration.IsDeleted))]
    public partial RegistrationListDto ToListDto(Entities.Registrations.Registration entity);
}
