using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

/// <summary>
/// 批量删除医案命令（软删除，仅限已完成医案）。
/// </summary>
public sealed record BatchDeleteMedicalCaseCommand(
    List<Guid> Ids,
    Guid OperatorId,
    bool IsAdmin
) : IRequest<Result<BatchOperationResultDto>>;
