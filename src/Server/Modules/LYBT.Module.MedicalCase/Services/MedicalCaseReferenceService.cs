using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 医案引用查询服务实现 - 用于跨模块查询
    /// Architecture Fix: 集中处理医案查询逻辑，供其他模块（如Patient）使用
    /// Task 6: Repository 规范统一 — 委托 IMedicalCaseReferenceRepository
    /// </summary>
    public class MedicalCaseReferenceService : IMedicalCaseCrossModuleService
    {
        private readonly IMedicalCaseReferenceRepository _referenceRepository;
        private readonly IMedicalCaseCommandService _commandService;

        public MedicalCaseReferenceService(
            IMedicalCaseReferenceRepository referenceRepository,
            IMedicalCaseCommandService commandService)
        {
            _referenceRepository = referenceRepository ?? throw new ArgumentNullException(nameof(referenceRepository));
            _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
        }

        /// <inheritdoc/>
        public async Task<int> CountUnfinishedMedicalCasesAsync(Guid patientId, CancellationToken cancellationToken = default)
        {
            return await _referenceRepository.CountUnfinishedAsync(patientId, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<int> CountMedicalCasesAsync(Guid patientId, CancellationToken cancellationToken = default)
        {
            return await _referenceRepository.CountAllAsync(patientId, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<MedicalCaseReferenceDto>> GetRecentMedicalCasesAsync(Guid patientId, int count, CancellationToken cancellationToken = default)
        {
            return await _referenceRepository.GetRecentAsync(patientId, count, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Guid?> CreateQuickVisitMedicalCaseAsync(Guid patientId, Guid registrationId, Guid doctorId, CancellationToken cancellationToken = default)
        {
            var input = new MedicalCaseInputDto
            {
                PatientId = patientId,
                UserId = doctorId,
                RegistrationId = registrationId
            };
            var medicalCase = await _commandService.SaveAsync(input, doctorId, isAdmin: false, cancellationToken);
            return medicalCase?.Id;
        }
    }
}


