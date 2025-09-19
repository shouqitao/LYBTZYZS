using System.ComponentModel;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Herbs.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Herbs.Repositories
{

    /// <summary>
    /// 药材仓储实现类 - 数据层统一化重构
    /// 继承OptimizedBaseRepository获得缓存和性能优化，实现药材特定业务逻辑
    /// </summary>
    public class HerbRepository : OptimizedBaseRepository<Herb>, IHerbRepository
    {

        public HerbRepository(
            AppDbContext context,
            ILogger<HerbRepository> logger,
            IMemoryCache cache) : base(context, logger, cache)
        {
        }

        // 注意：GetByIdAsync, AddAsync, UpdateAsync, DeleteAsync等基础CRUD方法由OptimizedBaseRepository提供，带有缓存优化



    }
}
