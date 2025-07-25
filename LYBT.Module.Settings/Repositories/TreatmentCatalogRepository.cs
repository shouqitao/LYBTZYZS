using LYBT.Infrastructure;
using LYBT.Module.Settings.Interfaces;
using LYBT.Module.Settings.Models;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Settings.Repositories {

    /// <summary>
    /// 表示TreatmentCatalogRepository。
    /// </summary>
    public class TreatmentCatalogRepository : ITreatmentCatalogRepository {
        private readonly AppDbContext _db;

        public TreatmentCatalogRepository(AppDbContext db) {
            _db = db;
        }

        /// <summary>
        /// 执行GetAllAsync操作。
        /// </summary>
        /// <returns>返回值</returns>
        public async Task<List<TreatmentCatalogModel>> GetAllAsync() {
            return await _db.Set<TreatmentCatalogModel>().ToListAsync();
        }

        /// <summary>
        /// 执行AddAsync操作。
        /// </summary>
        /// <param name="model">参数model</param>
        /// <returns>返回值</returns>
        public async Task<bool> AddAsync(TreatmentCatalogModel model) {
            _db.Add(model);
            return await _db.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 执行UpdateAsync操作。
        /// </summary>
        /// <param name="model">参数model</param>
        /// <returns>返回值</returns>
        public async Task<bool> UpdateAsync(TreatmentCatalogModel model) {
            _db.Update(model);
            return await _db.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 执行DeleteAsync操作。
        /// </summary>
        /// <param name="id">参数id</param>
        /// <returns>返回值</returns>
        public async Task<bool> DeleteAsync(Guid id) {
            var entity = await _db.Set<TreatmentCatalogModel>().FindAsync(id);
            if (entity == null)
                return false;
            _db.Remove(entity);
            return await _db.SaveChangesAsync() > 0;
        }
    }
}