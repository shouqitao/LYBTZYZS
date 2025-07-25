using LYBT.Common.Enums;
using LYBT.Infrastructure;
using LYBT.Module.Billing.Interfaces;
using LYBT.Module.Billing.Models;

namespace LYBT.Module.Billing.Repositories {

    /// <summary>
    /// 费用结算仓储实现类，封装与数据库的交互
    /// </summary>
    public class BillingRepository : IBillingRepository {
        private readonly AppDbContext _appDbContext;

        /// <summary>
        /// 构造函数，注入数据库上下文
        /// </summary>
        public BillingRepository(AppDbContext appDbContext) {
            _appDbContext = appDbContext;
        }

        /// <summary>
        /// 根据ID获取费用结算记录
        /// </summary>
        public async Task<BillingModel?> GetByIdAsync(Guid id) {
            return await _appDbContext.Billings.FindAsync(id);
        }

        /// <summary>
        /// 获取所有费用结算记录
        /// </summary>
        public async Task<List<BillingModel>> GetListAsync() {
            return await Task.FromResult(_appDbContext.Billings.ToList());
        }

        /// <summary>
        /// 新增费用结算记录
        /// </summary>
        public async Task<bool> AddAsync(BillingModel billingModel) {
            _appDbContext.Billings.Add(billingModel);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新费用结算记录
        /// </summary>
        public async Task<bool> UpdateAsync(BillingModel billingModel) {
            _appDbContext.Billings.Update(billingModel);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除费用结算记录
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var billingModel = await _appDbContext.Billings.FindAsync(id);
            if (billingModel == null)
                return false;
            _appDbContext.Billings.Remove(billingModel);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 根据病人ID获取账单列表
        /// </summary>
        public async Task<List<BillingModel>> GetByPatientIdAsync(Guid patientId) {
            var list = _appDbContext.Billings.Where(b => b.PatientId == patientId && !b.IsDeleted).ToList();
            return await Task.FromResult(list);
        }

        /// <summary>
        /// 关键字搜索（病人名、订单号等）
        /// </summary>
        public async Task<List<BillingModel>> SearchAsync(string keyword) {
            var list = _appDbContext.Billings
                .Where(b => (b.BillingId.Contains(keyword) || b.Remark!.Contains(keyword)) && !b.IsDeleted)
                .ToList();
            return await Task.FromResult(list);
        }

        /// <summary>
        /// 根据账单状态筛选记录
        /// </summary>
        /// <param name="status">账单状态</param>
        /// <returns>筛选后的账单列表</returns>
        public async Task<List<BillingModel>> GetByStatusAsync(BillingStatus status) {
            var list = _appDbContext.Billings
                .Where(b => b.Status == status && !b.IsDeleted)
                .ToList();
            return await Task.FromResult(list);
        }
    }
}