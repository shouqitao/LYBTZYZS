using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 医疗案例API服务接口 - Refit定义
    /// </summary>
    public interface IMedicalCaseApiService
    {
        /// <summary>
        /// 分页查询医疗案例
        /// </summary>
        [Get("/api/v1/MedicalCase")]
        Task<Refit.ApiResponse<PaginatedResult<MedicalCaseDto>>> GetPagedAsync(
            [Query] int pageIndex = 1, 
            [Query] int pageSize = 20);

        /// <summary>
        /// 根据ID获取医疗案例详情
        /// </summary>
        [Get("/api/v1/MedicalCase/{id}")]
        Task<Refit.ApiResponse<MedicalCaseDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建医疗案例
        /// </summary>
        [Post("/api/v1/MedicalCase")]
        Task<Refit.ApiResponse<MedicalCaseDto>> CreateAsync([Body] MedicalCaseCreateDto createDto);

        /// <summary>
        /// 更新医疗案例
        /// </summary>
        [Put("/api/v1/MedicalCase")]
        Task<Refit.ApiResponse<bool>> UpdateAsync([Body] MedicalCaseEditDto editDto);

        /// <summary>
        /// 获取患者的医疗案例列表
        /// </summary>
        [Get("/api/v1/MedicalCase/patient/{patientId}")]
        Task<Refit.ApiResponse<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 获取今日医疗案例列表
        /// </summary>
        [Get("/api/v1/MedicalCase/user/{userId}/today")]
        Task<Refit.ApiResponse<List<MedicalCaseDto>>> GetTodayByUserIdAsync(Guid userId);

        /// <summary>
        /// 更新医疗案例状态
        /// </summary>
        [Put("/api/v1/MedicalCase/{id}/status")]
        Task<Refit.ApiResponse<bool>> UpdateStatusAsync(Guid id, [Body] MedicalCaseStatus status);

        /// <summary>
        /// 删除医疗案例（软删除）
        /// </summary>
        [Delete("/api/v1/MedicalCase/{id}")]
        Task<Refit.ApiResponse<bool>> DeleteAsync(Guid id);
    }
}