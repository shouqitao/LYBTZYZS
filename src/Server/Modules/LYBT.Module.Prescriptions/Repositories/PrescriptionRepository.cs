using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Repositories
{
    /// <summary>
    /// 处方仓储 - 继承BaseReadRepository标准实现（Epic #2016 Phase 3）
    /// </summary>
    /// <remarks>
    /// 设计原则：
    /// - 统一共性：继承BaseReadRepository获得5个标准只读方法
    /// - 保持特性：保留处方模块特定业务方法
    /// - Read-only模式：所有写操作必须通过MedicalCase聚合根
    /// - Include策略：预加载Items关联以避免N+1查询
    /// </remarks>
    internal class PrescriptionRepository : BaseReadRepository<Prescription>, IPrescriptionRepository
    {
        public PrescriptionRepository(AppDbContext context, ILogger<PrescriptionRepository> logger) 
            : base(context, logger)
        {
        }

        /// <summary>
        /// 根据ID获取处方（包含处方项和药材信息）
        /// </summary>
        public async Task<Prescription?> GetByIdWithDetailsAsync(Guid id)
        {
            return await DbSet
                .AsNoTracking()
                .Include(p => p.Items)
                .Where(p => p.Id == id && !p.IsDeleted)
                .SingleOrDefaultAsync();
        }

        /// <summary>
        /// 获取分页列表（包含关联数据）
        /// 优化：预加载Items信息，避免N+1查询
        /// Phase 2: Repository层简化（Epic #1725）- 使用BaseRepository辅助方法
        /// </summary>
        public async Task<PagedResult<Prescription>> GetPagedWithDetailsAsync(
            int pageNumber,
            int pageSize,
            string? keyword = null)
        {
            var query = DbSet
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

            // 分页处理
            query = query.OrderByDescending(p => p.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Prescription>(items, totalCount, pageNumber, pageSize);
        }

        /// <summary>
        /// 根据患者ID获取处方列表
        /// OpenSpec: optimize-entity-data-flow - 通过MedicalCase关联获取PatientId
        /// </summary>
        public async Task<List<Prescription>> GetByPatientIdAsync(Guid patientId)
        {
            return await DbSet
                .AsNoTracking()
                .Include(p => p.Items)
                .Include(p => p.MedicalCase)
                .Where(p => p.MedicalCase != null && p.MedicalCase.PatientId == patientId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 根据病案ID获取处方
        /// </summary>
        public async Task<List<Prescription>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            return await DbSet
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
            return await DbSet
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.PrescriptionNumber != null && p.PrescriptionNumber.StartsWith(prefix))
                .Select(p => p.PrescriptionNumber!)
                .ToListAsync();
        }

        /// <summary>
        /// 批量获取处方详情（包含处方项和药材信息）
        /// Task 1.5: 解决N+1查询问题
        /// </summary>
        /// <param name="prescriptionIds">处方ID列表</param>
        /// <returns>处方详情列表（按ID匹配，不存在的ID不返回）</returns>
        public async Task<List<Prescription>> GetByIdsWithItemsAsync(IEnumerable<Guid> prescriptionIds)
        {
            var idList = prescriptionIds.ToList();
            if (!idList.Any())
                return new List<Prescription>();

            return await DbSet
                .AsNoTracking()
                .Include(p => p.Items)
                .Where(p => idList.Contains(p.Id) && !p.IsDeleted)
                .ToListAsync();
        }
    }
}
