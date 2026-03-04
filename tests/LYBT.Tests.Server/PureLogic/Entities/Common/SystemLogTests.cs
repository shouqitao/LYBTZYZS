using FluentAssertions;
using LYBT.Entities.Common;
using Xunit;

namespace LYBT.Tests.Server.PureLogic.Entities.Common
{
    /// <summary>
    /// SystemLog实体单元测试 - 测试系统日志实体的所有属性
    /// </summary>
    public class SystemLogTests
    {
        [Fact]
        public void Constructor_ShouldInitializePropertiesWithDefaultValues()
        {
            // Arrange & Act
            var log = new SystemLog();

            // Assert
            log.Id.Should().Be(0);
            log.Timestamp.Should().Be(default(DateTime));
            log.Level.Should().Be(string.Empty);
            log.Message.Should().Be(string.Empty);
            log.Exception.Should().BeNull();
            log.LoggerName.Should().BeNull();
            log.UserId.Should().BeNull();
            log.RequestId.Should().BeNull();
            log.MachineName.Should().BeNull();
            log.ThreadId.Should().BeNull();
            log.Properties.Should().BeNull();
        }

        [Fact]
        public void Id_PropertyCanBeSetAndGet()
        {
            // Arrange
            var log = new SystemLog();
            const int testId = 123;

            // Act
            log.Id = testId;

            // Assert
            log.Id.Should().Be(testId);
        }

        [Fact]
        public void Timestamp_PropertyCanBeSetAndGet()
        {
            // Arrange
            var log = new SystemLog();
            var testTime = new DateTime(2024, 1, 1, 12, 0, 0);

            // Act
            log.Timestamp = testTime;

            // Assert
            log.Timestamp.Should().Be(testTime);
        }

        [Fact]
        public void Level_PropertyCanBeSetAndGet()
        {
            // Arrange
            var log = new SystemLog();
            const string testLevel = "Error";

            // Act
            log.Level = testLevel;

            // Assert
            log.Level.Should().Be(testLevel);
        }

        [Fact]
        public void Message_PropertyCanBeSetAndGet()
        {
            // Arrange
            var log = new SystemLog();
            const string testMessage = "测试日志消息";

            // Act
            log.Message = testMessage;

            // Assert
            log.Message.Should().Be(testMessage);
        }

        [Fact]
        public void Exception_PropertyCanBeSetAndGet()
        {
            // Arrange
            var log = new SystemLog();
            const string testException = "System.ArgumentException: 参数错误";

            // Act
            log.Exception = testException;

            // Assert
            log.Exception.Should().Be(testException);
        }

        [Fact]
        public void LoggerName_PropertyCanBeSetAndGet()
        {
            // Arrange
            var log = new SystemLog();
            const string testLoggerName = "LYBT.WebAPI.Controllers.UsersController";

            // Act
            log.LoggerName = testLoggerName;

            // Assert
            log.LoggerName.Should().Be(testLoggerName);
        }

        [Fact]
        public void UserId_PropertyCanBeSetAndGet()
        {
            // Arrange
            var log = new SystemLog();
            var testUserId = Guid.NewGuid();

            // Act
            log.UserId = testUserId;

            // Assert
            log.UserId.Should().Be(testUserId);
        }

        [Fact]
        public void RequestId_PropertyCanBeSetAndGet()
        {
            // Arrange
            var log = new SystemLog();
            var testRequestId = Guid.NewGuid().ToString();

            // Act
            log.RequestId = testRequestId;

            // Assert
            log.RequestId.Should().Be(testRequestId);
        }

        [Fact]
        public void MachineName_PropertyCanBeSetAndGet()
        {
            // Arrange
            var log = new SystemLog();
            const string testMachineName = "LYBT-SERVER-01";

            // Act
            log.MachineName = testMachineName;

            // Assert
            log.MachineName.Should().Be(testMachineName);
        }

        [Fact]
        public void ThreadId_PropertyCanBeSetAndGet()
        {
            // Arrange
            var log = new SystemLog();
            const int testThreadId = 12345;

            // Act
            log.ThreadId = testThreadId;

            // Assert
            log.ThreadId.Should().Be(testThreadId);
        }

        [Fact]
        public void Properties_PropertyCanBeSetAndGet()
        {
            // Arrange
            var log = new SystemLog();
            const string testProperties = "{\"CustomProperty\":\"CustomValue\"}";

            // Act
            log.Properties = testProperties;

            // Assert
            log.Properties.Should().Be(testProperties);
        }

        [Fact]
        public void AllNullableProperties_CanBeSetToNull()
        {
            // Arrange
            var log = new SystemLog();

            // Act
            log.Exception = null;
            log.LoggerName = null;
            log.UserId = null;
            log.RequestId = null;
            log.MachineName = null;
            log.ThreadId = null;
            log.Properties = null;

            // Assert
            log.Exception.Should().BeNull();
            log.LoggerName.Should().BeNull();
            log.UserId.Should().BeNull();
            log.RequestId.Should().BeNull();
            log.MachineName.Should().BeNull();
            log.ThreadId.Should().BeNull();
            log.Properties.Should().BeNull();
        }

        [Fact]
        public void CreateCompleteLogEntry_ShouldSetAllProperties()
        {
            // Arrange
            var log = new SystemLog();
            var timestamp = DateTime.Now;
            var userId = Guid.NewGuid();
            var requestId = Guid.NewGuid().ToString();

            // Act
            log.Id = 1;
            log.Timestamp = timestamp;
            log.Level = "Information";
            log.Message = "用户登录成功";
            log.Exception = null;
            log.LoggerName = "LYBT.WebAPI.Controllers.AuthController";
            log.UserId = userId;
            log.RequestId = requestId;
            log.MachineName = "LYBT-SERVER";
            log.ThreadId = 1234;
            log.Properties = "{\"IPAddress\":\"192.168.1.100\"}";

            // Assert
            log.Id.Should().Be(1);
            log.Timestamp.Should().Be(timestamp);
            log.Level.Should().Be("Information");
            log.Message.Should().Be("用户登录成功");
            log.Exception.Should().BeNull();
            log.LoggerName.Should().Be("LYBT.WebAPI.Controllers.AuthController");
            log.UserId.Should().Be(userId);
            log.RequestId.Should().Be(requestId);
            log.MachineName.Should().Be("LYBT-SERVER");
            log.ThreadId.Should().Be(1234);
            log.Properties.Should().Be("{\"IPAddress\":\"192.168.1.100\"}");
        }
    }
}