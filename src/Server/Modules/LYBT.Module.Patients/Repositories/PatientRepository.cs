using System.Linq.Expressions;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Patients.Repositories
{
    /// <summary>
    /// 患者仓储实现 - 实现IBaseRepository<Patient>标准接口
    /// Phase 1 Task 1.3: 基础数据模块Repository层统一重构
    /// </summary>
    /// <remarks>
    /// 设计原则：
    /// - 统一共性：实现IBaseRepository<Patient>的11个标准CRUD方法
    /// - 保持特性：保留患者模块特定业务方法
    /// - 软删除模式：所有查询自动过滤IsDeleted=true的数据
    /// - 查询优化：只读查询使用AsNoTracking提升性能
    /// </remarks>
    internal class PatientRepository : IPatientRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<Patient> _dbSet;

        public PatientRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<Patient>();
        }

        #region IBaseRepository<Patient> 标准方法实现

        /// <summary>
        /// 根据ID获取患者（包含软删除过滤）
        /// </summary>
        public async Task<Patient?> GetByIdAsync(Guid id)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        }

        /// <summary>
        /// 获取所有患者（⚠️ 仅用于下拉列表等小数据量场景）
        /// </summary>
        public async Task<IEnumerable<Patient>> GetAllAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 分页查询患者（支持姓名/拼音码搜索）
        /// </summary>
        public async Task<PagedResult<Patient>> GetPagedAsync(int pageNumber, int pageSize, string? keyword = null)
        {
            var query = _dbSet
                .AsNoTracking()
                .Where(p => !p.IsDeleted);

            // 关键字搜索：姓名、拼音码
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var searchTerm = keyword.Trim();
                query = query.Where(p =>
                    p.Name.Contains(searchTerm) ||
                    (p.PinYinCode != null && p.PinYinCode.Contains(searchTerm))
                );
            }

            query = query.OrderBy(p => p.Name);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Patient>(items, totalCount, pageNumber, pageSize);
        }

        /// <summary>
        /// 条件查询（⚠️ 谨慎使用，建议使用具体业务方法）
        /// </summary>
        public async Task<IEnumerable<Patient>> FindAsync(Expression<Func<Patient, bool>> predicate)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .Where(predicate)
                .ToListAsync();
        }

        /// <summary>
        /// 获取单个患者（条件查询）
        /// </summary>
        public async Task<Patient?> GetSingleAsync(Expression<Func<Patient, bool>> predicate)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .FirstOrDefaultAsync(predicate);
        }

        /// <summary>
        /// 新增患者
        /// </summary>
        public async Task<Patient> AddAsync(Patient entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        /// <summary>
        /// 更新患者
        /// </summary>
        public async Task<Patient> UpdateAsync(Patient entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            entity.UpdatedAt = DateTime.UtcNow;
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null) return false;

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// 检查患者是否存在
        /// </summary>
        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _dbSet.AnyAsync(p => p.Id == id && !p.IsDeleted);
        }

        /// <summary>
        /// 获取患者总数
        /// </summary>
        public async Task<int> CountAsync()
        {
            return await _dbSet.CountAsync(p => !p.IsDeleted);
        }

        /// <summary>
        /// 保存更改（⚠️ 通常由Service层调用）
        /// </summary>
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        #endregion

        #region IPatientRepository 特定业务方法

        /// <summary>
        /// 搜索患者（支持多条件和分页）
        /// </summary>
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
        /// 批量创建患者（Epic #1934 FR-001）
        /// 使用AddRangeAsync批量添加，减少数据库往返次数
        /// </summary>
        /// <param name="patients">待创建的患者列表</param>
        /// <returns>创建成功的患者列表</returns>
        public async Task<List<Patient>> BatchCreateAsync(IEnumerable<Patient> patients)
        {
            var patientList = patients.ToList();

            // 批量添加到DbSet（性能优化：单次操作）
            await _dbSet.AddRangeAsync(patientList);

            // 保存到数据库
            await _context.SaveChangesAsync();

            return patientList;
        }

        /// <summary>
        /// 根据手机号查询患者（Epic #1934 BR-004重复检查）
        /// </summary>
        /// <param name="phoneNumber">手机号</param>
        /// <returns>患者对象，不存在返回null</returns>
        public async Task<Patient?> GetByPhoneNumberAsync(string phoneNumber)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PhoneNumber == phoneNumber && !p.IsDeleted);
        }

        #endregion
    }
}
