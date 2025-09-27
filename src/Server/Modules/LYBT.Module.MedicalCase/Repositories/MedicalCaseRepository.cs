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
    /// 病案仓储 - 简化版，减少过度复杂的Include策略
    /// </summary>
    public class MedicalCaseRepository : BaseRepository<MedicalCaseEntity>, IMedicalCaseRepository
    {
        private readonly ILogger<MedicalCaseRepository> _logger;

        public MedicalCaseRepository(AppDbContext context) : base(context)
        {
            _logger = null;
        }

        public MedicalCaseRepository(AppDbContext context, ILogger<MedicalCaseRepository> logger)
            : base(context, logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 基础查询 - 简化Include逻辑
        /// </summary>
        private IQueryable<MedicalCaseEntity> GetBaseQuery()
        {
            return _dbSet.Where(m => !m.IsDeleted);
        }

        /// <summary>
        /// 详细查询 - 仅在需要时Include关联数据
        /// </summary>
        private IQueryable<MedicalCaseEntity> GetDetailQuery()
        {
            return _dbSet
                .Include(m => m.Consultation)
                .Include(m => m.Prescription)
                .Where(m => !m.IsDeleted);
        }

        /// <summary>
        /// 根据患者ID获取医疗案例（简化版）
        /// </summary>
        public async Task<List<MedicalCaseEntity>> GetByPatientIdAsync(Guid patientId)
        {
            return await GetBaseQuery()
                .Where(m => m.PatientId == patientId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 根据ID获取病案（包含关联数据）
        /// </summary>
        public async Task<MedicalCaseEntity> GetByIdWithDetailsAsync(Guid id)
        {
            return await GetDetailQuery()
                .Where(m => m.Id == id)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// 获取分页列表（简化版，按需Include）
        /// </summary>
        public async Task<PagedResult<MedicalCaseEntity>> GetPagedWithDetailsAsync(
            int pageNumber,
            int pageSize,
            string keyword = null)
        {
            var query = GetBaseQuery();

            // 简化搜索逻辑 - 只搜索基本字段
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(m =>
                    m.PatientName.Contains(keyword) ||
                    m.DoctorName.Contains(keyword));
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
        /// 根据医生ID获取病案列表（简化版）
        /// </summary>
        public async Task<List<MedicalCaseEntity>> GetByDoctorIdAsync(Guid doctorId)
        {
            return await GetBaseQuery()
                .Where(m => m.DoctorId == doctorId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }
    }
}