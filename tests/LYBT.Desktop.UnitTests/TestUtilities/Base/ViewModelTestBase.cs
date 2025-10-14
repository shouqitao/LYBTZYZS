using System;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Prism.Regions;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Infrastructure.Interfaces;
using Xunit;

namespace LYBT.Desktop.UnitTests.TestUtilities.Base
{
    /// <summary>
    /// ViewModel测试基类 - 统一测试框架
    /// 提供标准化的测试环境和Mock对象管理
    /// 支持UnifiedViewModelBase和其子类
    /// </summary>
    public abstract class ViewModelTestBase<TViewModel> : IDisposable
        where TViewModel : UnifiedViewModelBase
    {
        protected Mock<IEventAggregator> MockEventAggregator { get; private set; } = null!;
        protected Mock<ILoggerFactory> MockLoggerFactory { get; private set; } = null!;
        protected Mock<ILogger> MockLogger { get; private set; } = null!;
        protected Mock<IRegionManager> MockRegionManager { get; private set; } = null!;
        protected Mock<ISessionManager> MockSessionManager { get; private set; } = null!;
        protected Mock<IUserNotificationService> MockUserNotificationService { get; private set; } = null!;
        protected TViewModel ViewModel { get; set; } = null!;

        protected ViewModelTestBase()
        {
            SetUp();
        }

        /// <summary>
        /// 测试环境初始化
        /// </summary>
        public virtual void SetUp()
        {
            // 初始化Mock对象
            MockEventAggregator = new Mock<IEventAggregator>();
            MockLoggerFactory = new Mock<ILoggerFactory>();
            MockRegionManager = new Mock<IRegionManager>();
            MockSessionManager = new Mock<ISessionManager>();
            MockUserNotificationService = new Mock<IUserNotificationService>();
            MockLogger = new Mock<ILogger>();

            // 配置LoggerFactory返回Mock Logger
            MockLoggerFactory
                .Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(MockLogger.Object);
            MockLoggerFactory
                .Setup(x => x.CreateLogger(It.IsAny<Type>()))
                .Returns(MockLogger.Object);

            // 设置通知服务默认行为
            MockUserNotificationService
                .Setup(x => x.HandleExceptionAsync(It.IsAny<Exception>(), It.IsAny<string>()))
                .ReturnsAsync((Exception ex, string context) =>
                {
                    // 记录错误以便测试验证
                    RecordError(ex, context);
                    return Task.CompletedTask;
                });

            // 创建ViewModel实例
            ViewModel = CreateViewModel();
        }

        /// <summary>
        /// 创建ViewModel实例 - 子类必须实现
        /// </summary>
        protected abstract TViewModel CreateViewModel();

        /// <summary>
        /// 清理测试环境
        /// </summary>
        public virtual void TearDown()
        {
            ViewModel?.Dispose();
            ViewModel = null!;

            MockEventAggregator = null!;
            MockLoggerFactory = null!;
            MockLogger = null!;
            MockRegionManager = null!;
            MockSessionManager = null!;
            MockUserNotificationService = null!;
        }

        public void Dispose()
        {
            TearDown();
        }

        #region 辅助方法

        /// <summary>
        /// 记录错误信息用于验证
        /// </summary>
        private readonly List<(Exception exception, string? context)> _recordedErrors = new();

        protected void RecordError(Exception exception, string? context)
        {
            _recordedErrors.Add((exception, context));
        }

        /// <summary>
        /// 验证是否记录了特定类型的错误
        /// </summary>
        protected bool HasRecordedError<TException>() where TException : Exception
        {
            return _recordedErrors.Any(e => e.exception is TException);
        }

        /// <summary>
        /// 获取记录的错误数量
        /// </summary>
        protected int RecordedErrorCount => _recordedErrors.Count;

        /// <summary>
        /// 模拟事件发布
        /// </summary>
        protected void PublishEvent<TEvent>(TEvent eventData) where TEvent : PubSubEvent, new()
        {
            var mockEvent = new Mock<TEvent>();
            MockEventAggregator
                .Setup(x => x.GetEvent<TEvent>())
                .Returns(mockEvent.Object);
            
            mockEvent.Raise(e => e.Subscribe(It.IsAny<Action<TEvent>>(), 
                It.IsAny<ThreadOption>(), 
                It.IsAny<bool>(), 
                It.IsAny<Predicate<TEvent>>()));
        }

        /// <summary>
        /// 验证事件是否被订阅
        /// </summary>
        protected void VerifyEventSubscription<TEvent>() where TEvent : PubSubEvent, new()
        {
            MockEventAggregator.Verify(x => x.GetEvent<TEvent>(), Times.AtLeastOnce);
        }

        /// <summary>
        /// 验证日志记录
        /// </summary>
        protected void VerifyLogged(LogLevel logLevel, string? message = null)
        {
            if (message != null)
            {
                MockLogger.Verify(
                    x => x.Log(
                        logLevel,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains(message)),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.AtLeastOnce);
            }
            else
            {
                MockLogger.Verify(
                    x => x.Log(
                        logLevel,
                        It.IsAny<EventId>(),
                        It.IsAny<It.IsAnyType>(),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.AtLeastOnce);
            }
        }

        /// <summary>
        /// 等待异步操作完成
        /// </summary>
        protected async Task WaitForAsync(Func<bool> condition, int timeoutMilliseconds = 1000)
        {
            var startTime = DateTime.Now;
            while (!condition())
            {
                if ((DateTime.Now - startTime).TotalMilliseconds > timeoutMilliseconds)
                {
                    throw new TimeoutException("等待条件超时");
                }
                await Task.Delay(10);
            }
        }

        #endregion

        #region 断言扩展

        /// <summary>
        /// 验证ViewModel忙碌状态
        /// </summary>
        protected void AssertBusy(bool expectedBusy)
        {
            Assert.Equal(expectedBusy, ViewModel.IsBusy);
        }

        /// <summary>
        /// 验证状态消息
        /// </summary>
        protected void AssertStatusMessage(string expectedMessage)
        {
            Assert.Equal(expectedMessage, ViewModel.StatusMessage);
        }

        /// <summary>
        /// 验证属性变更通知
        /// </summary>
        protected void AssertPropertyChanged(Action action, params string[] propertyNames)
        {
            var changedProperties = new List<string>();
            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != null)
                    changedProperties.Add(e.PropertyName);
            };

            action();

            foreach (var propertyName in propertyNames)
            {
                Assert.Contains(propertyName, changedProperties);
            }
        }

        #endregion
    }
}