using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.Mappers;
using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Validators.BusinessRules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.Repositories;

/// <summary>
/// 医案仓储 - 本地模式实现 (SYNC-D02)
/// 通过 EF Core + LocalDbContext 直接访问本地数据库。
/// 医案是聚合根，管理 Consultation 和 Prescription 子实体。
/// DI 工厂根据 IConnectionModeProvider 在本地模式下选择此实现。
/// </summary>
public sealed class LocalMedicalCaseRepository : IMedicalCaseRepository
{
    private readonly LocalDbContext _context;
    private readonly ILogger<LocalMedicalCaseRepository> _logger;
    private readonly LocalMedicalCaseMapper _mapper = new();

    public LocalMedicalCaseRepository(
        LocalDbContext context,
        ILogger<LocalMedicalCaseRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 标准 CRUD 操作

    public async Task<PagedResult<MedicalCaseListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
    {
        try
        {
            _logger.LogDebug("[REPO:Local] MedicalCase.GetPaged - Page={Page} PageSize={PageSize} Keyword={Keyword}",
                page, pageSize, keyword);

            var query = _context.MedicalCases.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(mc =>
                    mc.PatientName.Contains(keyword) ||
                    mc.DoctorName.Contains(keyword) ||
                    (mc.CaseNumber != null && mc.CaseNumber.Contains(keyword)));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(mc => mc.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(mc => mc.Consultation)
                .Include(mc => mc.Prescription)
                .ToListAsync();

            var listDtos = items.Select(e => new MedicalCaseListDto
            {
                Id = e.Id,
                PatientId = e.PatientId,
                PatientName = e.PatientName,
                PatientGender = default,
                PatientAge = null,
                UserId = e.UserId,
                DoctorName = e.DoctorName,
                CaseStatus = e.CaseStatus,
                HasConsultation = e.Consultation != null,
                HasPrescription = e.Prescription != null,
                CreatedAt = e.CreatedAt,
                CompletedAt = e.CompletedAt
            }).ToList();

            return new PagedResult<MedicalCaseListDto>
            {
                Items = listDtos,
                TotalCount = total,
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] MedicalCase.GetPaged failed");
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto?> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("[REPO:Local] MedicalCase.GetById - Id={Id}", id);

            var entity = await _context.MedicalCases
                .AsNoTracking()
                .Include(mc => mc.Consultation)
                .Include(mc => mc.Prescription)
                    .ThenInclude(p => p!.Items)
                .FirstOrDefaultAsync(mc => mc.Id == id);

            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] MedicalCase.GetById - NotFound: {Id}", id);
                return null;
            }

            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] MedicalCase.GetById failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto> CreateAsync(MedicalCaseInputDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            _logger.LogInformation("[REPO:Local] MedicalCase.Create - PatientId={PatientId}", dto.PatientId);

            var entity = _mapper.ToEntity(dto);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.CaseStatus = MedicalCaseStatus.Suspended;

            // 补充患者/医生名称 (InputDto 不携带，需从数据库查找)
            var patient = await _context.Patients.FindAsync(dto.PatientId);
            entity.PatientName = patient?.Name ?? string.Empty;
            var user = await _context.Users.FindAsync(dto.UserId);
            entity.DoctorName = user?.RealName ?? string.Empty;

            // 业务规则: 患者同时只能有一个 Active/Draft 医案
            var existingStatuses = await _context.MedicalCases
                .Where(mc => mc.PatientId == dto.PatientId && !mc.IsDeleted)
                .Select(mc => mc.CaseStatus)
                .ToListAsync();

            if (!MedicalCaseBusinessRules.CanCreateNewCase(existingStatuses))
                throw new InvalidOperationException("该患者已有进行中或暂存的医案，不能创建新医案");

            // 生成医案编号
            entity.CaseNumber = GenerateCaseNumber();

            // 创建关联的 Consultation (共享主键)
            if (dto.Consultation != null)
            {
                entity.Consultation = new Consultation
                {
                    Id = entity.Id,
                    PresentIllness = dto.Consultation.PresentIllness,
                    TongueDiagnosis = dto.Consultation.TongueDiagnosis,
                    PulseDiagnosis = dto.Consultation.PulseDiagnosis,
                    TcmDiagnosis = dto.Consultation.TcmDiagnosis,
                    CreatedAt = DateTime.UtcNow
                };
            }

            // 创建关联的 Prescription
            if (dto.Prescription != null)
            {
                var prescription = new Prescription
                {
                    Id = Guid.NewGuid(),
                    MedicalCaseId = entity.Id,
                    DosageCount = dto.Prescription.DosageCount,
                    Usage = dto.Prescription.Usage,
                    Advice = dto.Prescription.Advice,
                    ReferencedFormulas = dto.Prescription.ReferencedFormulas,
                    Discount = dto.Prescription.Discount,
                    Remark = dto.Prescription.Remark,
                    CreatedAt = DateTime.UtcNow
                };

                foreach (var itemInput in dto.Prescription.Items)
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
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] MedicalCase.Create completed - Id={Id}, CaseNumber={CaseNumber}",
                entity.Id, entity.CaseNumber);
            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] MedicalCase.Create failed");
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto> UpdateAsync(MedicalCaseInputDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var id = dto.Id ?? throw new ArgumentException("更新DTO必须包含有效的ID", nameof(dto));

        try
        {
            _logger.LogInformation("[REPO:Local] MedicalCase.Update - Id={Id}", id);

            var existing = await _context.MedicalCases
                .Include(mc => mc.Consultation)
                .Include(mc => mc.Prescription)
                    .ThenInclude(p => p!.Items)
                .FirstOrDefaultAsync(mc => mc.Id == id)
                ?? throw new InvalidOperationException($"医案不存在: {id}");

            // 更新基本属性
            existing.Remark = dto.Remark;
            existing.UpdatedAt = DateTime.UtcNow;

            // 更新 Consultation
            if (dto.Consultation != null)
            {
                if (existing.Consultation == null)
                {
                    existing.Consultation = new Consultation
                    {
                        Id = id,
                        PresentIllness = dto.Consultation.PresentIllness,
                        TongueDiagnosis = dto.Consultation.TongueDiagnosis,
                        PulseDiagnosis = dto.Consultation.PulseDiagnosis,
                        TcmDiagnosis = dto.Consultation.TcmDiagnosis,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Consultations.Add(existing.Consultation);
                }
                else
                {
                    existing.Consultation.PresentIllness = dto.Consultation.PresentIllness;
                    existing.Consultation.TongueDiagnosis = dto.Consultation.TongueDiagnosis;
                    existing.Consultation.PulseDiagnosis = dto.Consultation.PulseDiagnosis;
                    existing.Consultation.TcmDiagnosis = dto.Consultation.TcmDiagnosis;
                    existing.Consultation.UpdatedAt = DateTime.UtcNow;
                }
            }

            // 更新 Prescription
            if (dto.Prescription != null)
            {
                if (existing.Prescription == null)
                {
                    var prescription = new Prescription
                    {
                        Id = Guid.NewGuid(),
                        MedicalCaseId = id,
                        DosageCount = dto.Prescription.DosageCount,
                        Usage = dto.Prescription.Usage,
                        Advice = dto.Prescription.Advice,
                        ReferencedFormulas = dto.Prescription.ReferencedFormulas,
                        Discount = dto.Prescription.Discount,
                        Remark = dto.Prescription.Remark,
                        CreatedAt = DateTime.UtcNow
                    };

                    foreach (var itemInput in dto.Prescription.Items)
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
                    existing.Prescription.DosageCount = dto.Prescription.DosageCount;
                    existing.Prescription.Usage = dto.Prescription.Usage;
                    existing.Prescription.Advice = dto.Prescription.Advice;
                    existing.Prescription.ReferencedFormulas = dto.Prescription.ReferencedFormulas;
                    existing.Prescription.Discount = dto.Prescription.Discount;
                    existing.Prescription.Remark = dto.Prescription.Remark;
                    existing.Prescription.UpdatedAt = DateTime.UtcNow;

                    // 更新处方项 (删除旧的，添加新的)
                    _context.PrescriptionItems.RemoveRange(existing.Prescription.Items);
                    foreach (var itemInput in dto.Prescription.Items)
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

            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] MedicalCase.Update completed - Id={Id}", id);
            return _mapper.ToDetailDto(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] MedicalCase.Update failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] MedicalCase.Delete - Id={Id}", id);

            var entity = await _context.MedicalCases.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] MedicalCase.Delete - NotFound: {Id}", id);
                return false;
            }

            entity.IsDeleted = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] MedicalCase.Delete completed - Id={Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] MedicalCase.Delete failed - Id={Id}", id);
            return false;
        }
    }

    #endregion

    #region 高级查询

    public async Task<PagedResult<MedicalCaseDetailDto>> SearchAsync(
        string? patientName = null,
        string? diagnosisKeyword = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            _logger.LogDebug("[REPO:Local] MedicalCase.Search - PatientName={PatientName}", patientName ?? "无");

            var query = _context.MedicalCases
                .AsNoTracking()
                .Include(mc => mc.Consultation)
                .Include(mc => mc.Prescription)
                    .ThenInclude(p => p!.Items)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(patientName))
                query = query.Where(mc => mc.PatientName.Contains(patientName));

            if (!string.IsNullOrWhiteSpace(diagnosisKeyword))
                query = query.Where(mc =>
                    mc.Consultation != null &&
                    mc.Consultation.TcmDiagnosis != null &&
                    mc.Consultation.TcmDiagnosis.Contains(diagnosisKeyword));

            if (startDate.HasValue)
                query = query.Where(mc => mc.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(mc => mc.CreatedAt <= endDate.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(mc => mc.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<MedicalCaseDetailDto>
            {
                Items = items.Select(e => _mapper.ToDetailDto(e)).ToList(),
                TotalCount = total,
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] MedicalCase.Search failed");
            throw;
        }
    }

    public async Task<PagedResult<MedicalCaseListDto>> QueryAsync(MedicalCaseQueryDto query)
    {
        try
        {
            _logger.LogDebug("[REPO:Local] MedicalCase.Query - QueryType={QueryType}", query.QueryType);

            var dbQuery = _context.MedicalCases
                .AsNoTracking()
                .Include(mc => mc.Consultation)
                .Include(mc => mc.Prescription)
                .AsQueryable();

            if (query.PatientId.HasValue)
                dbQuery = dbQuery.Where(mc => mc.PatientId == query.PatientId.Value);

            if (query.DoctorId.HasValue)
                dbQuery = dbQuery.Where(mc => mc.UserId == query.DoctorId.Value);

            if (!string.IsNullOrWhiteSpace(query.Keyword))
                dbQuery = dbQuery.Where(mc =>
                    mc.PatientName.Contains(query.Keyword) ||
                    mc.DoctorName.Contains(query.Keyword));

            var total = await dbQuery.CountAsync();
            var items = await dbQuery
                .OrderByDescending(mc => mc.CreatedAt)
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var listDtos = items.Select(e => new MedicalCaseListDto
            {
                Id = e.Id,
                PatientId = e.PatientId,
                PatientName = e.PatientName,
                PatientGender = default,
                PatientAge = null,
                UserId = e.UserId,
                DoctorName = e.DoctorName,
                CaseStatus = e.CaseStatus,
                HasConsultation = e.Consultation != null,
                HasPrescription = e.Prescription != null,
                CreatedAt = e.CreatedAt,
                CompletedAt = e.CompletedAt
            }).ToList();

            return new PagedResult<MedicalCaseListDto>
            {
                Items = listDtos,
                TotalCount = total,
                CurrentPage = query.PageIndex,
                PageSize = query.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] MedicalCase.Query failed");
            throw;
        }
    }

    #endregion

    #region 生命周期操作

    public async Task<MedicalCaseDetailDto?> CloseCaseAsync(Guid medicalCaseId)
    {
        if (medicalCaseId == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));

        try
        {
            _logger.LogInformation("[REPO:Local] MedicalCase.CloseCase - Id={Id}", medicalCaseId);

            var entity = await _context.MedicalCases
                .Include(mc => mc.Consultation)
                .Include(mc => mc.Prescription)
                    .ThenInclude(p => p!.Items)
                .FirstOrDefaultAsync(mc => mc.Id == medicalCaseId);

            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] MedicalCase.CloseCase - NotFound: {Id}", medicalCaseId);
                return null;
            }

            entity.CaseStatus = MedicalCaseStatus.Completed;
            entity.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] MedicalCase.CloseCase completed - Id={Id}", medicalCaseId);
            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] MedicalCase.CloseCase failed - Id={Id}", medicalCaseId);
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto?> CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequestDto? request)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(id));

        try
        {
            _logger.LogInformation("[REPO:Local] MedicalCase.Cancel - Id={Id}, Reason={Reason}",
                id, request?.Reason ?? "无");

            var entity = await _context.MedicalCases
                .Include(mc => mc.Consultation)
                .Include(mc => mc.Prescription)
                    .ThenInclude(p => p!.Items)
                .FirstOrDefaultAsync(mc => mc.Id == id);

            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] MedicalCase.Cancel - NotFound: {Id}", id);
                return null;
            }

            // 取消操作: 软删除 + 记录原因
            entity.IsDeleted = true;
            entity.Remark = string.IsNullOrEmpty(entity.Remark)
                ? $"取消原因: {request?.Reason}"
                : $"{entity.Remark}\n取消原因: {request?.Reason}";
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] MedicalCase.Cancel completed - Id={Id}", id);
            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] MedicalCase.Cancel failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto?> SuspendAsync(Guid id, ConsultationInputDto? request)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(id));

        try
        {
            _logger.LogInformation("[REPO:Local] MedicalCase.Suspend - Id={Id}", id);

            var entity = await _context.MedicalCases
                .Include(mc => mc.Consultation)
                .Include(mc => mc.Prescription)
                    .ThenInclude(p => p!.Items)
                .FirstOrDefaultAsync(mc => mc.Id == id);

            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] MedicalCase.Suspend - NotFound: {Id}", id);
                return null;
            }

            entity.CaseStatus = MedicalCaseStatus.Suspended;
            entity.UpdatedAt = DateTime.UtcNow;

            // 挂起时可选保存诊断数据
            if (request != null && entity.Consultation != null)
            {
                entity.Consultation.PresentIllness = request.PresentIllness;
                entity.Consultation.TongueDiagnosis = request.TongueDiagnosis;
                entity.Consultation.PulseDiagnosis = request.PulseDiagnosis;
                entity.Consultation.TcmDiagnosis = request.TcmDiagnosis;
                entity.Consultation.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] MedicalCase.Suspend completed - Id={Id}", id);
            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] MedicalCase.Suspend failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto?> UpdateStatusAsync(Guid id, MedicalCaseStatusInputDto request)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(id));
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            _logger.LogInformation("[REPO:Local] MedicalCase.UpdateStatus - Id={Id}, Status={Status}",
                id, request.Status);

            var entity = await _context.MedicalCases
                .Include(mc => mc.Consultation)
                .Include(mc => mc.Prescription)
                    .ThenInclude(p => p!.Items)
                .FirstOrDefaultAsync(mc => mc.Id == id);

            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] MedicalCase.UpdateStatus - NotFound: {Id}", id);
                return null;
            }

            entity.CaseStatus = request.Status;
            entity.UpdatedAt = DateTime.UtcNow;

            if (request.Status == MedicalCaseStatus.Completed)
                entity.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] MedicalCase.UpdateStatus completed - Id={Id}", id);
            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] MedicalCase.UpdateStatus failed - Id={Id}", id);
            throw;
        }
    }

    #endregion

    #region 权限与聚合保存

    public Task<MedicalCasePermissionDto?> GetPermissionsAsync(Guid medicalCaseId)
    {
        if (medicalCaseId == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));

        // 本地模式: 返回完全权限
        _logger.LogDebug("[REPO:Local] MedicalCase.GetPermissions - 本地模式返回完全权限");
        return Task.FromResult<MedicalCasePermissionDto?>(new MedicalCasePermissionDto
        {
            CanEdit = true,
            CanDelete = true,
            RequiresEditReason = false
        });
    }

    public async Task<MedicalCaseDetailDto> SaveAsync(Guid medicalCaseId, MedicalCaseInputDto dto)
    {
        if (medicalCaseId == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            _logger.LogInformation("[REPO:Local] MedicalCase.Save - Id={Id}", medicalCaseId);

            // Upsert 模式: 存在则更新，不存在则创建
            dto.Id = medicalCaseId;

            var existing = await _context.MedicalCases
                .AsNoTracking()
                .FirstOrDefaultAsync(mc => mc.Id == medicalCaseId);

            if (existing != null)
                return await UpdateAsync(dto);

            return await CreateAsync(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] MedicalCase.Save failed - Id={Id}", medicalCaseId);
            throw;
        }
    }

    #endregion

    #region 处方标志与打印

    public async Task<MedicalCaseDetailDto?> SetPrescriptionFlagAsync(Guid id, SetPrescriptionFlagRequest request)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(id));
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            _logger.LogInformation("[REPO:Local] MedicalCase.SetPrescriptionFlag - Id={Id}, NeedsPrescription={NeedsPrescription}",
                id, request.NeedsPrescription);

            var entity = await _context.MedicalCases
                .Include(mc => mc.Consultation)
                .Include(mc => mc.Prescription)
                    .ThenInclude(p => p!.Items)
                .FirstOrDefaultAsync(mc => mc.Id == id);

            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] MedicalCase.SetPrescriptionFlag - NotFound: {Id}", id);
                return null;
            }

            entity.NeedsPrescription = request.NeedsPrescription;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] MedicalCase.SetPrescriptionFlag completed - Id={Id}", id);
            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] MedicalCase.SetPrescriptionFlag failed - Id={Id}", id);
            throw;
        }
    }

    public async Task<MedicalCaseDetailDto?> RecordPrintCompletedAsync(Guid medicalCaseId, PrintCompletedRequest request)
    {
        if (medicalCaseId == Guid.Empty)
            throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));

        try
        {
            _logger.LogInformation("[REPO:Local] MedicalCase.RecordPrintCompleted - Id={Id}", medicalCaseId);

            var entity = await _context.MedicalCases
                .Include(mc => mc.Consultation)
                .Include(mc => mc.Prescription)
                    .ThenInclude(p => p!.Items)
                .FirstOrDefaultAsync(mc => mc.Id == medicalCaseId);

            if (entity == null)
            {
                _logger.LogWarning("[REPO:Local] MedicalCase.RecordPrintCompleted - NotFound: {Id}", medicalCaseId);
                return null;
            }

            entity.IsPrinted = true;
            entity.PrintCount++;
            entity.LastPrintedAt = DateTime.UtcNow;
            entity.PrintVersion++;
            await _context.SaveChangesAsync();

            _logger.LogInformation("[REPO:Local] MedicalCase.RecordPrintCompleted completed - Id={Id}", medicalCaseId);
            return _mapper.ToDetailDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] MedicalCase.RecordPrintCompleted failed - Id={Id}", medicalCaseId);
            throw;
        }
    }

    #endregion

    #region 批量操作

    public async Task<List<MedicalCaseDetailDto>> GetBatchDetailsAsync(List<Guid> ids)
    {
        if (ids == null || ids.Count == 0)
            return [];

        if (ids.Count > 50)
            throw new ArgumentException("单次最多查询50个医案", nameof(ids));

        try
        {
            _logger.LogInformation("[REPO:Local] MedicalCase.GetBatchDetails - Count={Count}", ids.Count);

            var entities = await _context.MedicalCases
                .AsNoTracking()
                .Include(mc => mc.Consultation)
                .Include(mc => mc.Prescription)
                    .ThenInclude(p => p!.Items)
                .Where(mc => ids.Contains(mc.Id))
                .ToListAsync();

            var results = entities.Select(e => _mapper.ToDetailDto(e)).ToList();

            _logger.LogInformation("[REPO:Local] MedicalCase.GetBatchDetails completed - Count={Count}", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] MedicalCase.GetBatchDetails failed");
            throw;
        }
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
    {
        try
        {
            _logger.LogInformation("[REPO:Local] MedicalCase.BatchDelete - Count={Count}", ids.Count);

            var result = new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                IsSuccess = true
            };

            foreach (var id in ids)
            {
                var entity = await _context.MedicalCases.FindAsync(id);
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

            await _context.SaveChangesAsync();

            result.IsSuccess = result.FailureCount == 0;
            _logger.LogInformation("[REPO:Local] MedicalCase.BatchDelete completed - Success={Success}, Failure={Failure}",
                result.SuccessCount, result.FailureCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPO:Local] MedicalCase.BatchDelete failed");
            return null;
        }
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 生成医案编号 (格式: MC + 年月日 + 3位序号)
    /// </summary>
    private string GenerateCaseNumber()
    {
        var today = DateTime.Today;
        var dateStr = today.ToString("yyyyMMdd");

        // 查询今天的医案数量 (含软删除，确保编号不重复)
        var count = _context.MedicalCases
            .IgnoreQueryFilters()
            .Count(mc => mc.CaseNumber != null && mc.CaseNumber.StartsWith($"MC{dateStr}"));

        return $"MC{dateStr}{(count + 1):D3}";
    }

    #endregion
}
