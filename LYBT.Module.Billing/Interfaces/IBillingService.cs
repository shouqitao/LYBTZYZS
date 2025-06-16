using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Module.Billing.Dtos;

namespace LYBT.Module.Billing.Interfaces {
    /// <summary>
    /// 费用结算业务服务接口
    /// </summary>
    public interface IBillingService {
        /// <summary>
        /// 根据ID获取费用结算详情
        /// </summary>
        Task<BillingDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取费用结算列表
        /// </summary>
        Task<List<BillingDto>> GetListAsync();

        /// <summary>
        /// 新增费用结算记录
        /// </summary>
        Task<bool> AddAsync(BillingCreateDto billingCreateDto);

        /// <summary>
        /// 编辑费用结算记录
        /// </summary>
        Task<bool> UpdateAsync(BillingEditDto billingEditDto);

        /// <summary>
        /// 删除费用结算记录
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
    }
}
