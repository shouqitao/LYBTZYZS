using AutoMapper;
using LYBT.Models;
using LYBT.Models.Billing;
using LYBT.Module.Billing.Dtos;
using LYBT.Module.Billing.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            return await _billingRepository.UpdateAsync(model);
        }

        /// <summary>
        /// 删除费用结算
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            return await _billingRepository.DeleteAsync(id);
        }
    }
}
