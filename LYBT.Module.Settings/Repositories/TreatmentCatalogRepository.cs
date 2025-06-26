using LYBT.Infrastructure;
using LYBT.Models.Settings;
using LYBT.Module.Settings.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Settings.Repositories {

    public class TreatmentCatalogRepository : ITreatmentCatalogRepository {
        private readonly AppDbContext _db;

        public TreatmentCatalogRepository(AppDbContext db) {
            _db = db;
        }

        public async Task<List<TreatmentCatalogModel>> GetAllAsync() {
            return await _db.Set<TreatmentCatalogModel>().ToListAsync();
        }

        public async Task<bool> AddAsync(TreatmentCatalogModel model) {
            _db.Add(model);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAsync(TreatmentCatalogModel model) {
            _db.Update(model);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(Guid id) {
            var entity = await _db.Set<TreatmentCatalogModel>().FindAsync(id);
            if (entity == null)
                return false;
            _db.Remove(entity);
            return await _db.SaveChangesAsync() > 0;
        }
    }
}