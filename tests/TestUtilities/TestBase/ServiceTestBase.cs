using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Logging;
using LYBT.Tests.Backend.Core;
using LYBT.Tests.UltraThink.TestInfrastructure.Factories;

namespace LYBT.Tests.Backend.TestBase
{
    /// <summary>
    /// Service层测试基类 - 提供完整的Service测试基础设施
    /// 基于UltraThink三层架构设计，支持QueryService、BusinessService和主Service测试
    /// </summary>
    public abstract class ServiceTestBase<TService> : BaseTestFixture
        where TService : class
    {
        #region 服务相关

        /// <summary>
        /// 被测试的服务实例
        /// </summary>
        protected readonly TService Service;

        /// <summary>
        /// Mock工厂
        /// </summary>
        protected readonly LYBT.Tests.UltraThink.TestInfrastructure.Factories.MockFactory MockFactory;

        #endregion

        protected ServiceTestBase()
        {
            MockFactory = new LYBT.Tests.UltraThink.TestInfrastructure.Factories.MockFactory();
            Service = CreateServiceInstance();
        }

        #region 抽象方法

        /// <summary>
        /// 创建服务实例 - 子类必须实现
        /// </summary>
        protected abstract TService CreateServiceInstance();

        /// <summary>
        /// 创建AutoMapper配置 - 子类可以重写
        /// </summary>
        protected override IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                ConfigureMapper(cfg);
            });
            
            return config.CreateMapper();
        }

        /// <summary>
        /// 配置AutoMapper - 子类可以重写
        /// </summary>
        protected virtual void ConfigureMapper(IMapperConfigurationExpression cfg)
        {
            // 基础配置在这里
        }

        #endregion

        #region Mock创建辅助方法

        /// <summary>
        /// 创建Logger Mock
        /// </summary>
        protected Mock<ILogger<T>> CreateLoggerMock<T>()
        {
            return MockFactory.CreateLoggerMock<T>();
        }

        /// <summary>
        /// 创建统一日志服务Mock
        /// </summary>
        protected Mock<IUnifiedLogService> CreateUnifiedLogServiceMock()
        {
            var mock = new Mock<IUnifiedLogService>();
            
            // P4-Fix简化：移除已不存在的日志方法设置
            // 简化日志Mock以适应接口更改

            return mock;
        }

        #endregion

        #region 测试数据辅助方法

        /// <summary>
        /// 添加测试数据到上下文
        /// </summary>
        protected async Task<TEntity> AddTestEntityAsync<TEntity>(TEntity entity)
            where TEntity : class
        {
            Context.Set<TEntity>().Add(entity);
            await Context.SaveChangesAsync();
            return entity;
        }

        /// <summary>
        /// 添加多个测试数据到上下文
        /// </summary>
        protected async Task<List<TEntity>> AddTestEntitiesAsync<TEntity>(IEnumerable<TEntity> entities)
            where TEntity : class
        {
            var entityList = entities.ToList();
            Context.Set<TEntity>().AddRange(entityList);
            await Context.SaveChangesAsync();
            return entityList;
        }

        /// <summary>
        /// 获取实体数量
        /// </summary>
        protected async Task<int> GetEntityCountAsync<TEntity>()
            where TEntity : class
        {
            return await Context.Set<TEntity>().CountAsync();
        }

        /// <summary>
        /// 验证实体存在
        /// </summary>
        protected async Task<bool> EntityExistsAsync<TEntity>(Guid id)
            where TEntity : class
        {
            return await Context.Set<TEntity>().FindAsync(id) != null;
        }

        #endregion

        #region 断言辅助方法

        /// <summary>
        /// 验证ServiceResult成功
        /// </summary>
        protected void AssertSuccess<T>(LYBT.Shared.Models.Contracts.Common.ServiceResult<T> result, string message = "操作应该成功")
        {
            Assert.True(result.IsSuccess, $"{message}. 错误: {result.ErrorMessage}");
            Assert.NotNull(result.Data);
        }

        /// <summary>
        /// 验证ServiceResult失败
        /// </summary>
        protected void AssertFailure<T>(LYBT.Shared.Models.Contracts.Common.ServiceResult<T> result, string expectedError = null, string message = "操作应该失败")
        {
            Assert.False(result.IsSuccess, message);
            Assert.Null(result.Data);
            
            if (!string.IsNullOrEmpty(expectedError))
            {
                Assert.Contains(expectedError, result.ErrorMessage);
            }
        }

        /// <summary>
        /// 验证分页结果
        /// </summary>
        protected void AssertPagedResult<T>(LYBT.Shared.Models.Contracts.Common.PagedResult<T> result, int expectedTotal, int expectedPage, int expectedPageSize)
        {
            Assert.NotNull(result);
            Assert.Equal(expectedTotal, result.TotalCount);
            Assert.Equal(expectedPage, result.CurrentPage);
            Assert.Equal(expectedPageSize, result.PageSize);
            Assert.NotNull(result.Items);
        }

        #endregion

        #region 清理方法

        public override void Dispose()
        {
            MockFactory.ClearCache();
            base.Dispose();
        }

        #endregion
    }
}