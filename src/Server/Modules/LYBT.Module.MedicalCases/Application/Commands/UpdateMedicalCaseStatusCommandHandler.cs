using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

/// <summary>
/// 更新医案状态命令处理器 — 委托 <see cref="IMedicalCaseStateService.UpdateStatusAsync"/>，
/// 返回 DTO 与 Create/Update 对齐（R-14：双端契约一致，禁止实体泄漏）。
/// </summary>
public sealed class UpdateMedicalCaseStatusCommandHandler
    : IRequestHandler<UpdateMedicalCaseStatusCommand, Result<MedicalCaseDetailDto>>
{
    private readonly IMedicalCaseStateService _stateService;
    private readonly MedicalCaseMapper _mapper;

    public UpdateMedicalCaseStatusCommandHandler(IMedicalCaseStateService stateService, MedicalCaseMapper mapper)
    {
        _stateService = stateService;
        _mapper = mapper;
    }

    public async Task<Result<MedicalCaseDetailDto>> Handle(
        UpdateMedicalCaseStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await _stateService.UpdateStatusAsync(
            request.Id, request.Status, request.OperatorId, request.IsAdmin, cancellationToken);

        if (entity == null)
            return Result<MedicalCaseDetailDto>.Failure(ErrorCode.MedicalCaseNotFound, "医案不存在");

        return Result<MedicalCaseDetailDto>.Success(_mapper.MapToMedicalCaseDetailDto(entity));
    }
}
