using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using Bogus;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Logging.Dtos;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace LYBT.Tests.Backend.Core
{
    /// <summary>
    /// 统一测试基础设施 - 整合Repository和Service测试的所有功能
    /// 提供内存数据库、Mock对象、数据生成器和验证工具
    /// </summary>
    public abstract class BaseTestFixture : IDisposable
    {
        #region 数据库相关

        /// <summary>
        /// 测试数据库上下文
        /// </summary>
        protected readonly AppDbContext Context;

        private readonly string _databaseName;

        #endregion

        #region Mock对象

        /// <summary>
        /// 统一日志服务Mock
        /// </summary>
        protected readonly Mock<IUnifiedLogService> MockLogService;

        /// <summary>
        /// 通用日志Mock
        /// </summary>
        protected readonly Mock<ILogger> MockLogger;

        /// <summary>
        /// 捕获的日志记录
        /// </summary>
        protected readonly List<object> CapturedLogs;

        #endregion

        #region 工具对象

        /// <summary>
        /// AutoMapper实例
        /// </summary>
        protected readonly IMapper Mapper;

        /// <summary>
        /// 数据生成器工厂
        /// </summary>
        protected readonly TestDataFactory DataFactory;

        #endregion

        protected BaseTestFixture()
        {
            // 初始化数据库
            _databaseName = $"TestDb_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .EnableSensitiveDataLogging()
                .Options;

            Context = new AppDbContext(options);
            Context.Database.EnsureCreated();

            // 初始化Mock对象
            MockLogService = new Mock<IUnifiedLogService>();
            MockLogger = new Mock<ILogger>();
            CapturedLogs = new List<object>();

            // 设置日志服务Mock
            SetupLogServiceMocks();

            // 初始化AutoMapper
            Mapper = CreateMapper();

            // 初始化数据生成器
            DataFactory = new TestDataFactory();
        }

        #region 数据库操作

        /// <summary>
        /// 初始化测试数据
        /// </summary>
        protected virtual async Task SeedTestDataAsync()
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// 清理测试数据
        /// </summary>
        protected virtual async Task ClearTestDataAsync()
        {
            // 清理所有数据
            Context.Users.RemoveRange(Context.Users);
            Context.Patients.RemoveRange(Context.Patients);
            Context.Herbs.RemoveRange(Context.Herbs);
            Context.Consultations.RemoveRange(Context.Consultations);
            Context.Prescriptions.RemoveRange(Context.Prescriptions);
            Context.MedicalCases.RemoveRange(Context.MedicalCases);
            
            await Context.SaveChangesAsync();
        }

        #endregion

        #region Mock设置

        /// <summary>
        /// 设置日志服务Mock行为
        /// </summary>
        private void SetupLogServiceMocks()
        {
            // 设置创建日志
            MockLogService
                .Setup(x => x.CreateLogAsync(It.IsAny<LogCreateDto>()))
                .Callback<LogCreateDto>(log => CapturedLogs.Add(log))
                .ReturnsAsync(true);

            // 设置用户操作日志
            MockLogService
                .Setup(x => x.LogUserActionAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<LogActionType>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                    It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<long>()))
                .Callback<Guid, string, LogActionType, string, string, string, string, string, string, bool, string, string, string, long>(
                    (userId, userName, actionType, module, function, description, requestPath, httpMethod, parameters, isSuccess, errorMessage, clientIP, userAgent, duration) => 
                    {
                        CapturedLogs.Add(new UserActionLogDto
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            Username = userName,
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

            // 设置错误日志
            MockLogService
                .Setup(x => x.LogErrorAsync(It.IsAny<string>(), It.IsAny<string>(), 
                    It.IsAny<Exception>(), It.IsAny<Guid?>(), It.IsAny<string>()))
                .Callback<string, string, Exception?, Guid?, string?>(
                    (source, message, exception, userId, requestId) => 
                    {
                        CapturedLogs.Add(new { 
                            Source = source, Message = message, Exception = exception, 
                            UserId = userId, RequestId = requestId, Type = "SystemError" 
                        });
                    })
                .Returns(Task.CompletedTask);
        }

        /// <summary>
        /// 创建Mock Repository
        /// </summary>
        protected Mock<TRepository> CreateMockRepository<TRepository, TEntity>()
            where TRepository : class
            where TEntity : BaseModel
        {
            var mockRepo = new Mock<TRepository>();
            return mockRepo;
        }

        /// <summary>
        /// 创建Mock Options
        /// </summary>
        protected IOptions<TOptions> CreateOptions<TOptions>(TOptions options)
            where TOptions : class
        {
            return Options.Create(options);
        }

        #endregion

        #region AutoMapper配置

        /// <summary>
        /// 创建AutoMapper配置 - 子类应该重写此方法
        /// </summary>
        protected virtual IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg => { }, NullLoggerFactory.Instance);
            return config.CreateMapper();
        }

        #endregion

        #region 测试数据生成

        /// <summary>
        /// 创建测试实体
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
        /// 创建测试实体列表
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

        #endregion

        #region 验证方法

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
        /// 验证操作日志数量
        /// </summary>
        protected void VerifyLogCount<TLogType>(int expectedCount)
            where TLogType : class
        {
            var actualCount = CapturedLogs.OfType<TLogType>().Count();
            if (actualCount != expectedCount)
            {
                throw new InvalidOperationException(
                    $"期望 {expectedCount} 个 {typeof(TLogType).Name} 日志，实际 {actualCount} 个");
            }
        }

        #endregion

        #region 辅助方法

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

        /// <summary>
        /// 获取捕获的日志
        /// </summary>
        protected IEnumerable<TLogType> GetCapturedLogs<TLogType>()
            where TLogType : class
        {
            return CapturedLogs.OfType<TLogType>();
        }

        #endregion

        #region 资源清理

        public virtual void Dispose()
        {
            CapturedLogs.Clear();
            Context?.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}