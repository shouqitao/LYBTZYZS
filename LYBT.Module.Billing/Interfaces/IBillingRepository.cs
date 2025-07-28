using LYBT.Common.Enums.System;
using LYBT.Models.Billing;

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

        /// <summary>
        /// 根据病人ID获取账单
        /// </summary>
        Task<List<BillingModel>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 搜索账单
        /// </summary>
        Task<List<BillingModel>> SearchAsync(string keyword);

        /// <summary>
        /// 根据状态获取账单列表
        /// </summary>
        Task<List<BillingModel>> GetByStatusAsync(BillingStatus status);
    }
}