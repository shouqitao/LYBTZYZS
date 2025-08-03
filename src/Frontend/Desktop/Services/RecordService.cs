using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Services;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Records;
using LYBT.Shared.Models.Records;
using LYBT.WPF.Client.Core.Interfaces.Services;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 病历服务实现
    /// </summary>
    public class RecordService : IRecordService
    {
        private readonly IApiService _apiService;
        private readonly IRecordApiService _recordApiService;

        public RecordService(IApiService apiService, IRecordApiService recordApiService)
        {
            _apiService = apiService;
            _recordApiService = recordApiService;
        }

        /// <summary>
        /// 获取病历列表
        /// </summary>
        public async Task<ApiResponse<List<RecordDto>>> GetListAsync()
        {
            try
            {
                var response = await _recordApiService.GetRecordsAsync();
                return response;
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
                var response = await _recordApiService.GetPatientRecordsAsync(patientId);
                return response;
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
                var response = await _recordApiService.GetRecordByIdAsync(id);
                return response;
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
                // Dto已经是CreateRecordDto类型，直接使用
                var response = await _recordApiService.CreateRecordAsync(dto);
                return new ApiResponse<object>
                {
                    IsSuccess = response.IsSuccess,
                    Message = response.Message,
                    Data = response.Data
                };
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
                // RecordEditDto包含Id属性
                var response = await _recordApiService.UpdateRecordAsync(dto.Id, dto);
                return new ApiResponse<object>
                {
                    IsSuccess = response.IsSuccess,
                    Message = response.Message,
                    Data = response.Data
                };
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
                var response = await _recordApiService.DeleteRecordAsync(id);
                return new ApiResponse<object>
                {
                    IsSuccess = response.IsSuccess,
                    Message = response.Message,
                    Data = response.Data
                };
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
                var response = await _recordApiService.GetDoctorRecordsAsync(doctorId);
                return response;
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

        /// <summary>
        /// 获取今日病例
        /// </summary>
        public async Task<ApiResponse<List<RecordDto>>> GetTodayRecordsAsync()
        {
            try
            {
                return await _recordApiService.GetTodayRecordsAsync();
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<RecordDto>>
                {
                    IsSuccess = false,
                    Message = $"获取今日病例失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 导出病例
        /// </summary>
        public async Task<ApiResponse<byte[]>> ExportRecordAsync(Guid id, string format = "pdf")
        {
            try
            {
                return await _recordApiService.ExportRecordAsync(id, format);
            }
            catch (Exception ex)
            {
                return new ApiResponse<byte[]>
                {
                    IsSuccess = false,
                    Message = $"导出病例失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取病例统计
        /// </summary>
        public async Task<ApiResponse<RecordStatisticsDto>> GetStatisticsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _recordApiService.GetStatisticsAsync(startDate, endDate);
            }
            catch (Exception ex)
            {
                return new ApiResponse<RecordStatisticsDto>
                {
                    IsSuccess = false,
                    Message = $"获取病例统计失败: {ex.Message}"
                };
            }
        }

    }
}