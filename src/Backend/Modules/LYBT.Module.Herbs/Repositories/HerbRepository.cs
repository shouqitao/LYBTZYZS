using LYBT.Infrastructure.Data;
using LYBT.Models.Herbs;
using LYBT.Module.Herbs.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Herbs.Repositories {

    /// <summary>
    /// 药材仓储实现类，实现数据库操作
    /// </summary>
    public class HerbRepository : IHerbRepository {
        private readonly AppDbContext _herbDbContext;
        private readonly ILogger<HerbRepository> _logger;

        /// <summary>
        /// 构造方法，注入数据库上下文
        /// </summary>
        public HerbRepository(AppDbContext herbDbContext, ILogger<HerbRepository> logger) {
            _herbDbContext = herbDbContext;
            _logger = logger;
        }

        /// <summary>
        /// 根据ID获取药材详情
        /// </summary>
        public async Task<HerbModel?> GetByIdAsync(Guid id) {
            try {
                return await _herbDbContext.Herbs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(h => h.Id == id);
            } catch (Exception ex) {
                _logger.LogError(ex, "根据ID获取药材失败，ID: {HerbId}", id);
                throw;
            }
        }

        /// <summary>
        /// 获取所有药材列表
        /// </summary>
        public async Task<List<HerbModel>> GetListAsync() {
            try {
                return await _herbDbContext.Herbs
                    .AsNoTracking()
                    .OrderByDescending(h => h.CreateTime)
                    .ToListAsync();
            } catch (Exception ex) {
                _logger.LogError(ex, "获取药材列表失败");
                throw;
            }
        }

        /// <summary>
        /// 新增药材
        /// </summary>
        public async Task<bool> AddAsync(HerbModel herb) {
            try {
                _herbDbContext.Herbs.Add(herb);
                var result = await _herbDbContext.SaveChangesAsync();
                return result > 0;
            } catch (Exception ex) {
                _logger.LogError(ex, "新增药材失败，药材名称: {HerbName}", herb.Name);
                throw;
            }
        }

        /// <summary>
        /// 更新药材
        /// </summary>
        public async Task<bool> UpdateAsync(HerbModel herb) {
            try {
                _herbDbContext.Herbs.Update(herb);
                var result = await _herbDbContext.SaveChangesAsync();
                return result > 0;
            } catch (Exception ex) {
                _logger.LogError(ex, "更新药材失败，药材ID: {HerbId}", herb.Id);
                throw;
            }
        }

        /// <summary>
        /// 删除药材
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            try {
                var model = await _herbDbContext.Herbs.FindAsync(id);
                if (model == null) {
                    _logger.LogWarning("尝试删除不存在的药材，ID: {HerbId}", id);
                    return false;
                }

                _herbDbContext.Herbs.Remove(model);
                var result = await _herbDbContext.SaveChangesAsync();
                return result > 0;
            } catch (Exception ex) {
                _logger.LogError(ex, "删除药材失败，药材ID: {HerbId}", id);
                throw;
            }
        }

        /// <summary>
        /// 批量新增药材
        /// </summary>
        public async Task<bool> AddRangeAsync(List<HerbModel> herbs) {
            try {
                if (herbs == null || herbs.Count == 0) {
                    return false;
                }

                await _herbDbContext.Herbs.AddRangeAsync(herbs);
                var result = await _herbDbContext.SaveChangesAsync();
                return result > 0;
            } catch (Exception ex) {
                _logger.LogError(ex, "批量新增药材失败，数量: {Count}", herbs?.Count ?? 0);
                throw;
            }
        }

        /// <summary>
        /// 分页查询药材
        /// </summary>
        public async Task<(List<HerbModel> list, int total)> GetPagedAsync(string? keyword, int page, int pageSize) {
            try {
                var query = _herbDbContext.Herbs.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(keyword)) {
                    var upper = keyword.ToUpperInvariant();
                    query = query.Where(h => h.Name.Contains(keyword) ||
                                           (h.PinyinCode != null && h.PinyinCode.Contains(upper)));
                }

                int total = await query.CountAsync();
                var list = await query
                    .OrderByDescending(h => h.CreateTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (list, total);
            } catch (Exception ex) {
                _logger.LogError(ex, "分页查询药材失败，关键词: {Keyword}, 页码: {Page}, 页大小: {PageSize}",
                    keyword, page, pageSize);
                throw;
            }
        }

        /// <summary>
        /// 检查药材名称是否存在
        /// </summary>
        public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null) {
            try {
                var query = _herbDbContext.Herbs.AsNoTracking()
                    .Where(h => h.Name == name);

                if (excludeId.HasValue) {
                    query = query.Where(h => h.Id != excludeId.Value);
                }

                return await query.AnyAsync();
            } catch (Exception ex) {
                _logger.LogError(ex, "检查药材名称是否存在失败，名称: {Name}", name);
                throw;
            }
        }

        /// <summary>
        /// 根据拼音码搜索药材
        /// </summary>
        public async Task<List<HerbModel>> SearchByPinyinAsync(string pinyin) {
            try {
                return await _herbDbContext.Herbs
                    .AsNoTracking()
                    .Where(h => h.PinyinCode != null && h.PinyinCode.Contains(pinyin.ToUpperInvariant()))
                    .OrderBy(h => h.Name)
                    .ToListAsync();
            } catch (Exception ex) {
                _logger.LogError(ex, "根据拼音码搜索药材失败，拼音: {Pinyin}", pinyin);
                throw;
            }
        }
    }
}