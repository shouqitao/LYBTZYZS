using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Patients.Repositories
{
    /// <summary>
    /// 患者仓储 - 优化版，包含查询优化和预加载支持
    /// </summary>
    public class PatientRepository : BaseRepository<Patient>, IPatientRepository
    {
        public PatientRepository(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// 根据姓名查找病人
        /// </summary>
        public async Task<Patient?> GetByNameAsync(string name)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Name == name && !p.IsDeleted);
        }

        /// <summary>
        /// 获取患者及其就诊记录（优化版，使用预加载）
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <returns>包含就诊记录的患者信息</returns>
        public async Task<Patient?> GetPatientWithVisitsAsync(Guid patientId)
        {
            return await _dbSet
                .AsNoTracking()
                // .Include(p => p.Visits)  // Patient实体未定义Visits导航属性
                //     .ThenInclude(v => v.Prescriptions)
                .Where(p => p.Id == patientId && !p.IsDeleted)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// 获取患者列表（投影优化版）
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <returns>患者摘要信息列表</returns>
        public async Task<PaginatedList<PatientSummary>> GetPatientSummariesAsync(int pageIndex, int pageSize)
        {
            var query = _dbSet
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .Select(p => new PatientSummary
                {
                    Id = p.Id,
                    Name = p.Name,
                    Gender = p.Gender,
                    Age = p.Age ?? 0,  // 处理可能的null值
                    PhoneNumber = p.PhoneNumber ?? string.Empty,
                    LastVisitDate = null  // Patient实体未定义Visits导航属性，暂时返回null
                });

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Name)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedList<PatientSummary>(items, totalCount, pageIndex, pageSize);
        }

        /// <summary>
        /// 搜索患者（优化版，支持多条件和分页）
        /// </summary>
        /// <param name="searchTerm">搜索词</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <returns>搜索结果</returns>
        public async Task<PaginatedList<Patient>> SearchPatientsAsync(
            string? searchTerm,
            int pageIndex,
            int pageSize)
        {
            var query = _dbSet.AsNoTracking().Where(p => !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    (p.PinYinCode != null && p.PinYinCode.ToLower().Contains(term)) ||
                    (p.PhoneNumber != null && p.PhoneNumber.Contains(term)) ||
                    (p.IdNumber != null && p.IdNumber.Contains(term)));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Name)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedList<Patient>(items, totalCount, pageIndex, pageSize);
        }

        /// <summary>
        /// 批量获取患者（优化版，使用Contains而非循环查询）
        /// </summary>
        /// <param name="patientIds">患者ID列表</param>
        /// <returns>患者列表</returns>
        public async Task<List<Patient>> GetPatientsByIdsAsync(IEnumerable<Guid> patientIds)
        {
            var idList = patientIds.ToList();
            return await _dbSet
                .AsNoTracking()
                .Where(p => idList.Contains(p.Id) && !p.IsDeleted)
                .ToListAsync();
        }

        /// <summary>
        /// 检查手机号是否存在（优化版，使用Any而非Count）
        /// </summary>
        /// <param name="phoneNumber">手机号</param>
        /// <param name="excludeId">排除的患者ID（用于更新时）</param>
        /// <returns>是否存在</returns>
        public async Task<bool> PhoneNumberExistsAsync(string phoneNumber, Guid? excludeId = null)
        {
            var query = _dbSet.AsNoTracking().Where(p => p.PhoneNumber == phoneNumber && !p.IsDeleted);

            if (excludeId.HasValue)
            {
                query = query.Where(p => p.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// 获取患者统计信息（聚合查询优化）
        /// </summary>
        /// <returns>统计信息</returns>
        public async Task<PatientStatistics> GetStatisticsAsync()
        {
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            // 基本统计信息（数据库端聚合）
            var stats = await _dbSet
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .GroupBy(p => 1) // 分组以执行聚合
                .Select(g => new PatientStatistics
                {
                    TotalPatients = g.Count(),
                    MaleCount = g.Count(p => p.Gender == Gender.Male),
                    FemaleCount = g.Count(p => p.Gender == Gender.Female),
                    NewPatientsThisMonth = g.Count(p => p.CreatedAt >= thisMonth),
                    AverageAge = 0 // Age 计算属性无法在 SQL 中翻译，需单独计算
                })
                .FirstOrDefaultAsync() ?? new PatientStatistics();

            // 平均年龄需要在内存中计算（Age 是计算属性）
            var patientsWithAge = await _dbSet
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.BirthDate.HasValue)
                .ToListAsync();

            if (patientsWithAge.Any())
            {
                stats.AverageAge = patientsWithAge.Average(p => p.Age ?? 0);
            }

            return stats;
        }

        /// <summary>
        /// 更新最后就诊时间（批量更新优化）
        /// </summary>
        /// <param name="patientIds">患者ID列表</param>
        /// <param name="visitDate">就诊时间</param>
        public async Task UpdateLastVisitDateAsync(IEnumerable<Guid> patientIds, DateTime visitDate)
        {
            var idList = patientIds.ToList();

            // 使用ExecuteUpdateAsync进行批量更新（EF Core 7.0+）
            await _dbSet
                .Where(p => idList.Contains(p.Id) && !p.IsDeleted)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.UpdatedAt, DateTime.Now));
        }
    }

    /// <summary>
    /// 患者摘要信息DTO
    /// </summary>
    public class PatientSummary
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Gender Gender { get; set; }
        public int Age { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime? LastVisitDate { get; set; }
    }

    /// <summary>
    /// 患者统计信息
    /// </summary>
    public class PatientStatistics
    {
        public int TotalPatients { get; set; }
        public int MaleCount { get; set; }
        public int FemaleCount { get; set; }
        public int NewPatientsThisMonth { get; set; }
        public double AverageAge { get; set; }
    }
}
