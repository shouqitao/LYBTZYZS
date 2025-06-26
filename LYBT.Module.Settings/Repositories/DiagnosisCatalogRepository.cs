using LYBT.Infrastructure;
using LYBT.Models.Settings;
using LYBT.Module.Settings.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Settings.Repositories {

    public class DiagnosisCatalogRepository : IDiagnosisCatalogRepository {
        private readonly AppDbContext _db;

        public DiagnosisCatalogRepository(AppDbContext db) {
            _db = db;
        }

        public async Task<List<DiagnosisCatalogModel>> GetAllAsync() {
            return await _db.Set<DiagnosisCatalogModel>().ToListAsync();
        }

        public async Task<bool> AddAsync(DiagnosisCatalogModel model) {
            _db.Add(model);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAsync(DiagnosisCatalogModel model) {
            _db.Update(model);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(Guid id) {
            var entity = await _db.Set<DiagnosisCatalogModel>().FindAsync(id);
            if (entity == null)
                return false;
            _db.Remove(entity);
            return await _db.SaveChangesAsync() > 0;
        }
    }
}