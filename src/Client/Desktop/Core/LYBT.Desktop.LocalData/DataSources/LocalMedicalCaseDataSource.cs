using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.LocalData.Context;
using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.DataSources;

/// <summary>
/// 本地医案数据源实现 - SQLite EF Core
/// OpenSpec: implement-local-mode
/// 医案是聚合根，管理 Consultation 和 Prescription
/// </summary>
public class LocalMedicalCaseDataSource : IMedicalCaseDataSource
{
    private readonly LocalDbContext _context;
    private readonly ILogger<LocalMedicalCaseDataSource> _logger;

    public LocalMedicalCaseDataSource(LocalDbContext context, ILogger<LocalMedicalCaseDataSource> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<MedicalCase?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] MedicalCase.GetById - Id={Id}", id);
        return await _context.MedicalCases
            .AsNoTracking()
            .FirstOrDefaultAsync(mc => mc.Id == id, ct);
    }

    public async Task<MedicalCase?> GetWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] MedicalCase.GetWithDetails - Id={Id}", id);
        return await _context.MedicalCases
            .AsNoTracking()
            .Include(mc => mc.Consultation)
            .Include(mc => mc.Prescription)
                .ThenInclude(p => p!.Items)
            .FirstOrDefaultAsync(mc => mc.Id == id, ct);
    }

    public async Task<(List<MedicalCase> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] MedicalCase.GetPaged - Page={Page}, Keyword={Keyword}", page, keyword);

        var query = _context.MedicalCases.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(mc =>
                mc.PatientName.Contains(keyword) ||
                mc.DoctorName.Contains(keyword) ||
                (mc.CaseNumber != null && mc.CaseNumber.Contains(keyword)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(mc => mc.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<(List<MedicalCase> Items, int Total)> QueryAsync(
        Guid? patientId = null,
        Guid? userId = null,
        MedicalCaseStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] MedicalCase.Query - PatientId={PatientId}, UserId={UserId}, Status={Status}",
            patientId, userId, status);

        var query = _context.MedicalCases.AsNoTracking();

        if (patientId.HasValue)
            query = query.Where(mc => mc.PatientId == patientId.Value);

        if (userId.HasValue)
            query = query.Where(mc => mc.UserId == userId.Value);

        if (status.HasValue)
            query = query.Where(mc => mc.CaseStatus == status.Value);

        if (startDate.HasValue)
            query = query.Where(mc => mc.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(mc => mc.CreatedAt <= endDate.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(mc => mc.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<List<MedicalCase>> GetByPatientIdAsync(Guid patientId, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] MedicalCase.GetByPatientId - PatientId={PatientId}", patientId);

        return await _context.MedicalCases
            .AsNoTracking()
            .Where(mc => mc.PatientId == patientId)
            .OrderByDescending(mc => mc.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<MedicalCase> CreateAsync(MedicalCase entity, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] MedicalCase.Create - PatientName={PatientName}", entity.PatientName);

        entity.Id = Guid.NewGuid();
        entity.CaseStatus = MedicalCaseStatus.Draft;

        // 生成医案编号
        entity.CaseNumber = GenerateCaseNumber();

        // 创建关联的 Consultation（共享主键）
        if (entity.Consultation != null)
        {
            entity.Consultation.Id = entity.Id;
        }

        // 创建关联的 Prescription
        if (entity.Prescription != null)
        {
            entity.Prescription.Id = Guid.NewGuid();
            entity.Prescription.MedicalCaseId = entity.Id;

            foreach (var item in entity.Prescription.Items)
            {
                item.Id = Guid.NewGuid();
                item.PrescriptionId = entity.Prescription.Id;
            }
        }

        _context.MedicalCases.Add(entity);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalDataSource] MedicalCase.Create completed - Id={Id}, CaseNumber={CaseNumber}",
            entity.Id, entity.CaseNumber);

        return entity;
    }

    public async Task<MedicalCase> UpdateAsync(MedicalCase entity, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] MedicalCase.Update - Id={Id}", entity.Id);

        var existing = await _context.MedicalCases
            .Include(mc => mc.Consultation)
            .Include(mc => mc.Prescription)
                .ThenInclude(p => p!.Items)
            .FirstOrDefaultAsync(mc => mc.Id == entity.Id, ct)
            ?? throw new InvalidOperationException($"医案不存在: {entity.Id}");

        // 更新基本属性
        _context.Entry(existing).CurrentValues.SetValues(entity);

        // 更新 Consultation
        if (entity.Consultation != null)
        {
            if (existing.Consultation == null)
            {
                entity.Consultation.Id = entity.Id;
                _context.Consultations.Add(entity.Consultation);
            }
            else
            {
                _context.Entry(existing.Consultation).CurrentValues.SetValues(entity.Consultation);
            }
        }

        // 更新 Prescription
        if (entity.Prescription != null)
        {
            if (existing.Prescription == null)
            {
                entity.Prescription.Id = Guid.NewGuid();
                entity.Prescription.MedicalCaseId = entity.Id;
                foreach (var item in entity.Prescription.Items)
                {
                    item.Id = Guid.NewGuid();
                    item.PrescriptionId = entity.Prescription.Id;
                }
                _context.Prescriptions.Add(entity.Prescription);
            }
            else
            {
                _context.Entry(existing.Prescription).CurrentValues.SetValues(entity.Prescription);

                // 更新处方项（删除旧的，添加新的）
                _context.PrescriptionItems.RemoveRange(existing.Prescription.Items);
                foreach (var item in entity.Prescription.Items)
                {
                    item.Id = Guid.NewGuid();
                    item.PrescriptionId = existing.Prescription.Id;
                    _context.PrescriptionItems.Add(item);
                }
            }
        }

        await _context.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<MedicalCase> SaveAsync(MedicalCase entity, CancellationToken ct = default)
    {
        // SaveAsync 统一入口：根据是否存在决定创建或更新
        var existing = await _context.MedicalCases
            .AsNoTracking()
            .FirstOrDefaultAsync(mc => mc.Id == entity.Id, ct);

        if (existing == null)
        {
            return await CreateAsync(entity, ct);
        }
        else
        {
            return await UpdateAsync(entity, ct);
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] MedicalCase.Delete - Id={Id}", id);

        var entity = await _context.MedicalCases.FindAsync([id], ct);
        if (entity == null)
            return false;

        entity.IsDeleted = true;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> CompleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] MedicalCase.Complete - Id={Id}", id);

        var entity = await _context.MedicalCases.FindAsync([id], ct);
        if (entity == null)
            return false;

        entity.CaseStatus = MedicalCaseStatus.Completed;
        entity.CompletedAt = DateTime.Now;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalDataSource] MedicalCase.Complete succeeded - Id={Id}", id);
        return true;
    }

    public async Task<bool> CancelAsync(Guid id, string? reason = null, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] MedicalCase.Cancel - Id={Id}, Reason={Reason}", id, reason);

        var entity = await _context.MedicalCases.FindAsync([id], ct);
        if (entity == null)
            return false;

        entity.CaseStatus = MedicalCaseStatus.Cancelled;
        entity.Remark = string.IsNullOrEmpty(entity.Remark)
            ? $"取消原因: {reason}"
            : $"{entity.Remark}\n取消原因: {reason}";

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalDataSource] MedicalCase.Cancel succeeded - Id={Id}", id);
        return true;
    }

    /// <summary>
    /// 生成医案编号（格式：MC + 年月日 + 序号）
    /// </summary>
    private string GenerateCaseNumber()
    {
        var today = DateTime.Today;
        var dateStr = today.ToString("yyyyMMdd");

        // 查询今天的医案数量
        var count = _context.MedicalCases
            .IgnoreQueryFilters()
            .Count(mc => mc.CaseNumber != null && mc.CaseNumber.StartsWith($"MC{dateStr}"));

        return $"MC{dateStr}{(count + 1):D3}";
    }
}
