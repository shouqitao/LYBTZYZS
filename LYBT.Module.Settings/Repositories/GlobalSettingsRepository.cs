using LYBT.Infrastructure;
using LYBT.Models.Settings;
using LYBT.Module.Settings.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace LYBT.Module.Settings.Repositories {
    public class GlobalSettingsRepository : IGlobalSettingsRepository {
        private readonly AppDbContext _db;
        public GlobalSettingsRepository(AppDbContext db) {
            _db = db;
        }

        public async Task<GlobalSettingsModel?> GetAsync() {
            return await _db.Set<GlobalSettingsModel>().FirstOrDefaultAsync();
        }

        public async Task<bool> SaveAsync(GlobalSettingsModel model) {
            var existing = await _db.Set<GlobalSettingsModel>().FirstOrDefaultAsync();
            if (existing == null) {
                _db.Add(model);
            } else {
                existing.DefaultRecordSharing = model.DefaultRecordSharing;
                existing.SyncMode = model.SyncMode;
                _db.Update(existing);
            }
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
