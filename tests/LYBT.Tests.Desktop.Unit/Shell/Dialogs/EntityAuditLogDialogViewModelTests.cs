using FluentAssertions;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Shell.Dialogs.ViewModels;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Shell.Tests.Dialogs;

/// <summary>
/// EntityAuditLogDialogViewModel 单元测试
/// Issue #2249: 添加审计系统单元测试
/// OpenSpec: add-global-audit-system
/// OpenSpec: enhance-viewmodel-architecture - 更新为IViewModelServices构造函数
/// </summary>
public class EntityAuditLogDialogViewModelTests : IDisposable
{
    private bool _disposed;
    private readonly Mock<IApiService> _mockApiService;
    private readonly Mock<IViewModelServices> _mockServices;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger> _mockLogger;
    private readonly EntityAuditLogDialogViewModel _viewModel;

    public EntityAuditLogDialogViewModelTests()
    {
        _mockApiService = new Mock<IApiService>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger>();

        // 配置LoggerFactory返回Mock Logger
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);

        // 配置IViewModelServices
        _mockServices = new Mock<IViewModelServices>();
        _mockServices.Setup(x => x.LoggerFactory).Returns(_mockLoggerFactory.Object);
        _mockServices.Setup(x => x.EventAggregator).Returns(new Mock<IEventAggregator>().Object);

        // OpenSpec: enhance-viewmodel-architecture - 使用IViewModelServices构造函数（2个参数）
        _viewModel = new EntityAuditLogDialogViewModel(
            _mockServices.Object,
            _mockApiService.Object);
    }

    #region 构造函数测试

    [Fact]
    public void Constructor_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new EntityAuditLogDialogViewModel(
            null!,
            _mockApiService.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void Constructor_WithNullApiService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new EntityAuditLogDialogViewModel(
            _mockServices.Object,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("apiService");
    }

    [Fact]
    public void Constructor_ShouldInitializeCommands()
    {
        // Assert
        _viewModel.RefreshCommand.Should().NotBeNull();
        _viewModel.CloseCommand.Should().NotBeNull();
        _viewModel.PreviousPageCommand.Should().NotBeNull();
        _viewModel.NextPageCommand.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Assert
        _viewModel.Title.Should().Be("变更记录");
        _viewModel.CurrentPage.Should().Be(1);
        _viewModel.TotalPages.Should().Be(1);
        _viewModel.AuditLogs.Should().BeEmpty();
        _viewModel.IsLoading.Should().BeFalse();
    }

    #endregion

    #region OnDialogOpened 测试

    [Fact]
    public void OnDialogOpened_WithValidParameters_ShouldSetProperties()
    {
        // Arrange
        var entityType = "Patient";
        var entityId = Guid.NewGuid();
        var entityDescription = "张三";

        var parameters = new DialogParameters
        {
            { "EntityType", entityType },
            { "EntityId", entityId },
            { "EntityDescription", entityDescription }
        };

        // 配置API返回空结果（避免异步问题）
        _mockApiService.Setup(x => x.GetAsync<PagedResult<EntityAuditLogDto>>(
            It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<EntityAuditLogDto>(new List<EntityAuditLogDto>(), 0, 1, 20));

        // Act
        _viewModel.OnDialogOpened(parameters);

        // Assert
        _viewModel.EntityType.Should().Be(entityType);
        _viewModel.EntityId.Should().Be(entityId);
        _viewModel.EntityDescription.Should().Be(entityDescription);
    }

    [Theory]
    [InlineData("Patient", "患者变更记录")]
    [InlineData("Prescription", "处方变更记录")]
    [InlineData("Herb", "药材变更记录")]
    [InlineData("Formula", "验方变更记录")]
    [InlineData("User", "用户变更记录")]
    [InlineData("Consultation", "诊断变更记录")]
    [InlineData("Unknown", "变更记录")]
    public void OnDialogOpened_ShouldSetCorrectTitle(string entityType, string expectedTitle)
    {
        // Arrange
        var parameters = new DialogParameters
        {
            { "EntityType", entityType },
            { "EntityId", Guid.NewGuid() }
        };

        _mockApiService.Setup(x => x.GetAsync<PagedResult<EntityAuditLogDto>>(
            It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<EntityAuditLogDto>(new List<EntityAuditLogDto>(), 0, 1, 20));

        // Act
        _viewModel.OnDialogOpened(parameters);

        // Assert
        _viewModel.Title.Should().Be(expectedTitle);
    }

    #endregion

    #region 分页命令测试

    [Fact]
    public void CanGoPrevious_OnFirstPage_ShouldReturnFalse()
    {
        // Arrange
        _viewModel.GetType().GetProperty("CurrentPage")!.SetValue(_viewModel, 1);

        // Assert
        _viewModel.CanGoPrevious.Should().BeFalse();
    }

    [Fact]
    public void CanGoPrevious_OnSecondPage_ShouldReturnTrue()
    {
        // Arrange - 直接设置私有字段
        var currentPageProperty = _viewModel.GetType().GetField("_currentPage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        currentPageProperty?.SetValue(_viewModel, 2);

        // 手动触发属性通知
        _viewModel.GetType().GetMethod("RaisePropertyChanged",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .Invoke(_viewModel, new object[] { nameof(_viewModel.CanGoPrevious) });

        // Assert
        _viewModel.CanGoPrevious.Should().BeTrue();
    }

    [Fact]
    public void CanGoNext_OnLastPage_ShouldReturnFalse()
    {
        // Arrange - CurrentPage = 1, TotalPages = 1 (default)

        // Assert
        _viewModel.CanGoNext.Should().BeFalse();
    }

    [Fact]
    public void CanGoNext_WithMorePages_ShouldReturnTrue()
    {
        // Arrange
        var totalPagesProperty = _viewModel.GetType().GetField("_totalPages",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        totalPagesProperty?.SetValue(_viewModel, 3);

        // Assert
        _viewModel.CanGoNext.Should().BeTrue();
    }

    #endregion

    #region CloseCommand 测试

    [Fact]
    public void CloseCommand_ShouldRaiseRequestClose()
    {
        // Arrange
        IDialogResult? receivedResult = null;
        _viewModel.RequestClose += result => receivedResult = result;

        // Act
        _viewModel.CloseCommand.Execute(null);

        // Assert
        receivedResult.Should().NotBeNull();
        receivedResult!.Result.Should().Be(ButtonResult.OK);
    }

    #endregion

    #region IDialogAware 测试

    [Fact]
    public void CanCloseDialog_ShouldReturnTrue()
    {
        // Assert
        _viewModel.CanCloseDialog().Should().BeTrue();
    }

    [Fact]
    public void OnDialogClosed_ShouldNotThrow()
    {
        // Act & Assert
        var act = () => _viewModel.OnDialogClosed();
        act.Should().NotThrow();
    }

    #endregion

    #region AuditLogDisplayItem 测试

    [Fact]
    public void AuditLogDisplayItem_Constructor_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new AuditLogDisplayItem(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AuditLogDisplayItem_ChangedFieldsSummary_WithEmptyFields_ShouldReturnDash()
    {
        // Arrange
        var dto = new EntityAuditLogDto
        {
            Id = Guid.NewGuid(),
            EntityType = "Patient",
            EntityId = Guid.NewGuid(),
            OperatorId = Guid.NewGuid(),
            OperatorName = "测试",
            OperatorRole = UserRole.Doctor,
            OperationType = AuditOperationType.Update,
            ChangedFields = null,
            CreatedAt = DateTime.Now
        };

        var item = new AuditLogDisplayItem(dto);

        // Assert
        item.ChangedFieldsSummary.Should().Be("-");
    }

    [Fact]
    public void AuditLogDisplayItem_ChangedFieldsSummary_WithSingleField_ShouldReturnTranslatedName()
    {
        // Arrange
        var dto = new EntityAuditLogDto
        {
            Id = Guid.NewGuid(),
            EntityType = "Patient",
            EntityId = Guid.NewGuid(),
            OperatorId = Guid.NewGuid(),
            OperatorName = "测试",
            OperatorRole = UserRole.Doctor,
            OperationType = AuditOperationType.Update,
            ChangedFields = "[\"Name\"]",
            CreatedAt = DateTime.Now
        };

        var item = new AuditLogDisplayItem(dto);

        // Assert
        item.ChangedFieldsSummary.Should().Be("名称");
    }

    [Fact]
    public void AuditLogDisplayItem_ChangedFieldsSummary_WithMultipleFields_ShouldJoinWithComma()
    {
        // Arrange
        var dto = new EntityAuditLogDto
        {
            Id = Guid.NewGuid(),
            EntityType = "Patient",
            EntityId = Guid.NewGuid(),
            OperatorId = Guid.NewGuid(),
            OperatorName = "测试",
            OperatorRole = UserRole.Doctor,
            OperationType = AuditOperationType.Update,
            ChangedFields = "[\"Name\", \"Gender\", \"PhoneNumber\"]",
            CreatedAt = DateTime.Now
        };

        var item = new AuditLogDisplayItem(dto);

        // Assert
        item.ChangedFieldsSummary.Should().Be("名称, 性别, 手机号");
    }

    [Fact]
    public void AuditLogDisplayItem_ChangedFieldsSummary_WithMoreThan3Fields_ShouldTruncate()
    {
        // Arrange
        var dto = new EntityAuditLogDto
        {
            Id = Guid.NewGuid(),
            EntityType = "Patient",
            EntityId = Guid.NewGuid(),
            OperatorId = Guid.NewGuid(),
            OperatorName = "测试",
            OperatorRole = UserRole.Doctor,
            OperationType = AuditOperationType.Update,
            ChangedFields = "[\"Name\", \"Gender\", \"PhoneNumber\", \"Address\", \"Email\"]",
            CreatedAt = DateTime.Now
        };

        var item = new AuditLogDisplayItem(dto);

        // Assert
        item.ChangedFieldsSummary.Should().Contain("名称, 性别, 手机号");
        item.ChangedFieldsSummary.Should().Contain("等5项");
    }

    [Fact]
    public void AuditLogDisplayItem_ChangedFieldsSummary_WithUnknownField_ShouldReturnOriginalName()
    {
        // Arrange
        var dto = new EntityAuditLogDto
        {
            Id = Guid.NewGuid(),
            EntityType = "Patient",
            EntityId = Guid.NewGuid(),
            OperatorId = Guid.NewGuid(),
            OperatorName = "测试",
            OperatorRole = UserRole.Doctor,
            OperationType = AuditOperationType.Update,
            ChangedFields = "[\"UnknownField\"]",
            CreatedAt = DateTime.Now
        };

        var item = new AuditLogDisplayItem(dto);

        // Assert
        item.ChangedFieldsSummary.Should().Be("UnknownField");
    }

    [Fact]
    public void AuditLogDisplayItem_Properties_ShouldMapFromDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();
        var createdAt = DateTime.Now;

        var dto = new EntityAuditLogDto
        {
            Id = id,
            EntityType = "Patient",
            EntityId = entityId,
            OperatorId = operatorId,
            OperatorName = "张医生",
            OperatorRole = UserRole.Doctor,
            OperationType = AuditOperationType.Update,
            Reason = "更正信息",
            CreatedAt = createdAt
        };

        var item = new AuditLogDisplayItem(dto);

        // Assert
        item.Id.Should().Be(id);
        item.EntityType.Should().Be("Patient");
        item.EntityId.Should().Be(entityId);
        item.OperatorId.Should().Be(operatorId);
        item.OperatorName.Should().Be("张医生");
        item.Reason.Should().Be("更正信息");
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            // 释放ViewModel资源
            (_viewModel as IDisposable)?.Dispose();
        }

        _disposed = true;
    }

    #endregion
}
