using System.Linq.Expressions;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Patients.Repositories
{
    /// <summary>
    /// 患者仓储实现 - 继承BaseRepository并实现IPatientRepository
    /// Task 1.2: PatientRepository重构，适配新的简化Repository设计
    /// </summary>
    /// <remarks>
    /// 设计原则：
    /// - 继承BaseRepository：复用11个标准CRUD方法
    /// - 业务扩展：实现患者特定的业务查询方法
    /// - 软删除模式：所有查询自动过滤IsDeleted=true的数据
    /// - 查询优化：只读查询使用AsNoTracking提升性能
    /// </remarks>
    internal class PatientRepository : BaseRepository<Patient>, IPatientRepository
    {
        public PatientRepository(AppDbContext context, ILogger<PatientRepository> logger)
            : base(context, logger)
        {
        }

        #region 模板方法覆盖 - 患者关键字搜索和排序

        /// <summary>
        /// 关键字过滤：姓名、拼音码
        /// </summary>
        protected override IQueryable<Patient> ApplyKeywordFilter(IQueryable<Patient> query, string keyword)
        {
            return query.Where(p =>
                p.Name.Contains(keyword) ||
                (p.PinYinCode != null && p.PinYinCode.Contains(keyword))
            );
        }

        /// <summary>
        /// 默认排序：按姓名升序
        /// </summary>
        protected override IQueryable<Patient> ApplyDefaultOrdering(IQueryable<Patient> query)
        {
            return query.OrderBy(p => p.Name);
        }

        #endregion

        #region IPatientRepository 特定业务方法

        /// <summary>
        /// 根据姓名获取患者（支持模糊匹配）
        /// </summary>
        /// <param name="name">患者姓名</param>
        /// <returns>患者列表，不存在返回空列表</returns>
        public async Task<List<Patient>> GetByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<Patient>();

            return await _dbSet
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.Name.Contains(name))
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 检查患者姓名是否已存在
        /// </summary>
        /// <param name="name">患者姓名</param>
        /// <param name="excludeId">排除的患者ID（用于更新时检查）</param>
        /// <returns>存在返回true，否则返回false</returns>
        public async Task<bool> ExistsAsync(string name, Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var query = _dbSet.Where(p => !p.IsDeleted && p.Name == name);

            if (excludeId.HasValue)
                query = query.Where(p => p.Id != excludeId.Value);

            return await query.AnyAsync();
        }

        /// <summary>
        /// 根据日期范围获取患者（按创建日期）
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>患者列表</returns>
        public async Task<List<Patient>> GetByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(p => !p.IsDeleted &&
                           p.CreatedAt >= startDate &&
                           p.CreatedAt <= endDate)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
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
