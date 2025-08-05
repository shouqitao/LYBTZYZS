using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Pharmacy;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 药房API服务接口
    /// </summary>
    public interface IPharmacyApiService
    {
        /// <summary>
        /// 获取待抓药的处方列表
        /// </summary>
        [Get("/api/v1/pharmacy/waiting")]
        Task<Refit.ApiResponse<List<PharmacyDto>>> GetWaitingListAsync();

        /// <summary>
        /// 获取药房单列表 (RESTful GET)
        /// </summary>
        [Get("/api/v1/pharmacy")]
        Task<Refit.ApiResponse<PaginatedResult<PharmacyDto>>> GetListAsync(
            [Query] int page = 1,
            [Query] int pageSize = 20,
            [Query] string? keyword = null,
            [Query] string? patientName = null,
            [Query] string? doctorName = null,
            [Query] PharmacyStatus? status = null,
            [Query] DateTime? startDate = null,
            [Query] DateTime? endDate = null,
            [Query] bool? needDecoction = null);

        /// <summary>
        /// 分页获取药房单列表
        /// </summary>
        [Get("/api/v1/pharmacy/paged")]
        Task<Refit.ApiResponse<PaginatedResult<PharmacyDto>>> GetPagedListAsync([Query] PaginationRequest query);

        /// <summary>
        /// 获取药房单详情
        /// </summary>
        [Get("/api/v1/pharmacy/{id}")]
        Task<Refit.ApiResponse<PharmacyDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 新增药房单
        /// </summary>
        [Post("/api/v1/pharmacy")]
        Task<Refit.ApiResponse<object>> AddAsync([Body] PharmacyCreateDto pharmacyCreateDto);

        /// <summary>
        /// 编辑药房单
        /// </summary>
        [Put("/api/v1/pharmacy/{id}")]
        Task<Refit.ApiResponse<object>> UpdateAsync([Body] PharmacyEditDto pharmacyEditDto);

        /// <summary>
        /// 删除药房单
        /// </summary>
        [Delete("/api/v1/pharmacy/{id}")]
        Task<Refit.ApiResponse<object>> DeleteAsync(Guid id);

        /// <summary>
        /// 标记处方为已抓药
        /// </summary>
        [Post("/api/v1/pharmacy/{id}/prepared")]
        Task<Refit.ApiResponse<object>> MarkAsPreparedAsync(Guid id);
    }
}