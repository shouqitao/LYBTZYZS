using LYBT.Infrastructure;
using LYBT.Models.Settings;
using LYBT.Module.Settings.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Settings.Repositories {

/// <summary>
/// 表示DiagnosisCatalogRepository。
/// </summary>
    public class DiagnosisCatalogRepository : IDiagnosisCatalogRepository {
        private readonly AppDbContext _db;

        public DiagnosisCatalogRepository(AppDbContext db) {
            _db = db;
        }

/// <summary>
/// 执行GetAllAsync操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<List<DiagnosisCatalogModel>> GetAllAsync() {
            return await _db.Set<DiagnosisCatalogModel>().ToListAsync();
        }

/// <summary>
/// 执行AddAsync操作。
/// </summary>
/// <param name="model">参数model</param>
/// <returns>返回值</returns>
        public async Task<bool> AddAsync(DiagnosisCatalogModel model) {
            _db.Add(model);
            return await _db.SaveChangesAsync() > 0;
        }

/// <summary>
/// 执行UpdateAsync操作。
/// </summary>
/// <param name="model">参数model</param>
/// <returns>返回值</returns>
        public async Task<bool> UpdateAsync(DiagnosisCatalogModel model) {
            _db.Update(model);
            return await _db.SaveChangesAsync() > 0;
        }

/// <summary>
/// 执行DeleteAsync操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<bool> DeleteAsync(Guid id) {
            var entity = await _db.Set<DiagnosisCatalogModel>().FindAsync(id);
            if (entity == null)
                return false;
            _db.Remove(entity);
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
