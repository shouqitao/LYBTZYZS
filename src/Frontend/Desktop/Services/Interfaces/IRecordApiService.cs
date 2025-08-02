using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Records;
using ApiResponse = LYBT.Shared.Models.Common.ApiResponse;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 病例API服务接口
    /// </summary>
    public interface IRecordApiService
    {
        /// <summary>
        /// 获取病例列表
        /// </summary>
        [Get("/api/v1/records")]
        Task<ApiResponse<List<RecordDto>>> GetRecordsAsync(
            [Query] string? search = null, 
            [Query] DateTime? startDate = null, 
            [Query] DateTime? endDate = null,
            [Query] Guid? patientId = null,
            [Query] Guid? doctorId = null,
            [Query] int page = 1,
            [Query] int pageSize = 20);

        /// <summary>
        /// 获取病例详情
        /// </summary>
        [Get("/api/v1/records/{id}")]
        Task<ApiResponse<RecordDetailDto>> GetRecordByIdAsync(Guid id);

        /// <summary>
        /// 创建病例
        /// </summary>
        [Post("/api/v1/records")]
        Task<ApiResponse<RecordDto>> CreateRecordAsync([Body] CreateRecordDto dto);

        /// <summary>
        /// 更新病例
        /// </summary>
        [Put("/api/v1/records/{id}")]
        Task<ApiResponse<RecordDto>> UpdateRecordAsync(Guid id, [Body] UpdateRecordDto dto);

        /// <summary>
        /// 删除病例
        /// </summary>
        [Delete("/api/v1/records/{id}")]
        Task<ApiResponse<bool>> DeleteRecordAsync(Guid id);

        /// <summary>
        /// 获取患者病例历史
        /// </summary>
        [Get("/api/v1/records/patient/{patientId}")]
        Task<ApiResponse<List<RecordDto>>> GetPatientRecordsAsync(Guid patientId);

        /// <summary>
        /// 获取医生病例列表
        /// </summary>
        [Get("/api/v1/records/doctor/{doctorId}")]
        Task<ApiResponse<List<RecordDto>>> GetDoctorRecordsAsync(Guid doctorId);

        /// <summary>
        /// 获取今日病例
        /// </summary>
        [Get("/api/v1/records/today")]
        Task<ApiResponse<List<RecordDto>>> GetTodayRecordsAsync();

        /// <summary>
        /// 导出病例
        /// </summary>
        [Get("/api/v1/records/{id}/export")]
        Task<ApiResponse<byte[]>> ExportRecordAsync(Guid id, [Query] string format = "pdf");

        /// <summary>
        /// 获取病例统计
        /// </summary>
        [Get("/api/v1/records/statistics")]
        Task<ApiResponse<RecordStatisticsDto>> GetStatisticsAsync(
            [Query] DateTime startDate, 
            [Query] DateTime endDate);
    }
}