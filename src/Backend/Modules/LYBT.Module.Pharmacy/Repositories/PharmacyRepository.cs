using LYBT.Common.Enums.System;
using LYBT.Models.Pharmacy;
using LYBT.Infrastructure.Data;
using LYBT.Module.Pharmacy.Interfaces;

namespace LYBT.Module.Pharmacy.Repositories {

    /// <summary>
    /// 药房仓储实现类，封装与数据库的交互
    /// </summary>
    public class PharmacyRepository : IPharmacyRepository {
        private readonly AppDbContext _context;

        /// <summary>
        /// 构造函数，注入数据库上下文
        /// </summary>
        public PharmacyRepository(AppDbContext context) {
            _context = context;
        }

        /// <summary>
        /// 根据ID获取药房记录
        /// </summary>
        public async Task<PharmacyModel?> GetByIdAsync(Guid id) {
            return await _context.Pharmacies.FindAsync(id);
        }

        /// <summary>
        /// 获取所有药房记录
        /// </summary>
        public async Task<List<PharmacyModel>> GetListAsync() {
            return await Task.FromResult(_context.Pharmacies.ToList());
        }

        /// <summary>
        /// 新增药房记录
        /// </summary>
        public async Task<bool> AddAsync(PharmacyModel pharmacyModel) {
            _context.Pharmacies.Add(pharmacyModel);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新药房记录
        /// </summary>
        public async Task<bool> UpdateAsync(PharmacyModel pharmacyModel) {
            _context.Pharmacies.Update(pharmacyModel);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除药房记录
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var pharmacyModel = await _context.Pharmacies.FindAsync(id);
            if (pharmacyModel == null)
                return false;
            _context.Pharmacies.Remove(pharmacyModel);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 根据状态获取药房记录列表
        /// </summary>
        public async Task<List<PharmacyModel>> GetByStatusAsync(PharmacyStatus status) {
            return await Task.FromResult(_context.Pharmacies
                .Where(p => p.Status == status)
                .ToList());
        }
    }
}