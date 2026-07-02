using MediatR;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 更新患者命令。
/// </summary>
public record UpdatePatientCommand(
    Guid Id,
    PatientInputDto Input,
    Guid CurrentUserId
) : IRequest<Result<PatientDetailDto>>;


