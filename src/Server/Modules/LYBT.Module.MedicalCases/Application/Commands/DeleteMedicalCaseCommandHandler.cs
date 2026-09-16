using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

/// <summary>
/// 删除医案命令处理器 — 委托 <see cref="IMedicalCaseCommandService.DeleteAsync"/>。
/// </summary>
public sealed class DeleteMedicalCaseCommandHandler
    : IRequestHandler<DeleteMedicalCaseCommand, Result<bool>>
{
    private readonly IMedicalCaseCommandService _commandService;

    public DeleteMedicalCaseCommandHandler(IMedicalCaseCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(
        DeleteMedicalCaseCommand request, CancellationToken cancellationToken)
    {
        var deleted = await _commandService.DeleteAsync(
            request.Id, request.OperatorId, request.IsAdmin, cancellationToken);

        if (!deleted)
            return Result<bool>.Failure(ErrorCode.MedicalCaseNotFound, "医案不存在");

        return Result<bool>.Success(true);
    }
}
