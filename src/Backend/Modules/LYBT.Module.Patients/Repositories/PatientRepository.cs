using LYBT.Infrastructure.Data;
using LYBT.Models.Patients;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Patients.Repositories {

    /// <summary>
    /// 病人仓储实现，负责数据库具体操作
    /// 实现软删除策略：患者档案只能禁用/启用，不能物理删除
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
                query = query.Where(p => p.Status == PatientStatus.Normal);
            }

            return await query.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<PatientModel>> GetListAsync(string? keyword = null, int page = 1, int pageSize = 20, bool includeDisabled = false) {
            var query = _dbContext.Patients.AsQueryable();

            // 权限控制：是否包含禁用患者档案
            if (!includeDisabled) {
                query = query.Where(p => p.Status == PatientStatus.Normal);
            }

            if (!string.IsNullOrWhiteSpace(keyword)) {
                var upper = keyword.ToUpperInvariant();
                query = query.Where(x => x.Name.Contains(keyword)
                    || (x.PinyinCode != null && x.PinyinCode.Contains(upper))
                    || (x.PhoneNumber != null && x.PhoneNumber.Contains(keyword))
                    || (x.IdNumber != null && x.IdNumber.Contains(keyword)));
            }

            return await query
                .OrderByDescending(x => x.CreateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> UpdateAsync(PatientModel patient) {
            patient.UpdateTime = DateTime.Now;
            _dbContext.Patients.Update(patient);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> EnableAsync(Guid id) {
            var entity = await _dbContext.Patients.FindAsync(id);
            if (entity == null)
                return false;
            entity.Status = PatientStatus.Normal;
            entity.UpdateTime = DateTime.Now;
            _dbContext.Patients.Update(entity);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> DisableAsync(Guid id) {
            var entity = await _dbContext.Patients.FindAsync(id);
            if (entity == null)
                return false;
            entity.Status = PatientStatus.Inactive;
            entity.UpdateTime = DateTime.Now;
            _dbContext.Patients.Update(entity);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<int> BatchDisableAsync(List<Guid> ids) {
            var list = await _dbContext.Patients.Where(p => ids.Contains(p.Id)).ToListAsync();
            foreach (var p in list) {
                p.Status = PatientStatus.Inactive;
                p.UpdateTime = DateTime.Now;
            }
            _dbContext.Patients.UpdateRange(list);
            return await _dbContext.SaveChangesAsync();
        }

        public async Task<int> BatchEnableAsync(List<Guid> ids) {
            var list = await _dbContext.Patients.Where(p => ids.Contains(p.Id)).ToListAsync();
            foreach (var p in list) {
                p.Status = PatientStatus.Normal;
                p.UpdateTime = DateTime.Now;
            }
            _dbContext.Patients.UpdateRange(list);
            return await _dbContext.SaveChangesAsync();
        }

        public async Task<PatientModel?> GetByIdNumberAsync(string idNumber) {
            return await _dbContext.Patients.FirstOrDefaultAsync(x => x.IdNumber == idNumber);
        }

        public async Task<PatientModel?> GetByPhoneNumberAsync(string phoneNumber) {
            return await _dbContext.Patients.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        }

        public async Task<bool> IsIdNumberExistsAsync(string idNumber, Guid? excludeId = null) {
            var query = _dbContext.Patients.Where(p => p.IdNumber == idNumber);
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

            // 权限控制：是否包含禁用患者档案
            if (!includeDisabled) {
                query = query.Where(p => p.Status == PatientStatus.Normal);
            }

            if (!string.IsNullOrWhiteSpace(keyword)) {
                var upper = keyword.ToUpperInvariant();
                query = query.Where(x => x.Name.Contains(keyword)
                    || (x.PinyinCode != null && x.PinyinCode.Contains(upper))
                    || (x.PhoneNumber != null && x.PhoneNumber.Contains(keyword))
                    || (x.IdNumber != null && x.IdNumber.Contains(keyword)));
            }

            return await query.CountAsync();
        }

        public async Task<List<PatientModel>> SearchAsync(string keyword, bool includeDisabled = false) {
            var query = _dbContext.Patients.AsQueryable();

            // 权限控制：是否包含禁用患者档案
            if (!includeDisabled) {
                query = query.Where(p => p.Status == PatientStatus.Normal);
            }

            var upper = keyword.ToUpperInvariant();
            return await query
                .Where(p => p.Name.Contains(keyword)
                    || (p.PinyinCode != null && p.PinyinCode.Contains(upper))
                    || (p.IdNumber != null && p.IdNumber.Contains(keyword))
                    || (p.PhoneNumber != null && p.PhoneNumber.Contains(keyword)))
                .OrderByDescending(p => p.CreateTime)
                .Take(20)
                .ToListAsync();
        }

        public async Task<List<PatientModel>> ExactSearchAsync(string keyword, bool includeDisabled = false) {
            var results = new List<PatientModel>();

            // 基础查询，是否包含禁用患者档案
            var baseQuery = _dbContext.Patients.AsQueryable();
            if (!includeDisabled) {
                baseQuery = baseQuery.Where(p => p.Status == PatientStatus.Normal);
            }

            // 精确匹配手机号
            var phoneMatch = await baseQuery
                .FirstOrDefaultAsync(p => p.PhoneNumber == keyword);
            if (phoneMatch != null) {
                results.Add(phoneMatch);
            }

            // 精确匹配身份证号
            var idMatch = await baseQuery
                .FirstOrDefaultAsync(p => p.IdNumber == keyword);
            if (idMatch != null && !results.Any(r => r.Id == idMatch.Id)) {
                results.Add(idMatch);
            }

            return results;
        }

        public async Task<List<PatientModel>> GetActivePatientsAsync() {
            return await _dbContext.Patients
                .Where(p => p.Status == PatientStatus.Normal)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据身份证号获取患者档案列表（用于重复检查）
        /// </summary>
        public async Task<List<PatientModel>> GetPatientsByIdNumberAsync(string idNumber) {
            if (string.IsNullOrEmpty(idNumber))
                return new List<PatientModel>();

            return await _dbContext.Patients
                .Where(p => p.IdNumber == idNumber)
                .ToListAsync();
        }

        /// <summary>
        /// 根据姓名和手机号获取患者档案列表（用于重复检查）
        /// </summary>
        public async Task<List<PatientModel>> GetPatientsByNameAndPhoneAsync(string name, string phoneNumber) {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(phoneNumber))
                return new List<PatientModel>();

            return await _dbContext.Patients
                .Where(p => p.Name == name && p.PhoneNumber == phoneNumber)
                .ToListAsync();
        }

        /// <summary>
        /// 根据相似姓名获取患者档案列表（用于重复检查）
        /// </summary>
        public async Task<List<PatientModel>> GetPatientsBySimilarNameAsync(string name) {
            if (string.IsNullOrEmpty(name))
                return new List<PatientModel>();

            // 简单的相似性检查：拼音码匹配或包含关系
            var pinyinCode = CommonHelper.GetPinyinCode(name);

            return await _dbContext.Patients
                .Where(p => p.PinyinCode == pinyinCode ||
                           p.Name.Contains(name) ||
                           name.Contains(p.Name))
                .Where(p => p.Name != name) // 排除完全相同的姓名
                .Take(10) // 限制返回数量
                .ToListAsync();
        }

        /// <summary>
        /// 根据姓名获取患者档案列表（用于查询或创建场景）
        /// </summary>
        public async Task<List<PatientModel>> GetByNameAsync(string name) {
            if (string.IsNullOrEmpty(name))
                return new List<PatientModel>();

            return await _dbContext.Patients
                .Where(p => p.Name == name)
                .ToListAsync();
        }
    }
}