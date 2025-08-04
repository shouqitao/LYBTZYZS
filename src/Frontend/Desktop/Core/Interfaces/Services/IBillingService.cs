using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models.Billing;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.Shared.Models.Contracts.Billing;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Core.Interfaces.Services
{
    /// <summary>
    /// 账单服务接口
    /// </summary>
    public interface IBillingService
    {
        /// <summary>
        /// 获取今日账单记录
        /// </summary>
        Task<List<BillingInfo>> GetTodayBillingsAsync();

        /// <summary>
        /// 分页搜索账单记录
        /// </summary>
        Task<PagedResult<BillingInfo>> SearchBillingsAsync(BillingPagedQueryDto queryDto);

        /// <summary>
        /// 获取账单列表（简单查询）
        /// </summary>
        Task<List<BillingInfo>> GetBillingsAsync(DateTime? startDate = null, DateTime? endDate = null, BillingStatus? status = null);

        /// <summary>
        /// 创建账单记录
        /// </summary>
        Task<bool> CreateBillingAsync(BillingCreateDto billingDto);

        /// <summary>
        /// 收费
        /// </summary>
        Task<bool> ChargeAsync(Guid billingId, decimal actualAmount, string paymentMethod);

        /// <summary>
        /// 退费
        /// </summary>
        Task<bool> RefundAsync(Guid billingId, decimal refundAmount, string reason);

        /// <summary>
        /// 取消账单
        /// </summary>
        Task<bool> CancelAsync(Guid billingId, string reason);

        /// <summary>
        /// 获取账单详情
        /// </summary>
        Task<BillingInfo?> GetBillingDetailAsync(Guid billingId);

        /// <summary>
        /// 批量获取账单详情
        /// </summary>
        Task<List<BillingInfo>> GetBillingDetailsAsync(List<Guid> billingIds);

        /// <summary>
        /// 导出账单记录
        /// </summary>
        Task<bool> ExportBillingsAsync(List<BillingInfo> billings, string filePath);

        /// <summary>
        /// 打印账单
        /// </summary>
        Task<bool> PrintBillingAsync(Guid billingId);

        /// <summary>
        /// 批量打印账单
        /// </summary>
        Task<bool> PrintBillingsAsync(List<Guid> billingIds);

        /// <summary>
        /// 获取支付方式列表
        /// </summary>
        Task<List<string>> GetPaymentMethodsAsync();

        /// <summary>
        /// 获取账单类型列表
        /// </summary>
        Task<List<string>> GetBillingTypesAsync();
    }
}