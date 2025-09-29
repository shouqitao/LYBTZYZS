using ConsultationEntity = LYBT.Entities.Consultation.Consultation;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Consultation.Repositories
{
    /// <summary>
    /// 诊疗仓储 - 优化版，包含Include策略以解决N+1查询问题
    /// </summary>
    public class ConsultationRepository : BaseRepository<ConsultationEntity>, IConsultationRepository
    {
        private readonly ILogger<ConsultationRepository> _logger;

        public ConsultationRepository(AppDbContext context) : base(context)
        {
            _logger = null; // 暂时设为null，后续可通过DI注入
        }

        public ConsultationRepository(AppDbContext context, ILogger<ConsultationRepository> logger)
            : base(context, logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 根据患者ID获取诊疗记录
        /// </summary>
        public async Task<List<ConsultationEntity>> GetByPatientIdAsync(Guid patientId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(c => c.MedicalCase)  // 包含医疗案例信息
                .Where(c => c.MedicalCase.PatientId == patientId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 获取分页列表（包含关联数据）
        /// 优化：预加载Patient和User信息，避免N+1查询
        /// </summary>
        public async Task<PagedResult<ConsultationEntity>> GetPagedWithDetailsAsync(
            int pageNumber,
            int pageSize,
            string? keyword = null)
        {
            var query = _dbSet
                .AsNoTracking()
                .Include(c => c.MedicalCase)  // 预加载病案信息（包含患者和医生信息）
                .Where(c => !c.IsDeleted);

            // 关键字搜索
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(c =>
                    c.ChiefComplaint.Contains(keyword) ||
                    c.TCMDiagnosis.Contains(keyword) ||
                    c.MedicalCase.PatientName.Contains(keyword) ||
                    c.MedicalCase.DoctorName.Contains(keyword));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<ConsultationEntity>
            {
                Items = items,
                TotalCount = totalCount,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// 根据ID获取诊疗记录（包含所有关联数据）
        /// </summary>
        public async Task<ConsultationEntity> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet
            .AsNoTracking()
            .Include(c => c.MedicalCase)
                .Where(c => c.Id == id && !c.IsDeleted)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// 根据病案ID获取诊疗记录
        /// </summary>
        public async Task<ConsultationEntity> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            return await _dbSet
            .AsNoTracking()
            .Include(c => c.MedicalCase)
                .Where(c => c.Id == medicalCaseId && !c.IsDeleted)
                .FirstOrDefaultAsync();
        }
    }
}