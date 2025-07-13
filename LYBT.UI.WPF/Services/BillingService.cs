using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Module.Billing.Dtos;
using LYBT.UI.WPF.Apis;
using LYBT.UI.WPF.Interfaces;
using LYBT.Common.Enums;
using System.Linq;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 类 BillingService 的说明
    /// </summary>
    public class BillingService : IBillingService {
        private readonly IBillingApi _billingApi;
        public BillingService(IBillingApi billingApi) {
            _billingApi = billingApi;
        }
        /// <summary>
        /// 方法 GetAllAsync 的说明
        /// </summary>
        public async Task<IList<BillingDto>> GetAllAsync() {
            return await _billingApi.GetListAsync();
        }
        /// <summary>
        /// 方法 GetByIdAsync 的说明
        /// </summary>
        public async Task<BillingDetailDto?> GetByIdAsync(Guid id) {
            return await _billingApi.GetByIdAsync(id);
        }
        /// <summary>
        /// 方法 AddAsync 的说明
        /// </summary>
        public async Task<bool> AddAsync(BillingCreateDto dto) {
            var resp = await _billingApi.AddAsync(dto);
            return resp.Success;
        }
        /// <summary>
        /// 方法 UpdateAsync 的说明
        /// </summary>
        public async Task<bool> UpdateAsync(BillingEditDto dto) {
            var resp = await _billingApi.UpdateAsync(dto);
            return resp.Success;
        }
        /// <summary>
        /// 方法 DeleteAsync 的说明
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var resp = await _billingApi.DeleteAsync(id);
            return resp.Success;
        }
        /// <summary>
        /// 方法 MarkAsPaidAsync 的说明
        /// </summary>
        public async Task<bool> MarkAsPaidAsync(Guid id) {
            var resp = await _billingApi.MarkAsPaidAsync(id);
            return resp.Success;
        }
        /// <summary>
        /// 方法 MarkAsCompletedAsync 的说明
        /// </summary>
        public async Task<bool> MarkAsCompletedAsync(Guid id) {
            var resp = await _billingApi.MarkAsCompletedAsync(id);
            return resp.Success;
        }
        /// <summary>
        /// 方法 RequestRefundAsync 的说明
        /// </summary>
        public async Task<bool> RequestRefundAsync(Guid id, string reason) {
            var resp = await _billingApi.RequestRefundAsync(id, reason);
            return resp.Success;
        }
        /// <summary>
        /// 方法 ApproveRefundAsync 的说明
        /// </summary>
        public async Task<bool> ApproveRefundAsync(Guid id) {
            var resp = await _billingApi.ApproveRefundAsync(id);
            return resp.Success;
        }
        /// <summary>
        /// 方法 RejectRefundAsync 的说明
        /// </summary>
        public async Task<bool> RejectRefundAsync(Guid id) {
            var resp = await _billingApi.RejectRefundAsync(id);
            return resp.Success;
        }
        /// <summary>
        /// 方法 CancelAsync 的说明
        /// </summary>
        public async Task<bool> CancelAsync(Guid id) {
            var resp = await _billingApi.CancelAsync(id);
            return resp.Success;
        }
        /// <summary>
        /// 方法 GetByPatientIdAsync 的说明
        /// </summary>
        public async Task<IList<BillingDto>> GetByPatientIdAsync(Guid patientId) {
            return await _billingApi.GetByPatientIdAsync(patientId);
        }
        /// <summary>
        /// 方法 SearchAsync 的说明
        /// </summary>
        public async Task<IList<BillingDto>> SearchAsync(string keyword) {
            return await _billingApi.SearchAsync(keyword);
        }
        /// <summary>
        /// 方法 GetRefundableBillsAsync 的说明
        /// </summary>
        public async Task<IList<BillingDto>> GetRefundableBillsAsync() {
            return await _billingApi.GetRefundableBillsAsync();
        }

        public async Task<IList<BillingDto>> GetByStatusAsync(BillingStatus status) {
            var list = await _billingApi.GetListAsync();
            return list.Where(b => b.Status == status).ToList();
        }
    }
}
