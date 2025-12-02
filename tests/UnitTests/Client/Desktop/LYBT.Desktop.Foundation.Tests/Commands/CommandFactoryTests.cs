using FluentAssertions;
using LYBT.Desktop.Foundation.Commands;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Desktop.Foundation.Tests.Commands;

/// <summary>
/// CommandFactory单元测试
/// OpenSpec: refactor-viewmodel-layer Phase 3.1.4
/// </summary>
public class CommandFactoryTests
{
    private readonly ILogger<CommandFactory> _logger;
    private bool _isBusy;
    private Exception? _lastError;
    private string? _lastErrorContext;
    private readonly CommandFactory _sut;

    public CommandFactoryTests()
    {
        _logger = Substitute.For<ILogger<CommandFactory>>();
        _isBusy = false;
        _lastError = null;
        _lastErrorContext = null;

        _sut = new CommandFactory(
            _logger,
            () => _isBusy,
            value => _isBusy = value,
            (ex, context) =>
            {
                _lastError = ex;
                _lastErrorContext = context;
            });
    }

    #region CreateAsyncWithLoadingGuard测试

    [Fact]
    public void CreateAsyncWithLoadingGuard_ShouldThrow_WhenExecuteIsNull()
    {
        // Act & Assert
        var action = () => _sut.CreateAsyncWithLoadingGuard(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("execute");
    }

    [Fact]
    public void CreateAsyncWithLoadingGuard_ShouldReturnCommand()
    {
        // Act
        var command = _sut.CreateAsyncWithLoadingGuard(() => Task.CompletedTask);

        // Assert
        command.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsyncWithLoadingGuard_ShouldSetIsBusyDuringExecution()
    {
        // Arrange
        var busyDuringExecution = false;
        var tcs = new TaskCompletionSource();

        var command = _sut.CreateAsyncWithLoadingGuard(async () =>
        {
            busyDuringExecution = _isBusy;
            await tcs.Task;
        });

        // Act
        command.Execute();
        await Task.Delay(50); // 等待命令开始执行

        // Assert - 执行期间应该是busy
        busyDuringExecution.Should().BeTrue();

        // 完成任务
        tcs.SetResult();
        await Task.Delay(50);

        // Assert - 执行后应该不busy
        _isBusy.Should().BeFalse();
    }

    [Fact]
    public void CreateAsyncWithLoadingGuard_CanExecute_ShouldReturnFalse_WhenBusy()
    {
        // Arrange
        var command = _sut.CreateAsyncWithLoadingGuard(() => Task.CompletedTask);
        _isBusy = true;

        // Act & Assert
        command.CanExecute().Should().BeFalse();
    }

    [Fact]
    public void CreateAsyncWithLoadingGuard_CanExecute_ShouldReturnTrue_WhenNotBusy()
    {
        // Arrange
        var command = _sut.CreateAsyncWithLoadingGuard(() => Task.CompletedTask);
        _isBusy = false;

        // Act & Assert
        command.CanExecute().Should().BeTrue();
    }

    [Fact]
    public void CreateAsyncWithLoadingGuard_CanExecute_ShouldRespectCustomCanExecute()
    {
        // Arrange
        var canExecuteValue = false;
        var command = _sut.CreateAsyncWithLoadingGuard(
            () => Task.CompletedTask,
            () => canExecuteValue);

        // Act & Assert
        command.CanExecute().Should().BeFalse();

        canExecuteValue = true;
        command.CanExecute().Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsyncWithLoadingGuard_ShouldCallErrorHandler_OnException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test error");
        var command = _sut.CreateAsyncWithLoadingGuard(
            () => throw expectedException,
            operationName: "测试操作");

        // Act
        command.Execute();
        await Task.Delay(100);

        // Assert
        _lastError.Should().Be(expectedException);
        _lastErrorContext.Should().Be("测试操作");
        _isBusy.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsyncWithLoadingGuard_ShouldNotSetError_OnCancellation()
    {
        // Arrange
        var command = _sut.CreateAsyncWithLoadingGuard(
            () => throw new OperationCanceledException(),
            operationName: "取消测试");

        // Act
        command.Execute();
        await Task.Delay(100);

        // Assert
        _lastError.Should().BeNull();
        _isBusy.Should().BeFalse();
    }

    #endregion

    #region CreateWithParameter测试

    [Fact]
    public void CreateWithParameter_ShouldThrow_WhenExecuteIsNull()
    {
        // Act & Assert
        var action = () => _sut.CreateWithParameter<string>(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("execute");
    }

    [Fact]
    public async Task CreateWithParameter_ShouldPassParameter()
    {
        // Arrange
        string? receivedParam = null;
        var command = _sut.CreateWithParameter<string>(async param =>
        {
            receivedParam = param;
            await Task.CompletedTask;
        });

        // Act
        command.Execute("TestValue");
        await Task.Delay(100);

        // Assert
        receivedParam.Should().Be("TestValue");
    }

    [Fact]
    public void CreateWithParameter_CanExecute_ShouldRespectCustomCanExecute()
    {
        // Arrange - 使用引用类型，因为Prism的DelegateCommand<T>不支持值类型
        var command = _sut.CreateWithParameter<string>(
            _ => Task.CompletedTask,
            param => !string.IsNullOrEmpty(param));

        // Act & Assert
        command.CanExecute(null).Should().BeFalse();
        command.CanExecute("").Should().BeFalse();
        command.CanExecute("valid").Should().BeTrue();
    }

    #endregion

    #region CreateSyncWithParameter测试

    [Fact]
    public void CreateSyncWithParameter_ShouldThrow_WhenExecuteIsNull()
    {
        // Act & Assert
        var action = () => _sut.CreateSyncWithParameter<string>(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("execute");
    }

    [Fact]
    public void CreateSyncWithParameter_ShouldExecute()
    {
        // Arrange
        string? receivedParam = null;
        var command = _sut.CreateSyncWithParameter<string>(param => receivedParam = param);

        // Act
        command.Execute("SyncTest");

        // Assert
        receivedParam.Should().Be("SyncTest");
    }

    #endregion

    #region CreateSync测试

    [Fact]
    public void CreateSync_ShouldThrow_WhenExecuteIsNull()
    {
        // Act & Assert
        var action = () => _sut.CreateSync(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("execute");
    }

    [Fact]
    public void CreateSync_ShouldExecute()
    {
        // Arrange
        var executed = false;
        var command = _sut.CreateSync(() => executed = true);

        // Act
        command.Execute();

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public void CreateSync_CanExecute_ShouldRespectCustomCanExecute()
    {
        // Arrange
        var canExecuteValue = false;
        var command = _sut.CreateSync(
            () => { },
            () => canExecuteValue);

        // Act & Assert
        command.CanExecute().Should().BeFalse();

        canExecuteValue = true;
        command.CanExecute().Should().BeTrue();
    }

    #endregion

    #region 构造函数测试

    [Fact]
    public void Constructor_ShouldThrow_WhenLoggerIsNull()
    {
        // Act & Assert
        var action = () => new CommandFactory(
            null!,
            () => false,
            _ => { });

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenGetIsBusyIsNull()
    {
        // Act & Assert
        var action = () => new CommandFactory(
            _logger,
            null!,
            _ => { });

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("getIsBusy");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenSetIsBusyIsNull()
    {
        // Act & Assert
        var action = () => new CommandFactory(
            _logger,
            () => false,
            null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("setIsBusy");
    }

    [Fact]
    public void Constructor_ShouldAcceptNullErrorHandler()
    {
        // Act
        var factory = new CommandFactory(
            _logger,
            () => false,
            _ => { },
            null);

        // Assert
        factory.Should().NotBeNull();
    }

    #endregion

    #region 扩展方法测试

    [Fact]
    public void CreateCommandFactory_ShouldReturnInstance()
    {
        // Arrange
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>())
            .Returns(Substitute.For<ILogger>());

        // Act
        var factory = loggerFactory.CreateCommandFactory(
            () => false,
            _ => { });

        // Assert
        factory.Should().NotBeNull();
    }

    #endregion
}
