using MedicalCaseEntity = LYBT.Entities.MedicalCase.MedicalCase;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCase.Repositories
{
    /// <summary>
    /// 病案仓储 - 优化版，包含Include策略以解决N+1查询问题
    /// </summary>
    public class MedicalCaseRepository : BaseRepository<MedicalCaseEntity>, IMedicalCaseRepository
    {
        private readonly ILogger<MedicalCaseRepository> _logger;

        public MedicalCaseRepository(AppDbContext context) : base(context)
        {
            _logger = null; // 暂时设为null，后续可通过DI注入
        }

        public MedicalCaseRepository(AppDbContext context, ILogger<MedicalCaseRepository> logger)
            : base(context, logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 根据患者ID获取医疗案例（包含关联数据）
        /// </summary>
        public async Task<List<MedicalCaseEntity>> GetByPatientIdAsync(Guid patientId)
        {
            return await _dbSet
                .Include(m => m.Consultation)
                .Include(m => m.Prescription)
                    .ThenInclude(p => p.Items)  // 包含处方项
                .Where(m => m.PatientId == patientId && !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 根据ID获取病案（包含所有关联数据）
        /// </summary>
        public async Task<MedicalCaseEntity> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(m => m.Consultation)
                    .ThenInclude(c => c.Patient)  // 包含诊疗关联的患者
                .Include(m => m.Consultation)
                    .ThenInclude(c => c.User)     // 包含诊疗关联的医生
                .Include(m => m.Prescription)
                    .ThenInclude(p => p.Items)    // 包含处方项
                .Where(m => m.Id == id && !m.IsDeleted)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// 获取分页列表（包含关联数据）
        /// 优化：预加载Consultation和Prescription信息，避免N+1查询
        /// </summary>
        public async Task<PagedResult<MedicalCaseEntity>> GetPagedWithDetailsAsync(
            int pageNumber,
            int pageSize,
            string keyword = null)
        {
            var query = _dbSet
                .Include(m => m.Consultation)
                .Include(m => m.Prescription)
                .Where(m => !m.IsDeleted);

            // 关键字搜索
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(m =>
                    m.PatientName.Contains(keyword) ||
                    m.DoctorName.Contains(keyword) ||
                    m.Remark.Contains(keyword));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<MedicalCaseEntity>
            {
                Items = items,
                TotalCount = totalCount,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// 根据医生ID获取病案列表
        /// </summary>
        public async Task<List<MedicalCaseEntity>> GetByDoctorIdAsync(Guid doctorId)
        {
            return await _dbSet
                .Include(m => m.Consultation)
                .Include(m => m.Prescription)
                .Where(m => m.DoctorId == doctorId && !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }
    }
}
}