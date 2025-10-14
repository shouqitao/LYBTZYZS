using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Presentation.Components.PatientSelector;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Prism.Commands;
using Prism.Events;

namespace LYBT.Desktop.PatientSelector.Tests.ViewModels
{
    /// <summary>
    /// PatientSelectorViewModel单元测试
    /// </summary>
    public class PatientSelectorViewModelTests : IDisposable
    {
        private Mock<IEventAggregator> _mockEventAggregator;
        private Mock<PatientSelectedEvent> _mockPatientSelectedEvent;
        private PatientSelectorViewModel _viewModel;
        private Mock<ILogger<PatientSelectorViewModel>> _mockLogger;

        public PatientSelectorViewModelTests()
        {
            _mockEventAggregator = new Mock<IEventAggregator>();
            _mockPatientSelectedEvent = new Mock<PatientSelectedEvent>();
            _mockLogger = new Mock<ILogger<PatientSelectorViewModel>>();

            _mockEventAggregator.Setup(x => x.GetEvent<PatientSelectedEvent>())
                .Returns(_mockPatientSelectedEvent.Object);
        }

        [Fact]
        public void Constructor_ShouldInitializeProperties()
        {
            // Act
            _viewModel = new PatientSelectorViewModel(_mockEventAggregator.Object);

            // Assert
            Assert.Equal(string.Empty, _viewModel.SearchKeyword);
            Assert.NotNull(_viewModel.SearchResults);
            Assert.Empty(_viewModel.SearchResults);
            Assert.False(_viewModel.HasNoResults);
            Assert.Null(_viewModel.SelectedPatient);
            Assert.False(_viewModel.ShowQuickCreate);
            Assert.Equal(string.Empty, _viewModel.NewPatientName);
            Assert.Equal(string.Empty, _viewModel.NewPatientGender);
            Assert.Equal(string.Empty, _viewModel.NewPatientPhone);
            Assert.False(_viewModel.IsLoading);
            Assert.Equal(string.Empty, _viewModel.ErrorMessage);
            Assert.False(_viewModel.HasError);
        }

        [Fact]
        public void Constructor_ShouldInitializeCommands()
        {
            // Act
            _viewModel = new PatientSelectorViewModel(_mockEventAggregator.Object);

            // Assert
            Assert.NotNull(_viewModel.SearchCommand);
            Assert.NotNull(_viewModel.SelectPatientCommand);
            Assert.NotNull(_viewModel.QuickCreateCommand);
            Assert.NotNull(_viewModel.ToggleQuickCreateCommand);
        }

        [Fact]
        public void SearchKeyword_WhenSet_ShouldRaisePropertyChanged()
        {
            // Arrange
            _viewModel = new PatientSelectorViewModel(_mockEventAggregator.Object);
            var propertyChangedRaised = false;
            _viewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(_viewModel.SearchKeyword))
                    propertyChangedRaised = true;
            };

            // Act
            _viewModel.SearchKeyword = "test";

            // Assert
            Assert.True(propertyChangedRaised);
            Assert.Equal("test", _viewModel.SearchKeyword);
        }

        [Fact]
        public void HasNoResults_WhenSearchKeywordEmpty_ShouldReturnFalse()
        {
            // Arrange
            _viewModel = new PatientSelectorViewModel(_mockEventAggregator.Object);
            _viewModel.SearchKeyword = string.Empty;

            // Assert
            Assert.False(_viewModel.HasNoResults);
        }

        [Fact]
        public void HasNoResults_WhenSearchResultsEmpty_ShouldReturnTrue()
        {
            // Arrange
            _viewModel = new PatientSelectorViewModel(_mockEventAggregator.Object);
            _viewModel.SearchKeyword = "test";
            _viewModel.SearchResults.Clear();

            // Assert
            Assert.True(_viewModel.HasNoResults);
        }

        [Fact]
        public void HasNoResults_WhenSearchResultsNotEmpty_ShouldReturnFalse()
        {
            // Arrange
            _viewModel = new PatientSelectorViewModel(_mockEventAggregator.Object);
            _viewModel.SearchKeyword = "test";
            _viewModel.SearchResults.Add(new { Id = Guid.NewGuid(), Name = "Test Patient" });

            // Assert
            Assert.False(_viewModel.HasNoResults);
        }

        [Fact]
        public void HasError_WhenErrorMessageEmpty_ShouldReturnFalse()
        {
            // Arrange
            _viewModel = new PatientSelectorViewModel(_mockEventAggregator.Object);
            _viewModel.ErrorMessage = string.Empty;

            // Assert
            Assert.False(_viewModel.HasError);
        }

        [Fact]
        public void HasError_WhenErrorMessageNotEmpty_ShouldReturnTrue()
        {
            // Arrange
            _viewModel = new PatientSelectorViewModel(_mockEventAggregator.Object);
            _viewModel.ErrorMessage = "Error message";

            // Assert
            Assert.True(_viewModel.HasError);
        }

        [Fact]
        public void SearchCommand_ShouldExecute_WhenSearchKeywordNotEmpty()
        {
            // Arrange
            _viewModel = new PatientSelectorViewModel(_mockEventAggregator.Object);
            _viewModel.SearchKeyword = "test";

            // Act & Assert
            Assert.True(_viewModel.SearchCommand.CanExecute());
        }

        [Fact]
        public void SearchCommand_ShouldNotExecute_WhenSearchKeywordEmpty()
        {
            // Arrange
            _viewModel = new PatientSelectorViewModel(_mockEventAggregator.Object);
            _viewModel.SearchKeyword = string.Empty;

            // Act & Assert
            Assert.False(_viewModel.SearchCommand.CanExecute());
        }

        [Fact]
        public async Task SearchCommand_ShouldSetIsLoadingAndSearchResults()
        {
            // Arrange
            _viewModel = new PatientSelectorViewModel(_mockEventAggregator.Object);
            _viewModel.SearchKeyword = "test";

            // Act
            _viewModel.SearchCommand.Execute();
            await Task.Delay(600); // 等待异步搜索完成

            // Assert
            Assert.False(_viewModel.IsLoading);
            Assert.Equal(string.Empty, _viewModel.ErrorMessage);
            Assert.True(_viewModel.SearchResults.Count > 0);
        }

        [Fact]
        public void SelectPatientCommand_ShouldExecute_WhenPatientProvided()
        {
            // Arrange
            _viewModel = new PatientSelectorViewModel(_mockEventAggregator.Object);
            var patient = new { Id = Guid.NewGuid(), Name = "Test Patient" };

            // Act & Assert
            Assert.True(_viewModel.SelectPatientCommand.CanExecute(patient));
        }

        [Fact]
        public void SelectPatientCommand_ShouldNotExecute_WhenPatientNull()
        {
            // Arrange
            _viewModel = new PatientSelectorViewModel(_mockEventAggregator.Object);

            // Act & Assert
            Assert.False(_viewModel.SelectPatientCommand.CanExecute(null));
        }

        [Fact]
        public void SelectPatientCommand_ShouldPublishEventAndClearSelection()
        {
            // Arrange
            _viewModel = new PatientSelectorViewModel(_mockEventAggregator.Object);
            var patient = new
            {
                Id = Guid.NewGuid(),
                Name = "Test Patient",
                Gender = "男",
                Age = 30,
                PhoneNumber = "13800138000"
            };

            // Act
            _viewModel.SelectPatientCommand.Execute(patient);

            // Assert
            _mockPatientSelectedEvent.Verify(x => x.Publish(It.Is<PatientSelectedPayload>(p =>
                p.PatientId == patient.Id &&
                p.PatientName == patient.Name &&
                p.Gender == patient.Gender &&
                p.PhoneNumber == patient.PhoneNumber
            )), Times.Once);

            Assert.Equal(string.Empty, _viewModel.SearchKeyword);
            Assert.Empty(_viewModel.SearchResults);
            Assert.False(_viewModel.ShowQuickCreate);
        }

        [Fact]
        public void ToggleQuickCreateCommand_ShouldToggleShowQuickCreate()
        {
            // Arrange
            _viewModel = new PatientSelectorViewModel(_mockEventAggregator.Object);
            _viewModel.ShowQuickCreate = false;

            // Act
            _viewModel.ToggleQuickCreateCommand.Execute();

            // Assert
            Assert.True(_viewModel.ShowQuickCreate);

            // Act again
            _viewModel.ToggleQuickCreateCommand.Execute();

            // Assert
            Assert.False(_viewModel.ShowQuickCreate);
        }

        [Fact]
        public void QuickCreateCommand_ShouldExecute_WhenFormValid()
        {
            // Arrange
            _viewModel = new PatientSelectorViewModel(_mockEventAggregator.Object);
            _viewModel.NewPatientName = "Test Patient";
            _viewModel.NewPatientGender = "男";
            _viewModel.NewPatientPhone = "13800138000123";

            // Act & Assert
            Assert.True(_viewModel.QuickCreateCommand.CanExecute());
        }

        [Fact]
        public void QuickCreateCommand_ShouldNotExecute_WhenFormInvalid()
        {
            // Arrange
            _viewModel = new PatientSelectorViewModel(_mockEventAggregator.Object);
            _viewModel.NewPatientName = string.Empty;
            _viewModel.NewPatientGender = string.Empty;
            _viewModel.NewPatientPhone = string.Empty;

            // Act & Assert
            Assert.False(_viewModel.QuickCreateCommand.CanExecute());
        }

        [Fact]
        public void QuickCreateCommand_ShouldNotExecute_WhenPhoneTooShort()
        {
            // Arrange
            _viewModel = new PatientSelectorViewModel(_mockEventAggregator.Object);
            _viewModel.NewPatientName = "Test Patient";
            _viewModel.NewPatientGender = "男";
            _viewModel.NewPatientPhone = "123";

            // Act & Assert
            Assert.False(_viewModel.QuickCreateCommand.CanExecute());
        }

        [Fact]
        public async Task QuickCreateCommand_ShouldCreatePatientAndSelectIt()
        {
            // Arrange
            _viewModel = new PatientSelectorViewModel(_mockEventAggregator.Object);
            _viewModel.NewPatientName = "New Patient";
            _viewModel.NewPatientGender = "女";
            _viewModel.NewPatientPhone = "13900139000123";

            // Act
            _viewModel.QuickCreateCommand.Execute();
            await Task.Delay(600); // 等待异步创建完成

            // Assert
            Assert.Equal(string.Empty, _viewModel.NewPatientName);
            Assert.Equal(string.Empty, _viewModel.NewPatientGender);
            Assert.Equal(string.Empty, _viewModel.NewPatientPhone);
            Assert.False(_viewModel.ShowQuickCreate);
            Assert.False(_viewModel.IsLoading);
            Assert.Equal(string.Empty, _viewModel.ErrorMessage);

            // 验证发布了选择事件
            _mockPatientSelectedEvent.Verify(x => x.Publish(It.Is<PatientSelectedPayload>(p =>
                p.PatientName == "New Patient" &&
                p.Gender == "女" &&
                p.PhoneNumber == "13900139000123"
            )), Times.Once);
        }

        [Fact]
        public void Dispose_ShouldCancelAndDisposeCancellationTokenSource()
        {
            // Arrange
            _viewModel = new PatientSelectorViewModel(_mockEventAggregator.Object);

            // Act & Assert - 没有异常抛出即为成功
            _viewModel.Dispose();
        }

        public void Dispose()
        {
            _viewModel?.Dispose();
        }
    }
}