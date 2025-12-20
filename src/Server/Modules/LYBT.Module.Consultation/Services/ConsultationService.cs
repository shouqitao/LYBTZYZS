using AutoMapper;
using LYBT.Module.Consultations.Interfaces;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.ExceptionHandling.Exceptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Consultations.Services
{
    /// <summary>
    /// 诊疗服务 - Read Layer（Issue #1600 Phase 3）
    /// 职责：提供诊疗记录的只读查询功能
    /// 所有Write操作必须通过MedicalCaseService聚合根进行
    /// </summary>
    public class ConsultationService : IConsultationService
    {
        private readonly IConsultationRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<ConsultationService> _logger;

        public ConsultationService(
            IConsultationRepository repository,
            IMapper mapper,
            ILogger<ConsultationService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<ConsultationDetailDto> GetByIdAsync(Guid id)
        {
            _logger.LogDebug("获取诊疗记录详情: {Id}", id);

            // 使用优化后的查询方法，包含所有关联数据
            var entity = await _repository.GetByIdWithDetailsAsync(id)
                ?? throw NotFoundException.Consultation(id);

            var dto = _mapper.Map<ConsultationDetailDto>(entity);
            // 确保PatientName和DoctorName从预加载的导航属性获取
            dto.PatientName = entity.MedicalCase?.PatientName ?? string.Empty;
            dto.DoctorName = entity.MedicalCase?.DoctorName ?? string.Empty;

            return dto;
        }

        // ========== Write方法已移除（Issue #1600 Phase 1）==========
        // CreateAsync, UpdateAsync, DeleteAsync 已移除
        // 所有写操作必须通过MedicalCase聚合根进行

        /// <inheritdoc/>
        public async Task<List<ConsultationDetailDto>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            _logger.LogDebug("根据医案ID获取诊疗记录: {MedicalCaseId}", medicalCaseId);

            // 使用优化后的查询方法，直接从数据库获取相关记录
            var consultation = await _repository.GetByMedicalCaseIdAsync(medicalCaseId);
            if (consultation == null)
            {
                return new List<ConsultationDetailDto>();
            }

            var dto = _mapper.Map<ConsultationDetailDto>(consultation);
            // 确保PatientName和DoctorName从预加载的导航属性获取
            dto.PatientName = consultation.MedicalCase?.PatientName ?? string.Empty;
            dto.DoctorName = consultation.MedicalCase?.DoctorName ?? string.Empty;

            return new List<ConsultationDetailDto> { dto };
        }

        // ========== Write方法已全部移除（Issue #1600 Phase 3）==========
        // CompleteStep1Async 已移除，迁移至MedicalCaseService
        // 所有写操作必须通过MedicalCase聚合根进行

    }
}
