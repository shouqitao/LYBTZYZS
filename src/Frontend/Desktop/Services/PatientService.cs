using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Services;
using LYBT.WPF.Client.Core.Models;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Common;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Services.Interfaces;
using System.Linq;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 患者服务实现
    /// </summary>
    public class PatientService : IPatientService
    {
        private readonly IApiService _apiService;
        private readonly IPatientsApiService _patientsApiService;

        public PatientService(IApiService apiService, IPatientsApiService patientsApiService)
        {
            _apiService = apiService;
            _patientsApiService = patientsApiService;
        }

        /// <summary>
        /// 新增患者
        /// </summary>
        public async Task<ServiceResult> AddAsync(PatientDetailDto dto)
        {
            var result = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _patientsApiService.CreatePatientAsync(dto)
            );
            return result.IsSuccess ? ServiceResult.Success() : ServiceResult.Failure(result.ErrorMessage ?? "操作失败");
        }

        /// <summary>
        /// 编辑患者
        /// </summary>
        public async Task<ServiceResult> UpdateAsync(PatientDetailDto dto)
        {
            var result = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _patientsApiService.UpdatePatientAsync(dto.Id, dto)
            );
            return result.IsSuccess ? ServiceResult.Success() : ServiceResult.Failure(result.ErrorMessage ?? "操作失败");
        }

        /// <summary>
        /// 启用患者档案
        /// </summary>
        public async Task<ServiceResult> EnableAsync(Guid id)
        {
            return await ApiErrorHandler.HandleApiCallAsync(async () =>
                await _patientsApiService.ToggleStatusAsync(id)
            );
        }

        /// <summary>
        /// 禁用患者档案
        /// </summary>
        public async Task<ServiceResult> DisableAsync(Guid id)
        {
            return await ApiErrorHandler.HandleApiCallAsync(async () =>
                await _patientsApiService.ToggleStatusAsync(id)
            );
        }

        /// <summary>
        /// 获取患者详情
        /// </summary>
        public async Task<ServiceResult<PatientDetailDto>> GetByIdAsync(Guid id)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _patientsApiService.GetPatientAsync(id)
            );
        }

        /// <summary>
        /// 获取所有患者
        /// </summary>
        public async Task<ServiceResult<List<PatientDetailDto>>> GetAllAsync()
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _patientsApiService.GetAllAsync()
            );
        }

        /// <summary>
        /// 分页查询患者
        /// </summary>
        public async Task<LYBT.WPF.Client.Core.Models.Common.PagedResult<PatientInfo>> GetPagedAsync(PatientPagedQueryDto query)
        {
            try
            {
                // 使用更新后的RESTful GET接口
                var response = await _patientsApiService.GetPatientsAsync(
                    page: query.CurrentPage,
                    pageSize: query.PageSize,
                    keyword: query.SearchKeyword,
                    name: query.Name,
                    phoneNumber: query.PhoneNumber,
                    idNumber: query.IDNumber,
                    address: query.Address,
                    gender: query.Gender,
                    minAge: query.MinAge,
                    maxAge: query.MaxAge,
                    status: query.Status.HasValue ? (PatientStatus)(int)query.Status.Value : null
                );
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var patientInfos = response.Content.Items.Select(ConvertToPatientInfo).ToList();
                    return new LYBT.WPF.Client.Core.Models.Common.PagedResult<PatientInfo>
                    {
                        Items = patientInfos,
                        TotalCount = response.Content.TotalCount,
                        CurrentPage = response.Content.CurrentPage,
                        PageSize = response.Content.PageSize
                    };
                }

                return new LYBT.WPF.Client.Core.Models.Common.PagedResult<PatientInfo>
                {
                    Items = new List<PatientInfo>(),
                    TotalCount = 0,
                    CurrentPage = query.CurrentPage,
                    PageSize = query.PageSize,
                    ErrorMessage = "获取患者列表失败"
                };
            }
            catch (Exception ex)
            {
                return new LYBT.WPF.Client.Core.Models.Common.PagedResult<PatientInfo>
                {
                    Items = new List<PatientInfo>(),
                    TotalCount = 0,
                    CurrentPage = query.CurrentPage,
                    PageSize = query.PageSize,
                    ErrorMessage = $"分页查询患者失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 批量禁用患者 - 功能已移除
        /// </summary>
        public async Task<ServiceResult> BatchDisableAsync(List<Guid> ids)
        {
            // 批量操作接口已移除，返回失败
            return ServiceResult.Failure("批量操作功能已禁用");
        }

        /// <summary>
        /// 批量启用患者 - 功能已移除
        /// </summary>
        public async Task<ServiceResult> BatchEnableAsync(List<Guid> ids)
        {
            // 批量操作接口已移除，返回失败
            return ServiceResult.Failure("批量操作功能已禁用");
        }

        /// <summary>
        /// 搜索患者
        /// </summary>
        public async Task<ServiceResult<List<PatientDetailDto>>> SearchAsync(string keyword)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _patientsApiService.SearchAsync(keyword)
            );
        }

        /// <summary>
        /// 导入患者数据 - 功能已移除
        /// </summary>
        public async Task<ServiceResult> ImportAsync(List<PatientDetailDto> patients)
        {
            // 导入功能已移除，返回失败
            return ServiceResult.Failure("导入功能已禁用");
        }

        /// <summary>
        /// 导出患者数据
        /// </summary>
        public async Task<ServiceResult<List<PatientDetailDto>>> ExportAsync()
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _patientsApiService.ExportAsync()
            );
        }

        /// <summary>
        /// 获取患者历史病历 - 功能已移除
        /// </summary>
        public async Task<ServiceResult<List<object>>> GetHistoryRecordsAsync(Guid patientId)
        {
            // Records模块已移除，返回空列表
            return ServiceResult<List<object>>.Success(new List<object>());
        }

        /// <summary>
        /// 获取活跃患者列表
        /// </summary>
        public async Task<ServiceResult<List<PatientDetailDto>>> GetActivePatientsAsync()
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _patientsApiService.GetActivePatientsAsync()
            );
        }

        /// <summary>
        /// 查询或创建患者
        /// </summary>
        public async Task<ServiceResult<PatientDetailDto>> FindOrCreateAsync(PatientDetailDto dto)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _patientsApiService.FindOrCreateAsync(dto)
            );
        }

        /// <summary>
        /// 快速搜索患者（根据关键词）
        /// </summary>
        public async Task<ServiceResult<List<PatientDetailDto>>> QuickSearchAsync(string keyword)
        {
            return await SearchAsync(keyword);
        }

        /// <summary>
        /// 转换PatientDetailDto到PatientInfo
        /// </summary>
        private PatientInfo ConvertToPatientInfo(PatientDetailDto dto)
        {
            return new PatientInfo
            {
                Id = dto.Id,
                Name = dto.Name,
                Gender = dto.Gender,
                Age = dto.Age,
                PhoneNumber = dto.PhoneNumber,
                IdNumber = dto.IDNumber,  // 注意大小写
                Address = dto.Address,
                AllergyHistory = dto.AllergyHistory,
                BirthDate = dto.BirthDate,
                CreateTime = dto.CreateTime,
                UpdateTime = dto.UpdateTime,
                Status = dto.Status  // 直接使用CommonStatus
            };
        }

        /// <summary>
        /// 获取患者列表
        /// </summary>
        public async Task<List<PatientInfo>> GetListAsync()
        {
            try
            {
                // 使用现有的GetActivePatientsAsync方法获取启用的患者列表
                var response = await _patientsApiService.GetActivePatientsAsync();
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return response.Content.Select(ConvertToPatientInfo).ToList();
                }
                return new List<PatientInfo>();
            }
            catch (Exception ex)
            {
                throw new Exception($"获取患者列表失败: {ex.Message}", ex);
            }
        }
    }
}