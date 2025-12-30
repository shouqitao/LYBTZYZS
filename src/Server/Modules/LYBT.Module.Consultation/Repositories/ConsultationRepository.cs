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
        /// OpenSpec: refactor-server-ddd-aggregates - 使用子查询替代Include关联MedicalCase
        /// </summary>
        public async Task<List<Consultation>> GetByPatientIdAsync(Guid patientId)
        {
            // 使用子查询获取患者的所有MedicalCaseId
            // Consultation使用共享主键（Consultation.Id == MedicalCase.Id）
            var medicalCaseIds = _context.Set<LYBT.Entities.MedicalCases.MedicalCase>()
                .Where(mc => mc.PatientId == patientId && !mc.IsDeleted)
                .Select(mc => mc.Id);

            return await DbSet
                .AsNoTracking()
                .Where(c => medicalCaseIds.Contains(c.Id) && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 获取分页列表（包含关联数据）
        /// 优化：使用联接查询MedicalCase信息，避免N+1查询
        /// Phase 2: Repository层简化（Epic #1725）- 使用BaseRepository辅助方法
        /// OpenSpec: refactor-server-ddd-aggregates - 使用联接替代Include关联MedicalCase
        /// </summary>
        public async Task<PagedResult<Consultation>> GetPagedWithDetailsAsync(
            int pageNumber,
            int pageSize,
            string? keyword = null)
        {
            var query = DbSet
                .AsNoTracking()
                .Where(c => !c.IsDeleted);

            // 关键字搜索 - 使用联接查询MedicalCase信息
            // OpenSpec: refactor-diagnosis-fields - 更新为4个核心字段
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                // 使用子查询获取匹配关键字的MedicalCaseId
                var matchingMedicalCaseIds = _context.Set<LYBT.Entities.MedicalCases.MedicalCase>()
                    .Where(mc => !mc.IsDeleted &&
                        (mc.PatientName.Contains(keyword) || mc.DoctorName.Contains(keyword)))
                    .Select(mc => mc.Id);

                query = query.Where(c =>
                    (c.PresentIllness != null && c.PresentIllness.Contains(keyword)) ||
                    (c.TcmDiagnosis != null && c.TcmDiagnosis.Contains(keyword)) ||
                    matchingMedicalCaseIds.Contains(c.Id));
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
        /// OpenSpec: refactor-server-ddd-aggregates - 移除Include，PatientName/DoctorName由Service层通过共享主键查询MedicalCase获取
        /// </summary>
        public async Task<Consultation> GetByIdWithDetailsAsync(Guid id)
        {
            return (await DbSet
                .AsNoTracking()
                .Where(c => c.Id == id && !c.IsDeleted)
                .SingleOrDefaultAsync())!;
        }

        /// <summary>
        /// 根据病案ID获取诊疗记录
        /// </summary>
        /// <remarks>
        /// 设计说明：由于Consultation采用共享主键设计（Consultation.Id == MedicalCase.Id），
        /// 此方法与GetByIdAsync(medicalCaseId)在功能上等价，查询条件为c.Id == medicalCaseId。
        /// 保留此方法是为了语义清晰，明确表达"通过病案ID查询诊疗记录"的业务意图。
        /// 参见：ConsultationConfiguration.cs的Fluent API配置
        /// OpenSpec: refactor-server-ddd-aggregates - 移除Include
        /// </remarks>
        public async Task<Consultation> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            return (await DbSet
                .AsNoTracking()
                .Where(c => c.Id == medicalCaseId && !c.IsDeleted)  // c.Id == MedicalCase.Id（共享主键）
                .SingleOrDefaultAsync())!;
        }

        /// <summary>
        /// 根据ID获取MedicalCase信息（PatientName, DoctorName）
        /// OpenSpec: refactor-server-ddd-aggregates - 提供跨聚合查询的辅助方法
        /// </summary>
        /// <param name="id">共享主键ID（Consultation.Id == MedicalCase.Id）</param>
        /// <returns>包含PatientName和DoctorName的元组，不存在则返回null</returns>
        public async Task<(string PatientName, string DoctorName)?> GetMedicalCaseInfoAsync(Guid id)
        {
            var result = await _context.Set<LYBT.Entities.MedicalCases.MedicalCase>()
                .AsNoTracking()
                .Where(mc => mc.Id == id && !mc.IsDeleted)
                .Select(mc => new { mc.PatientName, mc.DoctorName })
                .FirstOrDefaultAsync();

            return result != null ? (result.PatientName, result.DoctorName) : null;
        }
    }
}
