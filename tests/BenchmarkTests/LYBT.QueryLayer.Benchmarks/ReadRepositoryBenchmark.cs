using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.QueryLayer.Benchmarks
{
    /// <summary>
    /// ReadRepository性能基准测试
    /// 对比缓存和非缓存查询的性能差异
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, targetCount: 10)]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class ReadRepositoryBenchmark
    {
        private AppDbContext _context = null!;
        private TestReadOnlyRepository _repositoryWithCache = null!;
        private TestReadOnlyRepository _repositoryWithoutCache = null!;
        private IMapper _mapper = null!;
        private IMemoryCache _cache = null!;
        private IMemoryCache _noOpCache = null!;
        private readonly List<Guid> _userIds = new();

        [GlobalSetup]
        public void Setup()
        {
            // 设置内存数据库
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            // 设置AutoMapper
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<User, UserDetailDto>();
            });
            _mapper = mapperConfig.CreateMapper();

            // 设置缓存
            _cache = new MemoryCache(new MemoryCacheOptions());
            _noOpCache = new NoOpMemoryCache(); // 模拟无缓存

            var logger = NullLogger<TestReadOnlyRepository>.Instance;

            // 创建两个仓储实例
            _repositoryWithCache = new TestReadOnlyRepository(_context, _mapper, logger, _cache);
            _repositoryWithoutCache = new TestReadOnlyRepository(_context, _mapper, logger, _noOpCache);

            // 初始化测试数据
            InitializeTestData();
        }

        private void InitializeTestData()
        {
            var users = new List<User>();
            for (int i = 1; i <= 1000; i++)
            {
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    UserName = $"user{i}",
                    Email = $"user{i}@example.com",
                    FullName = $"Test User {i}",
                    PhoneNumber = $"1234567890{i:0000}",
                    IsDeleted = false,
                    Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                    CreatedAt = DateTime.Now.AddDays(-i),
                    CreatedBy = Guid.Empty,
                    UpdatedAt = null,
                    UpdatedBy = null
                };
                users.Add(user);
                
                if (i <= 10)
                {
                    _userIds.Add(user.Id); // 保存前10个用户ID用于测试
                }
            }
            _context.Users.AddRange(users);
            _context.SaveChanges();
        }

        #region 单个实体查询基准测试

        [Benchmark(Baseline = true)]
        public async Task GetByIdAsync_WithoutCache()
        {
            foreach (var id in _userIds)
            {
                await _repositoryWithoutCache.GetByIdAsync(id);
            }
        }

        [Benchmark]
        public async Task GetByIdAsync_WithCache_FirstTime()
        {
            // 清空缓存，模拟首次查询
            _cache = new MemoryCache(new MemoryCacheOptions());
            var repository = new TestReadOnlyRepository(_context, _mapper, 
                NullLogger<TestReadOnlyRepository>.Instance, _cache);
            
            foreach (var id in _userIds)
            {
                await repository.GetByIdAsync(id);
            }
        }

        [Benchmark]
        public async Task GetByIdAsync_WithCache_Cached()
        {
            // 预热缓存
            foreach (var id in _userIds)
            {
                await _repositoryWithCache.GetByIdAsync(id);
            }

            // 测试缓存命中
            foreach (var id in _userIds)
            {
                await _repositoryWithCache.GetByIdAsync(id);
            }
        }

        #endregion

        #region 分页查询基准测试

        [Benchmark]
        public async Task GetPagedAsync_WithoutCache()
        {
            for (int page = 1; page <= 5; page++)
            {
                await _repositoryWithoutCache.GetPagedAsync(null, page, 20);
            }
        }

        [Benchmark]
        public async Task GetPagedAsync_WithCache_FirstTime()
        {
            _cache = new MemoryCache(new MemoryCacheOptions());
            var repository = new TestReadOnlyRepository(_context, _mapper,
                NullLogger<TestReadOnlyRepository>.Instance, _cache);
            
            for (int page = 1; page <= 5; page++)
            {
                await repository.GetPagedAsync(null, page, 20);
            }
        }

        [Benchmark]
        public async Task GetPagedAsync_WithCache_Cached()
        {
            // 预热缓存
            for (int page = 1; page <= 5; page++)
            {
                await _repositoryWithCache.GetPagedAsync(null, page, 20);
            }

            // 测试缓存命中
            for (int page = 1; page <= 5; page++)
            {
                await _repositoryWithCache.GetPagedAsync(null, page, 20);
            }
        }

        #endregion

        #region 全表查询基准测试

        [Benchmark]
        public async Task GetAllAsync_WithoutCache()
        {
            await _repositoryWithoutCache.GetAllAsync();
        }

        [Benchmark]
        public async Task GetAllAsync_WithCache_FirstTime()
        {
            _cache = new MemoryCache(new MemoryCacheOptions());
            var repository = new TestReadOnlyRepository(_context, _mapper,
                NullLogger<TestReadOnlyRepository>.Instance, _cache);
            
            await repository.GetAllAsync();
        }

        [Benchmark]
        public async Task GetAllAsync_WithCache_Cached()
        {
            // 预热缓存
            await _repositoryWithCache.GetAllAsync();

            // 测试缓存命中
            await _repositoryWithCache.GetAllAsync();
        }

        #endregion

        [GlobalCleanup]
        public void Cleanup()
        {
            _context?.Dispose();
            _cache?.Dispose();
        }
    }

    /// <summary>
    /// 测试用的ReadOnlyRepository实现
    /// </summary>
    internal class TestReadOnlyRepository : ReadOnlyRepository<User>
    {
        public TestReadOnlyRepository(
            AppDbContext context,
            IMapper mapper,
            ILogger<TestReadOnlyRepository> logger,
            IMemoryCache cache)
            : base(context, mapper, logger, cache)
        {
        }

        protected override IQueryable<User> ApplyGlobalFilters(IQueryable<User> query)
        {
            return query.Where(u => !u.IsDeleted);
        }
    }

    /// <summary>
    /// 测试用DTO
    /// </summary>
    internal class UserDetailDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 无操作的缓存实现（用于模拟无缓存）
    /// </summary>
    internal class NoOpMemoryCache : IMemoryCache
    {
        public ICacheEntry CreateEntry(object key)
        {
            return new NoOpCacheEntry();
        }

        public void Dispose() { }

        public void Remove(object key) { }

        public bool TryGetValue(object key, out object? value)
        {
            value = null;
            return false;
        }
    }

    internal class NoOpCacheEntry : ICacheEntry
    {
        public object Key { get; set; } = new object();
        public object? Value { get; set; }
        public DateTimeOffset? AbsoluteExpiration { get; set; }
        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public IList<IChangeToken> ExpirationTokens { get; } = new List<IChangeToken>();
        public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; } = new List<PostEvictionCallbackRegistration>();
        public CacheItemPriority Priority { get; set; }
        public long? Size { get; set; }

        public void Dispose() { }
    }
}