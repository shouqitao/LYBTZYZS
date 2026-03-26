using System.Threading;
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
    /// 医案仓储 - 简化版，减少过度复杂的Include策略
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
        public async Task<List<MedicalCase>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default)
        {
            return await GetBaseQuery()
                .Where(m => m.PatientId == patientId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 根据ID获取医案（包含关联数据）
        /// </summary>
        public async Task<MedicalCase> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return (await GetDetailQuery()
                .Where(m => m.Id == id)
                .SingleOrDefaultAsync(cancellationToken))!;
        }

        /// <summary>
        /// 根据ID获取医案（包含关联数据，强制从数据库刷新，不使用缓存）
        /// 用于处理并发场景，确保获取最新的RowVersion
        /// </summary>
        public async Task<MedicalCase?> GetByIdWithDetailsFreshAsync(Guid id, CancellationToken cancellationToken = default)
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
                .SingleOrDefaultAsync(cancellationToken);
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

            return await GetPagedResultAsync(query, pageNumber, pageSize, cancellationToken);
        }

        /// <summary>
        /// 根据医生ID获取医案列表（简化版）
        /// OpenSpec: simplify-medicalcase-dataflow - DoctorId→UserId
        /// </summary>


        /// <summary>
        /// 更新医案（Issue #1571 - 级联删除关联数据）
        /// 当医案状态变更为Closed时，自动删除关联的Consultation和Prescription
        /// Issue #1669 Phase 7: 支持tracked和detached两种entity状态
        /// </summary>
        public override async Task<MedicalCase> UpdateAsync(MedicalCase entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // Issue #1669 Phase 7: 记录初始状态
            var entry = _context.Entry(entity);
            _logger?.LogInformation(" [诊断] UpdateAsync开始 - MedicalCaseId: {Id}, EntryState: {State}, HasPrescription: {HasPrescription}",
                entity.Id, entry.State, entity.Prescription != null);

            // 修复Prescription及Items的实体状态
            await FixPrescriptionEntityStatesAsync(entity, cancellationToken);

            // 获取或加载已存在的实体
            var existingEntity = await GetOrLoadExistingEntityAsync(entity, cancellationToken);

            // Issue #2242: 完成医案时保留关联数据供历史查询
            if (entity.CaseStatus == MedicalCaseStatus.Completed)
            {
                _logger?.LogInformation("医案状态变更为Completed，保留关联数据供历史查询，MedicalCaseId: {MedicalCaseId}", entity.Id);
            }

            // 诊断日志
            LogTrackedEntitiesState();

            await SaveChangesAsync(cancellationToken);
            return existingEntity;
        }


        /// <summary>
        /// 修复Prescription及PrescriptionItems的实体状态
        /// consolidate-code-quality: 从UpdateAsync提取，降低圈复杂度
        /// </summary>
        private async Task FixPrescriptionEntityStatesAsync(MedicalCase entity, CancellationToken cancellationToken = default)
        {
            if (entity.Prescription == null) return;

            var prescriptionEntry = _context.Entry(entity.Prescription);
            _logger?.LogInformation(" [诊断] Prescription状态 - PrescriptionId: {Id}, State: {State}",
                entity.Prescription.Id, prescriptionEntry.State);

            if (prescriptionEntry.State != EntityState.Modified) return;

            var prescriptionExistsInDb = await _context.Set<Prescription>()
                .AnyAsync(p => p.Id == entity.Prescription.Id, cancellationToken);

            if (!prescriptionExistsInDb)
            {
                _logger?.LogInformation(" [修复] 检测到新Prescription被错误标记为Modified，改为Added");
                prescriptionEntry.State = EntityState.Added;
                FixNewPrescriptionItemsState(entity.Prescription);
            }
            else
            {
                await FixExistingPrescriptionItemsStateAsync(entity.Prescription, cancellationToken);
            }
        }

        /// <summary>
        /// 修复新Prescription的Items状态(全部改为Added)
        /// </summary>
        private void FixNewPrescriptionItemsState(Prescription prescription)
        {
            if (prescription.Items == null || !prescription.Items.Any()) return;

            foreach (var item in prescription.Items)
            {
                var itemEntry = _context.Entry(item);
                if (itemEntry.State == EntityState.Modified)
                {
                    _logger?.LogInformation(" [修复] 检测到新PrescriptionItem被错误标记为Modified，改为Added - ItemId: {ItemId}", item.Id);
                    itemEntry.State = EntityState.Added;
                }
            }
        }

        /// <summary>
        /// 修复已存在Prescription的Items状态(检查每个Item是否存在)
        /// Issue #2250 Phase 3: 更新时新添加的Items需改为Added
        /// </summary>
        private async Task FixExistingPrescriptionItemsStateAsync(Prescription prescription, CancellationToken cancellationToken = default)
        {
            if (prescription.Items == null || !prescription.Items.Any()) return;

            foreach (var item in prescription.Items)
            {
                var itemEntry = _context.Entry(item);
                if (itemEntry.State != EntityState.Modified) continue;

                var itemExistsInDb = await _context.Set<PrescriptionItem>()
                    .AnyAsync(pi => pi.Id == item.Id, cancellationToken);

                if (!itemExistsInDb)
                {
                    _logger?.LogInformation(" [修复] 检测到更新时新添加的PrescriptionItem被错误标记为Modified，改为Added - ItemId: {ItemId}", item.Id);
                    itemEntry.State = EntityState.Added;
                }
            }
        }

        /// <summary>
        /// 获取或加载已存在的医案实体
        /// consolidate-code-quality: 处理Detached vs Tracked场景
        /// </summary>
        private async Task<MedicalCase> GetOrLoadExistingEntityAsync(MedicalCase entity, CancellationToken cancellationToken = default)
        {
            var entry = _context.Entry(entity);

            if (entry.State != EntityState.Detached)
            {
                // Tracked场景：entity本身就是existingEntity（Service层场景）
                return entity;
            }

            // Detached场景：查询existingEntity并使用SetValues复制属性
            var existingEntity = await _dbSet
                .Include(m => m.Consultation)
                .Include(m => m.Prescription)
                .FirstOrDefaultAsync(m => m.Id == entity.Id, cancellationToken);

            if (existingEntity == null)
                throw new InvalidOperationException($"医案 {entity.Id} 不存在");

            // 复制属性值到已跟踪的existingEntity
            _context.Entry(existingEntity).CurrentValues.SetValues(entity);
            return existingEntity;
        }

        /// <summary>
        /// 记录ChangeTracker中所有实体状态（诊断用）
        /// </summary>
        private void LogTrackedEntitiesState()
        {
            _logger?.LogInformation(" [诊断] SaveChangesAsync前 - ChangeTracker状态:");
            foreach (var trackedEntry in _context.ChangeTracker.Entries())
            {
                var entityType = trackedEntry.Entity.GetType().Name;
                var entityIdProperty = trackedEntry.Entity.GetType().GetProperty("Id");
                var entityId = entityIdProperty?.GetValue(trackedEntry.Entity) ?? "N/A";
                _logger?.LogInformation("   - {EntityType} (Id: {EntityId}): State={State}",
                    entityType, entityId, trackedEntry.State);
            }
        }

        /// <summary>
        /// 获取待看诊医案列表（Status=Suspended或Active）
        /// Epic #1583 - Phase 5
        /// Bug Fix: 应包含Suspended和Active两种未完成状态
        /// OpenSpec: redesign-pending-queue - 正确的状态判定和序号计算
        /// OpenSpec: unify-pending-query-api - 添加patientId参数支持按患者筛选
        /// </summary>
        public async Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(Guid doctorId, Guid? patientId = null, CancellationToken cancellationToken = default)
        {
            // Epic #2210 Phase 3: 按医生ID过滤，实现多医生数据隔离
            // Bug Fix: 包含Suspended和Active两种未完成状态，挂起后的医案应显示在待诊队列
            // OpenSpec: simplify-medicalcase-dataflow - DoctorId→UserId
            // OpenSpec: unify-case-status - 直接使用CaseStatus，已移除PendingCaseType枚举
            var query = _dbSet
                .Where(m => !m.IsDeleted
                    && (m.CaseStatus == MedicalCaseStatus.Suspended || m.CaseStatus == MedicalCaseStatus.Active)
                    && m.UserId == doctorId);

            // OpenSpec: unify-pending-query-api - 按患者筛选
            if (patientId.HasValue)
            {
                query = query.Where(m => m.PatientId == patientId.Value);
            }

            // Bug Fix: MaskPhoneNumber无法在EF Core查询中翻译，先查询原始数据再在内存中处理
            var rawData = await query
                .Join(
                    _context.Set<Patient>(),
                    m => m.PatientId,
                    p => p.Id,
                    (m, p) => new { MedicalCase = m, Patient = p })
                .OrderBy(r => r.MedicalCase.CreatedAt) // 按创建时间升序
                .Select(r => new
                {
                    PatientId = r.Patient.Id,
                    PatientName = r.Patient.Name,
                    PhoneNumber = r.Patient.PhoneNumber ?? string.Empty,
                    // OpenSpec: unify-case-status - 直接使用MedicalCaseStatus，移除PendingCaseType映射
                    CaseStatus = r.MedicalCase.CaseStatus,
                    MedicalCaseId = r.MedicalCase.Id,
                    CreatedAt = r.MedicalCase.CreatedAt
                })
                .ToListAsync(cancellationToken);

            // 在内存中应用电话脱敏并转换为DTO
            var result = rawData.Select(r => new PendingMedicalCaseDto
            {
                PatientId = r.PatientId,
                PatientName = r.PatientName,
                PhoneNumber = r.PhoneNumber,
                PhoneMasked = MaskPhoneNumber(r.PhoneNumber),
                CaseStatus = r.CaseStatus,
                MedicalCaseId = r.MedicalCaseId,
                CreatedAt = r.CreatedAt
            }).ToList();

            // OpenSpec: redesign-pending-queue - 添加队列序号（基于查询结果顺序）
            for (int i = 0; i < result.Count; i++)
            {
                result[i].QueueNumber = i + 1;
            }

            _logger?.LogInformation("获取待看诊列表（DoctorId: {DoctorId}），共 {Count} 条记录",
                doctorId, result.Count);
            return result;
        }

        /// <summary>
        /// 获取所有待看诊医案列表（管理员专用）
        /// Bug Fix: 应包含Suspended和Active两种未完成状态
        /// OpenSpec: redesign-pending-queue - 正确的状态判定和序号计算
        /// </summary>
        public async Task<List<PendingMedicalCaseDto>> GetAllPendingCasesAsync(CancellationToken cancellationToken = default)
        {
            // Bug Fix: 包含Suspended和Active两种未完成状态
            // OpenSpec: unify-case-status - 直接使用CaseStatus，已移除PendingCaseType枚举
            // Bug Fix: MaskPhoneNumber无法在EF Core查询中翻译，先查询原始数据再在内存中处理
            var rawData = await _dbSet
                .Where(m => !m.IsDeleted && (m.CaseStatus == MedicalCaseStatus.Suspended || m.CaseStatus == MedicalCaseStatus.Active))
                .Join(
                    _context.Set<Patient>(),
                    m => m.PatientId,
                    p => p.Id,
                    (m, p) => new { MedicalCase = m, Patient = p })
                .OrderBy(r => r.MedicalCase.CreatedAt) // 按创建时间升序
                .Select(r => new
                {
                    PatientId = r.Patient.Id,
                    PatientName = r.Patient.Name,
                    PhoneNumber = r.Patient.PhoneNumber ?? string.Empty,
                    // OpenSpec: unify-case-status - 直接使用MedicalCaseStatus，移除PendingCaseType映射
                    CaseStatus = r.MedicalCase.CaseStatus,
                    MedicalCaseId = r.MedicalCase.Id,
                    CreatedAt = r.MedicalCase.CreatedAt
                })
                .ToListAsync(cancellationToken);

            // 在内存中应用电话脱敏并转换为DTO
            var result = rawData.Select(r => new PendingMedicalCaseDto
            {
                PatientId = r.PatientId,
                PatientName = r.PatientName,
                PhoneNumber = r.PhoneNumber,
                PhoneMasked = MaskPhoneNumber(r.PhoneNumber),
                CaseStatus = r.CaseStatus,
                MedicalCaseId = r.MedicalCaseId,
                CreatedAt = r.CreatedAt
            }).ToList();

            // OpenSpec: redesign-pending-queue - 添加队列序号
            for (int i = 0; i < result.Count; i++)
            {
                result[i].QueueNumber = i + 1;
            }

            _logger?.LogInformation("获取所有待看诊列表（管理员），共 {Count} 条记录", result.Count);
            return result;
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
        /// 查询医案列表（支持多条件组合查询）
        /// Issue #1592 - Phase 3
        /// </summary>
        public async Task<List<MedicalCase>> QueryAsync(
            string? patientName = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? diagnosisKeyword = null,
            CancellationToken cancellationToken = default)
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

            // 诊断关键字搜索（搜索Consultation.TcmDiagnosis字段）
            if (!string.IsNullOrWhiteSpace(diagnosisKeyword))
            {
                query = query.Where(m =>
                    m.Consultation != null &&
                    m.Consultation.TcmDiagnosis != null &&
                    m.Consultation.TcmDiagnosis.Contains(diagnosisKeyword));
            }

            // 按创建时间倒序排列
            var result = await query
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync(cancellationToken);

            _logger?.LogInformation("查询医案列表，共 {Count} 条记录，条件：患者={PatientName}, 日期={StartDate}~{EndDate}, 诊断={DiagnosisKeyword}",
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
            // OpenSpec: simplify-medicalcase-dataflow - DoctorId→UserId
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

        /// <summary>
        /// 批量获取医案详情（包含所有关联数据）
        /// OpenSpec: consolidate-medicalcase-detail-queries
        /// 使用EF Core的Contains优化为单次数据库查询
        /// </summary>
        /// <param name="ids">医案ID列表</param>
        /// <returns>医案实体列表</returns>
        public async Task<List<MedicalCase>> GetBatchWithDetailsAsync(List<Guid> ids, CancellationToken cancellationToken = default)
        {
            if (ids == null || !ids.Any())
            {
                return new List<MedicalCase>();
            }

            _logger?.LogInformation("批量获取医案详情，ID数量: {Count}", ids.Count);

            // OPENJSON-COMPAT: 逐个查询避免 EF Core 8 List<Guid>.Contains() 生成 OPENJSON WITH 语法
            // SQL Server 兼容级别 < 130 不支持此语法
            var result = new List<MedicalCase>();
            foreach (var id in ids)
            {
                var entity = await GetDetailQuery()
                    .Where(m => m.Id == id)
                    .SingleOrDefaultAsync(cancellationToken);

                if (entity != null)
                {
                    result.Add(entity);
                }
            }

            // 保持原排序: 按创建时间降序
            result = result.OrderByDescending(m => m.CreatedAt).ToList();

            _logger?.LogInformation("批量获取医案详情完成，返回数量: {Count}", result.Count);

            return result;
        }

        /// <summary>
        /// 直接保存变更（绕过 BaseRepository 的全局 RowVersion 同步）
        /// AD-04: FreshAsync 加载的实体 OriginalValue 已经是最新的 DB 值，
        /// BaseRepository.SaveChangesAsync 的 OriginalValue=CurrentValue 同步
        /// 反而可能在 AppDbContext.SetAuditFields 触发 DetectChanges 后产生冲突。
        /// </summary>
        /// <summary>
        /// 添加打印日志并保存（AD-04 Fix）
        /// 通过 DbContext.Add 显式标记 PrintLog 为 Added 状态，
        /// 避免通过导航属性添加时 EF Core 将有预设 Guid 的新实体错误标记为 Modified。
        /// </summary>
        public async Task<int> AddPrintLogAndSaveAsync(MedicalCasePrintLog printLog, CancellationToken cancellationToken = default)
        {
            // 显式标记为 Added，避免 DetectChanges 将预设 Guid 的新实体标记为 Modified
            _context.Set<MedicalCasePrintLog>().Add(printLog);
            return await SaveChangesAsync(cancellationToken);
        }
    }
}
