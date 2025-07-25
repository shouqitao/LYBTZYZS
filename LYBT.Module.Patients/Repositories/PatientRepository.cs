using LYBT.Common.Enums.Patient;
using LYBT.Infrastructure;
using LYBT.Models.Patients;
using LYBT.Module.Patients.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Patients.Repositories {

    /// <summary>
    /// 病人仓储实现，负责数据库具体操作
    /// </summary>
    public class PatientRepository : IPatientRepository {
        private readonly AppDbContext _dbContext;

        public PatientRepository(AppDbContext dbContext) {
            _dbContext = dbContext;
        }

        public async Task<bool> AddAsync(PatientModel patient) {
            await _dbContext.Patients.AddAsync(patient);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<PatientModel?> GetByIdAsync(Guid id) {
            return await _dbContext.Patients.FindAsync(id);
        }

        public async Task<List<PatientModel>> GetListAsync(string? keyword = null, int page = 1, int pageSize = 20) {
            var query = _dbContext.Patients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword)) {
                var upper = keyword.ToUpperInvariant();
                query = query.Where(x => x.Name.Contains(keyword)
                    || x.PinyinCode.Contains(upper)
                    || x.PhoneNumber.Contains(keyword)
                    || x.IDNumber.Contains(keyword));
            }

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> UpdateAsync(PatientModel patient) {
            patient.UpdatedAt = DateTime.Now;
            _dbContext.Patients.Update(patient);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(Guid id) {
            var entity = await _dbContext.Patients.FindAsync(id);
            if (entity == null)
                return false;
            _dbContext.Patients.Remove(entity);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<int> BatchDeleteAsync(List<Guid> ids) {
            var entities = await _dbContext.Patients
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();

            if (!entities.Any())
                return 0;

            _dbContext.Patients.RemoveRange(entities);
            return await _dbContext.SaveChangesAsync();
        }

        public async Task<PatientModel?> GetByIDNumberAsync(string idNumber) {
            return await _dbContext.Patients.FirstOrDefaultAsync(x => x.IDNumber == idNumber);
        }

        public async Task<PatientModel?> GetByPhoneNumberAsync(string phoneNumber) {
            return await _dbContext.Patients.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        }

        public async Task<bool> IsIDNumberExistsAsync(string idNumber, Guid? excludeId = null) {
            var query = _dbContext.Patients.Where(p => p.IDNumber == idNumber);
            if (excludeId.HasValue) {
                query = query.Where(p => p.Id != excludeId.Value);
            }
            return await query.AnyAsync();
        }

        public async Task<bool> IsPhoneNumberExistsAsync(string phoneNumber, Guid? excludeId = null) {
            var query = _dbContext.Patients.Where(p => p.PhoneNumber == phoneNumber);
            if (excludeId.HasValue) {
                query = query.Where(p => p.Id != excludeId.Value);
            }
            return await query.AnyAsync();
        }

        public async Task<int> GetCountAsync(string? keyword = null) {
            var query = _dbContext.Patients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword)) {
                var upper = keyword.ToUpperInvariant();
                query = query.Where(x => x.Name.Contains(keyword)
                    || x.PinyinCode.Contains(upper)
                    || x.PhoneNumber.Contains(keyword)
                    || x.IDNumber.Contains(keyword));
            }

            return await query.CountAsync();
        }

        public async Task<bool> EnableAsync(Guid id) {
            var entity = await _dbContext.Patients.FindAsync(id);
            if (entity == null)
                return false;
            entity.Status = PatientStatus.Active;
            entity.UpdatedAt = DateTime.Now;
            _dbContext.Patients.Update(entity);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> DisableAsync(Guid id) {
            var entity = await _dbContext.Patients.FindAsync(id);
            if (entity == null)
                return false;
            entity.Status = PatientStatus.Disabled;
            entity.UpdatedAt = DateTime.Now;
            _dbContext.Patients.Update(entity);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<int> BatchDisableAsync(List<Guid> ids) {
            var list = await _dbContext.Patients.Where(p => ids.Contains(p.Id)).ToListAsync();
            foreach (var p in list) {
                p.Status = PatientStatus.Disabled;
                p.UpdatedAt = DateTime.Now;
            }
            _dbContext.Patients.UpdateRange(list);
            return await _dbContext.SaveChangesAsync();
        }

        public async Task<List<PatientModel>> SearchAsync(string keyword) {
            var upper = keyword.ToUpperInvariant();
            return await _dbContext.Patients
                .Where(p => p.Name.Contains(keyword)
                    || p.PinyinCode.Contains(upper)
                    || p.IDNumber.Contains(keyword)
                    || p.PhoneNumber.Contains(keyword))
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .ToListAsync();
        }

        public async Task<List<PatientModel>> ExactSearchAsync(string keyword) {
            var results = new List<PatientModel>();

            // 精确匹配手机号
            var phoneMatch = await _dbContext.Patients
                .FirstOrDefaultAsync(p => p.PhoneNumber == keyword);
            if (phoneMatch != null) {
                results.Add(phoneMatch);
            }

            // 精确匹配身份证号
            var idMatch = await _dbContext.Patients
                .FirstOrDefaultAsync(p => p.IDNumber == keyword);
            if (idMatch != null && !results.Any(r => r.Id == idMatch.Id)) {
                results.Add(idMatch);
            }

            return results;
        }

        public async Task<List<PatientModel>> GetForDoctorAsync(Guid doctorId) {
            var query = _dbContext.Patients.Where(p => !p.IsSpecial);
            var specialIds = await _dbContext.SpecialPatientDoctors
                .Where(s => s.DoctorId == doctorId)
                .Select(s => s.PatientId)
                .ToListAsync();
            query = query.Union(_dbContext.Patients.Where(p => specialIds.Contains(p.Id)));
            return await query.ToListAsync();
        }

        public async Task<bool> AssignDoctorAsync(Guid patientId, Guid doctorId) {
            // 检查是否已经授权
            var exists = await _dbContext.SpecialPatientDoctors
                .AnyAsync(s => s.PatientId == patientId && s.DoctorId == doctorId);

            if (exists) {
                return true; // 已经授权，直接返回成功
            }

            var relation = new SpecialPatientDoctor {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                DoctorId = doctorId,
                CreatedAt = DateTime.Now
            };

            await _dbContext.SpecialPatientDoctors.AddAsync(relation);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> IsDoctorAssignedAsync(Guid patientId, Guid doctorId) {
            return await _dbContext.SpecialPatientDoctors
                .AnyAsync(s => s.PatientId == patientId && s.DoctorId == doctorId);
        }
    }
}