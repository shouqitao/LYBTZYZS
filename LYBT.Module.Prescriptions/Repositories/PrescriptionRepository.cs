using LYBT.Infrastructure;
using LYBT.Models.Prescriptions;
using LYBT.Common.Enums.Prescriptions;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Prescriptions.Repositories {
    public class PrescriptionRepository : IPrescriptionRepository {
        private readonly AppDbContext _db;
        public PrescriptionRepository(AppDbContext db) {
            _db = db;
        }

        public async Task<PrescriptionModel?> GetByIdAsync(Guid id) {
            return await _db.Prescriptions
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<PrescriptionModel>> GetListAsync() {
            return await _db.Prescriptions
                .Include(p => p.Items)
                .ToListAsync();
        }

        public async Task<bool> AddAsync(PrescriptionModel model) {
            _db.Prescriptions.Add(model);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAsync(PrescriptionModel model) {
            _db.Prescriptions.Update(model);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(Guid id) {
            var m = await _db.Prescriptions.FindAsync(id);
            if (m == null)
                return false;
            _db.Prescriptions.Remove(m);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> CancelAsync(Guid id) {
            var model = await _db.Prescriptions.FindAsync(id);
            if (model == null)
                return false;
            model.Status = PrescriptionStatus.Cancelled;
            _db.Prescriptions.Update(model);
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
