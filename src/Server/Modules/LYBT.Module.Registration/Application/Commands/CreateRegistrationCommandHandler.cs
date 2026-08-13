using LYBT.Entities.Registrations;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Registrations.Mappers;
using LYBT.Module.Registrations.Interfaces;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using MediatR;

namespace LYBT.Module.Registrations.Application.Commands;

/// <summary>
/// 创建挂号处理器。
/// </summary>
public sealed class CreateRegistrationCommandHandler
    : IRequestHandler<CreateRegistrationCommand, Result<RegistrationDetailDto>>
{
    private readonly IRegistrationRepository _repository;
    private readonly RegistrationMapper _mapper;
    private readonly INotificationService _notificationService;
    private readonly IPatientCrossModuleService _patientCrossModule;
    private readonly IUserCrossModuleService _userCrossModule;

    public CreateRegistrationCommandHandler(
        IRegistrationRepository repository,
        RegistrationMapper mapper,
        INotificationService notificationService,
        IPatientCrossModuleService patientCrossModule,
        IUserCrossModuleService userCrossModule)
    {
        _repository = repository;
        _mapper = mapper;
        _notificationService = notificationService;
        _patientCrossModule = patientCrossModule;
        _userCrossModule = userCrossModule;
    }

    public async Task<Result<RegistrationDetailDto>> Handle(
        CreateRegistrationCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Input;

        // P1 (US-REG-001): 患者存在且启用校验（原仅 UI 层选择患者）
        var patient = await _patientCrossModule.GetPatientBasicInfoAsync(dto.PatientId, cancellationToken);
        if (patient == null)
            return Result<RegistrationDetailDto>.Failure(ErrorCode.NotFound, "患者不存在");
        if (patient.Status != CommonStatus.Enabled)
            return Result<RegistrationDetailDto>.Failure(ErrorCode.InvalidRequest, "患者已被禁用，无法挂号");

        // P1 (US-REG-BR-009): 挂号费自动带出——前台 Create 未显式传值时取医生挂号费
        if (dto.RegistrationFee == 0)
        {
            var doctor = await _userCrossModule.GetUserBasicInfoAsync(dto.DoctorId, cancellationToken);
            if (doctor != null)
            {
                dto.RegistrationFee = doctor.RegistrationFee;
            }
        }

        // T5-1 #9 (US-REG-BR-007): 患者当日已有待诊挂号则拒绝（同日重复挂号保护）
        var hasSameDayWaiting = await _repository.HasSameDayWaitingAsync(dto.PatientId, cancellationToken);
        if (hasSameDayWaiting)
            return Result<RegistrationDetailDto>.Failure(ErrorCode.InvalidRequest, "该患者今日已有待诊挂号，请勿重复挂号");

        var maxQueueNumber = await _repository.GetTodayMaxQueueNumberAsync(cancellationToken);

        var registration = new Registration
        {
            Id = Guid.NewGuid(),
            PatientId = dto.PatientId,
            PatientName = dto.PatientName,
            DoctorId = dto.DoctorId,
            DoctorName = dto.DoctorName,
            Source = dto.Source,
            // 2026-08-13（quickvisit-twostep）: 两步改造——POST 一律 Waiting（医生建号 Source=Doctor 也 Waiting）；
            // InProgress 由 start-visit 后置（断网残留 Waiting 可被待诊列表捕捉 → 自愈——产品决策）
            Status = RegistrationStatus.Waiting,
            QueueNumber = maxQueueNumber + 1,
            RegistrationFee = dto.RegistrationFee,
            Remark = dto.Remark,
            // 2026-08-13（startvisit-createdby-fix 同类排查）: 创建者必记（语义统一——医生建号/前台建号均记操作者）
            CreatedBy = request.OperatorId
        };

        await _repository.AddAsync(registration, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        // US-REG-008: 新挂号实时推送 — 仅 Waiting 状态会进入医生待诊列表
        var detailDto = _mapper.ToDetailDto(registration);
        if (registration.Status == RegistrationStatus.Waiting)
        {
            await _notificationService.NotifyNewRegistrationAsync(
                registration.DoctorId, detailDto, cancellationToken);
        }

        return Result<RegistrationDetailDto>.Success(detailDto);
    }
}


