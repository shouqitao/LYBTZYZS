using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Repositories
{

    /// <summary>
    /// 表示PrescriptionRepository。
    /// </summary>
    public class PrescriptionRepository : OptimizedBaseRepository<Prescription>, IPrescriptionRepository
    {

        public PrescriptionRepository(
            AppDbContext context,
            ILogger<PrescriptionRepository> logger,
            IMemoryCache cache)
            : base(context, logger, cache)
        {
        }

        /// <summary>
        /// 执行GetByIdAsync操作。
        /// </summary>
        /// <param name="id">参数id</param>        /// <returns>返回值</returns>
        public override async Task<Prescription?> GetByIdAsync(Guid id)
        {
            var cacheKey = $"{CacheKeyPrefix}withItems:{id}";

            if (_cache.TryGetValue<Prescription>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取处方详情 {Id}", id);
                return cached;
            }

            var prescription = await _dbSet
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prescription != null)
            {
                _cache.Set(cacheKey, prescription, DefaultCacheDuration);
            }

            return prescription;
        }

        /// <summary>
        /// 执行GetListAsync操作。
        /// </summary>
        /// <returns>返回值</returns>
        public async Task<List<Prescription>> GetListAsync()
        {
            var cacheKey = $"{CacheKeyPrefix}allWithItems";

            if (_cache.TryGetValue<List<Prescription>>(cacheKey, out var cached) && cached != null)
            {
                _logger.LogDebug("从缓存获取处方列表");
                return cached;
            }

            var prescriptions = await _dbSet
                .Include(p => p.Items)
                .ToListAsync();

            _cache.Set(cacheKey, prescriptions, DefaultCacheDuration);
            return prescriptions;
        }

        /// <summary>
        /// 执行AddAsync操作。
        /// </summary>
        /// <param name="model">参数model</param>        /// <returns>返回值</returns>
        /// <summary>
        /// 新增处方（业务接口）
        /// </summary>
        public new async Task<bool> AddAsync(Prescription model)
        {
            var addedEntity = await base.AddAsync(model);
            var result = await _context.SaveChangesAsync() > 0;

            if (result)
            {
                _logger.LogInformation("新增处方成功 {Id}", model.Id);
            }

            return result;
        }

        /// <summary>
        /// 执行UpdateAsync操作。
        /// </summary>
        /// <param name="model">参数model</param>        /// <returns>返回值</returns>
        /// <summary>
        /// 更新处方（业务接口）
        /// </summary>
        public new async Task<bool> UpdateAsync(Prescription model)
        {
            var updatedEntity = await base.UpdateAsync(model);
            var result = await _context.SaveChangesAsync() > 0;

            if (result)
            {
                _logger.LogInformation("更新处方成功 {Id}", model.Id);
            }

            return result;
        }

        /// <summary>
        /// 执行DeleteAsync操作。
        /// </summary>
        /// <param name="id">参数id</param>        /// <returns>返回值</returns>
        /// <summary>
        /// 删除处方（业务接口）
        /// </summary>
        public new async Task<bool> DeleteAsync(Guid id)
        {
            var result = await base.DeleteAsync(id);

            if (result)
            {
                var saveResult = await _context.SaveChangesAsync() > 0;
                if (saveResult)
                {
                    _logger.LogInformation("删除处方成功 {Id}", id);
                }

                return saveResult;
            }

            return false;
        }

        /// <summary>
        /// 执行CancelAsync操作。
        /// </summary>
        /// <param name="id">参数id</param>
        /// <returns>返回值</returns>
        public async Task<bool> CancelAsync(Guid id)
        {
            var model = await _dbSet.FindAsync(id);
            if (model == null)
            {
                return false;
            }

            model.Status = PrescriptionStatus.Draft;
            await base.UpdateAsync(model);
            var result = await _context.SaveChangesAsync() > 0;

            if (result)
            {
                _logger.LogInformation("取消处方成功 {Id}", id);
            }

            return result;
        }
    }
}
