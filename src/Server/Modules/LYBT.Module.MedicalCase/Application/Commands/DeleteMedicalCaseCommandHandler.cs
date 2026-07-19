using MediatR;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.MedicalCases.Application.Commands;

public class DeleteMedicalCaseCommandHandler(
    IMedicalCaseRepository repository,
    IRegistrationCrossModuleService registrationCrossModule
) : IRequestHandler<DeleteMedicalCaseCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        DeleteMedicalCaseCommand request, CancellationToken cancellationToken)
    {
        var medicalCase = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (medicalCase == null)
            return Result<bool>.Failure(ErrorCode.NotFound, "医案不存在");

        medicalCase.SoftDelete();

        await registrationCrossModule.HandleMedicalCaseCancelledAsync(request.Id, cancellationToken);
        await repository.UpdateAsync(medicalCase, cancellationToken);

        return Result<bool>.Success(true);
    }
}


