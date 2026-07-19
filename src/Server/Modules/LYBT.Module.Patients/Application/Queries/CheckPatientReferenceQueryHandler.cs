using MediatR;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Patients.Interfaces;

namespace LYBT.Module.Patients.Application.Queries;

/// <summary>
/// 检查患者引用关系查询处理器。
/// </summary>
public class CheckPatientReferenceQueryHandler(
    IPatientRepository patientRepository,
    IMedicalCaseCrossModuleService medicalCaseCrossModuleService) : IRequestHandler<CheckPatientReferenceQuery, Result<PatientReferenceCheckDto>>
{
    private readonly IPatientRepository _patientRepository = patientRepository;
    private readonly IMedicalCaseCrossModuleService _medicalCaseCrossModuleService = medicalCaseCrossModuleService;

    public async Task<Result<PatientReferenceCheckDto>> Handle(
        CheckPatientReferenceQuery request, CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient == null)
            return Result<PatientReferenceCheckDto>.Failure(ErrorCode.PatientNotFound, "患者不存在");

        var refCount = await _medicalCaseCrossModuleService.CountMedicalCasesAsync(request.PatientId, cancellationToken);
        var recentCases = await _medicalCaseCrossModuleService.GetRecentMedicalCasesAsync(request.PatientId, 5, cancellationToken);

        var dto = new PatientReferenceCheckDto
        {
            PatientId = request.PatientId,
            PatientName = patient.Name,
            HasReferences = refCount > 0,
            ReferenceCount = refCount,
            CanDelete = true,
            DeleteWarning = refCount > 0 ? $"该患者有 {refCount} 条医案记录，建议使用禁用功能替代删除" : null,
            RecentMedicalCases = recentCases
        };

        return Result<PatientReferenceCheckDto>.Success(dto);
    }
}
