using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Module.Billing.Dtos;
using LYBT.UI.WPF.Services.Api;

namespace LYBT.UI.WPF.Services {
    public class BillingService : IBillingService {
        private readonly IBillingApi _billingApi;
        public BillingService(IBillingApi billingApi) {
            _billingApi = billingApi;
        }
        public async Task<IList<BillingDto>> GetAllAsync() {
            return await _billingApi.GetListAsync();
        }
        public async Task<BillingDetailDto?> GetByIdAsync(Guid id) {
            return await _billingApi.GetByIdAsync(id);
        }
        public async Task<bool> AddAsync(BillingCreateDto dto) {
            var resp = await _billingApi.AddAsync(dto);
            return resp.Success;
        }
        public async Task<bool> UpdateAsync(BillingEditDto dto) {
            var resp = await _billingApi.UpdateAsync(dto);
            return resp.Success;
        }
        public async Task<bool> DeleteAsync(Guid id) {
            var resp = await _billingApi.DeleteAsync(id);
            return resp.Success;
        }
        public async Task<bool> MarkAsPaidAsync(Guid id) {
            var resp = await _billingApi.MarkAsPaidAsync(id);
            return resp.Success;
        }
        public async Task<bool> MarkAsCompletedAsync(Guid id) {
            var resp = await _billingApi.MarkAsCompletedAsync(id);
            return resp.Success;
        }
        public async Task<bool> RequestRefundAsync(Guid id, string reason) {
            var resp = await _billingApi.RequestRefundAsync(id, reason);
            return resp.Success;
        }
        public async Task<bool> ApproveRefundAsync(Guid id) {
            var resp = await _billingApi.ApproveRefundAsync(id);
            return resp.Success;
        }
        public async Task<bool> RejectRefundAsync(Guid id) {
            var resp = await _billingApi.RejectRefundAsync(id);
            return resp.Success;
        }
        public async Task<bool> CancelAsync(Guid id) {
            var resp = await _billingApi.CancelAsync(id);
            return resp.Success;
        }
        public async Task<IList<BillingDto>> GetByPatientIdAsync(Guid patientId) {
            return await _billingApi.GetByPatientIdAsync(patientId);
        }
        public async Task<IList<BillingDto>> SearchAsync(string keyword) {
            return await _billingApi.SearchAsync(keyword);
        }
        public async Task<IList<BillingDto>> GetRefundableBillsAsync() {
            return await _billingApi.GetRefundableBillsAsync();
        }
    }
}
