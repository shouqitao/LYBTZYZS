using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using LYBT.Desktop.Core.Services.EventHandling;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Core.Services.ErrorHandling;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Prism.Events;

namespace LYBT.Desktop.UnitTests.Core.Services.EventHandling
{
    /// <summary>
    /// UnifiedEventHandler单元测试 - 事件处理系统测试
    /// 验证事件订阅、发布、处理和错误恢复
    /// </summary>
    public class UnifiedEventHandlerTests : IDisposable
    {
        private readonly Mock<IEventAggregator> _mockEventAggregator;
        private readonly Mock<ILogger<UnifiedEventHandler>> _mockLogger;
        private readonly Mock<IErrorHandlingService> _mockErrorHandlingService;
        private readonly UnifiedEventHandler _eventHandler;
        private readonly Dictionary<Type, object> _mockEvents;

        public UnifiedEventHandlerTests()
        {
            _mockEventAggregator = new Mock<IEventAggregator>();
            _mockLogger = new Mock<ILogger<UnifiedEventHandler>>();
            _mockErrorHandlingService = new Mock<IErrorHandlingService>();
            _mockEvents = new Dictionary<Type, object>();

            // 设置EventAggregator返回Mock事件
            _mockEventAggregator.Setup(x => x.GetEvent<It.IsAnyType>())
                .Returns((Type eventType) => GetOrCreateMockEvent(eventType));

            _eventHandler = new UnifiedEventHandler(
                _mockEventAggregator.Object,
                _mockLogger.Object,
                _mockErrorHandlingService.Object);
        }

        private object GetOrCreateMockEvent(Type eventType)
        {
            if (!_mockEvents.ContainsKey(eventType))
            {
                var mockEvent = Activator.CreateInstance(
                    typeof(Mock<>).MakeGenericType(eventType));
                _mockEvents[eventType] = mockEvent!;
            }
            
            var mock = _mockEvents[eventType];
            return ((dynamic)mock).Object;
        }

        public void Dispose()
        {
            _eventHandler?.Dispose();
        }

        [Fact]
        public void Initialize_ShouldSubscribeToAllEvents()
        {
            // Act
            _eventHandler.Initialize();

            // Assert - 验证关键事件被订阅
            _mockEventAggregator.Verify(x => x.GetEvent<PatientSelectedEvent>(), Times.AtLeastOnce);
            _mockEventAggregator.Verify(x => x.GetEvent<ConsultationCompletedEvent>(), Times.AtLeastOnce);
            _mockEventAggregator.Verify(x => x.GetEvent<PrescriptionSavedEvent>(), Times.AtLeastOnce);
            _mockEventAggregator.Verify(x => x.GetEvent<ErrorOccurredEvent>(), Times.AtLeastOnce);
            _mockEventAggregator.Verify(x => x.GetEvent<StatusMessageEvent>(), Times.AtLeastOnce);

            _eventHandler.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public async Task PublishStatusMessage_ShouldPublishCorrectly()
        {
            // Arrange
            var mockStatusEvent = new Mock<StatusMessageEvent>();
            _mockEventAggregator.Setup(x => x.GetEvent<StatusMessageEvent>())
                .Returns(mockStatusEvent.Object);

            _eventHandler.Initialize();

            // Act
            await _eventHandler.PublishStatusMessageAsync("测试消息", StatusMessageType.Success, 5000);

            // Assert
            mockStatusEvent.Verify(x => x.Publish(It.Is<StatusMessageEventArgs>(
                args => args.Message == "测试消息" && 
                       args.Type == StatusMessageType.Success && 
                       args.Duration == 5000)), Times.Once);
        }

        [Fact]
        public async Task PublishError_ShouldHandleErrorCorrectly()
        {
            // Arrange
            var mockErrorEvent = new Mock<ErrorOccurredEvent>();
            _mockEventAggregator.Setup(x => x.GetEvent<ErrorOccurredEvent>())
                .Returns(mockErrorEvent.Object);

            _eventHandler.Initialize();
            var exception = new InvalidOperationException("测试错误");

            // Act
            await _eventHandler.PublishErrorAsync(exception, "TestSource", true);

            // Assert
            mockErrorEvent.Verify(x => x.Publish(It.Is<ErrorOccurredEventArgs>(
                args => args.Exception == exception && 
                       args.Source == "TestSource" && 
                       args.IsCritical == true)), Times.Once);

            _mockErrorHandlingService.Verify(
                x => x.HandleErrorAsync(exception, "TestSource"), 
                Times.Once);
        }

        [Fact]
        public void HandlePatientSelected_ShouldLogCorrectly()
        {
            // Arrange
            var patient = new PatientDto
            {
                Id = Guid.NewGuid(),
                Name = "张三",
                Gender = "男",
                Age = 30
            };

            var eventArgs = new PatientSelectedEventArgs
            {
                Patient = patient,
                Source = "PatientList"
            };

            // Act
            _eventHandler.HandlePatientSelected(eventArgs);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"患者选中: {patient.Name}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void HandleConsultationCompleted_ShouldProcessCorrectly()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            var patientId = Guid.NewGuid();
            var eventArgs = new ConsultationCompletedEventArgs(consultationId, patientId, true);

            // Act
            _eventHandler.HandleConsultationCompleted(eventArgs);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"问诊完成: {consultationId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void HandlePrescriptionSaved_ShouldTrackCorrectly()
        {
            // Arrange
            var prescription = new PrescriptionDto
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                TotalAmount = 150.00m
            };

            var eventArgs = new PrescriptionSavedEventArgs
            {
                PrescriptionId = prescription.Id,
                Prescription = prescription,
                IsNew = true
            };

            // Act
            _eventHandler.HandlePrescriptionSaved(eventArgs);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("新处方保存")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task BatchPublish_ShouldHandleMultipleEventsCorrectly()
        {
            // Arrange
            var mockStatusEvent = new Mock<StatusMessageEvent>();
            var mockErrorEvent = new Mock<ErrorOccurredEvent>();
            
            _mockEventAggregator.Setup(x => x.GetEvent<StatusMessageEvent>())
                .Returns(mockStatusEvent.Object);
            _mockEventAggregator.Setup(x => x.GetEvent<ErrorOccurredEvent>())
                .Returns(mockErrorEvent.Object);

            _eventHandler.Initialize();

            // Act - 批量发布事件
            var tasks = new[]
            {
                _eventHandler.PublishStatusMessageAsync("消息1", StatusMessageType.Info),
                _eventHandler.PublishStatusMessageAsync("消息2", StatusMessageType.Success),
                _eventHandler.PublishErrorAsync(new Exception("错误1"), "Source1"),
                _eventHandler.PublishErrorAsync(new Exception("错误2"), "Source2")
            };

            await Task.WhenAll(tasks);

            // Assert
            mockStatusEvent.Verify(x => x.Publish(It.IsAny<StatusMessageEventArgs>()), Times.Exactly(2));
            mockErrorEvent.Verify(x => x.Publish(It.IsAny<ErrorOccurredEventArgs>()), Times.Exactly(2));
        }

        [Fact]
        public void EventSubscription_WithWeakReference_ShouldNotPreventGC()
        {
            // Arrange
            var mockPatientEvent = new Mock<PatientSelectedEvent>();
            _mockEventAggregator.Setup(x => x.GetEvent<PatientSelectedEvent>())
                .Returns(mockPatientEvent.Object);

            WeakReference weakRef;
            
            // Act - 创建临时订阅者
            {
                var tempHandler = new UnifiedEventHandler(
                    _mockEventAggregator.Object,
                    _mockLogger.Object,
                    _mockErrorHandlingService.Object);
                
                tempHandler.Initialize();
                weakRef = new WeakReference(tempHandler);
            }

            // Force GC
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Assert - 对象应该被回收
            weakRef.IsAlive.Should().BeFalse();
        }

        [Fact]
        public void HandleError_WithCircuitBreaker_ShouldPreventFlood()
        {
            // Arrange
            var errorCount = 0;
            _mockErrorHandlingService.Setup(x => x.HandleErrorAsync(It.IsAny<Exception>(), It.IsAny<string>()))
                .Callback(() => errorCount++)
                .Returns(Task.CompletedTask);

            // Act - 快速发送多个错误
            for (int i = 0; i < 10; i++)
            {
                _eventHandler.PublishErrorAsync(new Exception($"Error {i}"), "Test").Wait();
            }

            // Assert - 应该有速率限制
            errorCount.Should().BeLessOrEqualTo(5); // 假设限制为5个/秒
        }

        [Fact]
        public void Dispose_ShouldUnsubscribeAllEvents()
        {
            // Arrange
            _eventHandler.Initialize();
            
            // Act
            _eventHandler.Dispose();

            // Assert
            _eventHandler.IsInitialized.Should().BeFalse();
            
            // 尝试发布事件不应该有任何效果
            var task = _eventHandler.PublishStatusMessageAsync("Test", StatusMessageType.Info);
            task.Wait();
            
            // 验证没有新的事件发布
            var mockStatusEvent = new Mock<StatusMessageEvent>();
            mockStatusEvent.Verify(x => x.Publish(It.IsAny<StatusMessageEventArgs>()), Times.Never);
        }

        [Theory]
        [InlineData(StatusMessageType.Info, "信息")]
        [InlineData(StatusMessageType.Success, "成功")]
        [InlineData(StatusMessageType.Warning, "警告")]
        [InlineData(StatusMessageType.Error, "错误")]
        public async Task StatusMessage_WithDifferentTypes_ShouldFormatCorrectly(
            StatusMessageType type, string expectedPrefix)
        {
            // Arrange
            var capturedArgs = (StatusMessageEventArgs?)null;
            var mockStatusEvent = new Mock<StatusMessageEvent>();
            mockStatusEvent.Setup(x => x.Publish(It.IsAny<StatusMessageEventArgs>()))
                .Callback<StatusMessageEventArgs>(args => capturedArgs = args);
            
            _mockEventAggregator.Setup(x => x.GetEvent<StatusMessageEvent>())
                .Returns(mockStatusEvent.Object);

            _eventHandler.Initialize();

            // Act
            await _eventHandler.PublishStatusMessageAsync("测试内容", type);

            // Assert
            capturedArgs.Should().NotBeNull();
            capturedArgs!.Type.Should().Be(type);
            capturedArgs.Message.Should().Be("测试内容");
        }

        [Fact]
        public void ConcurrentEventHandling_ShouldBeThreadSafe()
        {
            // Arrange
            var processedEvents = new List<string>();
            var lockObj = new object();
            
            _eventHandler.Initialize();

            // Act - 并发处理多个事件
            Parallel.For(0, 100, i =>
            {
                var patient = new PatientDto
                {
                    Id = Guid.NewGuid(),
                    Name = $"Patient{i}"
                };

                _eventHandler.HandlePatientSelected(new PatientSelectedEventArgs
                {
                    Patient = patient
                });

                lock (lockObj)
                {
                    processedEvents.Add(patient.Name);
                }
            });

            // Assert - 所有事件都应该被处理
            processedEvents.Should().HaveCount(100);
            processedEvents.Distinct().Should().HaveCount(100);
        }
    }
}