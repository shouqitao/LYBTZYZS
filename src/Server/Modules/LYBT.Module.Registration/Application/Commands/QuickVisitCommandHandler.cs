using LYBT.Entities.Registrations;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Registration.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using RegistrationEntity = LYBT.Entities.Registrations.Registration;
using RegistrationSource = LYBT.Shared.Models.Enums.RegistrationSource;

namespace LYBT.Module.Registration.Application.Commands;

public class QuickVisitCommandHandler(
    IRegistrationRepository repository,
    IPatientCrossModuleService patientCrossModule,
    IMedicalCaseCrossModuleService medicalCaseCrossModule
) : IRequestHandler<QuickVisitCommand, Result<QuickVisitResultDto>>
{
    public async Task<Result<QuickVisitResultDto>> Handle(
        QuickVisitCommand request, CancellationToken cancellationToken)
    {
        var patientInfo = await patientCrossModule.GetPatientBasicInfoAsync(request.Input.PatientId, cancellationToken);
        if (patientInfo == null)
            return Result<QuickVisitResultDto>.Failure(ErrorCode.NotFound, "患者不存在");

        var maxQueueNumber = await repository.GetTodayMaxQueueNumberAsync(cancellationToken);
        var registration = new RegistrationEntity
        {
            Id = Guid.NewGuid(),
            PatientId = request.Input.PatientId,
            PatientName = patientInfo.Name,
            DoctorId = request.DoctorId,
            DoctorName = request.DoctorName,
            Source = RegistrationSource.Doctor,
            Status = RegistrationStatus.InProgress,
            QueueNumber = maxQueueNumber + 1,
            Remark = request.Input.Remark,
            CreatedBy = request.DoctorId
        };

        await repository.AddAsync(registration, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var medicalCaseId = await medicalCaseCrossModule.CreateQuickVisitMedicalCaseAsync(
            request.Input.PatientId, registration.Id, request.DoctorId, cancellationToken);

        if (medicalCaseId is null)
            return Result<QuickVisitResultDto>.Failure(ErrorCode.Unknown, "医案创建失败，挂号记录已回滚");

        return Result<QuickVisitResultDto>.Success(new QuickVisitResultDto
        {
            RegistrationId = registration.Id,
            MedicalCaseId = medicalCaseId.Value,
            PatientId = registration.PatientId,
            PatientName = registration.PatientName,
            DoctorId = request.DoctorId,
            DoctorName = request.DoctorName,
            CreatedAt = registration.CreatedAt
        });
    }
}


