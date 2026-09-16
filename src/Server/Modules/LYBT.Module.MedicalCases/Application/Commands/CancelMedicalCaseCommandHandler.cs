using LYBT.Entities.MedicalCases;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

/// <summary>
/// 取消医案命令处理器 — 委托 <see cref="IMedicalCaseStateService.CancelAsync"/>。
/// </summary>
public sealed class CancelMedicalCaseCommandHandler
    : IRequestHandler<CancelMedicalCaseCommand, Result<MedicalCase>>
{
    private readonly IMedicalCaseStateService _stateService;

    public CancelMedicalCaseCommandHandler(IMedicalCaseStateService stateService)
    {
        _stateService = stateService;
    }

    public async Task<Result<MedicalCase>> Handle(
        CancelMedicalCaseCommand request, CancellationToken cancellationToken)
    {
        var entity = await _stateService.CancelAsync(
            request.Id, request.OperatorId, request.IsAdmin, request.Reason, cancellationToken);

        if (entity == null)
            return Result<MedicalCase>.Failure(ErrorCode.MedicalCaseNotFound, "医案不存在");

        return Result<MedicalCase>.Success(entity);
    }
}
