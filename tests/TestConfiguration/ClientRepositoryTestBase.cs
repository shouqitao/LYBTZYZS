using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using System.Net;
using System.Linq.Expressions;

namespace LYBT.Tests.Configuration
{
    /// <summary>
    /// Client端Repository单元测试基类
    /// 提供Mock配置和通用测试方法
    /// </summary>
    /// <typeparam name="TRepository">Repository类型</typeparam>
    /// <typeparam name="TApi">API接口类型</typeparam>
    public abstract class ClientRepositoryTestBase<TRepository, TApi> : IDisposable
        where TRepository : class
        where TApi : class
    {
        protected readonly Mock<TApi> _mockApi;
        protected readonly Mock<ILogger<TRepository>> _mockLogger;
        protected readonly TRepository _repository;

        protected ClientRepositoryTestBase()
        {
            _mockApi = new Mock<TApi>();
            _mockLogger = new Mock<ILogger<TRepository>>();
            _repository = CreateRepository(_mockApi.Object, _mockLogger.Object);
        }

        /// <summary>
        /// 创建Repository实例，由子类实现
        /// </summary>
        protected abstract TRepository CreateRepository(TApi api, ILogger<TRepository> logger);

        /// <summary>
        /// 验证API方法调用
        /// </summary>
        protected void VerifyApiCall<T>(Expression<Action<TApi>> apiCall, Times? times = null)
        {
            var verifyTimes = times ?? Times.Once();
            _mockApi.Verify(apiCall, verifyTimes);
        }

        /// <summary>
        /// 验证日志记录
        /// </summary>
        protected void VerifyLogCall<TState>(Expression<Action<ILogger<TRepository>>> logCall, Times? times = null)
        {
            var verifyTimes = times ?? Times.Once();
            _mockLogger.Verify(logCall, verifyTimes);
        }

        /// <summary>
        /// 设置API返回成功结果
        /// </summary>
        protected void SetupApiSuccess<TResult>(Expression<Func<TApi, Task<TResult>>> apiCall, TResult result)
        {
            _mockApi.Setup(apiCall).ReturnsAsync(result);
        }

        /// <summary>
        /// 设置API返回失败结果
        /// </summary>
        protected void SetupApiFailure<TResult>(Expression<Func<TApi, Task<TResult>>> apiCall, Exception exception)
        {
            _mockApi.Setup(apiCall).ThrowsAsync(exception);
        }

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
            _mockApi?.Reset();
            _mockLogger?.Reset();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Repository测试断言扩展方法
    /// </summary>
    public static class RepositoryTestAssertions
    {
        /// <summary>
        /// 验证API调用次数
        /// </summary>
        public static void ShouldHaveBeenCalled<T>(this Mock<T> mock, Expression<Action<T>> call, Times times) where T : class
        {
            mock.Verify(call, times);
        }

        /// <summary>
        /// 验证API从未被调用
        /// </summary>
        public static void ShouldNotHaveBeenCalled<T>(this Mock<T> mock, Expression<Action<T>> call) where T : class
        {
            mock.Verify(call, Times.Never());
        }
    }
}