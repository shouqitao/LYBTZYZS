using MediatR;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 删除患者命令（软删除）。
/// </summary>
public record DeletePatientCommand(
    Guid Id,
    Guid CurrentUserId
) : IRequest<Result>;


