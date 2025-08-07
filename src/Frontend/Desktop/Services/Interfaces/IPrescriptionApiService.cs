using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 处方API服务接口 - 统一的处方数据访问接口
    /// </summary>
    public interface IPrescriptionApiService
    {
        /// <summary>
        /// 获取处方列表 (RESTful GET)
        /// </summary>
        [Get("/api/v1/Prescriptions")]
        Task<ApiResponse<PaginatedResult<PrescriptionDto>>> GetListAsync(
            [Query] int page = 1,
            [Query] int pageSize = 20,
            [Query] string? keyword = null,
            [Query] string? patientName = null,
            [Query] string? doctorName = null,
            [Query] string? diagnosis = null,
            [Query] PrescriptionStatus? status = null,
            [Query] DateTime? startDate = null,
            [Query] DateTime? endDate = null,
            [Query] int? minDosageCount = null,
            [Query] int? maxDosageCount = null);

        /// <summary>
        /// 分页获取处方列表
        /// </summary>
        [Get("/api/v1/Prescriptions/paged")]
        Task<ApiResponse<PaginatedResult<PrescriptionDto>>> GetPagedListAsync([Query] PaginationRequest query);

        /// <summary>
        /// 获取处方详情
        /// </summary>
        [Get("/api/v1/Prescriptions/{id}")]
        Task<ApiResponse<PrescriptionDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建处方
        /// </summary>
        [Post("/api/v1/Prescriptions")]
        Task<ApiResponse<PrescriptionDto>> CreatePrescriptionAsync([Body] PrescriptionCreateDto dto);

        /// <summary>
        /// 更新处方
        /// </summary>
        [Put("/api/v1/Prescriptions")]
        Task<ApiResponse<PrescriptionDto>> UpdatePrescriptionAsync([Body] PrescriptionEditDto dto);

        /// <summary>
        /// 删除处方
        /// </summary>
        [Delete("/api/v1/Prescriptions/{id}")]
        Task<ApiResponse<bool>> DeletePrescriptionAsync(Guid id);

        /// <summary>
        /// 作废处方
        /// </summary>
        [Post("/api/v1/Prescriptions/void/{id}")]
        Task<ApiResponse<PrescriptionDto>> CancelPrescriptionAsync(Guid id);

        // 为了保持兼容性，提供别名方法
        /// <summary>
        /// 获取处方详情（别名方法）
        /// </summary>
        Task<ApiResponse<PrescriptionDetailDto>> GetPrescriptionAsync(Guid id) => GetByIdAsync(id);

        /// <summary>
        /// 获取处方列表（别名方法）
        /// </summary>
        Task<ApiResponse<PaginatedResult<PrescriptionDto>>> GetPrescriptionsAsync(
            int page = 1,
            int pageSize = 20,
            string? keyword = null,
            string? patientName = null,
            string? doctorName = null,
            string? diagnosis = null) => GetListAsync(page, pageSize, keyword, patientName, doctorName, diagnosis);

        /// <summary>
        /// 删除处方（别名方法）
        /// </summary>
        Task<ApiResponse<bool>> DeleteAsync(Guid id) => DeletePrescriptionAsync(id);

        /// <summary>
        /// 作废处方（别名方法）
        /// </summary>
        Task<ApiResponse<PrescriptionDto>> CancelAsync(Guid id) => CancelPrescriptionAsync(id);
    }
}