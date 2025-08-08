using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Registration.Services.Interfaces
{
    /// <summary>
    /// 挂号管理API服务接口 - Refit定义
    /// 挂号功能通过MedicalCase API实现
    /// </summary>
    public interface IRegistrationApiService
    {
        /// <summary>
        /// 分页查询今日挂号记录
        /// </summary>
        [Get("/api/v1/MedicalCase")]
        Task<ApiResponse<PaginatedResult<MedicalCaseDto>>> GetTodayRegistrationsAsync(
            [Query] int pageIndex = 1,
            [Query] int pageSize = 20,
            [Query] DateTime? date = null);

        /// <summary>
        /// 根据ID获取挂号详情
        /// </summary>
        [Get("/api/v1/MedicalCase/{id}")]
        Task<ApiResponse<MedicalCaseDetailDto>> GetRegistrationDetailAsync(Guid id);

        /// <summary>
        /// 创建新挂号
        /// </summary>
        [Post("/api/v1/MedicalCase")]
        Task<ApiResponse<MedicalCaseDto>> CreateRegistrationAsync([Body] MedicalCaseCreateDto createDto);

        /// <summary>
        /// 获取患者的挂号历史
        /// </summary>
        [Get("/api/v1/MedicalCase/patient/{patientId}")]
        Task<ApiResponse<List<MedicalCaseDto>>> GetPatientRegistrationsAsync(Guid patientId);

        /// <summary>
        /// 获取医生今日的挂号列表
        /// </summary>
        [Get("/api/v1/MedicalCase/user/{userId}/today")]
        Task<ApiResponse<List<MedicalCaseDto>>> GetDoctorTodayRegistrationsAsync(Guid userId);

        /// <summary>
        /// 更新挂号状态（开始看诊、取消等）
        /// </summary>
        [Put("/api/v1/MedicalCase/{id}/status")]
        Task<ApiResponse<bool>> UpdateRegistrationStatusAsync(Guid id, [Body] MedicalCaseStatus status);

        /// <summary>
        /// 获取排队信息
        /// </summary>
        [Get("/api/v1/Queueing/medicalCase/{medicalCaseId}")]
        Task<ApiResponse<object>> GetQueueInfoAsync(Guid medicalCaseId);

        /// <summary>
        /// 创建排队记录
        /// </summary>
        [Post("/api/v1/Queueing")]
        Task<ApiResponse<object>> CreateQueueEntryAsync([Body] object queueEntry);

        /// <summary>
        /// 叫号
        /// </summary>
        [Put("/api/v1/Queueing/{id}/call")]
        Task<ApiResponse<bool>> CallNumberAsync(Guid id);
    }
}