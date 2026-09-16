using LYBT.Entities.MedicalCases;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

/// <summary>
/// 挂起医案命令处理器 — 委托 <see cref="IMedicalCaseStateService.SuspendAsync"/>。
/// </summary>
public sealed class SuspendMedicalCaseCommandHandler
    : IRequestHandler<SuspendMedicalCaseCommand, Result<MedicalCase>>
{
    private readonly IMedicalCaseStateService _stateService;

    public SuspendMedicalCaseCommandHandler(IMedicalCaseStateService stateService)
    {
        _stateService = stateService;
    }

    public async Task<Result<MedicalCase>> Handle(
        SuspendMedicalCaseCommand request, CancellationToken cancellationToken)
    {
        var entity = await _stateService.SuspendAsync(
            request.Id, request.Request, request.OperatorId, request.IsAdmin, cancellationToken);

        if (entity == null)
            return Result<MedicalCase>.Failure(ErrorCode.MedicalCaseNotFound, "医案不存在");

        return Result<MedicalCase>.Success(entity);
    }
}
