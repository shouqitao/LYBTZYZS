using LYBT.Infrastructure;
using LYBT.Models.Patient;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Models;
using Microsoft.EntityFrameworkCore;
using LYBT.Common.Enums.Patient;

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
                query = query.Where(x => x.Name.Contains(keyword)
                    || x.PinyinCode.Contains(keyword)
                    || x.PhoneNumber.Contains(keyword));
            }
            return await query
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> UpdateAsync(PatientModel patient) {
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

        public async Task<PatientModel?> GetByIDNumberAsync(string idNumber) {
            return await _dbContext.Patients.FirstOrDefaultAsync(x => x.IDNumber == idNumber);
        }

        public async Task<PatientModel?> GetByPhoneNumberAsync(string phoneNumber) {
            return await _dbContext.Patients.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        }

        public async Task<int> GetCountAsync(string? keyword = null) {
            var query = _dbContext.Patients.AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword)) {
                query = query.Where(x => x.Name.Contains(keyword)
                    || x.PinyinCode.Contains(keyword)
                    || x.PhoneNumber.Contains(keyword));
            }
            return await query.CountAsync();
        }

        public async Task<bool> EnableAsync(Guid id) {
            var entity = await _dbContext.Patients.FindAsync(id);
            if (entity == null)
                return false;
            entity.Status = PatientStatus.Active;
            _dbContext.Patients.Update(entity);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> DisableAsync(Guid id) {
            var entity = await _dbContext.Patients.FindAsync(id);
            if (entity == null)
                return false;
            entity.Status = PatientStatus.Disabled;
            _dbContext.Patients.Update(entity);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<int> BatchDisableAsync(List<Guid> ids) {
            var list = await _dbContext.Patients.Where(p => ids.Contains(p.Id)).ToListAsync();
            foreach (var p in list) {
                p.Status = PatientStatus.Disabled;
            }
            _dbContext.Patients.UpdateRange(list);
            return await _dbContext.SaveChangesAsync();
        }

        public async Task<List<PatientModel>> SearchAsync(string keyword) {
            return await _dbContext.Patients
                .Where(p => p.Name.Contains(keyword) || p.PinyinCode.Contains(keyword)
                    || p.IDNumber.Contains(keyword) || p.PhoneNumber.Contains(keyword))
                .OrderByDescending(p => p.Id)
                .Take(20)
                .ToListAsync();
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
            var relation = new SpecialPatientDoctor {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                DoctorId = doctorId,
                CreatedAt = DateTime.Now
            };
            await _dbContext.SpecialPatientDoctors.AddAsync(relation);
            return await _dbContext.SaveChangesAsync() > 0;
        }
    }
}
