using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 批量删除患者命令。
/// </summary>
public record BatchDeletePatientsCommand(
    List<Guid> Ids,
    Guid CurrentUserId
) : IRequest<Result<BatchOperationResultDto>>;


