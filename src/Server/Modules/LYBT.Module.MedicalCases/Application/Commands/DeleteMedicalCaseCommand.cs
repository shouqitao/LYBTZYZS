using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

/// <summary>
/// 删除医案命令（软删除）。
/// </summary>
public sealed record DeleteMedicalCaseCommand(
    Guid Id,
    Guid OperatorId,
    bool IsAdmin
) : IRequest<Result<bool>>;
