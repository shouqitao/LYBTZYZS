using AutoMapper;
using LYBT.Common.Enums;
using LYBT.Models.Billing;
using LYBT.Module.Billing.Dtos;
using LYBT.Module.Billing.Interfaces;

namespace LYBT.Module.Billing.Services {

    /// <summary>
    /// 费用结算业务服务实现
    /// </summary>
    public class BillingService : IBillingService {
        private readonly IBillingRepository _billingRepository;
        private readonly IMapper _mapper;

        /// <summary>
        /// 构造方法，注入仓储与映射
        /// </summary>
        public BillingService(IBillingRepository billingRepository, IMapper mapper) {
            _billingRepository = billingRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// 获取费用结算详情
        /// </summary>
        public async Task<BillingDetailDto?> GetByIdAsync(Guid id) {
            var model = await _billingRepository.GetByIdAsync(id);
            return model == null ? null : _mapper.Map<BillingDetailDto>(model);
        }

        /// <summary>
        /// 获取费用结算列表
        /// </summary>
        public async Task<List<BillingDto>> GetListAsync() {
            var list = await _billingRepository.GetListAsync();
            return _mapper.Map<List<BillingDto>>(list);
        }

        /// <summary>
        /// 新增费用结算
        /// </summary>
        public async Task<bool> AddAsync(BillingCreateDto billingCreateDto) {
            var model = _mapper.Map<BillingModel>(billingCreateDto);
            model.Id = Guid.NewGuid();
            model.BillingTime = DateTime.Now;
            return await _billingRepository.AddAsync(model);
        }

        /// <summary>
        /// 编辑费用结算
        /// </summary>
        public async Task<bool> UpdateAsync(BillingEditDto billingEditDto) {
            var model = await _billingRepository.GetByIdAsync(billingEditDto.Id);
            if (model == null)
                return false;
            // 仅允许修改部分字段
            model.PaidAmount = billingEditDto.PaidAmount;
            model.BillingTime = billingEditDto.BillingTime;
            model.Remark = billingEditDto.Remark;
            model.Status = billingEditDto.Status;
            return await _billingRepository.UpdateAsync(model);
        }

        /// <summary>
        /// 删除费用结算
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            return await _billingRepository.DeleteAsync(id);
        }

        public async Task<bool> MarkAsPaidAsync(Guid id) {
            var model = await _billingRepository.GetByIdAsync(id);
            if (model == null)
                return false;
            model.Status = BillingStatus.Paid;
            model.PaidTime = DateTime.Now;
            return await _billingRepository.UpdateAsync(model);
        }

        public async Task<bool> MarkAsCompletedAsync(Guid id) {
            var model = await _billingRepository.GetByIdAsync(id);
            if (model == null)
                return false;
            model.Status = BillingStatus.Completed;
            model.CompletedTime = DateTime.Now;
            return await _billingRepository.UpdateAsync(model);
        }

        public async Task<bool> RequestRefundAsync(Guid id, string reason) {
            var model = await _billingRepository.GetByIdAsync(id);
            if (model == null)
                return false;
            model.Status = BillingStatus.RefundRequested;
            model.RefundReason = reason;
            return await _billingRepository.UpdateAsync(model);
        }

        public async Task<bool> ApproveRefundAsync(Guid id) {
            var model = await _billingRepository.GetByIdAsync(id);
            if (model == null)
                return false;
            model.Status = BillingStatus.Refunded;
            model.RefundTime = DateTime.Now;
            return await _billingRepository.UpdateAsync(model);
        }

        public async Task<bool> RejectRefundAsync(Guid id) {
            var model = await _billingRepository.GetByIdAsync(id);
            if (model == null)
                return false;
            model.Status = BillingStatus.Paid;
            model.RefundReason = null;
            return await _billingRepository.UpdateAsync(model);
        }

        public async Task<bool> CancelAsync(Guid id) {
            var model = await _billingRepository.GetByIdAsync(id);
            if (model == null)
                return false;
            model.Status = BillingStatus.Cancelled;
            model.IsDeleted = true;
            return await _billingRepository.UpdateAsync(model);
        }

        public async Task<List<BillingDto>> GetByPatientIdAsync(Guid patientId) {
            var list = await _billingRepository.GetByPatientIdAsync(patientId);
            return _mapper.Map<List<BillingDto>>(list);
        }

        public async Task<List<BillingDto>> SearchAsync(string keyword) {
            var list = await _billingRepository.SearchAsync(keyword);
            return _mapper.Map<List<BillingDto>>(list);
        }

        public async Task<List<BillingDto>> GetRefundableBillsAsync() {
            var list = await _billingRepository.SearchAsync(string.Empty);
            var refundable = list.Where(b => b.Status == BillingStatus.Paid).ToList();
            return _mapper.Map<List<BillingDto>>(refundable);
        }
    }
}