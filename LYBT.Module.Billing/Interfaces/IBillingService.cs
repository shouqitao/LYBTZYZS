using LYBT.Module.Billing.Dtos;
using LYBT.Common.Enums;

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

        /// <summary>
        /// 标记已支付
        /// </summary>
        Task<bool> MarkAsPaidAsync(Guid id);

        /// <summary>
        /// 标记完成
        /// </summary>
        Task<bool> MarkAsCompletedAsync(Guid id);

        /// <summary>
        /// 申请退款
        /// </summary>
        Task<bool> RequestRefundAsync(Guid id, string reason);

        /// <summary>
        /// 同意退款
        /// </summary>
        Task<bool> ApproveRefundAsync(Guid id);

        /// <summary>
        /// 拒绝退款
        /// </summary>
        Task<bool> RejectRefundAsync(Guid id);

        /// <summary>
        /// 取消未支付账单
        /// </summary>
        Task<bool> CancelAsync(Guid id);

        /// <summary>
        /// 根据病人ID获取账单
        /// </summary>
        Task<List<BillingDto>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 关键字搜索
        /// </summary>
        Task<List<BillingDto>> SearchAsync(string keyword);

        /// <summary>
        /// 可退款账单
        /// </summary>
        Task<List<BillingDto>> GetRefundableBillsAsync();

        /// <summary>
        /// 根据状态获取账单
        /// </summary>
        Task<List<BillingDto>> GetByStatusAsync(BillingStatus status);
    }
}