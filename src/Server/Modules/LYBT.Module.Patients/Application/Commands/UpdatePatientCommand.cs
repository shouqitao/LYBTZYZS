using MediatR;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 更新患者命令（P1-9 2026-08-14: 加 OperatorRole——所有权检查移入 Handler）。
/// </summary>
public record UpdatePatientCommand(
    Guid Id,
    PatientInputDto Input,
    Guid CurrentUserId,
    UserRole OperatorRole
) : IRequest<Result<PatientDetailDto>>;
