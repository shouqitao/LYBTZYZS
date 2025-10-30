using System.Linq.Expressions;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrescriptionEntity = LYBT.Entities.Prescriptions.Prescription;

namespace LYBT.Module.Prescriptions.Repositories
{
    /// <summary>
    /// 处方仓储 - 优化版，包含Include策略以解决N+1查询问题
    /// </summary>
    internal class PrescriptionRepository : BaseRepository<PrescriptionEntity>, IPrescriptionRepository
    {
        public PrescriptionRepository(AppDbContext context) : base(context)
        {
        }

        public PrescriptionRepository(AppDbContext context, ILogger<PrescriptionRepository> logger)
            : base(context, logger)
        {
        }

        /// <summary>
        /// 根据ID获取处方（包含处方项和药材信息）
        /// </summary>
        public async Task<PrescriptionEntity?> GetByIdWithItemsAsync(Guid id)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(p => p.Items)
                .Where(p => p.Id == id && !p.IsDeleted)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// 获取分页列表（包含关联数据）
        /// 优化：预加载Items信息，避免N+1查询
        /// Phase 2: Repository层简化（Epic #1725）- 使用BaseRepository辅助方法
        /// </summary>
        public async Task<PagedResult<PrescriptionEntity>> GetPagedWithDetailsAsync(
            int pageNumber,
            int pageSize,
            string? keyword = null)
        {
            var query = _dbSet
                .AsNoTracking()
                .Include(p => p.Items)  // 预加载处方项
                .Where(p => !p.IsDeleted);

            // 关键字搜索
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p =>
                    (p.Indication != null && p.Indication.Contains(keyword)) ||
                    (p.FormulaSource != null && p.FormulaSource.Contains(keyword)) ||
                    p.Items.Any(i => i.HerbName.Contains(keyword)));
            }

            // 使用BaseRepository辅助方法处理分页（Epic #1725）
            return await GetPagedResultAsync(
                query.OrderByDescending(p => p.CreatedAt),
                pageNumber,
                pageSize);
        }

        /// <summary>
        /// 根据患者ID获取处方列表
        /// </summary>
        public async Task<List<PrescriptionEntity>> GetByPatientIdAsync(Guid patientId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(p => p.Items)
                .Where(p => p.PatientId == patientId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 根据病案ID获取处方
        /// </summary>
        public async Task<List<PrescriptionEntity>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(p => p.Items)
                .Where(p => p.MedicalCaseId == medicalCaseId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 根据前缀查询处方编号列表（用于编号生成）
        /// Issue #1551: 处方自动编号功能
        /// </summary>
        public async Task<List<string>> GetPrescriptionNumbersByPrefixAsync(string prefix)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.PrescriptionNumber != null && p.PrescriptionNumber.StartsWith(prefix))
                .Select(p => p.PrescriptionNumber!)
                .ToListAsync();
        }

        // ========== 显式接口实现（Issue #1600 Phase 1）==========
        // 由于BaseRepository返回List<T>,而IPrescriptionRepository定义返回IEnumerable<T>

        /// <summary>
        /// 获取所有实体（显式实现）
        /// </summary>
        async Task<IEnumerable<PrescriptionEntity>> IPrescriptionRepository.GetAllAsync()
        {
            return await GetAllAsync();
        }

        /// <summary>
        /// 根据条件查找（显式实现）
        /// </summary>
        async Task<IEnumerable<PrescriptionEntity>> IPrescriptionRepository.FindAsync(Expression<Func<PrescriptionEntity, bool>> predicate)
        {
            return await FindAsync(predicate);
        }
    }
}
