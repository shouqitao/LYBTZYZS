using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MedicalCaseEntity = LYBT.Entities.MedicalCase.MedicalCase;
using ConsultationEntity = LYBT.Entities.Consultation.Consultation;
using PrescriptionEntity = LYBT.Entities.Prescriptions.Prescription;
using PatientEntity = LYBT.Entities.Patients.Patient;

namespace LYBT.Module.MedicalCase.Repositories
{
    /// <summary>
    /// 病案仓储 - 简化版，减少过度复杂的Include策略
    /// </summary>
    public class MedicalCaseRepository : BaseRepository<MedicalCaseEntity>, IMedicalCaseRepository
    {
        public MedicalCaseRepository(AppDbContext context) : base(context)
        {
        }

        public MedicalCaseRepository(AppDbContext context, ILogger<MedicalCaseRepository> logger)
            : base(context, logger)
        {
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
        /// Epic #1612 Task 1.5: 增强Include策略，预加载Prescription.Items避免N+1查询
        /// </summary>
        private IQueryable<MedicalCaseEntity> GetDetailQuery()
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
            return (await GetDetailQuery()
                .Where(m => m.Id == id)
                .FirstOrDefaultAsync())!;
        }

        /// <summary>
        /// 获取分页列表（简化版，按需Include）
        /// </summary>
        public async Task<PagedResult<MedicalCaseEntity>> GetPagedWithDetailsAsync(
            int pageNumber,
            int pageSize,
            string? keyword = null)
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

        /// <summary>
        /// 更新医案（Issue #1571 - 级联删除关联数据）
        /// 当医案状态变更为Closed时，自动删除关联的Consultation和Prescription
        /// </summary>
        public override async Task<MedicalCaseEntity> UpdateAsync(MedicalCaseEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // 获取数据库中的原实体以检测状态变更
            var existingEntity = await _dbSet
                .Include(m => m.Consultation)
                .Include(m => m.Prescription)
                .FirstOrDefaultAsync(m => m.Id == entity.Id);

            if (existingEntity == null)
                throw new InvalidOperationException($"医案 {entity.Id} 不存在");

            // 检测状态变更：从Active变为Closed
            if (existingEntity.Status != MedicalCaseStatus.Closed && entity.Status == MedicalCaseStatus.Closed)
            {
                _logger?.LogInformation("检测到医案状态变更为Closed，准备级联删除关联数据，MedicalCaseId: {MedicalCaseId}", entity.Id);

                // 删除关联的Consultation（如果存在）
                if (existingEntity.Consultation != null)
                {
                    _logger?.LogInformation("删除关联的Consultation，ConsultationId: {ConsultationId}", existingEntity.Consultation.Id);
                    _context.Set<ConsultationEntity>().Remove(existingEntity.Consultation);
                }

                // 删除关联的Prescription（如果存在）
                if (existingEntity.Prescription != null)
                {
                    _logger?.LogInformation("删除关联的Prescription，PrescriptionId: {PrescriptionId}", existingEntity.Prescription.Id);
                    _context.Set<PrescriptionEntity>().Remove(existingEntity.Prescription);
                }

                _logger?.LogInformation("级联删除完成，即将更新医案状态");
            }

            // 调用基类UpdateAsync完成更新
            return await base.UpdateAsync(entity);
        }

        /// <summary>
        /// 获取待看诊医案列表（Status=Active）
        /// Epic #1583 - Phase 5
        /// </summary>
        public async Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync()
        {
            var result = await _dbSet
                .Where(m => !m.IsDeleted && m.Status == MedicalCaseStatus.Active)
                .Join(
                    _context.Set<PatientEntity>(),
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

            _logger?.LogInformation("获取待看诊列表，共 {Count} 条记录", result.Count);
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
        public async Task<List<MedicalCaseEntity>> QueryAsync(
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
    }
}
