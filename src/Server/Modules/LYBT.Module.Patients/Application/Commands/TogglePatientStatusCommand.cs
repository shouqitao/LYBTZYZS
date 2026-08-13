using MediatR;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 切换患者状态（启用/禁用）命令（P1-9 2026-08-14: 加 OperatorRole——所有权检查移入 Handler）。
/// </summary>
public record TogglePatientStatusCommand(
    Guid Id,
    Guid CurrentUserId,
    UserRole OperatorRole
) : IRequest<Result<PatientDetailDto>>;


