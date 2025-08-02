using LYBT.Infrastructure.Data;
using LYBT.Models.Doctors;
using LYBT.Module.Doctors.Interfaces;
using LYBT.Shared.Models.Contracts.Doctors;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Doctors.Repositories {

    /// <summary>
    /// 医生仓储实现类
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

        public async Task<DoctorModel?> GetByUserIdAsync(Guid userId, bool includeDisabled = false) {
            var query = _context.Doctors
                .Include(d => d.User)
                .Where(d => d.UserId == userId);

            if (!includeDisabled) {
                query = query.Where(d => d.Status == DoctorStatus.Active);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<List<DoctorModel>> GetActiveDoctorsAsync() {
            return await _context.Doctors
                .Include(d => d.User)
                .Where(d => d.Status == DoctorStatus.Active)
                .OrderBy(d => d.User.RealName)
                .ToListAsync();
        }

        public async Task<List<DoctorModel>> SearchAsync(string keyword, bool includeDisabled = false) {
            var query = _context.Doctors
                .Include(d => d.User)
                .AsQueryable();

            if (!includeDisabled) {
                query = query.Where(d => d.Status == DoctorStatus.Active);
            }

            if (!string.IsNullOrWhiteSpace(keyword)) {
                var upperKeyword = keyword.ToUpperInvariant();
                query = query.Where(d =>
                    d.User.RealName.Contains(keyword) ||
                    d.User.Username.Contains(keyword) ||
                    (d.User.PhoneNumber != null && d.User.PhoneNumber.Contains(keyword)) ||
                    d.PinyinCode.Contains(upperKeyword) ||
                    (d.LicenseNumber != null && d.LicenseNumber.Contains(keyword)) ||
                    d.Specialty.Contains(keyword));
            }

            return await query
                .OrderByDescending(d => d.CreatedTime)
                .Take(50) // 限制搜索结果数量
                .ToListAsync();
        }

        public async Task<(List<DoctorModel> list, int total)> GetPagedAsync(DoctorQueryDto query, bool includeDisabled = false) {
            var dbQuery = _context.Doctors
                .Include(d => d.User)
                .AsQueryable();

            if (!includeDisabled) {
                dbQuery = dbQuery.Where(d => d.Status == DoctorStatus.Active);
            }

            // 关键词搜索
            if (!string.IsNullOrWhiteSpace(query.Keyword)) {
                var upperKeyword = query.Keyword.ToUpperInvariant();
                dbQuery = dbQuery.Where(d =>
                    d.User.RealName.Contains(query.Keyword) ||
                    d.User.Username.Contains(query.Keyword) ||
                    (d.User.PhoneNumber != null && d.User.PhoneNumber.Contains(query.Keyword)) ||
                    d.PinyinCode.Contains(upperKeyword) ||
                    (d.LicenseNumber != null && d.LicenseNumber.Contains(query.Keyword)) ||
                    d.Specialty.Contains(query.Keyword));
            }

            // 状态筛选
            if (query.IsActive.HasValue) {
                var status = query.IsActive.Value ? DoctorStatus.Active : DoctorStatus.Inactive;
                dbQuery = dbQuery.Where(d => d.Status == status);
            }

            // 计算总数
            var total = await dbQuery.CountAsync();

            // 分页查询
            var list = await dbQuery
                .OrderByDescending(d => d.CreatedTime)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return (list, total);
        }

        public async Task<bool> AddAsync(DoctorModel model) {
            try {
                _context.Doctors.Add(model);
                return await _context.SaveChangesAsync() > 0;
            } catch {
                return false;
            }
        }

        public async Task<bool> UpdateAsync(DoctorModel model) {
            try {
                _context.Doctors.Update(model);

                // 只更新User的必要字段，避免冲突
                if (model.User != null) {
                    _context.Entry(model.User).Property(u => u.PinyinCode).IsModified = true;
                }

                return await _context.SaveChangesAsync() > 0;
            } catch {
                return false;
            }
        }

        public async Task<bool> DisableAsync(Guid id) {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
                return false;

            doctor.Status = DoctorStatus.Inactive;
            _context.Doctors.Update(doctor);

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> EnableAsync(Guid id) {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
                return false;

            doctor.Status = DoctorStatus.Active;
            _context.Doctors.Update(doctor);

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<int> BatchDisableAsync(List<Guid> ids) {
            if (ids == null || ids.Count == 0)
                return 0;

            var doctors = await _context.Doctors
                .Where(d => ids.Contains(d.Id))
                .ToListAsync();

            foreach (var doctor in doctors) {
                doctor.Status = DoctorStatus.Inactive;
            }

            _context.Doctors.UpdateRange(doctors);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> BatchEnableAsync(List<Guid> ids) {
            if (ids == null || ids.Count == 0)
                return 0;

            var doctors = await _context.Doctors
                .Where(d => ids.Contains(d.Id))
                .ToListAsync();

            foreach (var doctor in doctors) {
                doctor.Status = DoctorStatus.Active;
            }

            _context.Doctors.UpdateRange(doctors);
            return await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(Guid id) {
            return await _context.Doctors.AnyAsync(d => d.Id == id);
        }

        public async Task<List<DoctorModel>> SearchByPinyinAsync(string pinyin, bool includeDisabled = false) {
            if (string.IsNullOrWhiteSpace(pinyin)) {
                return new List<DoctorModel>();
            }

            var query = _context.Doctors
                .Include(d => d.User)
                .Where(d => d.PinyinCode.Contains(pinyin.ToUpperInvariant()));

            if (!includeDisabled) {
                query = query.Where(d => d.Status == DoctorStatus.Active);
            }

            return await query
                .OrderBy(d => d.User.RealName)
                .Take(20)
                .ToListAsync();
        }

        public async Task<bool> IsIdNumberExistsAsync(string idNumber, Guid? excludeId = null) {
            if (string.IsNullOrWhiteSpace(idNumber)) {
                return false;
            }

            var query = _context.Doctors.Where(d => d.IdNumber == idNumber);

            if (excludeId.HasValue) {
                query = query.Where(d => d.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }
    }
}