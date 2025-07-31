using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Services;
using LYBT.Shared.Models.Common;
using LYBT.WPF.Client.Core.Models.DTOs;
using LYBT.WPF.Client.Core.Interfaces.Services;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 病历服务实现
    /// </summary>
    public class RecordService : IRecordService
    {
        private readonly IApiService _apiService;

        public RecordService(IApiService apiService)
        {
            _apiService = apiService;
        }

        /// <summary>
        /// 获取病历列表
        /// </summary>
        public async Task<ApiResponse<List<RecordDto>>> GetListAsync()
        {
            try
            {
                return await _apiService.GetAsync<List<RecordDto>>("record");
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<RecordDto>>
                {
                    IsSuccess = false,
                    Message = $"获取病历列表失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 根据患者ID获取病历列表
        /// </summary>
        public async Task<ApiResponse<List<RecordDto>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                return await _apiService.GetAsync<List<RecordDto>>($"record/patient/{patientId}");
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<RecordDto>>
                {
                    IsSuccess = false,
                    Message = $"获取患者病历失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取病历详情
        /// </summary>
        public async Task<ApiResponse<RecordDetailDto>> GetByIdAsync(Guid id)
        {
            try
            {
                return await _apiService.GetAsync<RecordDetailDto>($"record/{id}");
            }
            catch (Exception ex)
            {
                return new ApiResponse<RecordDetailDto>
                {
                    IsSuccess = false,
                    Message = $"获取病历详情失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 新增病历
        /// </summary>
        public async Task<ApiResponse<object>> AddAsync(RecordCreateDto dto)
        {
            try
            {
                return await _apiService.PostAsync<object>("record", dto);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"创建病历失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 编辑病历
        /// </summary>
        public async Task<ApiResponse<object>> UpdateAsync(RecordEditDto dto)
        {
            try
            {
                return await _apiService.PutAsync<object>("record", dto);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"更新病历失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 删除病历
        /// </summary>
        public async Task<ApiResponse<object>> DeleteAsync(Guid id)
        {
            try
            {
                return await _apiService.DeleteAsync<object>($"record/{id}");
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"删除病历失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 标记病历为共享
        /// </summary>
        public async Task<ApiResponse<object>> MarkAsSharedAsync(Guid id, List<string> doctorIds)
        {
            try
            {
                return await _apiService.PostAsync<object>($"record/share/{id}", doctorIds);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"共享病历失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 撤销病历共享
        /// </summary>
        public async Task<ApiResponse<object>> RevokeSharingAsync(Guid id)
        {
            try
            {
                return await _apiService.PostAsync<object>($"record/unshare/{id}", new object());
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"撤销病历共享失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取共享给当前医生的病历
        /// </summary>
        public async Task<ApiResponse<List<RecordDto>>> GetSharedRecordsAsync(Guid doctorId)
        {
            try
            {
                return await _apiService.GetAsync<List<RecordDto>>($"record/shared/{doctorId}");
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<RecordDto>>
                {
                    IsSuccess = false,
                    Message = $"获取共享病历失败: {ex.Message}"
                };
            }
        }
    }
}