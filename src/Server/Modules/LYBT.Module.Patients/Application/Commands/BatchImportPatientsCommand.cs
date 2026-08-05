using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 批量导入患者命令（Excel/JSON 共用导入路径）。
/// </summary>
public record BatchImportPatientsCommand(
    List<PatientInputDto> Patients,
    DuplicateStrategy Strategy,
    Guid CurrentUserId
) : IRequest<Result<PatientBatchImportResultDto>>;
