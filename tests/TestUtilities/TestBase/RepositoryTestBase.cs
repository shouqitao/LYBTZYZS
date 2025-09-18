using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Common;
using LYBT.Tests.Backend.Core;

namespace LYBT.Tests.Backend.TestBase
{
    /// <summary>
    /// Repository层测试基类 - 提供完整的Repository测试基础设施
    /// 专注于数据访问层测试，包含内存数据库和数据验证功能
    /// </summary>
    public abstract class RepositoryTestBase<TRepository, TEntity> : BaseTestFixture
        where TRepository : class
        where TEntity : BaseModel, new()
    {
        #region Repository相关

        /// <summary>
        /// 被测试的Repository实例
        /// </summary>
        protected readonly TRepository Repository;

        #endregion

        protected RepositoryTestBase()
        {
            Repository = CreateRepositoryInstance();
        }

        #region 抽象方法

        /// <summary>
        /// 创建Repository实例 - 子类必须实现
        /// </summary>
        protected abstract TRepository CreateRepositoryInstance();

        #endregion

        #region 测试数据管理

        /// <summary>
        /// 添加测试实体到数据库
        /// </summary>
        protected async Task<TEntity> AddTestEntityAsync(TEntity entity)
        {
            Context.Set<TEntity>().Add(entity);
            await Context.SaveChangesAsync();
            
            // 重新查询以确保获得最新状态
            return await Context.Set<TEntity>()
                .FirstOrDefaultAsync(e => e.Id == entity.Id) ?? entity;
        }

        /// <summary>
        /// 添加多个测试实体到数据库
        /// </summary>
        protected async Task<List<TEntity>> AddTestEntitiesAsync(IEnumerable<TEntity> entities)
        {
            var entityList = entities.ToList();
            Context.Set<TEntity>().AddRange(entityList);
            await Context.SaveChangesAsync();
            
            // 重新查询以确保获得最新状态
            var ids = entityList.Select(e => e.Id).ToList();
            return await Context.Set<TEntity>()
                .Where(e => ids.Contains(e.Id))
                .ToListAsync();
        }

        /// <summary>
        /// 创建并添加测试实体
        /// </summary>
        protected async Task<TEntity> CreateAndAddTestEntityAsync(Action<TEntity> configure = null)
        {
            // P4-Fix简化：使用Activator创建基础实体，避免复杂的泛型工厂方法
            var entity = Activator.CreateInstance<TEntity>();
            configure?.Invoke(entity);
            return await AddTestEntityAsync(entity);
        }

        /// <summary>
        /// 创建并添加多个测试实体
        /// </summary>
        protected async Task<List<TEntity>> CreateAndAddTestEntitiesAsync(int count, Action<TEntity, int> configure = null)
        {
            var entities = new List<TEntity>();
            
            for (int i = 0; i < count; i++)
            {
                // P4-Fix简化：使用Activator创建基础实体
                var entity = Activator.CreateInstance<TEntity>();
                configure?.Invoke(entity, i);
                entities.Add(entity);
            }
            
            return await AddTestEntitiesAsync(entities);
        }

        #endregion

        #region 数据验证

        /// <summary>
        /// 验证实体存在于数据库中
        /// </summary>
        protected async Task<bool> EntityExistsInDatabaseAsync(Guid id)
        {
            return await Context.Set<TEntity>()
                .AnyAsync(e => e.Id == id);
        }

        /// <summary>
        /// 验证实体不存在于数据库中
        /// </summary>
        protected async Task<bool> EntityNotExistsInDatabaseAsync(Guid id)
        {
            return !await EntityExistsInDatabaseAsync(id);
        }

        /// <summary>
        /// 获取数据库中实体的数量
        /// </summary>
        protected async Task<int> GetEntityCountInDatabaseAsync()
        {
            return await Context.Set<TEntity>().CountAsync();
        }

        /// <summary>
        /// 从数据库重新加载实体
        /// </summary>
        protected async Task<TEntity?> ReloadEntityFromDatabaseAsync(Guid id)
        {
            return await Context.Set<TEntity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        /// <summary>
        /// 验证实体状态
        /// </summary>
        protected void AssertEntityState(TEntity entity, EntityState expectedState)
        {
            var entry = Context.Entry(entity);
            Assert.Equal(expectedState, entry.State);
        }

        #endregion

        #region Repository断言辅助方法

        /// <summary>
        /// 断言Repository操作成功且返回预期结果
        /// </summary>
        protected void AssertRepositoryResult<T>(T result, bool shouldBeNull = false, string message = null)
        {
            if (shouldBeNull)
            {
                Assert.Null(result);
            }
            else
            {
                Assert.NotNull(result);
            }
            
            if (!string.IsNullOrEmpty(message))
            {
                // 如果有具体消息，可以进一步验证
            }
        }

        /// <summary>
        /// 断言列表结果
        /// </summary>
        protected void AssertListResult<T>(IEnumerable<T> result, int expectedCount, string message = null)
        {
            Assert.NotNull(result);
            var list = result.ToList();
            Assert.Equal(expectedCount, list.Count);
            
            if (!string.IsNullOrEmpty(message))
            {
                // 可以添加更多验证逻辑
            }
        }

        /// <summary>
        /// 断言分页结果
        /// </summary>
        protected void AssertPagedRepositoryResult<T>(IEnumerable<T> result, int page, int pageSize, int totalExpected)
        {
            Assert.NotNull(result);
            var list = result.ToList();
            
            // 验证返回的项目数不超过页大小
            Assert.True(list.Count <= pageSize);
            
            // 如果不是最后一页，应该返回完整的页大小
            var expectedItemsOnThisPage = Math.Min(pageSize, Math.Max(0, totalExpected - (page - 1) * pageSize));
            if (totalExpected > (page - 1) * pageSize)
            {
                Assert.Equal(expectedItemsOnThisPage, list.Count);
            }
        }

        #endregion

        #region 性能测试辅助

        /// <summary>
        /// 测量操作执行时间
        /// </summary>
        protected async Task<TimeSpan> MeasureExecutionTimeAsync(Func<Task> operation)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await operation();
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        /// <summary>
        /// 测量操作执行时间（带返回值）
        /// </summary>
        protected async Task<(T Result, TimeSpan Duration)> MeasureExecutionTimeAsync<T>(Func<Task<T>> operation)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await operation();
            stopwatch.Stop();
            return (result, stopwatch.Elapsed);
        }

        /// <summary>
        /// 断言操作在指定时间内完成
        /// </summary>
        protected void AssertExecutionTime(TimeSpan actualTime, TimeSpan maxExpectedTime, string operation = "操作")
        {
            Assert.True(actualTime <= maxExpectedTime, 
                $"{operation}执行时间过长: 实际{actualTime.TotalMilliseconds}ms, 期望小于{maxExpectedTime.TotalMilliseconds}ms");
        }

        #endregion

        #region 数据一致性验证

        /// <summary>
        /// 验证实体的基础字段
        /// </summary>
        protected void AssertBaseEntityFields(TEntity entity, Guid? expectedId = null)
        {
            Assert.NotNull(entity);
            
            if (expectedId.HasValue)
            {
                Assert.Equal(expectedId.Value, entity.Id);
            }
            else
            {
                Assert.NotEqual(Guid.Empty, entity.Id);
            }
            
            // 可以添加更多基础字段的验证
        }

        /// <summary>
        /// 验证审计字段（如果实体支持）
        /// </summary>
        protected virtual void AssertAuditFields(TEntity entity)
        {
            // 子类可以重写此方法来验证审计字段
            // 如 CreatedBy, CreatedDate, UpdatedBy, UpdatedDate 等
        }

        #endregion

        #region 清理方法

        /// <summary>
        /// 清理测试数据
        /// </summary>
        protected override async Task ClearTestDataAsync()
        {
            // 先清理当前实体类型
            var entities = await Context.Set<TEntity>().ToListAsync();
            Context.Set<TEntity>().RemoveRange(entities);
            
            // 然后调用基类清理其他数据
            await base.ClearTestDataAsync();
        }

        #endregion
    }
}