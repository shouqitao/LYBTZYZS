using MediatR;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 切换患者状态（启用/禁用）命令。
/// </summary>
public record TogglePatientStatusCommand(
    Guid Id,
    Guid CurrentUserId
) : IRequest<Result<PatientDetailDto>>;


