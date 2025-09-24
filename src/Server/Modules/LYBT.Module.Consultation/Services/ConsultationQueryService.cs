using LYBT.Module.Consultation.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Consultation.Services
{

    /// <summary>
    /// 诊疗查询服务 - UltraThink架构重构版
    /// 职责：分页查询，搜索筛选，诊疗查询，历史记录获取
    /// 改为使用ReadRepository，移除直接的DbContext依赖
    /// </summary>
    public class ConsultationQueryService : IConsultationQueryService
    {
        private readonly IConsultationReadRepository _readRepository;
        private readonly ILogger<ConsultationQueryService> _logger;

        public ConsultationQueryService(
            IConsultationReadRepository readRepository,
            ILogger<ConsultationQueryService> logger)
        {
            _readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 分页查询诊疗记录
        /// </summary>
        public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                var consultationQuery = new ConsultationQueryDto
                {
                    PageIndex = query.PageIndex,
                    PageSize = query.PageSize,
                    Keyword = query.Keyword
                };
                var pagedResult = await _readRepository.GetPagedConsultationDtosAsync(consultationQuery);
                return ServiceResult<PagedResult<ConsultationDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询诊疗记录失败");
                return ServiceResult<PagedResult<ConsultationDto>>.Failure($"分页查询诊疗记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据患者ID获取诊疗记录
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                if (patientId == Guid.Empty)
                {
                    return ServiceResult<List<ConsultationDto>>.Failure("患者ID不能为空");
                }

                var consultations = await _readRepository.GetConsultationDtosByPatientIdAsync(patientId);
                return ServiceResult<List<ConsultationDto>>.Success(consultations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者诊疗记录失败: {PatientId}", patientId);
                return ServiceResult<List<ConsultationDto>>.Failure($"获取患者诊疗记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据医疗案例ID获取诊疗记录
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                if (medicalCaseId == Guid.Empty)
                {
                    return ServiceResult<List<ConsultationDto>>.Failure("医疗案例ID不能为空");
                }

                var consultations = await _readRepository.GetConsultationDtosByMedicalCaseIdAsync(medicalCaseId);
                return ServiceResult<List<ConsultationDto>>.Success(consultations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例诊疗记录失败: {MedicalCaseId}", medicalCaseId);
                return ServiceResult<List<ConsultationDto>>.Failure($"获取医疗案例诊疗记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据医生ID获取诊疗记录
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> GetByDoctorIdAsync(Guid doctorId)
        {
            try
            {
                if (doctorId == Guid.Empty)
                {
                    return ServiceResult<List<ConsultationDto>>.Failure("医生ID不能为空");
                }

                var consultations = await _readRepository.GetConsultationDtosByDoctorIdAsync(doctorId);
                return ServiceResult<List<ConsultationDto>>.Success(consultations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医生诊疗记录失败: {DoctorId}", doctorId);
                return ServiceResult<List<ConsultationDto>>.Failure($"获取医生诊疗记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 搜索诊疗记录
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto>());
                }

                var consultations = await _readRepository.SearchConsultationDtosAsync(keyword.Trim());
                return ServiceResult<List<ConsultationDto>>.Success(consultations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索诊疗记录失败: {Keyword}", keyword);
                return ServiceResult<List<ConsultationDto>>.Failure($"搜索诊疗记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取患者历史就诊记录
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> GetPatientHistoryAsync(Guid patientId)
        {
            try
            {
                if (patientId == Guid.Empty)
                {
                    return ServiceResult<List<ConsultationDto>>.Failure("患者ID不能为空");
                }

                var consultations = await _readRepository.GetPatientConsultationHistoryAsync(patientId);
                return ServiceResult<List<ConsultationDto>>.Success(consultations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者历史就诊记录失败: {PatientId}", patientId);
                return ServiceResult<List<ConsultationDto>>.Failure($"获取患者历史就诊记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据ID获取诊疗详情
        /// </summary>
        public async Task<ServiceResult<ConsultationDetailDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<ConsultationDetailDto>.Failure("诊疗ID不能为空");
                }

                var consultation = await _readRepository.GetConsultationDetailDtoByIdAsync(id);
                if (consultation == null)
                {
                    return ServiceResult<ConsultationDetailDto>.Failure("诊疗记录不存在");
                }

                return ServiceResult<ConsultationDetailDto>.Success(consultation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取诊疗详情失败: {Id}", id);
                return ServiceResult<ConsultationDetailDto>.Failure($"获取诊疗详情失败: {ex.Message}");
            }
        }
    }
}
