using LYBT.Models.Cashier;

namespace LYBT.Module.Cashier.Interfaces
{
    /// <summary>
    /// 收银仓储接口（替代IBillingRepository）
    /// </summary>
    public interface ICashierRepository
    {
        /// <summary>
        /// 获取所有收费记录
        /// </summary>
        Task<List<CashierModel>> GetListAsync();

        /// <summary>
        /// 根据ID获取收费记录
        /// </summary>
        Task<CashierModel?> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建收费记录
        /// </summary>
        Task<CashierModel> CreateAsync(CashierModel model);

        /// <summary>
        /// 更新收费记录
        /// </summary>
        Task<bool> UpdateAsync(CashierModel model);

        /// <summary>
        /// 删除收费记录
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 根据医疗案例ID获取收费记录
        /// </summary>
        Task<CashierModel?> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 根据患者ID获取收费记录列表
        /// </summary>
        Task<List<CashierModel>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据日期范围获取收费记录列表
        /// </summary>
        Task<List<CashierModel>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 根据支付状态获取收费记录列表
        /// </summary>
        Task<List<CashierModel>> GetByPaymentStatusAsync(PaymentStatus status);

        /// <summary>
        /// 根据发票号获取收费记录
        /// </summary>
        Task<CashierModel?> GetByInvoiceNumberAsync(string invoiceNumber);
    }
}