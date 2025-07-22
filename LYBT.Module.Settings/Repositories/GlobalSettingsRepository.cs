using LYBT.Infrastructure;
using LYBT.Models.Settings;
using LYBT.Module.Settings.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Settings.Repositories {

/// <summary>
/// 表示GlobalSettingsRepository。
/// </summary>
    public class GlobalSettingsRepository : IGlobalSettingsRepository {
        private readonly AppDbContext _db;

        public GlobalSettingsRepository(AppDbContext db) {
            _db = db;
        }

/// <summary>
/// 执行GetAsync操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<GlobalSettingsModel?> GetAsync() {
            return await _db.Set<GlobalSettingsModel>().FirstOrDefaultAsync();
        }

/// <summary>
/// 执行SaveAsync操作。
/// </summary>
/// <param name="model">参数model</param>
/// <returns>返回值</returns>
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
