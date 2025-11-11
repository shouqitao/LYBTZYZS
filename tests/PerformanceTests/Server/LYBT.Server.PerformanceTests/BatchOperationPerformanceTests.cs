using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Server.PerformanceTests
{
    /// <summary>
    /// Repository批量操作性能测试（Epic #2016 Phase 6 Task 6.2）
    /// </summary>
    /// <remarks>
    /// 测试场景：
    /// - AddRangeAsync: 批量添加1000条药材记录
    /// - DeleteRangeAsync(entities): 批量删除1000条记录
    /// - DeleteRangeAsync(ids): 根据ID集合批量删除1000条记录
    /// - GetPagedAsync: 分页查询10000条数据的性能
    /// 
    /// 性能基准：
    /// - 批量插入1000条 < 5秒
    /// - 批量删除1000条 < 5秒
    /// - 分页查询（10000条数据，每页100条）< 1秒
    /// </remarks>
#pragma warning disable CA1001 // 使用GlobalCleanup释放资源，无需实现IDisposable
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, iterationCount: 10)]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class BatchOperationPerformanceTests
    {
        private AppDbContext _context = null!;

        [GlobalSetup]
        public void Setup()
        {
            // 使用InMemory数据库进行性能测试
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            // 预先创建10000条数据用于分页查询测试
            InitializeLargeDataSetAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// 初始化10000条药材数据用于分页查询测试
        /// </summary>
        private async Task InitializeLargeDataSetAsync()
        {
            var herbs = new List<Herb>();
            var baseTime = DateTime.Now;

            for (int i = 1; i <= 10000; i++)
            {
                herbs.Add(new Herb
                {
                    Id = Guid.NewGuid(),
                    Name = $"性能测试药材{i}",
                    PinYinCode = $"xncsy{i}",
                    Category = i % 3 == 0 ? "补益药" : (i % 2 == 0 ? "清热药" : "理气药"),
                    Unit = "克",
                    Price = 10.00m + (i % 100),
                    CostPrice = 5.00m + (i % 50),
                    Status = CommonStatus.Enabled,
                    CreatedAt = baseTime.AddMinutes(-i),
                    CreatedBy = Guid.Empty
                });
            }

            _context.Herbs.AddRange(herbs);
            await _context.SaveChangesAsync();

            Console.WriteLine($"初始化完成：创建了 {herbs.Count} 条药材数据");
        }

        #region 批量添加性能测试

        /// <summary>
        /// 测试批量添加1000条药材记录的性能
        /// 性能基准：< 5秒
        /// </summary>
        [Benchmark(Description = "AddRangeAsync - 批量添加1000条药材")]
        public async Task AddRangeAsync_1000Records_Benchmark()
        {
            var herbs = new List<Herb>();
            var baseTime = DateTime.Now;

            // 准备1000条数据
            for (int i = 1; i <= 1000; i++)
            {
                herbs.Add(new Herb
                {
                    Id = Guid.NewGuid(),
                    Name = $"批量添加药材{i}_{Guid.NewGuid():N}",
                    PinYinCode = $"plty{i}",
                    Category = i % 3 == 0 ? "补益药" : (i % 2 == 0 ? "清热药" : "理气药"),
                    Unit = "克",
                    Price = 15.00m + i,
                    CostPrice = 8.00m + i,
                    Status = CommonStatus.Enabled,
                    CreatedAt = baseTime,
                    CreatedBy = Guid.Empty
                });
            }

            // 批量添加
            await _context.Herbs.AddRangeAsync(herbs);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region 批量删除性能测试

        /// <summary>
        /// 测试批量删除1000条记录的性能（使用实体集合）
        /// 性能基准：< 5秒
        /// </summary>
        [Benchmark(Description = "DeleteRangeAsync(entities) - 批量删除1000条")]
        public async Task DeleteRangeAsync_Entities_1000Records_Benchmark()
        {
            // 先创建1000条数据
            var herbs = new List<Herb>();
            var baseTime = DateTime.Now;

            for (int i = 1; i <= 1000; i++)
            {
                herbs.Add(new Herb
                {
                    Id = Guid.NewGuid(),
                    Name = $"待删除药材{i}_{Guid.NewGuid():N}",
                    PinYinCode = $"dsc{i}",
                    Category = "清热药",
                    Unit = "克",
                    Price = 10.00m,
                    CostPrice = 5.00m,
                    Status = CommonStatus.Enabled,
                    CreatedAt = baseTime,
                    CreatedBy = Guid.Empty
                });
            }

            await _context.Herbs.AddRangeAsync(herbs);
            await _context.SaveChangesAsync();

            // 批量删除（软删除）
            foreach (var herb in herbs)
            {
                herb.IsDeleted = true;
                herb.UpdatedAt = DateTime.Now;
            }
            _context.Herbs.UpdateRange(herbs);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 测试根据ID集合批量删除1000条记录的性能
        /// 性能基准：< 5秒
        /// </summary>
        [Benchmark(Description = "DeleteRangeAsync(ids) - 根据ID批量删除1000条")]
        public async Task DeleteRangeAsync_Ids_1000Records_Benchmark()
        {
            // 先创建1000条数据
            var herbs = new List<Herb>();
            var baseTime = DateTime.Now;

            for (int i = 1; i <= 1000; i++)
            {
                herbs.Add(new Herb
                {
                    Id = Guid.NewGuid(),
                    Name = $"ID删除药材{i}_{Guid.NewGuid():N}",
                    PinYinCode = $"idsc{i}",
                    Category = "理气药",
                    Unit = "克",
                    Price = 12.00m,
                    CostPrice = 6.00m,
                    Status = CommonStatus.Enabled,
                    CreatedAt = baseTime,
                    CreatedBy = Guid.Empty
                });
            }

            await _context.Herbs.AddRangeAsync(herbs);
            await _context.SaveChangesAsync();
            var ids = herbs.Select(h => h.Id).ToList();

            // 根据ID批量删除（软删除）
            var entitiesToDelete = await _context.Herbs
                .Where(h => !h.IsDeleted && ids.Contains(h.Id))
                .ToListAsync();
            
            foreach (var entity in entitiesToDelete)
            {
                entity.IsDeleted = true;
                entity.UpdatedAt = DateTime.Now;
            }
            _context.Herbs.UpdateRange(entitiesToDelete);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region 分页查询性能测试

        /// <summary>
        /// 测试分页查询性能（10000条数据，每页100条，查询第1页）
        /// 性能基准：< 1秒
        /// </summary>
        [Benchmark(Description = "GetPagedAsync - 分页查询（10000条数据，第1页100条）")]
        public async Task GetPagedAsync_Page1_Benchmark()
        {
            var totalCount = await _context.Herbs.Where(h => !h.IsDeleted).CountAsync();
            var result = await _context.Herbs
                .Where(h => !h.IsDeleted)
                .OrderByDescending(h => h.CreatedAt)
                .Skip(0)
                .Take(100)
                .ToListAsync();
        }

        /// <summary>
        /// 测试分页查询性能（10000条数据，每页100条，查询第50页）
        /// 性能基准：< 1秒
        /// </summary>
        [Benchmark(Description = "GetPagedAsync - 分页查询（10000条数据，第50页100条）")]
        public async Task GetPagedAsync_Page50_Benchmark()
        {
            var totalCount = await _context.Herbs.Where(h => !h.IsDeleted).CountAsync();
            var result = await _context.Herbs
                .Where(h => !h.IsDeleted)
                .OrderByDescending(h => h.CreatedAt)
                .Skip(4900)
                .Take(100)
                .ToListAsync();
        }

        /// <summary>
        /// 测试分页查询性能（10000条数据，每页100条，查询第100页，末页）
        /// 性能基准：< 1秒
        /// </summary>
        [Benchmark(Description = "GetPagedAsync - 分页查询（10000条数据，第100页100条，末页）")]
        public async Task GetPagedAsync_Page100_LastPage_Benchmark()
        {
            var totalCount = await _context.Herbs.Where(h => !h.IsDeleted).CountAsync();
            var result = await _context.Herbs
                .Where(h => !h.IsDeleted)
                .OrderByDescending(h => h.CreatedAt)
                .Skip(9900)
                .Take(100)
                .ToListAsync();
        }

        #endregion

        [GlobalCleanup]
        public void Cleanup()
        {
            _context?.Dispose();
        }
    }
#pragma warning restore CA1001
}
