using System;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.SharedCommon;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Xunit;

namespace LYBT.Desktop.Core.Tests.Services
{
    /// <summary>
    /// UnifiedEventHandler 单元测试
    /// 验证事件发布、订阅、状态消息处理、错误处理等核心功能
    /// </summary>
    public class UnifiedEventHandlerTests
    {
        private readonly Mock<IEventAggregator> _eventAggregatorMock;
        private readonly Mock<ILogger<UnifiedEventHandler>> _loggerMock;
        private readonly Mock<PubSubEvent<StatusMessageEvent>> _statusMessageEventMock;
        private readonly Mock<PubSubEvent<NavigationEvent>> _navigationEventMock;
        private readonly Mock<PubSubEvent<ErrorEvent>> _errorEventMock;
        private readonly UnifiedEventHandler _eventHandler;

        public UnifiedEventHandlerTests()
        {
            _eventAggregatorMock = new Mock<IEventAggregator>();
            _loggerMock = new Mock<ILogger<UnifiedEventHandler>>();
            _statusMessageEventMock = new Mock<PubSubEvent<StatusMessageEvent>>();
            _navigationEventMock = new Mock<PubSubEvent<NavigationEvent>>();
            _errorEventMock = new Mock<PubSubEvent<ErrorEvent>>();

            // 设置事件聚合器的GetEvent方法返回模拟的事件
            _eventAggregatorMock.Setup(x => x.GetEvent<StatusMessageEvent>())
                               .Returns(_statusMessageEventMock.Object);
            _eventAggregatorMock.Setup(x => x.GetEvent<NavigationEvent>())
                               .Returns(_navigationEventMock.Object);
            _eventAggregatorMock.Setup(x => x.GetEvent<ErrorEvent>())
                               .Returns(_errorEventMock.Object);

            _eventHandler = new UnifiedEventHandler(
                _eventAggregatorMock.Object,
                _loggerMock.Object);
        }

        #region 状态消息发布测试

        [Fact]
        public void PublishStatusMessage_InfoMessage_ShouldPublishCorrectly()
        {
            // Arrange
            var message = "操作成功";
            var messageType = StatusMessageType.Info;
            StatusMessageEvent publishedEvent = null;

            _statusMessageEventMock.Setup(x => x.Publish(It.IsAny<StatusMessageEvent>()))
                                  .Callback<StatusMessageEvent>(e => publishedEvent = e);

            // Act
            _eventHandler.PublishStatusMessage(message, messageType);

            // Assert
            _statusMessageEventMock.Verify(x => x.Publish(It.IsAny<StatusMessageEvent>()), Times.Once);
            publishedEvent.Should().NotBeNull();
            publishedEvent.Message.Should().Be(message);
            publishedEvent.MessageType.Should().Be(messageType);
            publishedEvent.Timestamp.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PublishStatusMessage_WarningMessage_ShouldLogWarning()
        {
            // Arrange
            var message = "警告信息";
            var messageType = StatusMessageType.Warning;

            // Act
            _eventHandler.PublishStatusMessage(message, messageType);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void PublishStatusMessage_ErrorMessage_ShouldLogError()
        {
            // Arrange
            var message = "错误信息";
            var messageType = StatusMessageType.Error;

            // Act
            _eventHandler.PublishStatusMessage(message, messageType);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void PublishStatusMessage_EmptyMessage_ShouldNotPublish()
        {
            // Act
            _eventHandler.PublishStatusMessage(string.Empty, StatusMessageType.Info);

            // Assert
            _statusMessageEventMock.Verify(x => x.Publish(It.IsAny<StatusMessageEvent>()), Times.Never);
        }

        #endregion

        #region 导航事件发布测试

        [Fact]
        public void PublishNavigationEvent_ValidTarget_ShouldPublishCorrectly()
        {
            // Arrange
            var target = "PatientManagement";
            NavigationEvent publishedEvent = null;

            _navigationEventMock.Setup(x => x.Publish(It.IsAny<NavigationEvent>()))
                               .Callback<NavigationEvent>(e => publishedEvent = e);

            // Act
            _eventHandler.PublishNavigationEvent(target);

            // Assert
            _navigationEventMock.Verify(x => x.Publish(It.IsAny<NavigationEvent>()), Times.Once);
            publishedEvent.Should().NotBeNull();
            publishedEvent.NavigationTarget.Should().Be(target);
        }

        [Fact]
        public void PublishNavigationEvent_WithParameters_ShouldIncludeParameters()
        {
            // Arrange
            var target = "PatientDetail";
            var patientId = Guid.NewGuid();
            NavigationEvent publishedEvent = null;

            _navigationEventMock.Setup(x => x.Publish(It.IsAny<NavigationEvent>()))
                               .Callback<NavigationEvent>(e => publishedEvent = e);

            // Act
            _eventHandler.PublishNavigationEvent(target, new { PatientId = patientId });

            // Assert
            publishedEvent.Should().NotBeNull();
            publishedEvent.Parameters.Should().NotBeNull();
            publishedEvent.Parameters.Should().ContainKey("PatientId");
        }

        #endregion

        #region 错误事件发布测试

        [Fact]
        public void PublishErrorEvent_Exception_ShouldPublishAndLog()
        {
            // Arrange
            var exception = new InvalidOperationException("测试异常");
            var context = "数据加载";
            ErrorEvent publishedEvent = null;

            _errorEventMock.Setup(x => x.Publish(It.IsAny<ErrorEvent>()))
                          .Callback<ErrorEvent>(e => publishedEvent = e);

            // Act
            _eventHandler.PublishErrorEvent(exception, context);

            // Assert
            _errorEventMock.Verify(x => x.Publish(It.IsAny<ErrorEvent>()), Times.Once);
            publishedEvent.Should().NotBeNull();
            publishedEvent.Exception.Should().Be(exception);
            publishedEvent.ErrorContext.Should().Be(context);
            publishedEvent.Timestamp.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PublishErrorEvent_CriticalError_ShouldLogCritical()
        {
            // Arrange
            var exception = new OutOfMemoryException("内存不足");
            var context = "系统初始化";

            // Act
            _eventHandler.PublishErrorEvent(exception, context, isCritical: true);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(context)),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void PublishErrorEvent_HandledError_ShouldPublishCorrectly()
        {
            // Arrange
            var handledError = new HandledError
            {
                Message = "处理的错误",
                Details = "错误详情",
                Severity = ErrorSeverity.Warning
            };
            ErrorEvent publishedEvent = null;

            _errorEventMock.Setup(x => x.Publish(It.IsAny<ErrorEvent>()))
                          .Callback<ErrorEvent>(e => publishedEvent = e);

            // Act
            _eventHandler.PublishErrorEvent(handledError);

            // Assert
            publishedEvent.Should().NotBeNull();
            publishedEvent.HandledError.Should().Be(handledError);
            publishedEvent.IsHandled.Should().BeTrue();
        }

        #endregion

        #region 事件订阅测试

        [Fact]
        public void SubscribeToStatusMessage_ShouldRegisterHandler()
        {
            // Arrange
            Action<StatusMessageEvent> handler = e => { };

            // Act
            _eventHandler.SubscribeToStatusMessage(handler);

            // Assert
            _statusMessageEventMock.Verify(x => x.Subscribe(
                It.IsAny<Action<StatusMessageEvent>>(),
                It.IsAny<ThreadOption>(),
                It.IsAny<bool>(),
                It.IsAny<Predicate<StatusMessageEvent>>()),
                Times.Once);
        }

        [Fact]
        public void SubscribeToNavigationEvent_ShouldRegisterHandler()
        {
            // Arrange
            Action<NavigationEvent> handler = e => { };

            // Act
            _eventHandler.SubscribeToNavigationEvent(handler);

            // Assert
            _navigationEventMock.Verify(x => x.Subscribe(
                It.IsAny<Action<NavigationEvent>>(),
                It.IsAny<ThreadOption>(),
                It.IsAny<bool>(),
                It.IsAny<Predicate<NavigationEvent>>()),
                Times.Once);
        }

        [Fact]
        public void SubscribeToErrorEvent_ShouldRegisterHandler()
        {
            // Arrange
            Action<ErrorEvent> handler = e => { };

            // Act
            _eventHandler.SubscribeToErrorEvent(handler);

            // Assert
            _errorEventMock.Verify(x => x.Subscribe(
                It.IsAny<Action<ErrorEvent>>(),
                It.IsAny<ThreadOption>(),
                It.IsAny<bool>(),
                It.IsAny<Predicate<ErrorEvent>>()),
                Times.Once);
        }

        #endregion

        #region 批量事件发布测试

        [Fact]
        public async Task PublishBatchEventsAsync_MultipleEvents_ShouldPublishAll()
        {
            // Arrange
            var events = new[]
            {
                new StatusMessageEvent { Message = "Event 1", MessageType = StatusMessageType.Info },
                new StatusMessageEvent { Message = "Event 2", MessageType = StatusMessageType.Success },
                new StatusMessageEvent { Message = "Event 3", MessageType = StatusMessageType.Warning }
            };

            var publishCount = 0;
            _statusMessageEventMock.Setup(x => x.Publish(It.IsAny<StatusMessageEvent>()))
                                  .Callback<StatusMessageEvent>(e => publishCount++);

            // Act
            await _eventHandler.PublishBatchEventsAsync(events);

            // Assert
            publishCount.Should().Be(3);
            _statusMessageEventMock.Verify(x => x.Publish(It.IsAny<StatusMessageEvent>()), Times.Exactly(3));
        }

        #endregion

        #region 事件过滤测试

        [Fact]
        public void SubscribeWithFilter_ShouldOnlyReceiveFilteredEvents()
        {
            // Arrange
            Predicate<StatusMessageEvent> filter = e => e.MessageType == StatusMessageType.Error;

            // Act
            _eventHandler.SubscribeToStatusMessage(e => { }, filter);

            // Assert
            _statusMessageEventMock.Verify(x => x.Subscribe(
                It.IsAny<Action<StatusMessageEvent>>(),
                It.IsAny<ThreadOption>(),
                It.IsAny<bool>(),
                It.Is<Predicate<StatusMessageEvent>>(p => p == filter)),
                Times.Once);
        }

        #endregion

        #region 内存泄漏防护测试

        [Fact]
        public void Unsubscribe_ShouldRemoveHandler()
        {
            // Arrange
            var subscription = new Mock<SubscriptionToken>(null);
            Action<StatusMessageEvent> handler = e => { };

            _statusMessageEventMock.Setup(x => x.Subscribe(
                It.IsAny<Action<StatusMessageEvent>>(),
                It.IsAny<ThreadOption>(),
                It.IsAny<bool>(),
                It.IsAny<Predicate<StatusMessageEvent>>()))
                .Returns(subscription.Object);

            var token = _eventHandler.SubscribeToStatusMessage(handler);

            // Act
            _eventHandler.Unsubscribe(token);

            // Assert
            _statusMessageEventMock.Verify(x => x.Unsubscribe(subscription.Object), Times.Once);
        }

        #endregion
    }
}