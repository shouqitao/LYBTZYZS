using System.Linq.Expressions;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ConsultationEntity = LYBT.Entities.Consultation.Consultation;

namespace LYBT.Module.Consultation.Repositories
{
    /// <summary>
    /// 诊疗仓储 - 优化版，包含Include策略以解决N+1查询问题
    /// </summary>
    internal class ConsultationRepository : BaseRepository<ConsultationEntity>, IConsultationRepository
    {
        public ConsultationRepository(AppDbContext context) : base(context)
        {
        }

        public ConsultationRepository(AppDbContext context, ILogger<ConsultationRepository> logger)
            : base(context, logger)
        {
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
        /// Phase 2: Repository层简化（Epic #1725）- 使用BaseRepository辅助方法
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
                    (c.ChiefComplaint != null && c.ChiefComplaint.Contains(keyword)) ||
                    (c.TCMDiagnosis != null && c.TCMDiagnosis.Contains(keyword)) ||
                    c.MedicalCase.PatientName.Contains(keyword) ||
                    c.MedicalCase.DoctorName.Contains(keyword));
            }

            // 使用BaseRepository辅助方法处理分页（Epic #1725）
            return await GetPagedResultAsync(
                query.OrderByDescending(c => c.CreatedAt),
                pageNumber,
                pageSize);
        }

        /// <summary>
        /// 根据ID获取诊疗记录（包含所有关联数据）
        /// </summary>
        public async Task<ConsultationEntity> GetByIdWithDetailsAsync(Guid id)
        {
            return (await _dbSet
            .AsNoTracking()
            .Include(c => c.MedicalCase)
                .Where(c => c.Id == id && !c.IsDeleted)
                .SingleOrDefaultAsync())!;
        }

        /// <summary>
        /// 根据病案ID获取诊疗记录
        /// </summary>
        /// <remarks>
        /// ⚠️ 设计说明：由于Consultation采用共享主键设计（Consultation.Id == MedicalCase.Id），
        /// 此方法与GetByIdAsync(medicalCaseId)在功能上等价，查询条件为c.Id == medicalCaseId。
        /// 保留此方法是为了语义清晰，明确表达"通过病案ID查询诊疗记录"的业务意图。
        /// 参见：ConsultationConfiguration.cs的Fluent API配置
        /// </remarks>
        public async Task<ConsultationEntity> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            return (await _dbSet
            .AsNoTracking()
            .Include(c => c.MedicalCase)
                .Where(c => c.Id == medicalCaseId && !c.IsDeleted)  // c.Id == MedicalCase.Id（共享主键）
                .SingleOrDefaultAsync())!;
        }

        // ========== 显式接口实现（Issue #1600 Phase 1）==========
        // 由于BaseRepository返回List<T>,而IConsultationRepository定义返回IEnumerable<T>

        /// <summary>
        /// 获取所有实体（显式实现）
        /// </summary>
        async Task<IEnumerable<ConsultationEntity>> IConsultationRepository.GetAllAsync()
        {
            return await GetAllAsync();
        }

        /// <summary>
        /// 根据条件查找（显式实现）
        /// </summary>
        async Task<IEnumerable<ConsultationEntity>> IConsultationRepository.FindAsync(Expression<Func<ConsultationEntity, bool>> predicate)
        {
            return await FindAsync(predicate);
        }
    }
}
