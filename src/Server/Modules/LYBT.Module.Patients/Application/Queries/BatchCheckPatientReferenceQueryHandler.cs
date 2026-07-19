using MediatR;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Patients.Interfaces;

namespace LYBT.Module.Patients.Application.Queries;

/// <summary>
/// 批量检查患者引用关系查询处理器。
/// </summary>
public class BatchCheckPatientReferenceQueryHandler(
    IPatientRepository patientRepository,
    IMedicalCaseCrossModuleService medicalCaseCrossModuleService) : IRequestHandler<BatchCheckPatientReferenceQuery, Result<List<PatientReferenceCheckDto>>>
{
    private readonly IPatientRepository _patientRepository = patientRepository;
    private readonly IMedicalCaseCrossModuleService _medicalCaseCrossModuleService = medicalCaseCrossModuleService;

    public async Task<Result<List<PatientReferenceCheckDto>>> Handle(
        BatchCheckPatientReferenceQuery request, CancellationToken cancellationToken)
    {
        var results = new List<PatientReferenceCheckDto>();

        foreach (var patientId in request.PatientIds)
        {
            var patient = await _patientRepository.GetByIdAsync(patientId, cancellationToken);
            if (patient == null)
                continue;

            var refCount = await _medicalCaseCrossModuleService.CountMedicalCasesAsync(patientId, cancellationToken);
            var recentCases = await _medicalCaseCrossModuleService.GetRecentMedicalCasesAsync(patientId, 5, cancellationToken);

            results.Add(new PatientReferenceCheckDto
            {
                PatientId = patientId,
                PatientName = patient.Name,
                HasReferences = refCount > 0,
                ReferenceCount = refCount,
                CanDelete = true,
                DeleteWarning = refCount > 0 ? $"该患者有 {refCount} 条医案记录，建议使用禁用功能替代删除" : null,
                RecentMedicalCases = recentCases
            });
        }

        return Result<List<PatientReferenceCheckDto>>.Success(results);
    }
}
