using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Billing;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 费用结算API服务接口
    /// </summary>
    public interface IBillingApiService
    {
        /// <summary>
        /// 获取费用结算列表 (RESTful GET)
        /// </summary>
        [Get("/api/v1/billing")]
        Task<Refit.ApiResponse<PaginatedResult<BillingDto>>> GetListAsync(
            [Query] int page = 1,
            [Query] int pageSize = 20,
            [Query] string? keyword = null,
            [Query] string? billingId = null,
            [Query] string? patientName = null,
            [Query] string? doctorName = null,
            [Query] BillingStatus? status = null,
            [Query] string? paymentMethod = null,
            [Query] DateTime? startDate = null,
            [Query] DateTime? endDate = null,
            [Query] decimal? minAmount = null,
            [Query] decimal? maxAmount = null);

        /// <summary>
        /// 分页获取费用结算列表
        /// </summary>
        [Get("/api/v1/billing/paged")]
        Task<Refit.ApiResponse<PaginatedResult<BillingDto>>> GetPagedListAsync([Query] PaginationRequest query);

        /// <summary>
        /// 获取费用结算详情
        /// </summary>
        [Get("/api/v1/billing/{id}")]
        Task<Refit.ApiResponse<BillingDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 新增费用结算
        /// </summary>
        [Post("/api/v1/billing")]
        Task<Refit.ApiResponse<object>> AddAsync([Body] BillingCreateDto billingCreateDto);

        /// <summary>
        /// 编辑费用结算
        /// </summary>
        [Put("/api/v1/billing/{id}")]
        Task<Refit.ApiResponse<object>> UpdateAsync([Body] BillingEditDto billingEditDto);

        /// <summary>
        /// 删除费用结算
        /// </summary>
        [Delete("/api/v1/billing/{id}")]
        Task<Refit.ApiResponse<object>> DeleteAsync(Guid id);

        /// <summary>
        /// 标记为已付款
        /// </summary>
        [Patch("/api/v1/billing/{id}/paid")]
        Task<Refit.ApiResponse<object>> MarkAsPaidAsync(Guid id);

        /// <summary>
        /// 标记为已完成
        /// </summary>
        [Patch("/api/v1/billing/{id}/completed")]
        Task<Refit.ApiResponse<object>> MarkAsCompletedAsync(Guid id);

        /// <summary>
        /// 申请退款
        /// </summary>
        [Post("/api/v1/billing/request-refund/{id}")]
        Task<Refit.ApiResponse<object>> RequestRefundAsync(Guid id, [Body] string reason);

        /// <summary>
        /// 批准退款
        /// </summary>
        [Post("/api/v1/billing/approve-refund/{id}")]
        Task<Refit.ApiResponse<object>> ApproveRefundAsync(Guid id);

        /// <summary>
        /// 拒绝退款
        /// </summary>
        [Post("/api/v1/billing/reject-refund/{id}")]
        Task<Refit.ApiResponse<object>> RejectRefundAsync(Guid id);

        /// <summary>
        /// 取消费用结算
        /// </summary>
        [Post("/api/v1/billing/cancel/{id}")]
        Task<Refit.ApiResponse<object>> CancelAsync(Guid id);

        /// <summary>
        /// 根据患者ID获取费用结算
        /// </summary>
        [Get("/api/v1/billing/patient/{patientId}")]
        Task<Refit.ApiResponse<List<BillingDto>>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 搜索费用结算
        /// </summary>
        [Get("/api/v1/billing/search")]
        Task<Refit.ApiResponse<List<BillingDto>>> SearchAsync([Query] string keyword = "");

        /// <summary>
        /// 获取可退款费用结算
        /// </summary>
        [Get("/api/v1/billing/refundable")]
        Task<Refit.ApiResponse<List<BillingDto>>> GetRefundableBillsAsync();

        /// <summary>
        /// 根据状态获取费用结算
        /// </summary>
        [Get("/api/v1/billing/status/{status}")]
        Task<Refit.ApiResponse<List<BillingDto>>> GetByStatusAsync(BillingStatus status);
    }
}