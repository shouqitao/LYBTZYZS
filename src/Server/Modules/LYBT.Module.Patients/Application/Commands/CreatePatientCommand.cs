using MediatR;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 创建患者命令。
/// </summary>
public record CreatePatientCommand(
    PatientInputDto Input,
    Guid CurrentUserId
) : IRequest<Result<PatientDetailDto>>;


