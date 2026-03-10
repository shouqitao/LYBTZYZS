using LYBT.Infrastructure.Services;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Registration.Interfaces;
using LYBT.Module.Registration.Mapping;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using RegistrationEntity = LYBT.Entities.Registrations.Registration;
using GenericErrorCode = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Module.Registration.Services;

/// <summary>
/// 挂号服务实现
/// PRD: registration.md US-REG-001~006
/// Design: registration-module-design.md (D1~D6)
/// </summary>
public class RegistrationService : BaseService<RegistrationEntity>, IRegistrationService
{
    private readonly IRegistrationRepository _repository;
    private readonly IPatientCrossModuleService _patientCrossModule;
    private readonly RegistrationMapper _mapper = new();

    public RegistrationService(
        IRegistrationRepository repository,
        IPatientCrossModuleService patientCrossModule,
        ILogger<RegistrationService> logger)
        : base(logger)
    {
        _repository = repository;
        _patientCrossModule = patientCrossModule;
    }

    /// <summary>
    /// 创建挂号 (前台模式)
    /// US-REG-001: Source=Receptionist, Status=Waiting
    /// </summary>
    public async Task<Result<RegistrationDetailDto>> CreateAsync(RegistrationInputDto dto)
    {
        // AD-01 Fix: 检查患者是否被禁用
        var patient = await _patientCrossModule.GetPatientBasicInfoAsync(dto.PatientId);
        if (patient == null)
        {
            return Result<RegistrationDetailDto>.Failure(
                GenericErrorCode.RegistrationNotFound,
                "患者不存在");
        }
        if (patient.Status == CommonStatus.Disabled)
        {
            _logger.LogWarning("挂号创建被拒绝: 患者已禁用 PatientId={PatientId}", dto.PatientId);
            return Result<RegistrationDetailDto>.Failure(
                GenericErrorCode.RegistrationPatientDisabled,
                "该患者已被禁用，无法创建挂号");
        }

        // REG-70007: 检查患者是否已有等待中的挂号
        var hasDuplicate = await _repository.HasWaitingRegistrationAsync(dto.PatientId);
        if (hasDuplicate)
        {
            return Result<RegistrationDetailDto>.Failure(
                GenericErrorCode.RegistrationDuplicateWaiting,
                "该患者已有等待中的挂号记录");
        }

        var entity = _mapper.ToEntity(dto);
        entity.Status = dto.Source == RegistrationSource.Doctor
            ? RegistrationStatus.InProgress
            : RegistrationStatus.Waiting;

        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();

        _logger.LogInformation(
            "挂号创建成功: Id={RegistrationId}, Patient={PatientName}, Source={Source}, Status={Status}",
            entity.Id, entity.PatientName, entity.Source, entity.Status);

        var result = _mapper.ToDetailDto(entity);
        return Result<RegistrationDetailDto>.Success(result);
    }

    /// <summary>
    /// 取消挂号
    /// US-REG-004: 仅 Waiting 状态可取消，REG-BR-001 前置校验
    /// </summary>
    public async Task<Result> CancelAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null)
        {
            return Result.Failure(GenericErrorCode.RegistrationNotFound, "挂号记录不存在");
        }

        if (entity.Status != RegistrationStatus.Waiting)
        {
            return Result.Failure(
                GenericErrorCode.RegistrationInvalidStatusTransition,
                "仅等待中的挂号记录可取消");
        }

        // REG-BR-001: 有关联医案时检查医案状态
        // 注: Waiting 状态下 MedicalCaseId 应为 null，此处防御性校验
        if (entity.MedicalCaseId.HasValue)
        {
            return Result.Failure(
                GenericErrorCode.RegistrationCancelNotAllowed,
                "挂号记录有关联医案，不允许取消");
        }

        entity.Status = RegistrationStatus.Cancelled;
        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();

        _logger.LogInformation(
            "挂号已取消: Id={RegistrationId}, Patient={PatientName}",
            entity.Id, entity.PatientName);

        return Result.Success();
    }

    /// <summary>
    /// 获取挂号详情
    /// </summary>
    public async Task<Result<RegistrationDetailDto>> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null)
        {
            return Result<RegistrationDetailDto>.Failure(
                GenericErrorCode.RegistrationNotFound, "挂号记录不存在");
        }

        return Result<RegistrationDetailDto>.Success(_mapper.ToDetailDto(entity));
    }

    /// <summary>
    /// 获取等待队列
    /// US-REG-003: Waiting 状态，按挂号时间升序
    /// </summary>
    public async Task<Result<List<RegistrationListDto>>> GetWaitingQueueAsync(Guid? doctorId = null)
    {
        var entities = await _repository.GetWaitingQueueAsync(doctorId);
        var dtos = _mapper.ToListDtos(entities);
        return Result<List<RegistrationListDto>>.Success(dtos);
    }

    /// <summary>
    /// 分页查询挂号记录
    /// US-REG-007: 支持按日期范围、患者、医生过滤
    /// </summary>
    public async Task<Result<PagedResult<RegistrationListDto>>> GetPagedAsync(
        int page = 1, int pageSize = 20, string? keyword = null,
        DateTime? startDate = null, DateTime? endDate = null,
        Guid? patientId = null, Guid? doctorId = null)
    {
        var pagedResult = await _repository.GetPagedAsync(page, pageSize, keyword,
            startDate, endDate, patientId, doctorId);
        var items = _mapper.ToListDtos(pagedResult.Items.ToList());

        var dto = new PagedResult<RegistrationListDto>
        {
            Items = items,
            TotalCount = pagedResult.TotalCount,
            CurrentPage = pagedResult.CurrentPage,
            PageSize = pagedResult.PageSize
        };
        return Result<PagedResult<RegistrationListDto>>.Success(dto);
    }

    /// <summary>
    /// 接诊: 从队列选中患者
    /// US-REG-003: Waiting -> InProgress，创建关联医案
    /// </summary>
    public async Task<Result<Guid>> StartVisitAsync(Guid registrationId)
    {
        var entity = await _repository.GetByIdAsync(registrationId);
        if (entity is null)
        {
            return Result<Guid>.Failure(GenericErrorCode.RegistrationNotFound, "挂号记录不存在");
        }

        if (entity.Status != RegistrationStatus.Waiting)
        {
            return Result<Guid>.Failure(
                GenericErrorCode.RegistrationInvalidStatusTransition,
                "仅等待中的挂号记录可以接诊");
        }

        // 状态更新为 InProgress
        // 注: MedicalCase 创建和 MedicalCaseId 回填由 Controller 层协调
        entity.Status = RegistrationStatus.InProgress;
        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();

        _logger.LogInformation(
            "挂号接诊: Id={RegistrationId}, Patient={PatientName}, Status=InProgress",
            entity.Id, entity.PatientName);

        return Result<Guid>.Success(entity.Id);
    }

    /// <summary>
    /// 医案完成联动: Registration -> Completed
    /// US-REG-005: MedicalCase.Completed 时自动跟随
    /// </summary>
    public async Task<Result> CompleteByMedicalCaseAsync(Guid medicalCaseId)
    {
        var entity = await _repository.GetByMedicalCaseIdAsync(medicalCaseId);
        if (entity is null)
        {
            // 医案可能没有关联挂号 (历史数据兼容)
            _logger.LogWarning("医案 {MedicalCaseId} 无关联挂号记录", medicalCaseId);
            return Result.Success();
        }

        entity.Status = RegistrationStatus.Completed;
        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();

        _logger.LogInformation(
            "挂号跟随医案完成: Id={RegistrationId}, MedicalCase={MedicalCaseId}",
            entity.Id, medicalCaseId);

        return Result.Success();
    }

    /// <summary>
    /// 医案取消联动: 根据 Source 分流
    /// US-REG-006, D4: Receptionist -> 回退 Waiting; Doctor -> 自动 Cancelled
    /// </summary>
    public async Task<Result> HandleMedicalCaseCancelledAsync(Guid medicalCaseId)
    {
        var entity = await _repository.GetByMedicalCaseIdAsync(medicalCaseId);
        if (entity is null)
        {
            _logger.LogWarning("医案 {MedicalCaseId} 无关联挂号记录", medicalCaseId);
            return Result.Success();
        }

        if (entity.Source == RegistrationSource.Receptionist)
        {
            // 前台模式: 回退 Waiting，清空 MedicalCaseId
            entity.Status = RegistrationStatus.Waiting;
            entity.MedicalCaseId = null;
            _logger.LogInformation(
                "挂号回退等待: Id={RegistrationId}, Source=Receptionist",
                entity.Id);
        }
        else
        {
            // 医生模式: 自动取消
            entity.Status = RegistrationStatus.Cancelled;
            _logger.LogInformation(
                "挂号自动取消: Id={RegistrationId}, Source=Doctor",
                entity.Id);
        }

        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();

        return Result.Success();
    }
}
