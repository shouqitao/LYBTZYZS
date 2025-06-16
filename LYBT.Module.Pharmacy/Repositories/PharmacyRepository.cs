using LYBT.Infrastructure;
using LYBT.Models;
using LYBT.Models.Pharmacy;
using LYBT.Module.Pharmacy.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LYBT.Module.Pharmacy.Repositories {
    /// <summary>
    /// 药房仓储实现类，封装与数据库的交互
    /// </summary>
    public class PharmacyRepository : IPharmacyRepository {
        private readonly AppDbContext _appDbContext;

        /// <summary>
        /// 构造函数，注入数据库上下文
        /// </summary>
        public PharmacyRepository(AppDbContext appDbContext) {
            _appDbContext = appDbContext;
        }

        /// <summary>
        /// 根据ID获取药房记录
        /// </summary>
        public async Task<PharmacyModel?> GetByIdAsync(Guid id) {
            return await _appDbContext.Pharmacies.FindAsync(id);
        }

        /// <summary>
        /// 获取所有药房记录
        /// </summary>
        public async Task<List<PharmacyModel>> GetListAsync() {
            return await Task.FromResult(_appDbContext.Pharmacies.ToList());
        }

        /// <summary>
        /// 新增药房记录
        /// </summary>
        public async Task<bool> AddAsync(PharmacyModel pharmacyModel) {
            _appDbContext.Pharmacies.Add(pharmacyModel);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新药房记录
        /// </summary>
        public async Task<bool> UpdateAsync(PharmacyModel pharmacyModel) {
            _appDbContext.Pharmacies.Update(pharmacyModel);
            return await _appDbContext.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除药房记录
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var pharmacyModel = await _appDbContext.Pharmacies.FindAsync(id);
            if (pharmacyModel == null)
                return false;
            _appDbContext.Pharmacies.Remove(pharmacyModel);
            return await _appDbContext.SaveChangesAsync() > 0;
        }
    }
}
