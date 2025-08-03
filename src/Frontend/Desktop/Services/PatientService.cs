using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Records;
using LYBT.WPF.Client.Core.Interfaces.Services;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 患者服务实现
    /// </summary>
    public class PatientService : IPatientService
    {
        private readonly IApiService _apiService;

        public PatientService(IApiService apiService)
        {
            _apiService = apiService;
        }

        /// <summary>
        /// 新增患者
        /// </summary>
        public async Task<ApiResponse<object>> AddAsync(PatientDetailDto dto)
        {
            try
            {
                return await _apiService.PostAsync<object>("patients", dto);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"创建患者失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 编辑患者
        /// </summary>
        public async Task<ApiResponse<object>> UpdateAsync(PatientDetailDto dto)
        {
            try
            {
                return await _apiService.PutAsync<object>($"patients/{dto.Id}", dto);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"更新患者失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 启用患者档案
        /// </summary>
        public async Task<ApiResponse<object>> EnableAsync(Guid id)
        {
            try
            {
                return await _apiService.PatchAsync<object>($"patients/{id}/enable", new object());
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"启用患者失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 禁用患者档案
        /// </summary>
        public async Task<ApiResponse<object>> DisableAsync(Guid id)
        {
            try
            {
                return await _apiService.PatchAsync<object>($"patients/{id}/disable", new object());
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"禁用患者失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取患者详情
        /// </summary>
        public async Task<ApiResponse<PatientDetailDto>> GetByIdAsync(Guid id)
        {
            try
            {
                return await _apiService.GetAsync<PatientDetailDto>($"patients/{id}");
            }
            catch (Exception ex)
            {
                return new ApiResponse<PatientDetailDto>
                {
                    IsSuccess = false,
                    Message = $"获取患者详情失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取所有患者
        /// </summary>
        public async Task<ApiResponse<List<PatientDetailDto>>> GetAllAsync()
        {
            try
            {
                return await _apiService.GetAsync<List<PatientDetailDto>>("patients");
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<PatientDetailDto>>
                {
                    IsSuccess = false,
                    Message = $"获取患者列表失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 分页查询患者
        /// </summary>
        public async Task<ApiResponse<PaginatedResult<PatientDetailDto>>> GetPagedAsync(PatientPagedQueryDto query)
        {
            try
            {
                return await _apiService.PostAsync<PaginatedResult<PatientDetailDto>>("patients/paged", query);
            }
            catch (Exception ex)
            {
                return new ApiResponse<PaginatedResult<PatientDetailDto>>
                {
                    IsSuccess = false,
                    Message = $"分页查询患者失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 批量禁用患者
        /// </summary>
        public async Task<ApiResponse<object>> BatchDisableAsync(List<Guid> ids)
        {
            try
            {
                var dto = new { Ids = ids };
                return await _apiService.PatchAsync<object>("patients/batch-disable", dto);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"批量禁用患者失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 批量启用患者
        /// </summary>
        public async Task<ApiResponse<object>> BatchEnableAsync(List<Guid> ids)
        {
            try
            {
                var dto = new { Ids = ids };
                return await _apiService.PatchAsync<object>("patients/batch-enable", dto);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"批量启用患者失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 搜索患者
        /// </summary>
        public async Task<ApiResponse<List<PatientDetailDto>>> SearchAsync(string keyword)
        {
            try
            {
                return await _apiService.GetAsync<List<PatientDetailDto>>($"patients/search?keyword={Uri.EscapeDataString(keyword)}");
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<PatientDetailDto>>
                {
                    IsSuccess = false,
                    Message = $"搜索患者失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 导入患者数据
        /// </summary>
        public async Task<ApiResponse<object>> ImportAsync(List<PatientDetailDto> patients)
        {
            try
            {
                return await _apiService.PostAsync<object>("patients/import", patients);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"导入患者数据失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 导出患者数据
        /// </summary>
        public async Task<ApiResponse<List<PatientDetailDto>>> ExportAsync()
        {
            try
            {
                return await _apiService.GetAsync<List<PatientDetailDto>>("patients/export");
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<PatientDetailDto>>
                {
                    IsSuccess = false,
                    Message = $"导出患者数据失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取患者历史病历
        /// </summary>
        public async Task<ApiResponse<List<RecordDto>>> GetHistoryRecordsAsync(Guid patientId)
        {
            try
            {
                return await _apiService.GetAsync<List<RecordDto>>($"patients/{patientId}/records");
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<RecordDto>>
                {
                    IsSuccess = false,
                    Message = $"获取患者病历历史失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取活跃患者列表
        /// </summary>
        public async Task<ApiResponse<List<PatientDetailDto>>> GetActivePatientsAsync()
        {
            try
            {
                return await _apiService.GetAsync<List<PatientDetailDto>>("patients/active");
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<PatientDetailDto>>
                {
                    IsSuccess = false,
                    Message = $"获取活跃患者列表失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 查询或创建患者
        /// </summary>
        public async Task<ApiResponse<PatientDetailDto>> FindOrCreateAsync(PatientDetailDto dto)
        {
            try
            {
                return await _apiService.PostAsync<PatientDetailDto>("patients/find-or-create", dto);
            }
            catch (Exception ex)
            {
                return new ApiResponse<PatientDetailDto>
                {
                    IsSuccess = false,
                    Message = $"查询或创建患者失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 快速搜索患者（根据关键词）
        /// </summary>
        public async Task<ApiResponse<List<PatientDetailDto>>> QuickSearchAsync(string keyword)
        {
            return await SearchAsync(keyword);
        }
    }
}