using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Module.Billing.Dtos;

namespace LYBT.UI.WPF.Interfaces {
    public interface IBillingService {
        Task<IList<BillingDto>> GetAllAsync();
        Task<BillingDetailDto?> GetByIdAsync(Guid id);
        Task<bool> AddAsync(BillingCreateDto dto);
        Task<bool> UpdateAsync(BillingEditDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> MarkAsPaidAsync(Guid id);
        Task<bool> MarkAsCompletedAsync(Guid id);
        Task<bool> RequestRefundAsync(Guid id, string reason);
        Task<bool> ApproveRefundAsync(Guid id);
        Task<bool> RejectRefundAsync(Guid id);
        Task<bool> CancelAsync(Guid id);
        Task<IList<BillingDto>> GetByPatientIdAsync(Guid patientId);
        Task<IList<BillingDto>> SearchAsync(string keyword);
        Task<IList<BillingDto>> GetRefundableBillsAsync();
        Task<IList<BillingDto>> GetByStatusAsync(int status);
    }
}
