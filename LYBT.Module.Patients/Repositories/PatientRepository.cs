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

/// <summary>
/// 执行AddAsync操作。
/// </summary>
/// <param name="patient">参数patient</param>
/// <returns>返回值</returns>
        public async Task<bool> AddAsync(PatientModel patient) {
            await _dbContext.Patients.AddAsync(patient);
            return await _dbContext.SaveChangesAsync() > 0;
        }

/// <summary>
/// 执行GetByIdAsync操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<PatientModel?> GetByIdAsync(Guid id) {
            return await _dbContext.Patients.FindAsync(id);
        }

/// <summary>
/// 执行GetListAsync操作。
/// </summary>
/// <param name="null">参数null</param>
/// <param name="1">参数1</param>
/// <param name="20">参数20</param>
/// <returns>返回值</returns>
        public async Task<List<PatientModel>> GetListAsync(string? keyword = null, int page = 1, int pageSize = 20) {
            var query = _dbContext.Patients.AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword)) {
                var upper = keyword.ToUpperInvariant();
                query = query.Where(x => x.Name.Contains(keyword)
                    || x.PinyinCode.Contains(upper)
                    || x.PhoneNumber.Contains(keyword));
            }
            return await query
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

/// <summary>
/// 执行UpdateAsync操作。
/// </summary>
/// <param name="patient">参数patient</param>
/// <returns>返回值</returns>
        public async Task<bool> UpdateAsync(PatientModel patient) {
            _dbContext.Patients.Update(patient);
            return await _dbContext.SaveChangesAsync() > 0;
        }

/// <summary>
/// 执行DeleteAsync操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<bool> DeleteAsync(Guid id) {
            var entity = await _dbContext.Patients.FindAsync(id);
            if (entity == null)
                return false;
            _dbContext.Patients.Remove(entity);
            return await _dbContext.SaveChangesAsync() > 0;
        }

/// <summary>
/// 执行GetByIDNumberAsync操作。
/// </summary>
/// <param name="idNumber">参数idNumber</param>
/// <returns>返回值</returns>
        public async Task<PatientModel?> GetByIDNumberAsync(string idNumber) {
            return await _dbContext.Patients.FirstOrDefaultAsync(x => x.IDNumber == idNumber);
        }

/// <summary>
/// 执行GetByPhoneNumberAsync操作。
/// </summary>
/// <param name="phoneNumber">参数phoneNumber</param>
/// <returns>返回值</returns>
        public async Task<PatientModel?> GetByPhoneNumberAsync(string phoneNumber) {
            return await _dbContext.Patients.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        }

/// <summary>
/// 执行GetCountAsync操作。
/// </summary>
/// <param name="null">参数null</param>
/// <returns>返回值</returns>
        public async Task<int> GetCountAsync(string? keyword = null) {
            var query = _dbContext.Patients.AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword)) {
                var upper = keyword.ToUpperInvariant();
                query = query.Where(x => x.Name.Contains(keyword)
                    || x.PinyinCode.Contains(upper)
                    || x.PhoneNumber.Contains(keyword));
            }
            return await query.CountAsync();
        }

/// <summary>
/// 执行EnableAsync操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<bool> EnableAsync(Guid id) {
            var entity = await _dbContext.Patients.FindAsync(id);
            if (entity == null)
                return false;
            entity.Status = PatientStatus.Active;
            _dbContext.Patients.Update(entity);
            return await _dbContext.SaveChangesAsync() > 0;
        }

/// <summary>
/// 执行DisableAsync操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<bool> DisableAsync(Guid id) {
            var entity = await _dbContext.Patients.FindAsync(id);
            if (entity == null)
                return false;
            entity.Status = PatientStatus.Disabled;
            _dbContext.Patients.Update(entity);
            return await _dbContext.SaveChangesAsync() > 0;
        }

/// <summary>
/// 执行BatchDisableAsync操作。
/// </summary>
/// <param name="ids">参数ids</param>
/// <returns>返回值</returns>
        public async Task<int> BatchDisableAsync(List<Guid> ids) {
            var list = await _dbContext.Patients.Where(p => ids.Contains(p.Id)).ToListAsync();
            foreach (var p in list) {
                p.Status = PatientStatus.Disabled;
            }
            _dbContext.Patients.UpdateRange(list);
            return await _dbContext.SaveChangesAsync();
        }

/// <summary>
/// 执行SearchAsync操作。
/// </summary>
/// <param name="keyword">参数keyword</param>
/// <returns>返回值</returns>
        public async Task<List<PatientModel>> SearchAsync(string keyword) {
            var upper = keyword.ToUpperInvariant();
            return await _dbContext.Patients
                .Where(p => p.Name.Contains(keyword) || p.PinyinCode.Contains(upper)
                    || p.IDNumber.Contains(keyword) || p.PhoneNumber.Contains(keyword))
                .OrderByDescending(p => p.Id)
                .Take(20)
                .ToListAsync();
        }

/// <summary>
/// 执行GetForDoctorAsync操作。
/// </summary>
/// <param name="doctorId">参数doctorId</param>
/// <returns>返回值</returns>
        public async Task<List<PatientModel>> GetForDoctorAsync(Guid doctorId) {
            var query = _dbContext.Patients.Where(p => !p.IsSpecial);
            var specialIds = await _dbContext.SpecialPatientDoctors
                .Where(s => s.DoctorId == doctorId)
                .Select(s => s.PatientId)
                .ToListAsync();
            query = query.Union(_dbContext.Patients.Where(p => specialIds.Contains(p.Id)));
            return await query.ToListAsync();
        }

/// <summary>
/// 执行AssignDoctorAsync操作。
/// </summary>
/// <param name="patientId">参数patientId</param>
/// <param name="doctorId">参数doctorId</param>
/// <returns>返回值</returns>
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
