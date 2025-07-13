using LYBT.Module.Billing.Dtos;
using LYBT.Common.Models;
using Refit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Apis {
    public interface IBillingApi {
        [Get("/api/Billing")] 
        Task<List<BillingDto>> GetListAsync();

        [Get("/api/Billing/{id}")]
        Task<BillingDetailDto> GetByIdAsync(Guid id);

        [Post("/api/Billing")]
        Task<ApiSuccessResponse> AddAsync([Body] BillingCreateDto dto);

        [Put("/api/Billing")]
        Task<ApiSuccessResponse> UpdateAsync([Body] BillingEditDto dto);

        [Delete("/api/Billing/{id}")]
        Task<ApiSuccessResponse> DeleteAsync(Guid id);

        [Post("/api/Billing/mark-paid/{id}")]
        Task<ApiSuccessResponse> MarkAsPaidAsync(Guid id);

        [Post("/api/Billing/complete/{id}")]
        Task<ApiSuccessResponse> MarkAsCompletedAsync(Guid id);

        [Post("/api/Billing/request-refund/{id}")]
        Task<ApiSuccessResponse> RequestRefundAsync(Guid id, [Body] string reason);

        [Post("/api/Billing/approve-refund/{id}")]
        Task<ApiSuccessResponse> ApproveRefundAsync(Guid id);

        [Post("/api/Billing/reject-refund/{id}")]
        Task<ApiSuccessResponse> RejectRefundAsync(Guid id);

        [Post("/api/Billing/cancel/{id}")]
        Task<ApiSuccessResponse> CancelAsync(Guid id);

        [Get("/api/Billing/patient/{patientId}")]
        Task<List<BillingDto>> GetByPatientIdAsync(Guid patientId);

        [Get("/api/Billing/search")]
        Task<List<BillingDto>> SearchAsync([Query] string keyword);

        [Get("/api/Billing/refundable")]
        Task<List<BillingDto>> GetRefundableBillsAsync();

        [Get("/api/Billing/status/{status}")]
        Task<List<BillingDto>> GetByStatusAsync(int status);
    }
}
