using System.Threading;
using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Repositories
{
    /// <summary>
    /// 医案仓储 - 更新相关逻辑（含并发刷新与实体状态修复）
    /// ctor 与 _context/_dbSet/_logger 由主文件提供
    /// </summary>
    internal partial class MedicalCaseRepository
    {
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
                throw new KeyNotFoundException($"医案 {entity.Id} 不存在");

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
    }
}
