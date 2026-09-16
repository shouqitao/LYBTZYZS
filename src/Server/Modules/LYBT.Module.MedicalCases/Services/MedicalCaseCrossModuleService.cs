using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 医案跨模块查询服务实现 - 用于跨模块查询
    /// 架构修复：集中处理医案查询逻辑，供其他模块（如 Patient）使用
    /// Task 6: Repository 规范统一 — 委托 IMedicalCaseReferenceRepository
    /// </summary>
    public class MedicalCaseCrossModuleService : IMedicalCaseCrossModuleService
    {
        private readonly IMedicalCaseReferenceRepository _referenceRepository;
        private readonly IMedicalCaseCommandService _commandService;

        public MedicalCaseCrossModuleService(
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
        public Task<Dictionary<Guid, int>> CountMedicalCasesBatchAsync(IEnumerable<Guid> patientIds, CancellationToken cancellationToken = default)
            => _referenceRepository.CountAllBatchAsync(patientIds, cancellationToken);

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

        /// <summary>
        /// 为快速看诊创建医案并关联挂号。
        /// 幂等（design-03 §2 方案 C）：幂等键 RegistrationId 的权威关联存放在挂号侧（Registrations.MedicalCaseId），
        /// MedicalCase 实体无 RegistrationId 列、本模块亦无按 RegistrationId 的查询，故判重不在本方法内实现——
        /// 由调用方 StartVisitCommandHandler 在读出的挂号聚合上幂等短路（已关联即复用，不重复建案）。
        /// 不得改用 patientId 判重：同一患者跨挂号/跨医生本就可有多条医案，按患者判重会误复用历史或他人医案。
        /// 跨 DbContext 写入（本方法 + 挂号侧）不具备原子性，失败补偿同样由调用方负责。
        /// </summary>
        /// <inheritdoc/>
        public async Task<Guid?> CreateMedicalCaseForRegistrationAsync(Guid patientId, Guid registrationId, Guid doctorId, CancellationToken cancellationToken = default)
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


