using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Patients;
using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Repositories
{
    /// <summary>
    /// 病案仓储 - 简化版，减少过度复杂的Include策略
    /// </summary>
    internal class MedicalCaseRepository : BaseRepository<MedicalCase>, IMedicalCaseRepository
    {
        public MedicalCaseRepository(AppDbContext context, ILogger<MedicalCaseRepository> logger)
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
        public async Task<List<MedicalCase>> GetByPatientIdAsync(Guid patientId)
        {
            return await GetBaseQuery()
                .Where(m => m.PatientId == patientId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 根据ID获取病案（包含关联数据）
        /// </summary>
        public async Task<MedicalCase> GetByIdWithDetailsAsync(Guid id)
        {
            return (await GetDetailQuery()
                .Where(m => m.Id == id)
                .SingleOrDefaultAsync())!;
        }

        /// <summary>
        /// 根据ID获取病案（包含关联数据，强制从数据库刷新，不使用缓存）
        /// 用于处理并发场景，确保获取最新的RowVersion
        /// </summary>
        public async Task<MedicalCase?> GetByIdWithDetailsFreshAsync(Guid id)
        {
            // 分离所有相关缓存实体：MedicalCase、Consultation、Prescription及PrescriptionItems
            var medicalCaseEntry = _context.ChangeTracker.Entries<MedicalCase>()
                .FirstOrDefault(e => e.Entity.Id == id);
            if (medicalCaseEntry != null)
            {
                // 分离关联的Consultation
                var consultationEntry = _context.ChangeTracker.Entries<Consultation>()
                    .FirstOrDefault(e => e.Entity.Id == id); // Consultation使用共享主键
                if (consultationEntry != null)
                {
                    consultationEntry.State = EntityState.Detached;
                }

                // 分离关联的Prescription及其Items
                if (medicalCaseEntry.Entity.Prescription != null)
                {
                    var prescriptionId = medicalCaseEntry.Entity.Prescription.Id;

                    // 先分离PrescriptionItems
                    var prescriptionItemEntries = _context.ChangeTracker.Entries<PrescriptionItem>()
                        .Where(e => e.Entity.PrescriptionId == prescriptionId)
                        .ToList();
                    foreach (var itemEntry in prescriptionItemEntries)
                    {
                        itemEntry.State = EntityState.Detached;
                    }

                    // 再分离Prescription
                    var prescriptionEntry = _context.ChangeTracker.Entries<Prescription>()
                        .FirstOrDefault(e => e.Entity.Id == prescriptionId);
                    if (prescriptionEntry != null)
                    {
                        prescriptionEntry.State = EntityState.Detached;
                    }
                }

                // 最后分离MedicalCase
                medicalCaseEntry.State = EntityState.Detached;
            }

            // 重新查询获取最新数据
            return await GetDetailQuery()
                .Where(m => m.Id == id)
                .SingleOrDefaultAsync();
        }

        /// <summary>
        /// 获取分页列表（简化版，按需Include）
        /// Phase 2: Repository层简化（Epic #1725）- 使用BaseRepository辅助方法
        /// </summary>
        public async Task<PagedResult<MedicalCase>> GetPagedWithDetailsAsync(
            int pageNumber,
            int pageSize,
            string? keyword = null)
        {
            var query = GetDetailQuery();

            // 简化搜索逻辑 - 只搜索基本字段
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(m =>
                    m.PatientName.Contains(keyword) ||
                    m.DoctorName.Contains(keyword));
            }

            // 使用BaseRepository辅助方法处理分页（Epic #1725）
            return await GetPagedResultAsync(
                query.OrderByDescending(m => m.CreatedAt),
                pageNumber,
                pageSize);
        }

        /// <summary>
        /// 根据医生ID获取病案列表（简化版）
        /// </summary>
        

        /// <summary>
        /// 更新医案（Issue #1571 - 级联删除关联数据）
        /// 当医案状态变更为Closed时，自动删除关联的Consultation和Prescription
        /// Issue #1669 Phase 7: 支持tracked和detached两种entity状态
        /// </summary>
        public override async Task<MedicalCase> UpdateAsync(MedicalCase entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            //  Issue #1669 Phase 7: 检查entity的跟踪状态
            // Service层通过GetByIdWithDetailsAsync获取的entity是tracked
            // 其他场景可能传入detached entity
            var entry = _context.Entry(entity);
            _logger?.LogInformation(" [诊断] UpdateAsync开始 - MedicalCaseId: {Id}, EntryState: {State}, HasPrescription: {HasPrescription}",
                entity.Id, entry.State, entity.Prescription != null);

            if (entity.Prescription != null)
            {
                var prescriptionEntry = _context.Entry(entity.Prescription);
                _logger?.LogInformation(" [诊断] Prescription状态 - PrescriptionId: {Id}, State: {State}",
                    entity.Prescription.Id, prescriptionEntry.State);

                //  Issue #1669 Phase 7 + Issue #2250: 修复Prescription及其Items状态错误
                // 如果Prescription是新创建的（State=Modified但在数据库中不存在），将其改为Added
                if (prescriptionEntry.State == EntityState.Modified)
                {
                    var existsInDb = await _context.Set<Prescription>()
                        .AnyAsync(p => p.Id == entity.Prescription.Id);

                    if (!existsInDb)
                    {
                        _logger?.LogInformation(" [修复] 检测到新Prescription被错误标记为Modified，改为Added");
                        prescriptionEntry.State = EntityState.Added;

                        // Issue #2250: 同时修复PrescriptionItem的状态
                        // PrescriptionItem不继承BaseEntity，没有RowVersion，需要单独处理
                        if (entity.Prescription.Items != null && entity.Prescription.Items.Any())
                        {
                            foreach (var item in entity.Prescription.Items)
                            {
                                var itemEntry = _context.Entry(item);
                                if (itemEntry.State == EntityState.Modified)
                                {
                                    _logger?.LogInformation(" [修复] 检测到新PrescriptionItem被错误标记为Modified，改为Added - ItemId: {ItemId}",
                                        item.Id);
                                    itemEntry.State = EntityState.Added;
                                }
                            }
                        }
                    }
                    else
                    {
                        // Issue #2250 Phase 3: Prescription已存在，但Items可能是更新时新添加的
                        // 当Service调用Items.Clear()后添加新Items时，这些新Items会被错误标记为Modified
                        // 需要检查每个Item是否真实存在于数据库中
                        if (entity.Prescription.Items != null && entity.Prescription.Items.Any())
                        {
                            foreach (var item in entity.Prescription.Items)
                            {
                                var itemEntry = _context.Entry(item);
                                if (itemEntry.State == EntityState.Modified)
                                {
                                    // 检查此Item是否存在于数据库
                                    var itemExistsInDb = await _context.Set<PrescriptionItem>()
                                        .AnyAsync(pi => pi.Id == item.Id);

                                    if (!itemExistsInDb)
                                    {
                                        _logger?.LogInformation(" [修复] 检测到更新时新添加的PrescriptionItem被错误标记为Modified，改为Added - ItemId: {ItemId}",
                                            item.Id);
                                        itemEntry.State = EntityState.Added;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            MedicalCase? existingEntity;

            if (entry.State == EntityState.Detached)
            {
                // Detached场景：查询existingEntity并使用SetValues复制属性
                existingEntity = await _dbSet
                    .Include(m => m.Consultation)
                    .Include(m => m.Prescription)
                    .FirstOrDefaultAsync(m => m.Id == entity.Id);

                if (existingEntity == null)
                    throw new InvalidOperationException($"医案 {entity.Id} 不存在");

                // 复制属性值到已跟踪的existingEntity
                _context.Entry(existingEntity).CurrentValues.SetValues(entity);
            }
            else
            {
                // Tracked场景：entity本身就是existingEntity（Service层场景）
                // 无需查询，entity的导航属性已通过GetByIdWithDetailsAsync加载
                existingEntity = entity;
                // RowVersion同步已由BaseRepository.SaveChangesAsync()全局处理（Issue #2250）
            }

            // Issue #2242 修正：完成医案时保留关联数据（Consultation、Prescription）
            // 这些数据需要供历史医案查询功能使用
            // 只有当医案被软删除（IsDeleted=true）时，关联数据才会被级联清理
            if (entity.CaseStatus == MedicalCaseStatus.Completed)
            {
                _logger?.LogInformation("医案状态变更为Completed，保留关联数据供历史查询，MedicalCaseId: {MedicalCaseId}", entity.Id);
            }

            //  Issue #1669 Phase 7: SaveChanges前诊断 - 记录所有tracked entities状态
            _logger?.LogInformation(" [诊断] SaveChangesAsync前 - ChangeTracker状态:");
            foreach (var trackedEntry in _context.ChangeTracker.Entries())
            {
                var entityType = trackedEntry.Entity.GetType().Name;
                var entityIdProperty = trackedEntry.Entity.GetType().GetProperty("Id");
                var entityId = entityIdProperty?.GetValue(trackedEntry.Entity) ?? "N/A";
                _logger?.LogInformation("   - {EntityType} (Id: {EntityId}): State={State}",
                    entityType, entityId, trackedEntry.State);
            }

            await SaveChangesAsync();
            return existingEntity;
        }

        /// <summary>
        /// 获取待看诊医案列表（Status=Draft或Active）
        /// Epic #1583 - Phase 5
        /// Bug Fix: 应包含Draft和Active两种未完成状态
        /// </summary>
        public async Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(Guid doctorId)
        {
            // Epic #2210 Phase 3: 按医生ID过滤，实现多医生数据隔离
            // Bug Fix: 包含Draft和Active两种未完成状态，暂存后的医案应显示在待诊队列
            var result = await _dbSet
                .Where(m => !m.IsDeleted
                    && (m.CaseStatus == MedicalCaseStatus.Draft || m.CaseStatus == MedicalCaseStatus.Active)
                    && m.DoctorId == doctorId)
                .Join(
                    _context.Set<Patient>(),
                    m => m.PatientId,
                    p => p.Id,
                    (m, p) => new { MedicalCase = m, Patient = p })
                .OrderBy(r => r.MedicalCase.CreatedAt) // 按创建时间升序
                .Select(r => new PendingMedicalCaseDto
                {
                    PatientId = r.Patient.Id,
                    PatientName = r.Patient.Name,
                    PhoneNumber = r.Patient.PhoneNumber ?? string.Empty,
                    PhoneMasked = MaskPhoneNumber(r.Patient.PhoneNumber ?? string.Empty),
                    Type = "暂存", // 当前只支持未完成医案
                    MedicalCaseId = r.MedicalCase.Id
                })
                .ToListAsync();

            _logger?.LogInformation("获取待看诊列表（DoctorId: {DoctorId}），共 {Count} 条记录",
                doctorId, result.Count);
            return result ?? new List<PendingMedicalCaseDto>();
        }

        /// <summary>
        /// 获取所有待看诊医案列表（管理员专用）
        /// Bug Fix: 应包含Draft和Active两种未完成状态
        /// </summary>
        public async Task<List<PendingMedicalCaseDto>> GetAllPendingCasesAsync()
        {
            // Bug Fix: 包含Draft和Active两种未完成状态
            var result = await _dbSet
                .Where(m => !m.IsDeleted && (m.CaseStatus == MedicalCaseStatus.Draft || m.CaseStatus == MedicalCaseStatus.Active))
                .Join(
                    _context.Set<Patient>(),
                    m => m.PatientId,
                    p => p.Id,
                    (m, p) => new { MedicalCase = m, Patient = p })
                .OrderBy(r => r.MedicalCase.CreatedAt) // 按创建时间升序
                .Select(r => new PendingMedicalCaseDto
                {
                    PatientId = r.Patient.Id,
                    PatientName = r.Patient.Name,
                    PhoneNumber = r.Patient.PhoneNumber ?? string.Empty,
                    PhoneMasked = MaskPhoneNumber(r.Patient.PhoneNumber ?? string.Empty),
                    Type = "暂存", // 当前只支持未完成医案
                    MedicalCaseId = r.MedicalCase.Id
                })
                .ToListAsync();

            _logger?.LogInformation("获取所有待看诊列表（管理员），共 {Count} 条记录", result.Count);
            return result ?? new List<PendingMedicalCaseDto>();
        }

        /// <summary>
        /// 手机号脱敏处理（138****1234格式）
        /// Epic #1583 - Phase 5
        /// </summary>
        private static string MaskPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length != 11)
                return phoneNumber;

            return $"{phoneNumber.Substring(0, 3)}****{phoneNumber.Substring(7)}";
        }

        /// <summary>
        /// 查询病案列表（支持多条件组合查询）
        /// Issue #1592 - Phase 3
        /// </summary>
        public async Task<List<MedicalCase>> QueryAsync(
            string? patientName = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? diagnosisKeyword = null)
        {
            // 使用GetDetailQuery()以包含Consultation数据（用于诊断关键字搜索）
            var query = GetDetailQuery();

            // 患者姓名模糊匹配
            if (!string.IsNullOrWhiteSpace(patientName))
            {
                query = query.Where(m => m.PatientName.Contains(patientName));
            }

            // 日期范围过滤
            if (startDate.HasValue)
            {
                query = query.Where(m => m.CreatedAt >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                // 结束日期包含当天全天（到23:59:59）
                var endOfDay = endDate.Value.Date.AddDays(1).AddSeconds(-1);
                query = query.Where(m => m.CreatedAt <= endOfDay);
            }

            // 诊断关键字搜索（搜索Consultation.TCMDiagnosis字段）
            if (!string.IsNullOrWhiteSpace(diagnosisKeyword))
            {
                query = query.Where(m =>
                    m.Consultation != null &&
                    m.Consultation.TCMDiagnosis != null &&
                    m.Consultation.TCMDiagnosis.Contains(diagnosisKeyword));
            }

            // 按创建时间倒序排列
            var result = await query
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            _logger?.LogInformation("查询病案列表，共 {Count} 条记录，条件：患者={PatientName}, 日期={StartDate}~{EndDate}, 诊断={DiagnosisKeyword}",
                result.Count, patientName ?? "无", startDate, endDate, diagnosisKeyword ?? "无");

            return result;
        }

        /// <summary>
        /// 获取患者的未完成医案（Status != Completed）
        /// Epic #1676 Phase 4 Task 4.1
        /// Epic #2210 Task 3.1.1: 添加doctorId筛选
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="doctorId">医生ID（为Guid.Empty时不筛选医生）</param>
        /// <returns>未完成的医案实体（包含关联数据），若无则返回null</returns>
        public async Task<MedicalCase?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId)
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
                query = query.Where(m => m.DoctorId == doctorId);
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
                .FirstOrDefaultAsync();

            if (result != null)
            {
                _logger?.LogInformation("找到未完成医案，MedicalCaseId: {MedicalCaseId}, CaseStatus: {CaseStatus}, DoctorId: {DoctorId}",
                    result.Id, result.CaseStatus, result.DoctorId);
            }
            else
            {
                _logger?.LogInformation("未找到患者的未完成医案，PatientId: {PatientId}, DoctorId: {DoctorId}",
                    patientId, doctorId);
            }

            return result;
        }
    }
}
