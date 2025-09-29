using PrescriptionEntity = LYBT.Entities.Prescriptions.Prescription;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Repositories
{
    /// <summary>
    /// 处方仓储 - 优化版，包含Include策略以解决N+1查询问题
    /// </summary>
    public class PrescriptionRepository : BaseRepository<PrescriptionEntity>, IPrescriptionRepository
    {
        private readonly ILogger<PrescriptionRepository> _logger;

        public PrescriptionRepository(AppDbContext context) : base(context)
        {
            _logger = null; // 暂时设为null，后续可通过DI注入
        }

        public PrescriptionRepository(AppDbContext context, ILogger<PrescriptionRepository> logger)
            : base(context, logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 根据ID获取处方（包含处方项和药材信息）
        /// </summary>
        public async Task<PrescriptionEntity> GetByIdWithItemsAsync(Guid id)
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
                    p.Indication.Contains(keyword) ||
                    p.FormulaSource.Contains(keyword) ||
                    p.Items.Any(i => i.HerbName.Contains(keyword)));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<PrescriptionEntity>
            {
                Items = items,
                TotalCount = totalCount,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
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
    }
}