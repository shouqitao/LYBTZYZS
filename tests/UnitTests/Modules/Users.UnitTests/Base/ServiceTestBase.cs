using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Users.Services;
using Microsoft.Extensions.Options;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Module.Users.Tests.Base
{
    /// <summary>
    /// Service层测试基类，提供通用的Mock设置和测试助手方法
    /// </summary>
    public abstract class ServiceTestBase : IDisposable
    {
        protected readonly Mock<ILogger<UserBusinessService>> MockLogger;
        protected readonly IMapper Mapper;
        protected readonly List<object> CapturedLogs;

        protected ServiceTestBase()
        {
            // 设置 Mock 对象
            MockLogger = new Mock<ILogger<UserBusinessService>>();
            CapturedLogs = new List<object>();

            // TODO: Logger配置 - Microsoft.Extensions.Logging.ILogger使用不同的接口
            // 标准的ILogger<T>没有CreateLogAsync等自定义方法

            // Logger配置已简化 - ILogger<T>接口与原IUnifiedLogService不兼容
            // TODO: 如需测试日志记录，可以使用ILogger扩展方法的验证

            // 初始化 AutoMapper
            Mapper = CreateMapper();
        }

        /// <summary>
        /// 创建AutoMapper配置
        /// 子类应该重写此方法来配置具体的映射
        /// </summary>
        protected virtual IMapper CreateMapper()
{
    var config = new MapperConfiguration(cfg => { });
    return config.CreateMapper();
}

        /// <summary>
        /// 创建 Mock Repository，支持基本的CRUD操作
        /// </summary>
        protected Mock<TRepository> CreateMockRepository<TRepository, TEntity>()
            where TRepository : class
            where TEntity : BaseModel
        {
            var mockRepo = new Mock<TRepository>();
            var dataStore = new List<TEntity>();

            // 设置通用的方法行为
            SetupBasicRepositoryMethods(mockRepo, dataStore);

            return mockRepo;
        }

        /// <summary>
        /// 设置基本的Repository方法
        /// </summary>
        private void SetupBasicRepositoryMethods<TRepository, TEntity>(
            Mock<TRepository> mockRepo,
            List<TEntity> dataStore)
            where TRepository : class
            where TEntity : BaseModel
        {
            // 这里可以设置通用的Repository方法，如果需要的话
            // 具体的设置应该在各个测试类中完成
        }

        /// <summary>
        /// 创建 Mock Options
        /// </summary>
        protected IOptions<TOptions> CreateOptions<TOptions>(TOptions options)
            where TOptions : class
        {
            return Options.Create(options);
        }

        /// <summary>
        /// 验证日志是否被记录
        /// </summary>
        protected void VerifyLogCreated<TLogType>(Expression<Func<TLogType, bool>> predicate)
            where TLogType : class
        {
            var log = CapturedLogs.OfType<TLogType>().FirstOrDefault(predicate.Compile());
            if (log == null)
            {
                throw new InvalidOperationException($"未找到符合条件的 {typeof(TLogType).Name} 日志");
            }
        }

        /// <summary>
        /// 验证没有错误日志
        /// </summary>
        protected void VerifyNoErrorLogs()
        {
            var errorLogs = CapturedLogs.Where(log => 
            {
                var type = log.GetType();
                return type.GetProperty("Type")?.GetValue(log)?.ToString()?.Contains("Error") == true;
            });
            
            if (errorLogs.Any())
            {
                throw new InvalidOperationException($"发现 {errorLogs.Count()} 个错误日志");
            }
        }

        /// <summary>
        /// 创建测试用的实体
        /// </summary>
        protected TEntity CreateTestEntity<TEntity>(Action<TEntity>? setup = null)
            where TEntity : BaseModel, new()
        {
            var entity = new TEntity
            {
                Id = Guid.NewGuid()
            };

            setup?.Invoke(entity);
            return entity;
        }

        /// <summary>
        /// 创建测试用的实体列表
        /// </summary>
        protected List<TEntity> CreateTestEntities<TEntity>(int count, Action<TEntity, int>? setup = null)
            where TEntity : BaseModel, new()
        {
            var entities = new List<TEntity>();
            for (int i = 0; i < count; i++)
            {
                var entity = CreateTestEntity<TEntity>(e => setup?.Invoke(e, i));
                entities.Add(entity);
            }
            return entities;
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public virtual void Dispose()
        {
            CapturedLogs.Clear();
        }

        /// <summary>
        /// 设置异步方法返回成功结果
        /// </summary>
        protected void SetupSuccessResult<TMock, TResult>(Mock<TMock> mock, Expression<Func<TMock, Task<TResult>>> expression, TResult result)
            where TMock : class
        {
            mock.Setup(expression).ReturnsAsync(result);
        }

        /// <summary>
        /// 设置异步方法抛出异常
        /// </summary>
        protected void SetupThrowsAsync<TMock, TResult>(Mock<TMock> mock, Expression<Func<TMock, Task<TResult>>> expression, Exception exception)
            where TMock : class
        {
            mock.Setup(expression).ThrowsAsync(exception);
        }
    }
}