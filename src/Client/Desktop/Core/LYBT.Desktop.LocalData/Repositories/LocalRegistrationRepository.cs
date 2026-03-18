using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.Repositories;

/// <summary>
/// 挂号仓储 - 本地模式实现 (SYNC-D02)
/// 通过 EF Core + LocalDbContext 直接访问 SQL Server LocalDB。
/// DI 工厂根据 IConnectionModeProvider 在本地模式下选择此实现。
/// </summary>
public sealed class LocalRegistrationRepository : IRegistrationRepository
{
    private readonly LocalDbContext _context;
    private readonly ILogger<LocalRegistrationRepository> _logger;
    private readonly LocalRegistrationMapper _mapper = new();

    public LocalRegistrationRepository(
        LocalDbContext context,
        ILogger<LocalRegistrationRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<RegistrationDetailDto> CreateAsync(RegistrationInputDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        try
        {
            _logger.LogInformation("[REPO:Local] Registration.Create - PatientName={PatientName}", input.PatientName);

            var entity = _mapper.ToEntity(input);
            entity.Id = Guid.NewGuid();
            entity.Status = RegistrationStatus.Waiting;
            entity.CreatedAt = DateTime.UtcNow;

            _context.Registrations.Add(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] Registration.Create completed - Id={Id}", entity.Id);
            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Registration.Create failed");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<RegistrationDetailDto?> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("[REPO:Local] Registration.GetById - Id={Id}", id);

            var entity = await _context.Registrations
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] Registration.GetById - NotFound: {Id}", id);
                return null;
            }

            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Registration.GetById failed - Id={Id}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PagedResult<RegistrationListDto>> GetPagedAsync(int page, int pageSize, string? keyword = null)
    {
        try
        {
            _logger.LogDebug("[REPO:Local] Registration.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword}",
                page, pageSize, keyword);

            var query = _context.Registrations.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(r =>
                    r.PatientName.Contains(keyword) ||
                    r.DoctorName.Contains(keyword));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<RegistrationListDto>
            {
                Items = items.Select(e => _mapper.ToListDto(e)).ToList(),
                TotalCount = total,
                CurrentPage = page
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Registration.GetPaged failed");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<RegistrationListDto>> GetWaitingQueueAsync(Guid? doctorId = null)
    {
        try
        {
            _logger.LogDebug("[REPO:Local] Registration.GetWaitingQueue - DoctorId={DoctorId}", doctorId);

            var query = _context.Registrations
                .AsNoTracking()
                .Where(r => r.Status == RegistrationStatus.Waiting);

            if (doctorId.HasValue)
            {
                query = query.Where(r => r.DoctorId == doctorId.Value);
            }

            var entities = await query
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            return entities.Select(e => _mapper.ToListDto(e)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Registration.GetWaitingQueue failed");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Guid?> StartVisitAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] Registration.StartVisit - Id={Id}", id);

            var entity = await _context.Registrations.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] Registration.StartVisit - NotFound: {Id}", id);
                return null;
            }

            if (entity.Status != RegistrationStatus.Waiting)
            {
                _logger.LogWarning("[REPO:Local] Registration.StartVisit - InvalidStatus: {Status}", entity.Status);
                return null;
            }

            entity.Status = RegistrationStatus.InProgress;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] Registration.StartVisit completed - Id={Id}", id);
            // 本地模式: MedicalCase 的创建由调用方处理，返回 Registration ID 作为占位
            return entity.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Registration.StartVisit failed - Id={Id}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CancelAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] Registration.Cancel - Id={Id}", id);

            var entity = await _context.Registrations.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] Registration.Cancel - NotFound: {Id}", id);
                return false;
            }

            if (entity.Status != RegistrationStatus.Waiting)
            {
                _logger.LogWarning("[REPO:Local] Registration.Cancel - InvalidStatus: {Status}", entity.Status);
                return false;
            }

            entity.Status = RegistrationStatus.Cancelled;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] Registration.Cancel completed - Id={Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] Registration.Cancel failed - Id={Id}", id);
            return false;
        }
    }
}
