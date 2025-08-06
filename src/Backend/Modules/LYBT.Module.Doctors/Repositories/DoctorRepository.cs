using LYBT.Infrastructure.Data;
using LYBT.Models.Doctors;
using LYBT.Module.Doctors.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Doctors.Repositories {

    /// <summary>
    /// 医生仓储实现类（简化版）
    /// </summary>
    public class DoctorRepository : IDoctorRepository {
        private readonly AppDbContext _context;

        public DoctorRepository(AppDbContext context) {
            _context = context;
        }

        public async Task<DoctorModel?> GetByIdAsync(Guid id, bool includeDisabled = false) {
            var query = _context.Doctors
                .Include(d => d.User)
                .Where(d => d.Id == id);

            if (!includeDisabled) {
                query = query.Where(d => d.Status == DoctorStatus.Active);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<DoctorModel?> GetByUserIdAsync(Guid userId) {
            return await _context.Doctors
                .Include(d => d.User)
                .Where(d => d.UserId == userId)
                .FirstOrDefaultAsync();
        }

        public async Task<List<DoctorModel>> GetAllAsync(bool includeDisabled = false) {
            var query = _context.Doctors
                .Include(d => d.User)
                .AsQueryable();

            if (!includeDisabled) {
                query = query.Where(d => d.Status == DoctorStatus.Active);
            }

            return await query
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<bool> AddAsync(DoctorModel model) {
            await _context.Doctors.AddAsync(model);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAsync(DoctorModel model) {
            _context.Doctors.Update(model);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> ExistsAsync(Guid id) {
            return await _context.Doctors.AnyAsync(d => d.Id == id);
        }
    }
}