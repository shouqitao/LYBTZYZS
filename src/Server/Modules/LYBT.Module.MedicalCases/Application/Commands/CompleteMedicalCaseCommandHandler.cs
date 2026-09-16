using LYBT.Entities.MedicalCases;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

/// <summary>
/// 完成医案命令处理器 — 委托 <see cref="IMedicalCaseStateService.CompleteAsync"/>。
/// </summary>
public sealed class CompleteMedicalCaseCommandHandler
    : IRequestHandler<CompleteMedicalCaseCommand, Result<MedicalCase>>
{
    private readonly IMedicalCaseStateService _stateService;

    public CompleteMedicalCaseCommandHandler(IMedicalCaseStateService stateService)
    {
        _stateService = stateService;
    }

    public async Task<Result<MedicalCase>> Handle(
        CompleteMedicalCaseCommand request, CancellationToken cancellationToken)
    {
        var entity = await _stateService.CompleteAsync(
            request.MedicalCaseId,
            request.OperatorId,
            request.IsAdmin,
            request.SkipWorkflowValidation,
            cancellationToken);

        if (entity == null)
            return Result<MedicalCase>.Failure(ErrorCode.MedicalCaseNotFound, "医案不存在");

        return Result<MedicalCase>.Success(entity);
    }
}
