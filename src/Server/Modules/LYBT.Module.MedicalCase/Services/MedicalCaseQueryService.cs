using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCase.Services
{

    /// <summary>
    /// 医疗案例查询服务 - UltraThink架构
    /// 职责：分页查询，搜索筛选，患者案例查询，活跃案例检查
    /// 改为使用ReadRepository，移除直接的DbContext依赖
    /// </summary>
    public class MedicalCaseQueryService : IMedicalCaseQueryService
    {
        private readonly IMedicalCaseReadRepository _readRepository;
        private readonly ILogger<MedicalCaseQueryService> _logger;

        public MedicalCaseQueryService(
            IMedicalCaseReadRepository readRepository,
            ILogger<MedicalCaseQueryService> logger)
        {
            _readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 根据ID获取医疗案例详情
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDto>> GetByIdAsync(Guid caseId)
        {
            try
            {
                if (caseId == Guid.Empty)
                {
                    return ServiceResult<MedicalCaseDto>.Failure("医疗案例ID不能为空");
                }

                var dto = await _readRepository.GetMedicalCaseDtoByIdAsync(caseId);
                if (dto == null)
                {
                    return ServiceResult<MedicalCaseDto>.Failure("医疗案例不存在");
                }

                return ServiceResult<MedicalCaseDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例详情失败: {Id}", caseId);
                return ServiceResult<MedicalCaseDto>.Failure($"获取医疗案例详情失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 分页查询医疗案例
        /// </summary>
        public async Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                var pageIndex = Math.Max(query.PageIndex, 1);
                var pageSize = Math.Clamp(query.PageSize, 10, 100);

                var queryDto = new MedicalCaseQueryDto
                {
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    Keyword = query.Keyword
                };

                var pagedResult = await _readRepository.GetPagedMedicalCaseDtosAsync(queryDto);

                return ServiceResult<PagedResult<MedicalCaseDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询医疗案例失败");
                return ServiceResult<PagedResult<MedicalCaseDto>>.Failure($"分页查询医疗案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据患者ID获取医疗案例列表
        /// </summary>
        public async Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                if (patientId == Guid.Empty)
                {
                    return ServiceResult<List<MedicalCaseDto>>.Failure("患者ID不能为空");
                }

                var dtos = await _readRepository.GetMedicalCaseDtosByPatientIdAsync(patientId);
                return ServiceResult<List<MedicalCaseDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据患者ID获取医疗案例失败: {PatientId}", patientId);
                return ServiceResult<List<MedicalCaseDto>>.Failure($"获取患者医疗案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取患者的活跃医疗案例
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDto>> GetActiveByPatientIdAsync(Guid patientId)
        {
            try
            {
                if (patientId == Guid.Empty)
                {
                    return ServiceResult<MedicalCaseDto>.Failure("患者ID不能为空");
                }

                var dto = await _readRepository.GetActiveMedicalCaseDtoByPatientIdAsync(patientId);
                if (dto == null)
                {
                    return ServiceResult<MedicalCaseDto>.Failure("患者暂无活跃的医疗案例");
                }

                return ServiceResult<MedicalCaseDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者活跃医疗案例失败: {PatientId}", patientId);
                return ServiceResult<MedicalCaseDto>.Failure($"获取患者活跃医疗案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 搜索医疗案例
        /// </summary>
        public async Task<ServiceResult<List<MedicalCaseDto>>> SearchAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return ServiceResult<List<MedicalCaseDto>>.Success(new List<MedicalCaseDto>());
                }

                var searchTerm = keyword.Trim();
                var dtos = await _readRepository.SearchMedicalCaseDtosAsync(searchTerm, 50);
                return ServiceResult<List<MedicalCaseDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索医疗案例失败: {Keyword}", keyword);
                return ServiceResult<List<MedicalCaseDto>>.Failure($"搜索医疗案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查患者是否有活跃案例
        /// </summary>
        public async Task<ServiceResult<bool>> HasActiveCaseAsync(Guid patientId)
        {
            try
            {
                if (patientId == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("患者ID不能为空");
                }

                var hasActiveCase = await _readRepository.HasActiveCaseAsync(patientId);
                return ServiceResult<bool>.Success(hasActiveCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查患者活跃案例失败: {PatientId}", patientId);
                return ServiceResult<bool>.Failure($"检查患者活跃案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取历史医疗案例
        /// </summary>
        public async Task<ServiceResult<List<MedicalCaseDto>>> GetHistoryAsync(Guid patientId)
        {
            try
            {
                if (patientId == Guid.Empty)
                {
                    return ServiceResult<List<MedicalCaseDto>>.Failure("患者ID不能为空");
                }

                var dtos = await _readRepository.GetHistoryMedicalCaseDtosAsync(patientId);
                return ServiceResult<List<MedicalCaseDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取历史医疗案例失败: {PatientId}", patientId);
                return ServiceResult<List<MedicalCaseDto>>.Failure($"获取历史医疗案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取医疗案例统计信息
        /// </summary>
        /// <summary>
        /// 获取医疗案例统计信息 - 简化版本仅返回基础信息
        /// </summary>
        public Task<ServiceResult<object>> GetStatisticsAsync()
        {
            try
            {
                // Record-Only模式：极简统计，仅业务运行必需
                var statistics = new
                {
                    Message = "统计功能在简化版本中暂不提供",
                    GeneratedAt = DateTime.Now
                };

                return Task.FromResult(ServiceResult<object>.Success(statistics));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例统计信息失败");
                return Task.FromResult(ServiceResult<object>.Failure($"获取统计信息失败: {ex.Message}"));
            }
        }


        /// <summary>
        /// 根据医生ID获取医疗案例
        /// </summary>
        public async Task<ServiceResult<List<MedicalCaseDto>>> GetByDoctorIdAsync(Guid doctorId)
        {
            try
            {
                if (doctorId == Guid.Empty)
                {
                    return ServiceResult<List<MedicalCaseDto>>.Failure("医生ID不能为空");
                }

                var dtos = await _readRepository.GetMedicalCaseDtosByDoctorIdAsync(doctorId);
                return ServiceResult<List<MedicalCaseDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据医生ID获取医疗案例失败: {DoctorId}", doctorId);
                return ServiceResult<List<MedicalCaseDto>>.Failure($"获取医生医疗案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据状态获取医疗案例
        /// </summary>
        public async Task<ServiceResult<List<MedicalCaseDto>>> GetByStatusAsync(MedicalCaseStatus status)
        {
            try
            {
                var dtos = await _readRepository.GetMedicalCaseDtosByStatusAsync(status);
                return ServiceResult<List<MedicalCaseDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据状态获取医疗案例失败: {Status}", status);
                return ServiceResult<List<MedicalCaseDto>>.Failure($"获取医疗案例失败: {ex.Message}");
            }
        }
    }
}
