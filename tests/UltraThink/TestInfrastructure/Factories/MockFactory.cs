using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Interfaces;

namespace LYBT.Tests.UltraThink.TestInfrastructure.Factories
{
    /// <summary>
    /// Mock对象工厂 - UltraThink设计
    /// 职责单一：专注于创建测试用的Mock对象
    /// 代码干净：提供清晰的Mock创建接口
    /// 性能出色：延迟创建，复用Mock实例
    /// </summary>
    public class MockFactory
    {
        private readonly Dictionary<Type, object> _mockCache = new();

        #region Logger Mocks

        /// <summary>
        /// 创建Logger Mock
        /// </summary>
        public Mock<ILogger<T>> CreateLoggerMock<T>()
        {
            var key = typeof(ILogger<T>);
            if (_mockCache.TryGetValue(key, out var cached))
            {
                return (Mock<ILogger<T>>)cached;
            }

            var mock = new Mock<ILogger<T>>();
            
            // 设置默认行为
            mock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception?, string>>()))
                .Verifiable();

            _mockCache[key] = mock;
            return mock;
        }

        /// <summary>
        /// 创建LoggerFactory Mock
        /// </summary>
        public Mock<ILoggerFactory> CreateLoggerFactoryMock()
        {
            var mock = new Mock<ILoggerFactory>();
            
            mock.Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns((string categoryName) => 
                {
                    var loggerMock = new Mock<ILogger>();
                    return loggerMock.Object;
                });

            return mock;
        }

        #endregion

        #region Repository Mocks

        /// <summary>
        /// 创建通用Repository Mock
        /// </summary>
        public Mock<IRepository<T>> CreateRepositoryMock<T>() where T : class
        {
            var mock = new Mock<IRepository<T>>();
            var dataStore = new List<T>();

            // Setup GetAllAsync
            mock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => dataStore.ToList());

            // Setup GetByIdAsync
            mock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, CancellationToken ct) =>
                {
                    // 简化实现，实际可能需要反射获取Id属性
                    return dataStore.FirstOrDefault();
                });

            // Setup AddAsync
            mock.Setup(x => x.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((T entity, CancellationToken ct) =>
                {
                    dataStore.Add(entity);
                    return entity;
                });

            // Setup UpdateAsync
            mock.Setup(x => x.UpdateAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
                .Returns((T entity, CancellationToken ct) =>
                {
                    // 简化实现
                    return Task.CompletedTask;
                });

            // Setup DeleteAsync
            mock.Setup(x => x.DeleteAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
                .Returns((T entity, CancellationToken ct) =>
                {
                    dataStore.Remove(entity);
                    return Task.CompletedTask;
                });

            // Setup FindAsync
            mock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<T, bool>> predicate, CancellationToken ct) =>
                {
                    var compiled = predicate.Compile();
                    return dataStore.Where(compiled).ToList();
                });

            // Setup ExistsAsync
            mock.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<T, bool>> predicate, CancellationToken ct) =>
                {
                    var compiled = predicate.Compile();
                    return dataStore.Any(compiled);
                });

            // Setup CountAsync
            mock.Setup(x => x.CountAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<T, bool>> predicate, CancellationToken ct) =>
                {
                    if (predicate == null)
                        return dataStore.Count;
                    
                    var compiled = predicate.Compile();
                    return dataStore.Count(compiled);
                });

            // Setup SaveChangesAsync
            mock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            return mock;
        }

        /// <summary>
        /// 创建预填充数据的Repository Mock
        /// </summary>
        public Mock<IRepository<T>> CreateRepositoryMockWithData<T>(List<T> initialData) where T : class
        {
            var mock = CreateRepositoryMock<T>();
            
            // 重新设置GetAllAsync以返回初始数据
            mock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(initialData);

            // 重新设置FindAsync
            mock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<T, bool>> predicate, CancellationToken ct) =>
                {
                    var compiled = predicate.Compile();
                    return initialData.Where(compiled).ToList();
                });

            return mock;
        }

        #endregion

        #region DbContext Mocks

        /// <summary>
        /// 创建DbContext Mock
        /// </summary>
        public Mock<AppDbContext> CreateDbContextMock()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var mock = new Mock<AppDbContext>(options);
            
            return mock;
        }

        /// <summary>
        /// 创建DbSet Mock
        /// </summary>
        public Mock<DbSet<T>> CreateDbSetMock<T>(List<T> data) where T : class
        {
            var queryable = data.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

            mockSet.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

            mockSet.Setup(m => m.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
                .Callback<T, CancellationToken>((entity, ct) => data.Add(entity))
                .ReturnsAsync((T entity, CancellationToken ct) => null!);

            mockSet.Setup(m => m.Remove(It.IsAny<T>()))
                .Callback<T>(entity => data.Remove(entity));

            return mockSet;
        }

        #endregion

        #region Service Mocks

        /// <summary>
        /// 创建缓存服务Mock
        /// </summary>
        public Mock<ICacheService> CreateCacheServiceMock()
        {
            var mock = new Mock<ICacheService>();
            var cache = new Dictionary<string, object>();

            mock.Setup(x => x.GetAsync<It.IsAnyType>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string key, CancellationToken ct) =>
                {
                    cache.TryGetValue(key, out var value);
                    return value;
                });

            mock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Returns((string key, object value, TimeSpan? expiry, CancellationToken ct) =>
                {
                    cache[key] = value;
                    return Task.CompletedTask;
                });

            mock.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string key, CancellationToken ct) =>
                {
                    cache.Remove(key);
                    return Task.CompletedTask;
                });

            return mock;
        }

        /// <summary>
        /// 创建UnitOfWork Mock
        /// </summary>
        public Mock<IUnitOfWork> CreateUnitOfWorkMock()
        {
            var mock = new Mock<IUnitOfWork>();
            
            mock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
                
            mock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
                
            mock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
                
            mock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            return mock;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 清除Mock缓存
        /// </summary>
        public void ClearCache()
        {
            _mockCache.Clear();
        }

        /// <summary>
        /// 验证所有Mock
        /// </summary>
        public void VerifyAll(params Mock[] mocks)
        {
            foreach (var mock in mocks)
            {
                mock.VerifyAll();
            }
        }

        /// <summary>
        /// 重置所有Mock
        /// </summary>
        public void ResetAll(params Mock[] mocks)
        {
            foreach (var mock in mocks)
            {
                mock.Reset();
            }
        }

        #endregion

        #region Async Support Classes

        /// <summary>
        /// 测试用异步枚举器
        /// </summary>
        private class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _inner;

            public TestAsyncEnumerator(IEnumerator<T> inner)
            {
                _inner = inner;
            }

            public T Current => _inner.Current;

            public ValueTask<bool> MoveNextAsync()
            {
                return new ValueTask<bool>(_inner.MoveNext());
            }

            public ValueTask DisposeAsync()
            {
                _inner.Dispose();
                return new ValueTask();
            }
        }

        #endregion
    }

    /// <summary>
    /// Mock扩展方法
    /// </summary>
    public static class MockExtensions
    {
        /// <summary>
        /// 设置异步方法返回值的简化方法
        /// </summary>
        public static IReturnsResult<TMock> ReturnsAsync<TMock, TResult>(
            this ISetup<TMock, Task<TResult>> setup, 
            TResult value) where TMock : class
        {
            return setup.Returns(Task.FromResult(value));
        }

        /// <summary>
        /// 设置异步方法抛出异常的简化方法
        /// </summary>
        public static IThrowsResult ThrowsAsync<TMock>(
            this ISetup<TMock, Task> setup, 
            Exception exception) where TMock : class
        {
            return setup.Throws(exception);
        }
    }
}