using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Interfaces.Services;
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
    /// 医疗案例服务实现
    /// </summary>
    public class MedicalCaseService : IMedicalCaseService
    {
        private readonly IMedicalCaseApiService _apiService;

        public MedicalCaseService(IMedicalCaseApiService apiService)
        {
            _apiService = apiService;
        }

        /// <summary>
        /// 分页查询医疗案例
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
        /// 根据ID获取医疗案例详情
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDetailDto>> GetByIdAsync(Guid id)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.GetByIdAsync(id)
            );
        }

        /// <summary>
        /// 创建医疗案例
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

            return ServiceResult<MedicalCaseInfo>.Failure(apiResponse.ErrorMessage ?? "创建医疗案例失败", null, apiResponse.Exception);
        }

        /// <summary>
        /// 更新医疗案例
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateAsync(MedicalCaseEditDto editDto)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.UpdateAsync(editDto.Id, editDto)
            );
        }

        /// <summary>
        /// 获取患者的医疗案例列表
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

            return ServiceResult<List<MedicalCaseInfo>>.Failure(apiResponse.ErrorMessage ?? "获取患者医疗案例列表失败", null, apiResponse.Exception);
        }

        /// <summary>
        /// 获取今日医疗案例列表
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

            return ServiceResult<List<MedicalCaseInfo>>.Failure(apiResponse.ErrorMessage ?? "获取今日医疗案例列表失败", null, apiResponse.Exception);
        }

        /// <summary>
        /// 更新医疗案例状态
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, MedicalCaseStatus status)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.UpdateStatusAsync(id, status)
            );
        }

        /// <summary>
        /// 删除医疗案例（软删除）
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.DeleteAsync(id)
            );
        }

        #region Private Methods

        /// <summary>
        /// 转换DTO为前端模型
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

        #endregion
    }
}