using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 删除患者命令（软删除；P1-9 2026-08-14: 加 OperatorRole——所有权检查移入 Handler）。
/// </summary>
public record DeletePatientCommand(
    Guid Id,
    Guid CurrentUserId,
    UserRole OperatorRole
) : IRequest<Result>;


