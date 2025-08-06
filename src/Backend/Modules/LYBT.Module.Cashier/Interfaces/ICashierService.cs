using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Cashier;

namespace LYBT.Module.Cashier.Interfaces
{
    /// <summary>
    /// 收银服务接口
    /// </summary>
    public interface ICashierService
    {
        /// <summary>
        /// 根据ID获取收银记录详情
        /// </summary>
        Task<CashierRecordDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取收银记录列表
        /// </summary>
        Task<List<CashierRecordDto>> GetListAsync();

        /// <summary>
        /// 分页查询收银记录
        /// </summary>
        Task<PaginatedResult<CashierRecordDto>> GetPagedAsync(CashierQueryDto query);

        /// <summary>
        /// 创建收银记录
        /// </summary>
        Task<CashierRecordDetailDto?> CreateAsync(CashierRecordCreateDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 退费处理
        /// </summary>
        Task<bool> RefundAsync(RefundRequestDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 根据医疗案例ID获取收银记录
        /// </summary>
        Task<CashierRecordDetailDto?> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 根据患者ID获取收银记录列表
        /// </summary>
        Task<List<CashierRecordDto>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据收银员ID获取收银记录列表
        /// </summary>
        Task<List<CashierRecordDto>> GetByCashierIdAsync(Guid cashierId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// 获取收银统计
        /// </summary>
        Task<CashierStatisticsDto> GetStatisticsAsync(DateTime startDate, DateTime endDate, Guid? cashierId = null);

        // ==================== 费用计算相关 ====================

        /// <summary>
        /// 预结算 - 计算医疗案例的总费用
        /// </summary>
        Task<BillingSummaryDto> CalculateBillingAsync(Guid medicalCaseId);

        /// <summary>
        /// 验证费用计算是否正确
        /// </summary>
        Task<bool> ValidateBillingAsync(Guid medicalCaseId, decimal expectedAmount);

        // ==================== 支付方式管理 ====================

        /// <summary>
        /// 获取支持的支付方式列表
        /// </summary>
        Task<List<string>> GetPaymentMethodsAsync();

        /// <summary>
        /// 验证支付方式是否有效
        /// </summary>
        Task<bool> ValidatePaymentMethodAsync(string paymentMethod);

        // ==================== 发票管理 ====================

        /// <summary>
        /// 打印发票
        /// </summary>
        Task<InvoiceDto?> PrintInvoiceAsync(Guid cashierRecordId, Guid operatorId, string operatorName);

        /// <summary>
        /// 获取发票信息
        /// </summary>
        Task<InvoiceDto?> GetInvoiceAsync(Guid cashierRecordId);

        /// <summary>
        /// 作废发票
        /// </summary>
        Task<bool> VoidInvoiceAsync(Guid invoiceId, string reason, Guid operatorId, string operatorName);

        // ==================== 日结对账 ====================

        /// <summary>
        /// 执行日结对账
        /// </summary>
        Task<DailySettlementDto?> PerformDailySettlementAsync(Guid cashierId, DateTime settlementDate, Guid operatorId, string operatorName);

        /// <summary>
        /// 获取日结对账记录
        /// </summary>
        Task<DailySettlementDto?> GetDailySettlementAsync(Guid cashierId, DateTime settlementDate);

        /// <summary>
        /// 获取日结对账历史
        /// </summary>
        Task<List<DailySettlementDto>> GetSettlementHistoryAsync(Guid? cashierId = null, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// 审核日结对账
        /// </summary>
        Task<bool> AuditSettlementAsync(Guid settlementId, bool approved, string? remark, Guid operatorId, string operatorName);

        // ==================== 搜索和报表 ====================

        /// <summary>
        /// 搜索收银记录
        /// </summary>
        Task<List<CashierRecordDto>> SearchRecordsAsync(string keyword, int maxResults = 50);

        /// <summary>
        /// 获取收银员工作量统计
        /// </summary>
        Task<Dictionary<Guid, int>> GetCashierWorkloadAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取支付方式使用统计
        /// </summary>
        Task<Dictionary<string, decimal>> GetPaymentMethodUsageAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取退费统计
        /// </summary>
        Task<Dictionary<string, object>> GetRefundStatisticsAsync(DateTime startDate, DateTime endDate);
    }
}