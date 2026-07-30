using MediatR;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.MedicalCases.Application.Commands;

public class CreateMedicalCaseCommandHandler(
    IMedicalCaseCommandService commandService,
    MedicalCaseMapper mapper
) : IRequestHandler<CreateMedicalCaseCommand, Result<MedicalCaseDetailDto>>
{
    public async Task<Result<MedicalCaseDetailDto>> Handle(
        CreateMedicalCaseCommand request, CancellationToken cancellationToken)
    {
        var medicalCase = await commandService.SaveAsync(
            request.Input,
            request.CurrentUserId,
            isAdmin: false,
            cancellationToken);

        if (medicalCase == null)
            return Result<MedicalCaseDetailDto>.Failure(Shared.Models.Primitives.ErrorCodes.ErrorCode.NotFound, "医案创建失败");

        var dto = mapper.MapToMedicalCaseDetailDto(medicalCase);
        return Result<MedicalCaseDetailDto>.Success(dto);
    }
}
