using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Core.Models;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Desktop.Core.Models.MedicalCase;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// 医疗案例服务实现 - UltraThink Phase 4: 实现Shared接口统一
    /// </summary>
    public class MedicalCaseService : LYBT.Shared.Interfaces.Services.IMedicalCaseService, 
                                      LYBT.Desktop.Core.Interfaces.Services.IMedicalCaseService
    {
        private readonly IMedicalCaseApiService _apiService;

        public MedicalCaseService(IMedicalCaseApiService apiService)
        {
            _apiService = apiService;
        }

        #region Shared Interface Implementation (显式实现)

        /// <summary>
        /// [Shared] 根据ID获取医疗案例详情
        /// </summary>
        async Task<ServiceResult<MedicalCaseDetailDto>> LYBT.Shared.Interfaces.Services.IMedicalCaseService.GetByIdAsync(Guid id)
        {
            var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.GetByIdAsync(id)
            );

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                return ServiceResult<MedicalCaseDetailDto>.Success(apiResponse.Data);
            }

            return ServiceResult<MedicalCaseDetailDto>.Failure(apiResponse.ErrorMessage ?? "获取医疗案例详情失败", apiResponse.Exception);
        }

        /// <summary>
        /// [Shared] 分页查询医疗案例
        /// </summary>
        async Task<ServiceResult<PagedResult<MedicalCaseDto>>> LYBT.Shared.Interfaces.Services.IMedicalCaseService.GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                var response = await _apiService.GetPagedAsync(query.PageIndex, query.PageSize);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    // 转换为Shared DTO格式
                    var dtos = response.Content.Items.ToList();
                    var result = new PagedResult<MedicalCaseDto>
                    {
                        Items = dtos,
                        TotalCount = response.Content.TotalCount,
                        CurrentPage = response.Content.CurrentPage,
                        PageSize = response.Content.PageSize
                    };
                    return ServiceResult<PagedResult<MedicalCaseDto>>.Success(result);
                }

                return ServiceResult<PagedResult<MedicalCaseDto>>.Failure("获取医疗案例失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<MedicalCaseDto>>.Failure("分页查询医疗案例时发生错误", ex);
            }
        }

        /// <summary>
        /// [Shared] 创建医疗案例
        /// </summary>
        async Task<ServiceResult<MedicalCaseDto>> LYBT.Shared.Interfaces.Services.IMedicalCaseService.CreateAsync(MedicalCaseCreateDto dto)
        {
            var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.CreateAsync(dto)
            );

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                return ServiceResult<MedicalCaseDto>.Success(apiResponse.Data);
            }

            return ServiceResult<MedicalCaseDto>.Failure(apiResponse.ErrorMessage ?? "创建医疗案例失败", apiResponse.Exception);
        }

        /// <summary>
        /// [Shared] 更新医疗案例
        /// </summary>
        async Task<ServiceResult<MedicalCaseDto>> LYBT.Shared.Interfaces.Services.IMedicalCaseService.UpdateAsync(Guid id, MedicalCaseUpdateDto dto)
        {
            var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.UpdateAsync(id, dto)
            );

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                return ServiceResult<MedicalCaseDto>.Success(apiResponse.Data);
            }

            return ServiceResult<MedicalCaseDto>.Failure(apiResponse.ErrorMessage ?? "更新医疗案例失败", apiResponse.Exception);
        }

        /// <summary>
        /// [Shared] 删除医疗案例
        /// </summary>
        async Task<ServiceResult<bool>> LYBT.Shared.Interfaces.Services.IMedicalCaseService.DeleteAsync(Guid id)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.DeleteAsync(id)
            );
        }

        /// <summary>
        /// [Shared] 根据患者ID获取医疗案例
        /// </summary>
        async Task<ServiceResult<List<MedicalCaseDto>>> LYBT.Shared.Interfaces.Services.IMedicalCaseService.GetByPatientIdAsync(Guid patientId)
        {
            var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.GetByPatientIdAsync(patientId)
            );

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                return ServiceResult<List<MedicalCaseDto>>.Success(apiResponse.Data);
            }

            return ServiceResult<List<MedicalCaseDto>>.Failure(apiResponse.ErrorMessage ?? "获取患者医疗案例列表失败", apiResponse.Exception);
        }

        /// <summary>
        /// [Shared] 获取患者活跃医疗案例
        /// </summary>
        async Task<ServiceResult<MedicalCaseDto>> LYBT.Shared.Interfaces.Services.IMedicalCaseService.GetActiveByPatientIdAsync(Guid patientId)
        {
            var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.GetActiveByPatientIdAsync(patientId)
            );

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                return ServiceResult<MedicalCaseDto>.Success(apiResponse.Data);
            }

            return ServiceResult<MedicalCaseDto>.Failure(apiResponse.ErrorMessage ?? "获取患者活跃医疗案例失败", apiResponse.Exception);
        }

        /// <summary>
        /// [Shared] 完成医疗案例
        /// </summary>
        async Task<ServiceResult<bool>> LYBT.Shared.Interfaces.Services.IMedicalCaseService.CompleteAsync(Guid id, string completionReason)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.CompleteAsync(id, completionReason)
            );
        }

        /// <summary>
        /// [Shared] 暂停医疗案例
        /// </summary>
        async Task<ServiceResult<bool>> LYBT.Shared.Interfaces.Services.IMedicalCaseService.SuspendAsync(Guid id, string reason)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.SuspendAsync(id, reason)
            );
        }

        /// <summary>
        /// [Shared] 恢复医疗案例
        /// </summary>
        async Task<ServiceResult<bool>> LYBT.Shared.Interfaces.Services.IMedicalCaseService.ResumeAsync(Guid id)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.ResumeAsync(id)
            );
        }

        /// <summary>
        /// [Shared] 归档医疗案例
        /// </summary>
        async Task<ServiceResult<bool>> LYBT.Shared.Interfaces.Services.IMedicalCaseService.ArchiveAsync(Guid id, string archiveReason)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.ArchiveAsync(id, archiveReason)
            );
        }

        /// <summary>
        /// [Shared] 获取医疗案例统计信息
        /// </summary>
        async Task<ServiceResult<object>> LYBT.Shared.Interfaces.Services.IMedicalCaseService.GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.GetStatisticsAsync(startDate, endDate)
            );
        }

        /// <summary>
        /// [Shared] 搜索医疗案例
        /// </summary>
        async Task<ServiceResult<List<MedicalCaseDto>>> LYBT.Shared.Interfaces.Services.IMedicalCaseService.SearchAsync(string keyword)
        {
            var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.SearchAsync(keyword)
            );

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                return ServiceResult<List<MedicalCaseDto>>.Success(apiResponse.Data);
            }

            return ServiceResult<List<MedicalCaseDto>>.Failure(apiResponse.ErrorMessage ?? "搜索医疗案例失败", apiResponse.Exception);
        }

        /// <summary>
        /// [Shared] 获取医疗案例历史记录
        /// </summary>
        async Task<ServiceResult<List<object>>> LYBT.Shared.Interfaces.Services.IMedicalCaseService.GetHistoryAsync(Guid id)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.GetHistoryAsync(id)
            );
        }

        #endregion

        #region UI-Specific Interface Implementation (向后兼容)

        /// <summary>
        /// [UI] 分页查询医疗案例
        /// </summary>
        public async Task<PagedResult<MedicalCaseInfo>> GetPagedAsync(int pageIndex = 1, int pageSize = 20)
        {
            try
            {
                var response = await _apiService.GetPagedAsync(pageIndex, pageSize);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var medicalCaseInfos = response.Content.Items.Select(ConvertToMedicalCaseInfo).ToList();
                    return new PagedResult<MedicalCaseInfo>
                    {
                        Items = medicalCaseInfos,
                        TotalCount = (int)response.Content.TotalCount,
                        CurrentPage = response.Content.CurrentPage,
                        PageSize = response.Content.PageSize
                    };
                }

                return new PagedResult<MedicalCaseInfo>
                {
                    Items = new List<MedicalCaseInfo>(),
                    TotalCount = 0,
                    CurrentPage = pageIndex,
                    PageSize = pageSize,
                    ErrorMessage = "获取医疗案例失败"
                };
            }
            catch (Exception ex)
            {
                return new PagedResult<MedicalCaseInfo>
                {
                    Items = new List<MedicalCaseInfo>(),
                    TotalCount = 0,
                    CurrentPage = pageIndex,
                    PageSize = pageSize,
                    ErrorMessage = $"分页查询医疗案例时发生错误：{ex.Message}"
                };
            }
        }

        /// <summary>
        /// [UI] 根据ID获取医疗案例详情
        /// </summary>
        public async Task<ServiceResult<MedicalCaseInfo>> GetByIdAsync(Guid id)
        {
            var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.GetByIdAsync(id)
            );

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                var medicalCaseInfo = ConvertToMedicalCaseInfo(apiResponse.Data);
                return ServiceResult<MedicalCaseInfo>.Success(medicalCaseInfo);
            }

            return ServiceResult<MedicalCaseInfo>.Failure(apiResponse.ErrorMessage ?? "获取医疗案例详情失败", apiResponse.Exception);
        }

        /// <summary>
        /// [UI] 创建医疗案例
        /// </summary>
        public async Task<ServiceResult<MedicalCaseInfo>> CreateAsync(MedicalCaseCreateDto createDto)
        {
            var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.CreateAsync(createDto)
            );

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                var createdMedicalCase = ConvertToMedicalCaseInfo(apiResponse.Data);
                return ServiceResult<MedicalCaseInfo>.Success(createdMedicalCase);
            }

            return ServiceResult<MedicalCaseInfo>.Failure(apiResponse.ErrorMessage ?? "创建医疗案例失败", apiResponse.Exception);
        }

        /// <summary>
        /// [UI] 更新医疗案例
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateAsync(MedicalCaseEditDto editDto)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.UpdateAsync(editDto.Id, editDto)
            );
        }

        /// <summary>
        /// [UI] 获取患者的医疗案例列表
        /// </summary>
        public async Task<ServiceResult<List<MedicalCaseInfo>>> GetByPatientIdAsync(Guid patientId)
        {
            var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.GetByPatientIdAsync(patientId)
            );

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                var medicalCases = apiResponse.Data.Select(ConvertToMedicalCaseInfo).ToList();
                return ServiceResult<List<MedicalCaseInfo>>.Success(medicalCases);
            }

            return ServiceResult<List<MedicalCaseInfo>>.Failure(apiResponse.ErrorMessage ?? "获取患者医疗案例列表失败", apiResponse.Exception);
        }

        /// <summary>
        /// [UI] 获取今日医疗案例列表
        /// </summary>
        public async Task<ServiceResult<List<MedicalCaseInfo>>> GetTodayByUserIdAsync(Guid userId)
        {
            var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.GetTodayByUserIdAsync(userId)
            );

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                var medicalCases = apiResponse.Data.Select(ConvertToMedicalCaseInfo).ToList();
                return ServiceResult<List<MedicalCaseInfo>>.Success(medicalCases);
            }

            return ServiceResult<List<MedicalCaseInfo>>.Failure(apiResponse.ErrorMessage ?? "获取今日医疗案例列表失败", apiResponse.Exception);
        }

        /// <summary>
        /// [UI] 更新医疗案例状态
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, MedicalCaseStatus status)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.UpdateStatusAsync(id, status)
            );
        }

        /// <summary>
        /// [UI] 删除医疗案例（软删除）
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.DeleteAsync(id)
            );
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// UltraThink重构: 转换DTO为前端模型
        /// </summary>
        private MedicalCaseInfo ConvertToMedicalCaseInfo(MedicalCaseDto dto)
        {
            // 解析状态字符串为枚举
            MedicalCaseStatus status = MedicalCaseStatus.Registered;
            if (!string.IsNullOrEmpty(dto.Status))
            {
                Enum.TryParse(dto.Status, out status);
            }

            return new MedicalCaseInfo
            {
                Id = dto.Id,
                PatientId = dto.PatientId,
                PatientName = dto.PatientName ?? "",
                UserId = dto.DoctorId, // DoctorId映射到UserId
                DoctorName = dto.DoctorName ?? "",
                Status = status,
                CreateTime = dto.CreateTime,
                CompleteTime = dto.CompleteTime,
                // 前端特有字段使用默认值
                IsSelected = false,
                Remark = "",
                PatientAge = null,
                PatientGender = "",
                UpdateTime = null,
                IsActive = true
            };
        }

        /// <summary>
        /// UltraThink重构: 转换DetailDTO为前端模型（增强版本，包含详细信息）
        /// </summary>
        private MedicalCaseInfo ConvertToMedicalCaseInfo(MedicalCaseDetailDto detailDto)
        {
            // 解析状态字符串为枚举
            MedicalCaseStatus status = MedicalCaseStatus.Registered;
            if (!string.IsNullOrEmpty(detailDto.Status))
            {
                Enum.TryParse(detailDto.Status, out status);
            }

            return new MedicalCaseInfo
            {
                Id = detailDto.Id,
                PatientId = detailDto.PatientId,
                PatientName = detailDto.PatientName ?? "",
                UserId = detailDto.DoctorId, // DoctorId映射到UserId
                DoctorId = detailDto.DoctorId,
                DoctorName = detailDto.DoctorName ?? "",
                Status = status,
                CreateTime = detailDto.CreateTime,
                CompleteTime = detailDto.CompleteTime,
                
                // 详细信息字段映射
                ChiefComplaint = detailDto.ChiefComplaint,
                Diagnosis = detailDto.DiagnosisResult,
                
                // 前端特有字段使用默认值
                IsSelected = false,
                Remark = detailDto.TreatmentPlan ?? "",
                PatientAge = null,
                PatientGender = "",
                UpdateTime = null,
                IsActive = true
            };
        }

        #endregion
    }
}