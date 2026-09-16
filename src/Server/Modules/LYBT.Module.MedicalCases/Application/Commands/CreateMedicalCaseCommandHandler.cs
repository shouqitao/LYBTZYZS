using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

/// <summary>
/// 创建医案命令处理器 — 委托 <see cref="IMedicalCaseCommandService.SaveWithDetailAsync"/>。
/// </summary>
public sealed class CreateMedicalCaseCommandHandler
    : IRequestHandler<CreateMedicalCaseCommand, Result<MedicalCaseDetailDto>>
{
    private readonly IMedicalCaseCommandService _commandService;

    public CreateMedicalCaseCommandHandler(IMedicalCaseCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<MedicalCaseDetailDto>> Handle(
        CreateMedicalCaseCommand request, CancellationToken cancellationToken)
    {
        // 创建语义：强制 Id=null，避免请求体误带 Id 走更新分支
        request.Input.Id = null;
        return await _commandService.SaveWithDetailAsync(
            request.Input, request.CurrentUserId, request.IsAdmin, cancellationToken);
    }
}
