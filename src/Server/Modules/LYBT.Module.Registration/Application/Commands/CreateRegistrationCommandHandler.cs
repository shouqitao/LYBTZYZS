using LYBT.Entities.Registrations;
using LYBT.Module.Registrations.Mappers;
using LYBT.Module.Registrations.Interfaces;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Common;
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

    public CreateRegistrationCommandHandler(
        IRegistrationRepository repository,
        RegistrationMapper mapper,
        INotificationService notificationService)
    {
        _repository = repository;
        _mapper = mapper;
        _notificationService = notificationService;
    }

    public async Task<Result<RegistrationDetailDto>> Handle(
        CreateRegistrationCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Input;

        var maxQueueNumber = await _repository.GetTodayMaxQueueNumberAsync(cancellationToken);

        var registration = new Registration
        {
            Id = Guid.NewGuid(),
            PatientId = dto.PatientId,
            PatientName = dto.PatientName,
            DoctorId = dto.DoctorId,
            DoctorName = dto.DoctorName,
            Source = dto.Source,
            Status = dto.Source == RegistrationSource.Doctor
                ? RegistrationStatus.InProgress
                : RegistrationStatus.Waiting,
            QueueNumber = maxQueueNumber + 1,
            RegistrationFee = dto.RegistrationFee,
            Remark = dto.Remark
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


