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
    /// 处方API服务接口
    /// </summary>
    public interface IPrescriptionsApiService
    {
        /// <summary>
        /// 获取处方列表 (RESTful GET)
        /// </summary>
        [Get("/api/v1/prescriptions")]
        Task<Refit.ApiResponse<PaginatedResult<PrescriptionDto>>> GetListAsync(
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
        [Get("/api/v1/prescriptions/paged")]
        Task<Refit.ApiResponse<PaginatedResult<PrescriptionDto>>> GetPagedListAsync([Query] PaginationRequest query);

        /// <summary>
        /// 获取处方详情
        /// </summary>
        [Get("/api/v1/prescriptions/{id}")]
        Task<Refit.ApiResponse<PrescriptionDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 新增处方
        /// </summary>
        [Post("/api/v1/prescriptions")]
        Task<Refit.ApiResponse<object>> AddAsync([Body] PrescriptionCreateDto dto);

        /// <summary>
        /// 编辑处方
        /// </summary>
        [Put("/api/v1/prescriptions")]
        Task<Refit.ApiResponse<object>> UpdateAsync([Body] PrescriptionEditDto dto);

        /// <summary>
        /// 删除处方
        /// </summary>
        [Delete("/api/v1/prescriptions/{id}")]
        Task<Refit.ApiResponse<object>> DeleteAsync(Guid id);

        /// <summary>
        /// 作废处方
        /// </summary>
        [Post("/api/v1/prescriptions/void/{id}")]
        Task<Refit.ApiResponse<object>> CancelAsync(Guid id);
    }
}