using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using LYBT.Entities.Users;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.QueryLayer.Benchmarks
{
    /// <summary>
    /// 批量操作性能基准测试
    /// OpenSpec: optimize-batch-operations Phase 2 - Task 2.8.3
    /// 对比N+1模式 vs 批量API调用的性能差异
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, iterationCount: 5)]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class BatchOperationsBenchmark
    {
        private AppDbContext _context = null!;
        private SqliteConnection _connection = null!;
        private List<Guid> _userIds = new();
        private List<Guid> _herbIds = new();

        // 测试参数：批量操作的数量
        [Params(5, 10, 20, 50)]
        public int BatchSize { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            // 使用SQLite内存数据库（支持ExecuteUpdate）
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;
            _context = new AppDbContext(options);

            // 确保数据库已创建
            _context.Database.EnsureCreated();

            // 初始化测试数据
            InitializeTestData();
        }

        private void InitializeTestData()
        {
            // 创建足够的测试用户（比最大BatchSize大）
            var users = new List<User>();
            for (int i = 0; i < 100; i++)
            {
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    UserName = $"benchuser{i}",
                    PasswordHash = "hash",
                    Email = $"bench{i}@test.com",
                    RealName = $"Benchmark User {i}",
                    PhoneNumber = $"13800{i:D6}",
                    IsDeleted = false,
                    Status = CommonStatus.Enabled,
                    CreatedAt = DateTime.Now,
                    CreatedBy = Guid.Empty,
                    Role = UserRole.Doctor
                };
                users.Add(user);
            }
            _context.Users.AddRange(users);
            _userIds = users.Select(u => u.Id).ToList();

            // 创建测试药材
            var herbs = new List<Herb>();
            for (int i = 0; i < 100; i++)
            {
                var herb = new Herb
                {
                    Id = Guid.NewGuid(),
                    Name = $"基准测试药材{i}",
                    PinYinCode = $"jzcyc{i}",
                    Category = "测试类",
                    IsDeleted = false,
                    Status = CommonStatus.Enabled,
                    CreatedAt = DateTime.Now,
                    CreatedBy = Guid.Empty
                };
                herbs.Add(herb);
            }
            _context.Herbs.AddRange(herbs);
            _herbIds = herbs.Select(h => h.Id).ToList();

            _context.SaveChanges();
        }

        #region 删除操作基准测试 - N+1 模式

        /// <summary>
        /// N+1模式：循环单个删除 - 模拟旧的foreach模式
        /// </summary>
        [Benchmark(Baseline = true)]
        public async Task Delete_N_Plus_1_Pattern()
        {
            var idsToDelete = _userIds.Take(BatchSize).ToList();

            foreach (var id in idsToDelete)
            {
                // 模拟单个删除操作
                var user = await _context.Users.FindAsync(id);
                if (user != null)
                {
                    user.IsDeleted = true;
                    user.UpdatedAt = DateTime.Now;
                }
            }
            await _context.SaveChangesAsync();

            // 恢复数据用于下次测试
            ResetDeletedUsers(idsToDelete);
        }

        /// <summary>
        /// 批量模式：单次批量删除 - 使用ExecuteUpdate
        /// </summary>
        [Benchmark]
        public async Task Delete_Batch_Pattern()
        {
            var idsToDelete = _userIds.Take(BatchSize).ToList();

            // 批量软删除 - 使用EF Core 7+ ExecuteUpdateAsync
            await _context.Users
                .Where(u => idsToDelete.Contains(u.Id))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(u => u.IsDeleted, true)
                    .SetProperty(u => u.UpdatedAt, DateTime.Now));

            // 恢复数据用于下次测试
            ResetDeletedUsers(idsToDelete);
        }

        #endregion

        #region 状态更新基准测试 - N+1 模式

        /// <summary>
        /// N+1模式：循环单个禁用
        /// </summary>
        [Benchmark]
        public async Task Disable_N_Plus_1_Pattern()
        {
            var idsToDisable = _herbIds.Take(BatchSize).ToList();

            foreach (var id in idsToDisable)
            {
                var herb = await _context.Herbs.FindAsync(id);
                if (herb != null)
                {
                    herb.Status = CommonStatus.Disabled;
                    herb.UpdatedAt = DateTime.Now;
                }
            }
            await _context.SaveChangesAsync();

            // 恢复数据
            ResetHerbStatus(idsToDisable, CommonStatus.Enabled);
        }

        /// <summary>
        /// 批量模式：单次批量禁用
        /// </summary>
        [Benchmark]
        public async Task Disable_Batch_Pattern()
        {
            var idsToDisable = _herbIds.Take(BatchSize).ToList();

            // 批量更新状态
            await _context.Herbs
                .Where(h => idsToDisable.Contains(h.Id))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(h => h.Status, CommonStatus.Disabled)
                    .SetProperty(h => h.UpdatedAt, DateTime.Now));

            // 恢复数据
            ResetHerbStatus(idsToDisable, CommonStatus.Enabled);
        }

        #endregion

        #region 辅助方法

        private void ResetDeletedUsers(List<Guid> ids)
        {
            _context.Users
                .Where(u => ids.Contains(u.Id))
                .ExecuteUpdate(setters => setters
                    .SetProperty(u => u.IsDeleted, false));
        }

        private void ResetHerbStatus(List<Guid> ids, CommonStatus status)
        {
            _context.Herbs
                .Where(h => ids.Contains(h.Id))
                .ExecuteUpdate(setters => setters
                    .SetProperty(h => h.Status, status));
        }

        #endregion

        [GlobalCleanup]
        public void Cleanup()
        {
            _context?.Dispose();
            _connection?.Dispose();
        }
    }

    /// <summary>
    /// 简单的性能比较测试 - 可在xUnit中运行
    /// 不使用BenchmarkDotNet，直接使用Stopwatch测量
    /// </summary>
    public class BatchOperationsPerformanceTests
    {
        /// <summary>
        /// 快速性能对比测试 - 验证批量操作比N+1模式快
        /// </summary>
        public static async Task<PerformanceComparisonResult> CompareDeletePerformanceAsync(int batchSize = 20)
        {
            // 使用SQLite内存数据库（支持ExecuteUpdate）
            using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            using var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();

            // 创建测试数据
            var users = Enumerable.Range(0, batchSize * 2).Select(i => new User
            {
                Id = Guid.NewGuid(),
                UserName = $"perftest{i}",
                PasswordHash = "hash",
                Email = $"perf{i}@test.com",
                RealName = $"Perf User {i}",
                IsDeleted = false,
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.Now,
                CreatedBy = Guid.Empty,
                Role = UserRole.Doctor
            }).ToList();

            context.Users.AddRange(users);
            await context.SaveChangesAsync();

            var group1Ids = users.Take(batchSize).Select(u => u.Id).ToList();
            var group2Ids = users.Skip(batchSize).Take(batchSize).Select(u => u.Id).ToList();

            // 测试N+1模式
            var sw1 = Stopwatch.StartNew();
            foreach (var id in group1Ids)
            {
                var user = await context.Users.FindAsync(id);
                if (user != null)
                {
                    user.IsDeleted = true;
                    user.UpdatedAt = DateTime.Now;
                }
            }
            await context.SaveChangesAsync();
            sw1.Stop();
            var nPlus1Time = sw1.ElapsedMilliseconds;

            // 测试批量模式
            var sw2 = Stopwatch.StartNew();
            await context.Users
                .Where(u => group2Ids.Contains(u.Id))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(u => u.IsDeleted, true)
                    .SetProperty(u => u.UpdatedAt, DateTime.Now));
            sw2.Stop();
            var batchTime = sw2.ElapsedMilliseconds;

            return new PerformanceComparisonResult
            {
                BatchSize = batchSize,
                NPlusOneTimeMs = nPlus1Time,
                BatchTimeMs = batchTime,
                ImprovementFactor = nPlus1Time > 0 ? (double)nPlus1Time / Math.Max(1, batchTime) : 1
            };
        }
    }

    /// <summary>
    /// 性能对比结果
    /// </summary>
    public class PerformanceComparisonResult
    {
        public int BatchSize { get; set; }
        public long NPlusOneTimeMs { get; set; }
        public long BatchTimeMs { get; set; }
        public double ImprovementFactor { get; set; }

        public override string ToString()
        {
            return $"BatchSize: {BatchSize}, N+1: {NPlusOneTimeMs}ms, Batch: {BatchTimeMs}ms, Improvement: {ImprovementFactor:F2}x";
        }
    }
}
