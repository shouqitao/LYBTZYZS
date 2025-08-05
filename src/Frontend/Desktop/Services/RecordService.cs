using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Services;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.WPF.Client.Core.Models;
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
        public async Task<ServiceResult<List<RecordDto>>> GetListAsync()
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _recordApiService.GetRecordsAsync()
            );
        }

        /// <summary>
        /// 根据患者ID获取病历列表
        /// </summary>
        public async Task<ServiceResult<List<RecordDto>>> GetByPatientIdAsync(Guid patientId)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _recordApiService.GetPatientRecordsAsync(patientId)
            );
        }

        /// <summary>
        /// 获取病历详情
        /// </summary>
        public async Task<ServiceResult<RecordDetailDto>> GetByIdAsync(Guid id)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _recordApiService.GetRecordByIdAsync(id)
            );
        }

        /// <summary>
        /// 新增病历
        /// </summary>
        public async Task<ServiceResult> AddAsync(RecordCreateDto dto)
        {
            var result = await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _recordApiService.CreateRecordAsync(dto)
            );
            return result.IsSuccess ? ServiceResult.Success() : ServiceResult.Failure(result.ErrorMessage ?? "操作失败", result.Exception);
        }

        /// <summary>
        /// 编辑病历
        /// </summary>
        public async Task<ServiceResult> UpdateAsync(RecordEditDto dto)
        {
            var result = await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _recordApiService.UpdateRecordAsync(dto.Id, dto)
            );
            return result.IsSuccess ? ServiceResult.Success() : ServiceResult.Failure(result.ErrorMessage ?? "操作失败", result.Exception);
        }

        /// <summary>
        /// 删除病历
        /// </summary>
        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            var result = await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _recordApiService.DeleteRecordAsync(id)
            );
            return result.IsSuccess ? ServiceResult.Success() : ServiceResult.Failure(result.ErrorMessage ?? "操作失败", result.Exception);
        }

        /// <summary>
        /// 标记病历为共享
        /// </summary>
        public async Task<ServiceResult> MarkAsSharedAsync(Guid id, List<string> doctorIds)
        {
            try
            {
                var response = await _apiService.PostAsync<object>($"record/share/{id}", doctorIds);
                if (response.IsSuccess)
                {
                    return ServiceResult.Success();
                }
                else
                {
                    return ServiceResult.Failure(response.ErrorMessage ?? "共享病历失败");
                }
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"共享病历失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 撤销病历共享
        /// </summary>
        public async Task<ServiceResult> RevokeSharingAsync(Guid id)
        {
            try
            {
                var response = await _apiService.PostAsync<object>($"record/unshare/{id}", new object());
                if (response.IsSuccess)
                {
                    return ServiceResult.Success();
                }
                else
                {
                    return ServiceResult.Failure(response.ErrorMessage ?? "撤销病历共享失败");
                }
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"撤销病历共享失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取共享给当前医生的病历
        /// </summary>
        public async Task<ServiceResult<List<RecordDto>>> GetSharedRecordsAsync(Guid doctorId)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _recordApiService.GetDoctorRecordsAsync(doctorId)
            );
        }

        /// <summary>
        /// 获取今日病例
        /// </summary>
        public async Task<ServiceResult<List<RecordDto>>> GetTodayRecordsAsync()
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _recordApiService.GetTodayRecordsAsync()
            );
        }

        /// <summary>
        /// 导出病例
        /// </summary>
        public async Task<ServiceResult<byte[]>> ExportRecordAsync(Guid id, string format = "pdf")
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _recordApiService.ExportRecordAsync(id, format)
            );
        }

        /// <summary>
        /// 获取病例统计
        /// </summary>
        public async Task<ServiceResult<RecordStatisticsDto>> GetStatisticsAsync(DateTime startDate, DateTime endDate)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _recordApiService.GetStatisticsAsync(startDate, endDate)
            );
        }

    }
}