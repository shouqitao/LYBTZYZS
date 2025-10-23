using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MedicalCaseEntity = LYBT.Entities.MedicalCase.MedicalCase;
using ConsultationEntity = LYBT.Entities.Consultation.Consultation;
using PrescriptionEntity = LYBT.Entities.Prescriptions.Prescription;

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
    }
}
