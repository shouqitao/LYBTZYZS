using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Extensions;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.MedicalCases.Infrastructure;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Infrastructure
{
    /// <summary>
    /// 医案仓储 - 简化版，减少过度复杂的Include策略
    /// 按职责拆分为 partial 文件：PendingCases（待看诊）、AuditLogs（审计日志）、Update（更新）
    /// ADR-0017: 注入医案模块自己的 DbContext
    /// P0-4/ADR-0024: 聚合根唯一入口 — Prescription/Consultation 仅经本仓储（MedicalCase 聚合内），禁止跨聚合直查 AppDbContext.Prescriptions；
    /// 跨聚合需经 IMedicalCaseCrossModuleService / ICatalogCrossModuleService 接口（见 0024 双JWT隔离 + 聚合边界标注）
    /// </summary>
    public partial class MedicalCaseRepository : BaseRepository<MedicalCase, MedicalCaseDbContext>, IMedicalCaseRepository
    {
        public MedicalCaseRepository(MedicalCaseDbContext context, ILogger<MedicalCaseRepository> logger)
            : base(context, logger)
        {
        }

        /// <summary>
        /// 基础查询 - 简化Include逻辑
        /// </summary>
        private IQueryable<MedicalCase> GetBaseQuery()
        {
            return _dbSet.Where(m => !m.IsDeleted);
        }

        /// <summary>
        /// 详细查询 - 仅在需要时Include关联数据
        /// Epic #1612 Task 1.5: 增强Include策略，预加载Prescription.Items避免N+1查询
        /// </summary>
        private IQueryable<MedicalCase> GetDetailQuery()
        {
            return _dbSet
                .Include(m => m.Consultation)
                .Include(m => m.Prescription!)
                    .ThenInclude(p => p.Items)
                .Where(m => !m.IsDeleted);
        }

        /// <summary>
        /// 根据患者ID获取医疗案例（简化版）
        /// </summary>
        public async Task<List<MedicalCase>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default)
        {
            return await GetBaseQuery()
                .Where(m => m.PatientId == patientId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 根据患者ID分页获取医疗案例（DB层分页）
        /// </summary>
        public async Task<PagedResult<MedicalCase>> GetByPatientIdPagedAsync(Guid patientId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = GetBaseQuery()
                .Where(m => m.PatientId == patientId)
                .OrderByDescending(m => m.CreatedAt);

            return await query.GetPagedResultAsync(pageNumber, pageSize, cancellationToken);
        }

        /// <summary>
        /// 根据ID获取医案（包含关联数据）
        /// </summary>
        /// <summary>批量详情（B1 US-MC-018）</summary>
        public async Task<List<MedicalCase>> GetByIdsWithDetailsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            var idList = ids.Distinct().ToList();
            if (idList.Count == 0)
                return new List<MedicalCase>();

            return await GetDetailQuery()
                .Where(m => idList.Contains(m.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task<MedicalCase> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return (await GetDetailQuery()
                .Where(m => m.Id == id)
                .SingleOrDefaultAsync(cancellationToken))!;
        }

        /// <summary>
        /// 获取分页列表（包含关联数据 + 全部筛选条件，DB 层执行）
        /// Sprint3-X6: 从 Service 内存过滤迁移到 Repository DB 查询
        /// </summary>
        public async Task<PagedResult<MedicalCase>> GetPagedWithDetailsAsync(
            int pageNumber, int pageSize,
            MedicalCaseStatus? status, Guid? patientId, Guid? doctorId,
            bool isAdmin, string? keyword = null, CancellationToken cancellationToken = default)
        {
            var query = GetDetailQuery();

            // 状态筛选
            if (status.HasValue)
            {
                query = query.Where(m => m.CaseStatus == status.Value);
            }

            // 患者筛选
            if (patientId.HasValue)
            {
                query = query.Where(m => m.PatientId == patientId.Value);
            }

            // 关键字搜索（患者姓名 + 中医诊断）
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                query = query.Where(m =>
                    m.PatientName.Contains(kw) ||
                    m.DoctorName.Contains(kw) ||
                    (m.Consultation != null && m.Consultation.TcmDiagnosis != null && m.Consultation.TcmDiagnosis.Contains(kw)));
            }

            // 角色过滤: 非管理员只能看到自己创建的医案
            if (!isAdmin && doctorId.HasValue)
            {
                query = query.Where(m => m.UserId == doctorId.Value);
            }

            // 按创建时间倒序
            query = query.OrderByDescending(m => m.CreatedAt);

            return await query.GetPagedResultAsync(pageNumber, pageSize, cancellationToken);
        }

        /// <summary>
        /// 分页查询医案列表（支持多条件组合查询，DB层分页）
        /// P1-2（2026-08-14）: Doctor 所有权过滤下推 DB（原 Service 内存过滤致 TotalCount 偏大）
        /// </summary>
        public async Task<PagedResult<MedicalCase>> QueryPagedAsync(
            string? patientName,
            DateTime? startDate,
            DateTime? endDate,
            string? diagnosisKeyword,
            int pageNumber,
            int pageSize,
            Guid? doctorId = null,
            bool isAdmin = false,
            CancellationToken cancellationToken = default)
        {
            var query = GetDetailQuery();

            if (!string.IsNullOrWhiteSpace(patientName))
                query = query.Where(m => m.PatientName.Contains(patientName));

            if (startDate.HasValue)
                query = query.Where(m => m.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddSeconds(-1);
                query = query.Where(m => m.CreatedAt <= endOfDay);
            }

            if (!string.IsNullOrWhiteSpace(diagnosisKeyword))
            {
                query = query.Where(m =>
                    m.Consultation != null &&
                    m.Consultation.TcmDiagnosis != null &&
                    m.Consultation.TcmDiagnosis.Contains(diagnosisKeyword));
            }

            // P1-2: 角色过滤下推——非管理员仅看本人医案（TotalCount 随之准确）
            if (!isAdmin && doctorId.HasValue)
                query = query.Where(m => m.UserId == doctorId.Value);

            query = query.OrderByDescending(m => m.CreatedAt);

            return await query.GetPagedResultAsync(pageNumber, pageSize, cancellationToken);
        }

        /// <summary>
        /// 获取患者的未完成医案（Status != Completed）
        /// Epic #1676 Phase 4 Task 4.1
        /// Epic #2210 Task 3.1.1: 添加doctorId筛选
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="doctorId">医生ID（为Guid.Empty时不筛选医生）</param>
        /// <returns>未完成的医案实体（包含关联数据），若无则返回null</returns>
        public async Task<MedicalCase?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId, CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("查询患者未完成医案，PatientId: {PatientId}, DoctorId: {DoctorId}",
                patientId, doctorId);

            // Epic #2210 Phase 3 P0 Bug修复：详细诊断日志
            _logger?.LogInformation("[诊断] 开始构建查询，PatientId: {PatientId}, DoctorId: {DoctorId}, DoctorId.IsEmpty: {IsEmpty}",
                patientId, doctorId, doctorId == Guid.Empty);

            var query = GetDetailQuery()
                .Where(m => m.PatientId == patientId && m.CaseStatus != MedicalCaseStatus.Completed);

            // Epic #2210 Task 3.1.1: Q4医生筛选链 - 仅当doctorId有效时添加筛选条件
            if (doctorId != Guid.Empty)
            {
                _logger?.LogInformation("[诊断] 添加医生ID过滤条件，DoctorId: {DoctorId}", doctorId);
                query = query.Where(m => m.UserId == doctorId);
            }
            else
            {
                _logger?.LogWarning("[诊断] doctorId为空，未添加医生ID过滤条件");
            }

            // Epic #2210 Phase 3 P0 Bug修复：记录生成的SQL
            var sql = query.ToQueryString();
            _logger?.LogInformation("[诊断] 生成的SQL查询：{Sql}", sql);

            var result = await query
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (result != null)
            {
                _logger?.LogInformation("找到未完成医案，MedicalCaseId: {MedicalCaseId}, CaseStatus: {CaseStatus}, UserId: {UserId}",
                    result.Id, result.CaseStatus, result.UserId);
            }
            else
            {
                _logger?.LogInformation("未找到患者的未完成医案，PatientId: {PatientId}, DoctorId: {DoctorId}",
                    patientId, doctorId);
            }

            return result;
        }

        /// <summary>
        /// 按前缀统计医案编号数量（包含软删除，避免编号重复）
        /// T5-P2-11: 医案编号自动生成
        /// </summary>
        public async Task<int> CountByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .IgnoreQueryFilters()
                .CountAsync(mc => mc.CaseNumber != null && mc.CaseNumber.StartsWith(prefix), cancellationToken);
        }

        /// <summary>
        /// 按前缀统计处方编号数量（包含软删除，避免编号重复）
        /// T5-P2-13: 处方编号自动生成
        /// </summary>
        public async Task<int> CountPrescriptionsByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Prescription>()
                .IgnoreQueryFilters()
                .CountAsync(p => p.PrescriptionNumber != null && p.PrescriptionNumber.StartsWith(prefix), cancellationToken);
        }
    }
}
