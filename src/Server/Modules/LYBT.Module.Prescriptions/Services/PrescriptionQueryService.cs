using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Services
{

    /// <summary>
    /// 处方查询服务 - UltraThink架构重构版
    /// 职责：分页查询，搜索筛选，处方查询，历史记录获取
    /// 改为使用ReadRepository，移除直接的DbContext依赖
    /// </summary>
    public class PrescriptionQueryService : IPrescriptionQueryService
    {
        private readonly IPrescriptionReadRepository _readRepository;
        private readonly ILogger<PrescriptionQueryService> _logger;

        public PrescriptionQueryService(
            IPrescriptionReadRepository readRepository,
            ILogger<PrescriptionQueryService> logger)
        {
            _readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 根据ID获取处方详情
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<PrescriptionDto>.Failure("处方ID不能为空");
                }

                var prescription = await _readRepository.GetPrescriptionDtoByIdAsync(id);
                if (prescription == null)
                {
                    return ServiceResult<PrescriptionDto>.Failure("处方不存在");
                }

                return ServiceResult<PrescriptionDto>.Success(prescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取处方详情失败: {Id}", id);
                return ServiceResult<PrescriptionDto>.Failure($"获取处方详情失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 分页查询处方
        /// </summary>
        public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionQueryDto query)
        {
            try
            {
                var pagedResult = await _readRepository.GetPagedPrescriptionDtosAsync(query);
                return ServiceResult<PagedResult<PrescriptionDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询处方失败");
                return ServiceResult<PagedResult<PrescriptionDto>>.Failure($"分页查询处方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据患者ID获取处方历史
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                if (patientId == Guid.Empty)
                {
                    return ServiceResult<List<PrescriptionDto>>.Failure("患者ID不能为空");
                }

                var prescriptions = await _readRepository.GetPrescriptionDtosByPatientIdAsync(patientId);
                return ServiceResult<List<PrescriptionDto>>.Success(prescriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者处方历史失败: {PatientId}", patientId);
                return ServiceResult<List<PrescriptionDto>>.Failure($"获取患者处方历史失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据医疗案例ID获取处方列表
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                if (medicalCaseId == Guid.Empty)
                {
                    return ServiceResult<List<PrescriptionDto>>.Failure("医疗案例ID不能为空");
                }

                var prescriptions = await _readRepository.GetPrescriptionDtosByMedicalCaseIdAsync(medicalCaseId);
                return ServiceResult<List<PrescriptionDto>>.Success(prescriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例处方失败: {MedicalCaseId}", medicalCaseId);
                return ServiceResult<List<PrescriptionDto>>.Failure($"获取医疗案例处方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 搜索处方
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> SearchAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return ServiceResult<List<PrescriptionDto>>.Success(new List<PrescriptionDto>());
                }

                var prescriptions = await _readRepository.SearchPrescriptionDtosAsync(keyword.Trim());
                return ServiceResult<List<PrescriptionDto>>.Success(prescriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索处方失败: {Keyword}", keyword);
                return ServiceResult<List<PrescriptionDto>>.Failure($"搜索处方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取所有处方列表
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> GetAllAsync()
        {
            try
            {
                var prescriptions = await _readRepository.GetAllPrescriptionDtosAsync();
                return ServiceResult<List<PrescriptionDto>>.Success(prescriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取处方列表失败");
                return ServiceResult<List<PrescriptionDto>>.Failure($"获取处方列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取医生今日处方
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> GetDoctorTodayPrescriptionsAsync(Guid doctorId)
        {
            try
            {
                if (doctorId == Guid.Empty)
                {
                    return ServiceResult<List<PrescriptionDto>>.Failure("医生ID不能为空");
                }

                var prescriptions = await _readRepository.GetDoctorTodayPrescriptionDtosAsync(doctorId);
                return ServiceResult<List<PrescriptionDto>>.Success(prescriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医生处方失败: {DoctorId}", doctorId);
                return ServiceResult<List<PrescriptionDto>>.Failure($"获取医生处方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取处方统计信息
        /// </summary>
        public async Task<ServiceResult<PrescriptionStatsDto>> GetStatsAsync()
        {
            try
            {
                var stats = await _readRepository.GetPrescriptionStatsAsync();
                return ServiceResult<PrescriptionStatsDto>.Success(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取处方统计失败");
                return ServiceResult<PrescriptionStatsDto>.Failure($"获取处方统计失败: {ex.Message}");
            }
        }
    }
}
