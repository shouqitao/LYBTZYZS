using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Cashier;

namespace LYBT.Module.Cashier.Interfaces
{
    /// <summary>
    /// 收银服务接口（替代IBillingService）
    /// </summary>
    public interface ICashierService
    {
        /// <summary>
        /// 获取收费记录列表
        /// </summary>
        Task<List<CashierDto>> GetListAsync();

        /// <summary>
        /// 分页获取收费记录列表
        /// </summary>
        Task<PaginatedResult<CashierDto>> GetPagedAsync(PaginationRequest request);

        /// <summary>
        /// 获取收费详情
        /// </summary>
        Task<CashierDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建收费记录
        /// </summary>
        Task<CashierDetailDto> CreateAsync(CashierCreateDto dto);

        /// <summary>
        /// 更新收费记录
        /// </summary>
        Task<bool> UpdateAsync(Guid id, CashierUpdateDto dto);

        /// <summary>
        /// 作废收费记录
        /// </summary>
        Task<bool> VoidAsync(Guid id, string reason);

        /// <summary>
        /// 根据医疗案例ID获取收费记录
        /// </summary>
        Task<CashierDetailDto?> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 根据患者ID获取收费记录
        /// </summary>
        Task<List<CashierDto>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 获取今日收费记录
        /// </summary>
        Task<List<CashierDto>> GetTodayBillsAsync();

        /// <summary>
        /// 获取日期范围内的收费记录
        /// </summary>
        Task<List<CashierDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 计算收费金额
        /// </summary>
        Task<decimal> CalculateAmountAsync(Guid medicalCaseId);

        /// <summary>
        /// 执行付款
        /// </summary>
        Task<PaymentResultDto> ProcessPaymentAsync(Guid id, PaymentDto payment);

        /// <summary>
        /// 打印收费单据
        /// </summary>
        Task<byte[]> PrintReceiptAsync(Guid id);

        /// <summary>
        /// 退费处理
        /// </summary>
        Task<RefundResultDto> ProcessRefundAsync(Guid id, RefundDto refund);

        /// <summary>
        /// 获取收费统计
        /// </summary>
        Task<CashierStatisticsDto> GetStatisticsAsync(DateTime startDate, DateTime endDate);
    }
}