using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LYBT.Tests.Configuration
{
    /// <summary>
    /// Client端Repository单元测试基类
    /// 提供Substitute配置和通用测试方法
    /// </summary>
    /// <typeparam name="TRepository">Repository类型</typeparam>
    /// <typeparam name="TApi">API接口类型</typeparam>
    public abstract class ClientRepositoryTestBase<TRepository, TApi> : IDisposable
        where TRepository : class
        where TApi : class
    {
        protected readonly TApi _api;
        protected readonly ILogger<TRepository> _logger;
        protected readonly TRepository _repository;

        protected ClientRepositoryTestBase()
        {
            _api = Substitute.For<TApi>();
            _logger = Substitute.For<ILogger<TRepository>>();
            _repository = CreateRepository(_api, _logger);
        }

        /// <summary>
        /// 创建Repository实例，由子类实现
        /// </summary>
        protected abstract TRepository CreateRepository(TApi api, ILogger<TRepository> logger);

        /// <summary>
        /// 验证HTTP异常处理
        /// </summary>
        protected void AssertHttpRequestException(Exception exception, string? expectedMessage = null)
        {
            exception.Should().NotBeNull();
            exception.Should().BeOfType<HttpRequestException>();
            if (!string.IsNullOrEmpty(expectedMessage))
            {
                exception.Message.Should().Contain(expectedMessage);
            }
        }

        /// <summary>
        /// 验证空引用异常处理
        /// </summary>
        protected void AssertArgumentNullException(Exception exception, string expectedParamName)
        {
            exception.Should().NotBeNull();
            exception.Should().BeOfType<ArgumentNullException>();
            ((ArgumentNullException)exception).ParamName.Should().Be(expectedParamName);
        }

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
