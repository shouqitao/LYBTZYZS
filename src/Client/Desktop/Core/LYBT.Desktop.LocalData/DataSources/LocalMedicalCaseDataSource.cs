using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.Mappers;
using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Validators.BusinessRules;
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
    private readonly LocalMedicalCaseMapper _mapper = new();

    public LocalMedicalCaseDataSource(LocalDbContext context, ILogger<LocalMedicalCaseDataSource> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<MedicalCaseDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] MedicalCase.GetById - Id={Id}", id);
        var entity = await _context.MedicalCases
            .AsNoTracking()
            .FirstOrDefaultAsync(mc => mc.Id == id, ct);

        return entity == null ? null : _mapper.ToDetailDto(entity);
    }

    public async Task<MedicalCaseDetailDto?> GetWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] MedicalCase.GetWithDetails - Id={Id}", id);
        var entity = await _context.MedicalCases
            .AsNoTracking()
            .Include(mc => mc.Consultation)
            .Include(mc => mc.Prescription)
                .ThenInclude(p => p!.Items)
            .FirstOrDefaultAsync(mc => mc.Id == id, ct);

        return entity == null ? null : _mapper.ToDetailDto(entity);
    }

    public async Task<(List<MedicalCaseDetailDto> Items, int Total)> GetPagedAsync(
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

        return (items.Select(e => _mapper.ToDetailDto(e)).ToList(), total);
    }

    public async Task<(List<MedicalCaseDetailDto> Items, int Total)> QueryAsync(
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

        return (items.Select(e => _mapper.ToDetailDto(e)).ToList(), total);
    }

    public async Task<List<MedicalCaseDetailDto>> GetByPatientIdAsync(Guid patientId, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] MedicalCase.GetByPatientId - PatientId={PatientId}", patientId);

        var entities = await _context.MedicalCases
            .AsNoTracking()
            .Where(mc => mc.PatientId == patientId)
            .OrderByDescending(mc => mc.CreatedAt)
            .ToListAsync(ct);

        return entities.Select(e => _mapper.ToDetailDto(e)).ToList();
    }

    public async Task<MedicalCaseDetailDto> CreateAsync(MedicalCaseInputDto input, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] MedicalCase.Create - PatientId={PatientId}", input.PatientId);

        var entity = _mapper.ToEntity(input);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.Now;
        entity.CaseStatus = MedicalCaseStatus.Suspended;

        // 补充患者/医生名称（InputDto 不携带，需从数据库查找）
        var patient = await _context.Patients.FindAsync(new object[] { input.PatientId }, ct);
        entity.PatientName = patient?.Name ?? string.Empty;
        var user = await _context.Users.FindAsync(new object[] { input.UserId }, ct);
        entity.DoctorName = user?.RealName ?? string.Empty;

        // 业务规则: 患者同时只能有一个 Active/Draft 医案
        var existingStatuses = await _context.MedicalCases
            .Where(mc => mc.PatientId == input.PatientId && !mc.IsDeleted)
            .Select(mc => mc.CaseStatus)
            .ToListAsync(ct);

        if (!MedicalCaseBusinessRules.CanCreateNewCase(existingStatuses))
            throw new InvalidOperationException("该患者已有进行中或暂存的医案，不能创建新医案");

        // 生成医案编号
        entity.CaseNumber = GenerateCaseNumber();

        // 创建关联的 Consultation（共享主键）
        if (input.Consultation != null)
        {
            entity.Consultation = new Consultation
            {
                Id = entity.Id,
                PresentIllness = input.Consultation.PresentIllness,
                TongueDiagnosis = input.Consultation.TongueDiagnosis,
                PulseDiagnosis = input.Consultation.PulseDiagnosis,
                TcmDiagnosis = input.Consultation.TcmDiagnosis,
                CreatedAt = DateTime.Now
            };
        }

        // 创建关联的 Prescription
        if (input.Prescription != null)
        {
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = entity.Id,
                DosageCount = input.Prescription.DosageCount,
                Usage = input.Prescription.Usage,
                Advice = input.Prescription.Advice,
                ReferencedFormulas = input.Prescription.ReferencedFormulas,
                Discount = input.Prescription.Discount,
                Remark = input.Prescription.Remark,
                CreatedAt = DateTime.Now
            };

            foreach (var itemInput in input.Prescription.Items)
            {
                prescription.Items.Add(new PrescriptionItem
                {
                    Id = Guid.NewGuid(),
                    PrescriptionId = prescription.Id,
                    HerbId = itemInput.HerbId,
                    HerbName = itemInput.HerbName ?? string.Empty,
                    Dosage = itemInput.Dosage,
                    Unit = itemInput.Unit,
                    DecocteMethod = itemInput.DecocteMethod,
                    UnitPrice = itemInput.UnitPrice,
                    Usage = itemInput.Usage,
                    Remark = itemInput.Remark
                });
            }

            entity.Prescription = prescription;
        }

        _context.MedicalCases.Add(entity);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalDataSource] MedicalCase.Create completed - Id={Id}, CaseNumber={CaseNumber}",
            entity.Id, entity.CaseNumber);

        return _mapper.ToDetailDto(entity);
    }

    public async Task<MedicalCaseDetailDto> UpdateAsync(MedicalCaseInputDto input, CancellationToken ct = default)
    {
        var id = input.Id ?? throw new InvalidOperationException("更新医案时必须提供ID");
        _logger.LogInformation("[LocalDataSource] MedicalCase.Update - Id={Id}", id);

        var existing = await _context.MedicalCases
            .Include(mc => mc.Consultation)
            .Include(mc => mc.Prescription)
                .ThenInclude(p => p!.Items)
            .FirstOrDefaultAsync(mc => mc.Id == id, ct)
            ?? throw new InvalidOperationException($"医案不存在: {id}");

        // 更新基本属性
        existing.Remark = input.Remark;
        existing.UpdatedAt = DateTime.Now;

        // 更新 Consultation
        if (input.Consultation != null)
        {
            if (existing.Consultation == null)
            {
                existing.Consultation = new Consultation
                {
                    Id = id,
                    PresentIllness = input.Consultation.PresentIllness,
                    TongueDiagnosis = input.Consultation.TongueDiagnosis,
                    PulseDiagnosis = input.Consultation.PulseDiagnosis,
                    TcmDiagnosis = input.Consultation.TcmDiagnosis,
                    CreatedAt = DateTime.Now
                };
                _context.Consultations.Add(existing.Consultation);
            }
            else
            {
                existing.Consultation.PresentIllness = input.Consultation.PresentIllness;
                existing.Consultation.TongueDiagnosis = input.Consultation.TongueDiagnosis;
                existing.Consultation.PulseDiagnosis = input.Consultation.PulseDiagnosis;
                existing.Consultation.TcmDiagnosis = input.Consultation.TcmDiagnosis;
                existing.Consultation.UpdatedAt = DateTime.Now;
            }
        }

        // 更新 Prescription
        if (input.Prescription != null)
        {
            if (existing.Prescription == null)
            {
                var prescription = new Prescription
                {
                    Id = Guid.NewGuid(),
                    MedicalCaseId = id,
                    DosageCount = input.Prescription.DosageCount,
                    Usage = input.Prescription.Usage,
                    Advice = input.Prescription.Advice,
                    ReferencedFormulas = input.Prescription.ReferencedFormulas,
                    Discount = input.Prescription.Discount,
                    Remark = input.Prescription.Remark,
                    CreatedAt = DateTime.Now
                };

                foreach (var itemInput in input.Prescription.Items)
                {
                    prescription.Items.Add(new PrescriptionItem
                    {
                        Id = Guid.NewGuid(),
                        PrescriptionId = prescription.Id,
                        HerbId = itemInput.HerbId,
                        HerbName = itemInput.HerbName ?? string.Empty,
                        Dosage = itemInput.Dosage,
                        Unit = itemInput.Unit,
                        DecocteMethod = itemInput.DecocteMethod,
                        UnitPrice = itemInput.UnitPrice,
                        Usage = itemInput.Usage,
                        Remark = itemInput.Remark
                    });
                }

                _context.Prescriptions.Add(prescription);
            }
            else
            {
                existing.Prescription.DosageCount = input.Prescription.DosageCount;
                existing.Prescription.Usage = input.Prescription.Usage;
                existing.Prescription.Advice = input.Prescription.Advice;
                existing.Prescription.ReferencedFormulas = input.Prescription.ReferencedFormulas;
                existing.Prescription.Discount = input.Prescription.Discount;
                existing.Prescription.Remark = input.Prescription.Remark;
                existing.Prescription.UpdatedAt = DateTime.Now;

                // 更新处方项（删除旧的，添加新的）
                _context.PrescriptionItems.RemoveRange(existing.Prescription.Items);
                foreach (var itemInput in input.Prescription.Items)
                {
                    _context.PrescriptionItems.Add(new PrescriptionItem
                    {
                        Id = Guid.NewGuid(),
                        PrescriptionId = existing.Prescription.Id,
                        HerbId = itemInput.HerbId,
                        HerbName = itemInput.HerbName ?? string.Empty,
                        Dosage = itemInput.Dosage,
                        Unit = itemInput.Unit,
                        DecocteMethod = itemInput.DecocteMethod,
                        UnitPrice = itemInput.UnitPrice,
                        Usage = itemInput.Usage,
                        Remark = itemInput.Remark
                    });
                }
            }
        }

        await _context.SaveChangesAsync(ct);
        return _mapper.ToDetailDto(existing);
    }

    public async Task<MedicalCaseDetailDto> SaveAsync(MedicalCaseInputDto input, CancellationToken ct = default)
    {
        // SaveAsync 统一入口：根据是否存在决定创建或更新
        if (input.Id.HasValue)
        {
            var existing = await _context.MedicalCases
                .AsNoTracking()
                .FirstOrDefaultAsync(mc => mc.Id == input.Id.Value, ct);

            if (existing != null)
            {
                return await UpdateAsync(input, ct);
            }
        }

        return await CreateAsync(input, ct);
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

        // 取消操作统一为软删除
        entity.IsDeleted = true;
        entity.Remark = string.IsNullOrEmpty(entity.Remark)
            ? $"取消原因: {reason}"
            : $"{entity.Remark}\n取消原因: {reason}";

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalDataSource] MedicalCase.Cancel succeeded - Id={Id}", id);
        return true;
    }

    /// <summary>
    /// 添加打印日志记录
    /// T4-S5-03: 本地模式打印日志存储
    /// </summary>
    public async Task<bool> AddPrintLogAsync(
        Guid medicalCaseId,
        bool isSuccess,
        LYBT.Shared.Models.Enums.PrintType printType = LYBT.Shared.Models.Enums.PrintType.Prescription,
        string? printerName = null,
        string? errorMessage = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] MedicalCase.AddPrintLog - MedicalCaseId={MedicalCaseId}, IsSuccess={IsSuccess}",
            medicalCaseId, isSuccess);

        var entity = await _context.MedicalCases.FindAsync([medicalCaseId], ct);
        if (entity == null)
        {
            _logger.LogWarning("[LocalDataSource] MedicalCase.AddPrintLog -> NotFound - MedicalCaseId={MedicalCaseId}",
                medicalCaseId);
            return false;
        }

        var printLog = new MedicalCasePrintLog
        {
            Id = Guid.NewGuid(),
            MedicalCaseId = medicalCaseId,
            PrintType = printType,
            PrintVersion = entity.PrintVersion + (isSuccess ? 1 : 0),
            PrintedAt = DateTime.Now,
            PrinterName = printerName,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage
        };

        _context.MedicalCasePrintLogs.Add(printLog);

        // 成功时同步更新打印管理字段
        if (isSuccess)
        {
            entity.IsPrinted = true;
            entity.PrintCount++;
            entity.LastPrintedAt = DateTime.Now;
            entity.PrintVersion++;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalDataSource] MedicalCase.AddPrintLog completed - MedicalCaseId={MedicalCaseId}, IsSuccess={IsSuccess}",
            medicalCaseId, isSuccess);
        return true;
    }

    /// <summary>
    /// CODE-40: 批量删除医案
    /// </summary>
    public async Task<BatchOperationResultDto> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] MedicalCase.BatchDelete - Count={Count}", ids.Count);

        var result = new BatchOperationResultDto
        {
            TotalCount = ids.Count,
            IsSuccess = true
        };

        foreach (var id in ids)
        {
            var entity = await _context.MedicalCases.FindAsync([id], ct);
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
                    Reason = "医案不存在"
                });
            }
        }

        await _context.SaveChangesAsync(ct);

        result.IsSuccess = result.FailureCount == 0;
        _logger.LogInformation("[LocalDataSource] MedicalCase.BatchDelete completed - Success={Success}, Failure={Failure}",
            result.SuccessCount, result.FailureCount);

        return result;
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
