using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.WPF.Client.Core.Models.Consultation;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 看诊服务实现
    /// </summary>
    public class ConsultationService : IConsultationService
    {
        private readonly IConsultationApiService _apiService;
        private readonly IMapper _mapper;
        private readonly ILogger<ConsultationService> _logger;

        public ConsultationService(
            IConsultationApiService apiService,
            IMapper mapper,
            ILogger<ConsultationService> logger)
        {
            _apiService = apiService;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// 分页查询看诊记录
        /// </summary>
        public async Task<PagedResult<ConsultationInfo>> SearchConsultationsAsync(PaginationRequest query)
        {
            try
            {
                var consultationQuery = new ConsultationPagedQueryDto
                {
                    PageIndex = query.CurrentPage,
                    PageSize = query.PageSize,
                    Keyword = query.SearchKeyword
                };

                var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                    await _apiService.GetPagedAsync(consultationQuery)
                );

                if (apiResponse.IsSuccess && apiResponse.Data != null)
                {
                    var consultations = apiResponse.Data.Data?.Select(MapToConsultationInfo).ToList() ?? new List<ConsultationInfo>();
                    
                    return new PagedResult<ConsultationInfo>
                    {
                        Items = consultations,
                        TotalCount = apiResponse.Data.TotalCount,
                        CurrentPage = apiResponse.Data.PageIndex,
                        PageSize = apiResponse.Data.PageSize
                    };
                }

                return new PagedResult<ConsultationInfo>
                {
                    Items = new List<ConsultationInfo>(),
                    TotalCount = 0,
                    CurrentPage = query.CurrentPage,
                    PageSize = query.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询看诊记录失败");
                return new PagedResult<ConsultationInfo>
                {
                    Items = new List<ConsultationInfo>(),
                    TotalCount = 0,
                    CurrentPage = query.CurrentPage,
                    PageSize = query.PageSize
                };
            }
        }

        /// <summary>
        /// 获取看诊详情
        /// </summary>
        public async Task<ServiceResult<ConsultationInfo>> GetByIdAsync(Guid id)
        {
            try
            {
                var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                    await _apiService.GetByIdAsync(id)
                );

                if (apiResponse.IsSuccess && apiResponse.Data != null)
                {
                    var consultation = MapToConsultationInfo(apiResponse.Data);
                    return ServiceResult<ConsultationInfo>.Success(consultation);
                }

                return ServiceResult<ConsultationInfo>.Failure(apiResponse.ErrorMessage ?? "获取看诊详情失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取看诊详情失败: {Id}", id);
                return ServiceResult<ConsultationInfo>.Failure("获取看诊详情失败");
            }
        }

        /// <summary>
        /// 根据医疗案例ID获取看诊信息
        /// </summary>
        public async Task<ServiceResult<ConsultationInfo>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                    await _apiService.GetByMedicalCaseIdAsync(medicalCaseId)
                );

                if (apiResponse.IsSuccess && apiResponse.Data != null)
                {
                    var consultation = MapToConsultationInfo(apiResponse.Data);
                    return ServiceResult<ConsultationInfo>.Success(consultation);
                }

                return ServiceResult<ConsultationInfo>.Failure(apiResponse.ErrorMessage ?? "获取看诊信息失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据医疗案例ID获取看诊信息失败: {MedicalCaseId}", medicalCaseId);
                return ServiceResult<ConsultationInfo>.Failure("获取看诊信息失败");
            }
        }

        /// <summary>
        /// 开始看诊
        /// </summary>
        public async Task<ServiceResult<ConsultationInfo>> StartConsultationAsync(ConsultationStartDto dto)
        {
            try
            {
                var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                    await _apiService.StartConsultationAsync(dto)
                );

                if (apiResponse.IsSuccess && apiResponse.Data != null)
                {
                    var consultation = MapToConsultationInfo(apiResponse.Data);
                    return ServiceResult<ConsultationInfo>.Success(consultation);
                }

                return ServiceResult<ConsultationInfo>.Failure(apiResponse.ErrorMessage ?? "开始看诊失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开始看诊失败");
                return ServiceResult<ConsultationInfo>.Failure("开始看诊失败");
            }
        }

        /// <summary>
        /// 更新看诊信息
        /// </summary>
        public async Task<ServiceResult<ConsultationInfo>> UpdateConsultationAsync(Guid id, ConsultationUpdateDto dto)
        {
            try
            {
                var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                    await _apiService.UpdateConsultationAsync(id, dto)
                );

                if (apiResponse.IsSuccess && apiResponse.Data != null)
                {
                    var consultation = MapToConsultationInfo(apiResponse.Data);
                    return ServiceResult<ConsultationInfo>.Success(consultation);
                }

                return ServiceResult<ConsultationInfo>.Failure(apiResponse.ErrorMessage ?? "更新看诊信息失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新看诊信息失败: {Id}", id);
                return ServiceResult<ConsultationInfo>.Failure("更新看诊信息失败");
            }
        }

        /// <summary>
        /// 完成看诊
        /// </summary>
        public async Task<ServiceResult<bool>> CompleteConsultationAsync(Guid id, ConsultationCompleteDto dto)
        {
            try
            {
                var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                    await _apiService.CompleteConsultationAsync(id, dto)
                );

                if (apiResponse.IsSuccess)
                {
                    return ServiceResult<bool>.Success(true);
                }

                return ServiceResult<bool>.Failure(apiResponse.ErrorMessage ?? "完成看诊失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成看诊失败: {Id}", id);
                return ServiceResult<bool>.Failure("完成看诊失败");
            }
        }

        /// <summary>
        /// 获取医生今日看诊列表
        /// </summary>
        public async Task<ServiceResult<List<ConsultationInfo>>> GetTodayConsultationsByDoctorAsync(Guid doctorId)
        {
            try
            {
                var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                    await _apiService.GetTodayConsultationsByDoctorAsync(doctorId)
                );

                if (apiResponse.IsSuccess && apiResponse.Data != null)
                {
                    var consultations = apiResponse.Data.Select(MapToConsultationInfo).ToList();
                    return ServiceResult<List<ConsultationInfo>>.Success(consultations);
                }

                return ServiceResult<List<ConsultationInfo>>.Success(new List<ConsultationInfo>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医生今日看诊列表失败: {DoctorId}", doctorId);
                return ServiceResult<List<ConsultationInfo>>.Failure("获取医生今日看诊列表失败");
            }
        }

        /// <summary>
        /// 获取患者历史看诊记录
        /// </summary>
        public async Task<ServiceResult<List<ConsultationInfo>>> GetPatientHistoryAsync(Guid patientId)
        {
            try
            {
                var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                    await _apiService.GetPatientHistoryAsync(patientId)
                );

                if (apiResponse.IsSuccess && apiResponse.Data != null)
                {
                    var consultations = apiResponse.Data.Select(MapToConsultationInfo).ToList();
                    return ServiceResult<List<ConsultationInfo>>.Success(consultations);
                }

                return ServiceResult<List<ConsultationInfo>>.Success(new List<ConsultationInfo>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者历史看诊记录失败: {PatientId}", patientId);
                return ServiceResult<List<ConsultationInfo>>.Failure("获取患者历史看诊记录失败");
            }
        }

        /// <summary>
        /// 统计医生看诊数量
        /// </summary>
        public async Task<ServiceResult<int>> GetDoctorConsultationCountAsync(Guid doctorId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                    await _apiService.GetDoctorConsultationCountAsync(doctorId, startDate, endDate)
                );

                if (apiResponse.IsSuccess)
                {
                    return ServiceResult<int>.Success(apiResponse.Data);
                }

                return ServiceResult<int>.Failure(apiResponse.ErrorMessage ?? "统计医生看诊数量失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "统计医生看诊数量失败: {DoctorId}", doctorId);
                return ServiceResult<int>.Failure("统计医生看诊数量失败");
            }
        }

        /// <summary>
        /// 删除看诊记录（软删除）
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                    await _apiService.DeleteAsync(id)
                );

                if (apiResponse.IsSuccess)
                {
                    return ServiceResult<bool>.Success(true);
                }

                return ServiceResult<bool>.Failure(apiResponse.ErrorMessage ?? "删除看诊记录失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除看诊记录失败: {Id}", id);
                return ServiceResult<bool>.Failure("删除看诊记录失败");
            }
        }

        #region 私有映射方法

        /// <summary>
        /// 映射DTO到前端模型
        /// </summary>
        private ConsultationInfo MapToConsultationInfo(ConsultationDto dto)
        {
            return new ConsultationInfo
            {
                Id = dto.Id,
                MedicalCaseId = dto.MedicalCaseId,
                PatientId = dto.PatientId,
                PatientName = dto.PatientName,
                UserId = dto.UserId,
                DoctorName = dto.DoctorName,
                Diagnosis = dto.Diagnosis,
                ConsultationTime = dto.ConsultationTime,
                Status = ParseStatus(dto.Status)
            };
        }

        /// <summary>
        /// 映射详细DTO到前端模型
        /// </summary>
        private ConsultationInfo MapToConsultationInfo(ConsultationDetailDto dto)
        {
            return new ConsultationInfo
            {
                Id = dto.Id,
                MedicalCaseId = dto.MedicalCaseId,
                PatientId = dto.PatientId,
                PatientName = dto.PatientName,
                UserId = dto.UserId,
                DoctorName = dto.DoctorName,
                ConsultationTime = dto.ConsultationTime,
                
                // 中医四诊
                Inspection = dto.Inspection,
                AuscultationOlfaction = dto.AuscultationOlfaction,
                Inquiry = dto.Inquiry,
                Palpation = dto.Palpation,
                TongueInspection = dto.TongueInspection,
                PulseCondition = dto.PulseCondition,
                
                // 诊断信息
                TCMDiagnosis = dto.TCMDiagnosis,
                Diagnosis = dto.Diagnosis,
                
                // 其他信息
                Remark = dto.Remark,
                
                Status = Shared.Models.Enums.CommonStatus.Enabled // 默认状态
            };
        }

        /// <summary>
        /// 解析状态字符串为枚举
        /// </summary>
        private Shared.Models.Enums.CommonStatus ParseStatus(string status)
        {
            return status?.ToLower() switch
            {
                "enabled" => Shared.Models.Enums.CommonStatus.Enabled,
                "disabled" => Shared.Models.Enums.CommonStatus.Disabled,
                _ => Shared.Models.Enums.CommonStatus.Enabled
            };
        }

        #endregion
    }
}