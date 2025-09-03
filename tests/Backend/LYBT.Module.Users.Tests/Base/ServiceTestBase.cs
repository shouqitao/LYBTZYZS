using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Logging.Dtos;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
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
        protected readonly Mock<IUnifiedLogService> MockLogService;
        protected readonly Mock<ILogger> MockLogger;
        protected readonly IMapper Mapper;
        protected readonly List<object> CapturedLogs;

        protected ServiceTestBase()
        {
            // 设置 Mock 对象
            MockLogService = new Mock<IUnifiedLogService>();
            MockLogger = new Mock<ILogger>();
            CapturedLogs = new List<object>();

            // 设置 IUnifiedLogService 默认行为
            MockLogService
                .Setup(x => x.CreateLogAsync(It.IsAny<LogCreateDto>()))
                .Callback<LogCreateDto>(log => CapturedLogs.Add(log))
                .ReturnsAsync(true);

            MockLogService
                .Setup(x => x.LogUserActionAsync(
                    It.IsAny<Guid>(), 
                    It.IsAny<string>(), 
                    It.IsAny<LogActionType>(),
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<string>(),
                    It.IsAny<string>(), 
                    It.IsAny<string>(),
                    It.IsAny<string>(), 
                    It.IsAny<bool>(),
                    It.IsAny<string>(), 
                    It.IsAny<string>(),
                    It.IsAny<string>(), 
                    It.IsAny<long>()))
                .Callback<Guid, string, LogActionType, string, string, string, string, string, string, bool, string, string, string, long>(
                    (userId, userName, actionType, module, function, description, requestPath, httpMethod, parameters, isSuccess, errorMessage, clientIP, userAgent, duration) => 
                    {
                        CapturedLogs.Add(new UserActionLogDto
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            UserName = userName,
                            ActionType = actionType,
                            Module = module,
                            Function = function,
                            Description = description,
                            RequestPath = requestPath,
                            HttpMethod = httpMethod,
                            Parameters = parameters,
                            IsSuccess = isSuccess,
                            ErrorMessage = errorMessage,
                            ClientIP = clientIP,
                            UserAgent = userAgent,
                            ActionTime = DateTime.UtcNow,
                            Duration = duration
                        });
                    })
                .Returns(Task.CompletedTask);

            // 设置错误日志方法
            MockLogService
                .Setup(x => x.LogErrorAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<Exception>(),
                    It.IsAny<Guid?>(), 
                    It.IsAny<string>()))
                .Callback<string, string, Exception?, Guid?, string?>(
                    (source, message, exception, userId, requestId) => 
                    {
                        CapturedLogs.Add(new { Source = source, Message = message, Exception = exception, UserId = userId, RequestId = requestId, Type = "SystemError" });
                    })
                .Returns(Task.CompletedTask);

            MockLogService
                .Setup(x => x.LogErrorAsync(
                    It.IsAny<Exception>(), 
                    It.IsAny<string>(), 
                    It.IsAny<string>(),
                    It.IsAny<Guid?>(), 
                    It.IsAny<string>(), 
                    It.IsAny<string>()))
                .Callback<Exception, string?, string?, Guid?, string?, string?>(
                    (exception, requestPath, httpMethod, userId, clientIP, userAgent) => 
                    {
                        CapturedLogs.Add(new { Exception = exception, RequestPath = requestPath, HttpMethod = httpMethod, UserId = userId, ClientIP = clientIP, UserAgent = userAgent, Type = "ApplicationError" });
                    })
                .Returns(Task.CompletedTask);

            // 初始化 AutoMapper
            Mapper = CreateMapper();
        }

        /// <summary>
        /// 创建AutoMapper配置
        /// 子类应该重写此方法来配置具体的映射
        /// </summary>
        protected virtual IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg => { }, NullLoggerFactory.Instance);
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