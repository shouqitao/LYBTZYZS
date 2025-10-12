using FluentAssertions;
using LYBT.Desktop.Consultation.Interfaces;
using LYBT.Desktop.Consultation.ViewModels;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Prism.Regions;
using Xunit;

namespace LYBT.Desktop.Consultation.Tests.ViewModels
{
    /// <summary>
    /// ConsultationManagementViewModel 单元测试
    /// 测试Desktop端Consultation模块的ViewModel业务逻辑
    /// 更新为使用Repository模式（Phase 3迁移）
    /// </summary>
    public class ConsultationManagementViewModelTests : IDisposable
    {
        private readonly Mock<IConsultationRepository> _consultationRepositoryMock;
        private readonly Mock<IEventAggregator> _eventAggregatorMock;
        private readonly Mock<ILoggerFactory> _loggerFactoryMock;
        private readonly Mock<ILogger<ConsultationManagementViewModel>> _loggerMock;
        private readonly Mock<IRegionManager> _regionManagerMock;
        private readonly Mock<ISessionManager> _sessionManagerMock;
        private readonly Mock<IUserNotificationService> _notificationServiceMock;
        private readonly ConsultationManagementViewModel _viewModel;

        public ConsultationManagementViewModelTests()
        {
            // 初始化Mocks
            _consultationRepositoryMock = new Mock<IConsultationRepository>();
            _eventAggregatorMock = new Mock<IEventAggregator>();
            _loggerFactoryMock = new Mock<ILoggerFactory>();
            _loggerMock = new Mock<ILogger<ConsultationManagementViewModel>>();
            _regionManagerMock = new Mock<IRegionManager>();
            _sessionManagerMock = new Mock<ISessionManager>();
            _notificationServiceMock = new Mock<IUserNotificationService>();

            // 设置LoggerFactory返回Logger
            _loggerFactoryMock
                .Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(_loggerMock.Object);

            // EventAggregator不需要特殊设置，使用默认Mock行为即可

            // 创建ViewModel实例（使用Repository）
            _viewModel = new ConsultationManagementViewModel(
                _consultationRepositoryMock.Object,
                _eventAggregatorMock.Object,
                _loggerFactoryMock.Object,
                _regionManagerMock.Object,
                _sessionManagerMock.Object,
                _notificationServiceMock.Object);

            // 等待InitializeAsync完成
            Task.Delay(100).Wait();
        }

        #region LoadData Tests

        [Fact]
        public async Task LoadDataAsync_WithValidData_ShouldPopulateConsultations()
        {
            // Arrange
            var consultations = new List<ConsultationDto>
            {
                new ConsultationDto { Id = Guid.NewGuid(), ChiefComplaint = "头痛" },
                new ConsultationDto { Id = Guid.NewGuid(), ChiefComplaint = "发热" }
            };

            var pagedResult = new PagedResult<ConsultationDto>
            {
                Items = consultations,
                TotalCount = 2,
                CurrentPage = 1,
                PageSize = 20
            };

            _consultationRepositoryMock
                .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(pagedResult);

            // Act
            if (_viewModel.LoadDataCommand.CanExecute(null))
            {
                _viewModel.LoadDataCommand.Execute(null);
                await Task.Delay(100); // 等待异步完成
            }

            // Assert
            _viewModel.Consultations.Should().HaveCount(2);
            _viewModel.Consultations[0].ChiefComplaint.Should().Be("头痛");
            _viewModel.Consultations[1].ChiefComplaint.Should().Be("发热");
        }

        [Fact]
        public async Task LoadDataAsync_WhenRepositoryReturnsNull_ShouldHandleError()
        {
            // Arrange - Repository 返回 null（模拟异常情况）
            _consultationRepositoryMock
                .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((PagedResult<ConsultationDto>)null!);

            // Act
            if (_viewModel.LoadDataCommand.CanExecute(null))
            {
                _viewModel.LoadDataCommand.Execute(null);
                await Task.Delay(100);
            }

            // Assert
            _viewModel.Consultations.Should().BeEmpty();
            // Repository 模式：异常由 UnifiedViewModelBase 捕获
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        #endregion

        #region Search Tests

        [Fact]
        public async Task SearchAsync_WithKeyword_ShouldFilterResults()
        {
            // Arrange
            var keyword = "头痛";
            _viewModel.SearchKeyword = keyword;

            var filteredConsultations = new List<ConsultationDto>
            {
                new ConsultationDto { Id = Guid.NewGuid(), ChiefComplaint = "头痛发热" }
            };

            var pagedResult = new PagedResult<ConsultationDto>
            {
                Items = filteredConsultations,
                TotalCount = 1,
                CurrentPage = 1,
                PageSize = 20
            };

            _consultationRepositoryMock
                .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), keyword))
                .ReturnsAsync(pagedResult);

            // Act
            if (_viewModel.SearchCommand.CanExecute(null))
            {
                _viewModel.SearchCommand.Execute(null);
                await Task.Delay(100);
            }

            // Assert
            _viewModel.Consultations.Should().HaveCount(1);
            _viewModel.Consultations[0].ChiefComplaint.Should().Contain(keyword);
        }

        #endregion

        #region ViewDetails Tests

        [Fact]
        public void ViewDetailsCommand_WithNullConsultation_ShouldNotExecute()
        {
            // Arrange
            ConsultationDto? nullConsultation = null;

            // Act & Assert
#pragma warning disable CS8604 // 可能传入 null 引用参数 - 这正是测试的目的
            _viewModel.ViewDetailsCommand.CanExecute(nullConsultation).Should().BeFalse();
#pragma warning restore CS8604
        }

        [Fact]
        public void ViewDetailsCommand_WithValidConsultation_ShouldExecute()
        {
            // Arrange
            var consultation = new ConsultationDto { Id = Guid.NewGuid(), ChiefComplaint = "测试" };

            // Act & Assert
            _viewModel.ViewDetailsCommand.CanExecute(consultation).Should().BeTrue();
        }

        #endregion

        #region Property Tests

        [Fact]
        public void SearchKeyword_WhenSet_ShouldRaisePropertyChanged()
        {
            // Arrange
            var propertyChangedRaised = false;
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ConsultationManagementViewModel.SearchKeyword))
                    propertyChangedRaised = true;
            };

            // Act
            _viewModel.SearchKeyword = "新关键字";

            // Assert
            propertyChangedRaised.Should().BeTrue();
            _viewModel.SearchKeyword.Should().Be("新关键字");
        }

        [Fact]
        public void SelectedConsultation_WhenSet_ShouldRaisePropertyChanged()
        {
            // Arrange
            var consultation = new ConsultationDto { Id = Guid.NewGuid() };
            var propertyChangedRaised = false;
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ConsultationManagementViewModel.SelectedConsultation))
                    propertyChangedRaised = true;
            };

            // Act
            _viewModel.SelectedConsultation = consultation;

            // Assert
            propertyChangedRaised.Should().BeTrue();
            _viewModel.SelectedConsultation.Should().Be(consultation);
        }

        #endregion

        #region IConsultationRepository Mock Tests

        [Fact]
        public async Task Repository_GetPagedAsync_ShouldBeCalledWithCorrectParameters()
        {
            // Arrange
            int expectedPage = 1;
            int expectedPageSize = 100; // ViewModel默认使用100
            string expectedKeyword = ""; // 默认SearchKeyword是空字符串

            _consultationRepositoryMock
                .Setup(x => x.GetPagedAsync(expectedPage, expectedPageSize, expectedKeyword))
                .ReturnsAsync(new PagedResult<ConsultationDto> { Items = new List<ConsultationDto>() });

            // Act
            if (_viewModel.LoadDataCommand.CanExecute(null))
            {
                _viewModel.LoadDataCommand.Execute(null);
                await Task.Delay(100);
            }

            // Assert
            _consultationRepositoryMock.Verify(
                x => x.GetPagedAsync(expectedPage, expectedPageSize, expectedKeyword),
                Times.AtLeastOnce());
        }

        #endregion

        public void Dispose()
        {
            // 清理资源
            GC.SuppressFinalize(this);
        }
    }
}
