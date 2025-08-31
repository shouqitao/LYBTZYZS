using System;
using System.Collections.Generic;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Moq;
using LYBT.Infrastructure.Data;

namespace LYBT.Tests.Core.UltraThink.Base
{
    /// <summary>
    /// UltraThink测试基类 - 简化版，专注于三层架构测试
    /// 摆脱过度工程化的统一日志服务，使用标准.NET组件
    /// </summary>
    public abstract class UltraThinkTestBase : IDisposable
    {
        // 标准.NET组件，简洁高效
        protected readonly Mock<ILogger> MockLogger;
        protected readonly IMemoryCache Cache;
        protected readonly IMapper Mapper;
        protected readonly AppDbContext DbContext;
        
        // 测试数据收集
        protected readonly List<string> LogMessages;

        protected UltraThinkTestBase()
        {
            MockLogger = new Mock<ILogger>();
            Cache = new MemoryCache(new MemoryCacheOptions());
            LogMessages = new List<string>();
            
            // 设置内存数据库用于测试
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            DbContext = new AppDbContext(options);
            
            // 创建AutoMapper
            Mapper = CreateMapper();
            
            // 设置Logger Mock捕获日志
            SetupLoggerMock();
        }

        /// <summary>
        /// 创建AutoMapper配置 - 包含测试所需的基本映射
        /// </summary>
        protected virtual IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg => {
                cfg.AddProfile<Mapping.TestMappingProfile>();
            }, NullLoggerFactory.Instance);
            
            return config.CreateMapper();
        }

        /// <summary>
        /// 设置Logger Mock以捕获日志消息
        /// </summary>
        private void SetupLoggerMock()
        {
            MockLogger.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                .Callback(new InvocationAction(invocation =>
                {
                    var logLevel = (LogLevel)invocation.Arguments[0];
                    var exception = (Exception?)invocation.Arguments[3];
                    var formatter = invocation.Arguments[4];
                    
                    var message = formatter?.GetType()
                        .GetMethod("Invoke")?
                        .Invoke(formatter, new[] { invocation.Arguments[2], exception })?.ToString() ?? "";
                    
                    LogMessages.Add($"[{logLevel}] {message}");
                }));
        }

        /// <summary>
        /// 创建测试实体
        /// </summary>
        protected TEntity CreateTestEntity<TEntity>(Action<TEntity>? setup = null)
            where TEntity : class, new()
        {
            var entity = new TEntity();
            setup?.Invoke(entity);
            return entity;
        }

        /// <summary>
        /// 验证日志包含指定消息
        /// </summary>
        protected void VerifyLogContains(string expectedMessage)
        {
            if (!LogMessages.Exists(log => log.Contains(expectedMessage)))
            {
                throw new InvalidOperationException($"未找到包含 '{expectedMessage}' 的日志消息");
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public virtual void Dispose()
        {
            DbContext?.Dispose();
            Cache?.Dispose();
            LogMessages.Clear();
        }
    }
}