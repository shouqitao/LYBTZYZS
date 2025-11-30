using LYBT.Entities.Consultations;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Consultations.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Consultations.Repositories
{
    /// <summary>
    /// 诊疗仓储 - 继承BaseReadRepository标准实现（Epic #2016 Phase 3）
    /// </summary>
    /// <remarks>
    /// 设计原则：
    /// - 统一共性：继承BaseReadRepository获得5个标准只读方法
    /// - 保持特性：保留诊疗模块特定业务方法
    /// - Read-only模式：所有写操作必须通过MedicalCase聚合根
    /// - Include策略：预加载MedicalCase关联以避免N+1查询
    /// </remarks>
    internal class ConsultationRepository : BaseReadRepository<Consultation>, IConsultationRepository
    {
        public ConsultationRepository(AppDbContext context, ILogger<ConsultationRepository> logger) 
            : base(context, logger)
        {
        }

        /// <summary>
        /// 根据患者ID获取诊疗记录
        /// </summary>
        public async Task<List<Consultation>> GetByPatientIdAsync(Guid patientId)
        {
            return await DbSet
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
        public async Task<PagedResult<Consultation>> GetPagedWithDetailsAsync(
            int pageNumber,
            int pageSize,
            string? keyword = null)
        {
            var query = DbSet
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

            // 分页处理
            query = query.OrderByDescending(c => c.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Consultation>(items, totalCount, pageNumber, pageSize);
        }

        /// <summary>
        /// 根据ID获取诊疗记录（包含所有关联数据）
        /// </summary>
        public async Task<Consultation> GetByIdWithDetailsAsync(Guid id)
        {
            return (await DbSet
            .AsNoTracking()
            .Include(c => c.MedicalCase)
                .Where(c => c.Id == id && !c.IsDeleted)
                .SingleOrDefaultAsync())!;
        }

        /// <summary>
        /// 根据病案ID获取诊疗记录
        /// </summary>
        /// <remarks>
        ///  设计说明：由于Consultation采用共享主键设计（Consultation.Id == MedicalCase.Id），
        /// 此方法与GetByIdAsync(medicalCaseId)在功能上等价，查询条件为c.Id == medicalCaseId。
        /// 保留此方法是为了语义清晰，明确表达"通过病案ID查询诊疗记录"的业务意图。
        /// 参见：ConsultationConfiguration.cs的Fluent API配置
        /// </remarks>
        public async Task<Consultation> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            return (await DbSet
            .AsNoTracking()
            .Include(c => c.MedicalCase)
                .Where(c => c.Id == medicalCaseId && !c.IsDeleted)  // c.Id == MedicalCase.Id（共享主键）
                .SingleOrDefaultAsync())!;
        }
    }
}
