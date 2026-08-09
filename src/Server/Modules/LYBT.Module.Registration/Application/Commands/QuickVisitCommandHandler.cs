using LYBT.Entities.Registrations;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Registrations.Interfaces;
using MediatR;

namespace LYBT.Module.Registrations.Application.Commands;

public class QuickVisitCommandHandler(
    IRegistrationRepository repository,
    IPatientCrossModuleService patientCrossModule,
    IUserService userCrossModule,
    IMedicalCaseCrossModuleService medicalCaseCrossModule
) : IRequestHandler<QuickVisitCommand, Result<QuickVisitResultDto>>
{
    public async Task<Result<QuickVisitResultDto>> Handle(
        QuickVisitCommand request, CancellationToken cancellationToken)
    {
        var patientInfo = await patientCrossModule.GetPatientBasicInfoAsync(request.Input.PatientId, cancellationToken);
        if (patientInfo == null)
            return Result<QuickVisitResultDto>.Failure(ErrorCode.NotFound, ErrorMessages.Get(ErrorCode.PatientNotFound));

        // REG-BR-009: 挂号费从医生自动带出
        var doctorInfo = await userCrossModule.GetUserBasicInfoAsync(request.DoctorId, cancellationToken);

        var maxQueueNumber = await repository.GetTodayMaxQueueNumberAsync(cancellationToken);
        var registration = new Registration
        {
            Id = Guid.NewGuid(),
            PatientId = request.Input.PatientId,
            PatientName = patientInfo.Name,
            DoctorId = request.DoctorId,
            DoctorName = request.DoctorName,
            Source = RegistrationSource.Doctor,
            Status = RegistrationStatus.InProgress,
            QueueNumber = maxQueueNumber + 1,
            RegistrationFee = doctorInfo?.RegistrationFee ?? 0m,
            Remark = request.Input.Remark,
            CreatedBy = request.DoctorId
        };

        // 原子事务：Registration(InProgress) + MedicalCase(Active) 同生共死
        await using var transaction = await repository.BeginTransactionAsync(cancellationToken);
        try
        {
            await repository.AddAsync(registration, cancellationToken);

            var medicalCaseId = await medicalCaseCrossModule.CreateMedicalCaseForRegistrationAsync(
                request.Input.PatientId, registration.Id, request.DoctorId, cancellationToken);

            if (medicalCaseId is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<QuickVisitResultDto>.Failure(ErrorCode.Unknown, "医案创建失败，挂号记录已回滚");
            }

            await transaction.CommitAsync(cancellationToken);

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
        catch (BusinessException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<QuickVisitResultDto>.Failure(ex.TypedErrorCode ?? ErrorCode.Unknown, ex.Message);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
