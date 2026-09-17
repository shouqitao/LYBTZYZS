using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

/// <summary>
/// 完成医案命令处理器 — 委托 <see cref="IMedicalCaseStateService.CompleteAsync"/>，
/// 返回 DTO 与 Create/Update 对齐（R-11：禁止实体泄漏）。
/// </summary>
public sealed class CompleteMedicalCaseCommandHandler
    : IRequestHandler<CompleteMedicalCaseCommand, Result<MedicalCaseDetailDto>>
{
    private readonly IMedicalCaseStateService _stateService;
    private readonly MedicalCaseMapper _mapper;

    public CompleteMedicalCaseCommandHandler(IMedicalCaseStateService stateService, MedicalCaseMapper mapper)
    {
        _stateService = stateService;
        _mapper = mapper;
    }

    public async Task<Result<MedicalCaseDetailDto>> Handle(
        CompleteMedicalCaseCommand request, CancellationToken cancellationToken)
    {
        var entity = await _stateService.CompleteAsync(
            request.MedicalCaseId,
            request.OperatorId,
            request.IsAdmin,
            request.SkipWorkflowValidation,
            cancellationToken);

        if (entity == null)
            return Result<MedicalCaseDetailDto>.Failure(ErrorCode.MedicalCaseNotFound, "医案不存在");

        return Result<MedicalCaseDetailDto>.Success(_mapper.MapToMedicalCaseDetailDto(entity));
    }
}
