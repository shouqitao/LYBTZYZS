using LYBT.Common.Enums.Patient;
using LYBT.Infrastructure;
using LYBT.Models.Patients;
using LYBT.Module.Patients.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Patients.Repositories {

    /// <summary>
    /// 病人仓储实现，负责数据库具体操作
    /// 实现软删除策略：患者只能禁用/启用，不能物理删除
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

        public async Task<PatientModel?> GetByIdAsync(Guid id, bool includeDisabled = false) {
            var query = _dbContext.Patients.AsQueryable();

            if (!includeDisabled) {
                query = query.Where(p => p.Status == PatientStatus.Active);
            }

            return await query.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<PatientModel>> GetListAsync(string? keyword = null, int page = 1, int pageSize = 20, bool includeDisabled = false) {
            var query = _dbContext.Patients.AsQueryable();

            // 权限控制：是否包含禁用患者
            if (!includeDisabled) {
                query = query.Where(p => p.Status == PatientStatus.Active);
            }

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

        public async Task<int> BatchEnableAsync(List<Guid> ids) {
            var list = await _dbContext.Patients.Where(p => ids.Contains(p.Id)).ToListAsync();
            foreach (var p in list) {
                p.Status = PatientStatus.Active;
                p.UpdatedAt = DateTime.Now;
            }
            _dbContext.Patients.UpdateRange(list);
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

        public async Task<int> GetCountAsync(string? keyword = null, bool includeDisabled = false) {
            var query = _dbContext.Patients.AsQueryable();

            // 权限控制：是否包含禁用患者
            if (!includeDisabled) {
                query = query.Where(p => p.Status == PatientStatus.Active);
            }

            if (!string.IsNullOrWhiteSpace(keyword)) {
                var upper = keyword.ToUpperInvariant();
                query = query.Where(x => x.Name.Contains(keyword)
                    || x.PinyinCode.Contains(upper)
                    || x.PhoneNumber.Contains(keyword)
                    || x.IDNumber.Contains(keyword));
            }

            return await query.CountAsync();
        }

        public async Task<List<PatientModel>> SearchAsync(string keyword, bool includeDisabled = false) {
            var query = _dbContext.Patients.AsQueryable();

            // 权限控制：是否包含禁用患者
            if (!includeDisabled) {
                query = query.Where(p => p.Status == PatientStatus.Active);
            }

            var upper = keyword.ToUpperInvariant();
            return await query
                .Where(p => p.Name.Contains(keyword)
                    || p.PinyinCode.Contains(upper)
                    || p.IDNumber.Contains(keyword)
                    || p.PhoneNumber.Contains(keyword))
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .ToListAsync();
        }

        public async Task<List<PatientModel>> ExactSearchAsync(string keyword, bool includeDisabled = false) {
            var results = new List<PatientModel>();

            // 基础查询，是否包含禁用患者
            var baseQuery = _dbContext.Patients.AsQueryable();
            if (!includeDisabled) {
                baseQuery = baseQuery.Where(p => p.Status == PatientStatus.Active);
            }

            // 精确匹配手机号
            var phoneMatch = await baseQuery
                .FirstOrDefaultAsync(p => p.PhoneNumber == keyword);
            if (phoneMatch != null) {
                results.Add(phoneMatch);
            }

            // 精确匹配身份证号
            var idMatch = await baseQuery
                .FirstOrDefaultAsync(p => p.IDNumber == keyword);
            if (idMatch != null && !results.Any(r => r.Id == idMatch.Id)) {
                results.Add(idMatch);
            }

            return results;
        }

        public async Task<List<PatientModel>> GetForDoctorAsync(Guid doctorId, bool includeDisabled = false) {
            // 获取普通患者（非特殊患者）
            var normalQuery = _dbContext.Patients.Where(p => !p.IsSpecial);

            // 获取授权给该医生的特殊患者ID
            var specialIds = await _dbContext.SpecialPatientDoctors
                .Where(s => s.DoctorId == doctorId)
                .Select(s => s.PatientId)
                .ToListAsync();

            // 获取特殊患者
            var specialQuery = _dbContext.Patients.Where(p => specialIds.Contains(p.Id));

            // 合并查询
            var query = normalQuery.Union(specialQuery);

            // 权限控制：是否包含禁用患者
            if (!includeDisabled) {
                query = query.Where(p => p.Status == PatientStatus.Active);
            }

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

        public async Task<List<PatientModel>> GetActivePatientsAsync() {
            return await _dbContext.Patients
                .Where(p => p.Status == PatientStatus.Active)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }
    }
}