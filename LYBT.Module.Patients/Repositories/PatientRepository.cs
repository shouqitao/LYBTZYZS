using LYBT.Infrastructure;
using LYBT.Models.Patient;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Models;
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

        public async Task<bool> DeleteAsync(string id) {
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
    }
}
