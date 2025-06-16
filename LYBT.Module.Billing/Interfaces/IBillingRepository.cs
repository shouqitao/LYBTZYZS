using LYBT.Models; // BillingModel 实体统一存放在 LYBT.Models
using LYBT.Models.Billing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.Module.Billing.Interfaces {
    /// <summary>
    /// 费用结算仓储接口，定义所有数据操作方法
    /// </summary>
    public interface IBillingRepository {
        /// <summary>
        /// 根据费用单ID获取费用结算记录
        /// </summary>
        Task<BillingModel?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取所有费用结算记录列表
        /// </summary>
        Task<List<BillingModel>> GetListAsync();

        /// <summary>
        /// 新增费用结算记录
        /// </summary>
        Task<bool> AddAsync(BillingModel billingModel);

        /// <summary>
        /// 更新费用结算记录
        /// </summary>
        Task<bool> UpdateAsync(BillingModel billingModel);

        /// <summary>
        /// 删除费用结算记录
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
    }
}
