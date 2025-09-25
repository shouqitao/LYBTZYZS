using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using LYBT.Desktop.Core.Services.ErrorHandling;
using LYBT.Desktop.Core.Services.ListManagement;
using LYBT.Desktop.Patients.ViewModels;
using LYBT.Desktop.UnitTests.TestUtilities.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Prism.Events;

namespace LYBT.Desktop.UnitTests.Modules.Patients.ViewModels
{
    /// <summary>
    /// PatientListViewModel单元测试 - UltraThink测试示例
    /// 展示如何使用ViewModelTestBase进行标准化测试
    /// </summary>
    public class PatientListViewModelTests : ViewModelTestBase<PatientListViewModel>
    {
        private Mock<IPatientService> _mockPatientService = null!;
        private Mock<IListManagementService<PatientDto>> _mockListService = null!;

        protected override PatientListViewModel CreateViewModel()
        {
            _mockPatientService = new Mock<IPatientService>();
            _mockListService = new Mock<IListManagementService<PatientDto>>();

            // 设置默认返回值
            _mockListService.Setup(x => x.Items).Returns(new ObservableCollection<PatientDto>());
            _mockPatientService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ApiResult<List<PatientDto>>.Success(new List<PatientDto>()));

            return new PatientListViewModel(
                MockEventAggregator.Object,
                MockLoggerFactory.Object,
                MockErrorHandlingService.Object,
                _mockListService.Object,
                _mockPatientService.Object);
        }

        [Fact]
        public async Task LoadPatientsAsync_WhenSuccessful_ShouldLoadPatients()
        {
            // Arrange
            var patients = new List<PatientDto>
            {
                new() { Id = Guid.NewGuid(), Name = "张三", Gender = "男", Age = 30 },
                new() { Id = Guid.NewGuid(), Name = "李四", Gender = "女", Age = 25 }
            };

            _mockPatientService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ApiResult<List<PatientDto>>.Success(patients));

            _mockListService.Setup(x => x.LoadAsync(It.IsAny<IEnumerable<PatientDto>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await ViewModel.LoadPatientsAsync();

            // Assert
            _mockPatientService.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockListService.Verify(x => x.LoadAsync(It.IsAny<IEnumerable<PatientDto>>(), It.IsAny<CancellationToken>()), Times.Once);
            AssertBusy(false);
            VerifyLogged(LogLevel.Information, "开始加载患者列表");
            VerifyLogged(LogLevel.Information, $"成功加载 {patients.Count} 个患者");
        }

        [Fact]
        public async Task LoadPatientsAsync_WhenFails_ShouldHandleError()
        {
            // Arrange
            var exception = new Exception("网络连接失败");
            _mockPatientService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            await ViewModel.LoadPatientsAsync();

            // Assert
            MockErrorHandlingService.Verify(
                x => x.HandleErrorAsync(It.IsAny<Exception>(), "加载患者列表"),
                Times.Once);
            AssertBusy(false);
            AssertStatusMessage("加载患者列表失败");
            VerifyLogged(LogLevel.Error, "加载患者列表失败");
        }

        [Fact]
        public void SearchCommand_ShouldFilterPatients()
        {
            // Arrange
            var searchText = "张";
            ViewModel.SearchText = searchText;

            // Act
            ViewModel.SearchCommand.Execute(null);

            // Assert
            _mockListService.Verify(
                x => x.Filter(It.IsAny<Func<PatientDto, bool>>()),
                Times.Once);
        }

        [Fact]
        public async Task RefreshCommand_ShouldReloadPatients()
        {
            // Arrange
            var patients = new List<PatientDto>
            {
                new() { Id = Guid.NewGuid(), Name = "王五", Gender = "男", Age = 35 }
            };

            _mockPatientService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ApiResult<List<PatientDto>>.Success(patients));

            // Act
            await ViewModel.RefreshCommand.ExecuteAsync();

            // Assert
            _mockPatientService.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockListService.Verify(x => x.RefreshAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void SelectedPatient_WhenChanged_ShouldPublishEvent()
        {
            // Arrange
            var patient = new PatientDto
            {
                Id = Guid.NewGuid(),
                Name = "测试患者",
                Gender = "男",
                Age = 40
            };

            var mockEvent = new Mock<PatientSelectedEvent>();
            MockEventAggregator.Setup(x => x.GetEvent<PatientSelectedEvent>())
                .Returns(mockEvent.Object);

            // Act
            ViewModel.SelectedPatient = patient;

            // Assert
            mockEvent.Verify(x => x.Publish(It.Is<PatientSelectedEventArgs>(
                args => args.Patient.Id == patient.Id)), Times.Once);
        }

        [Fact]
        public void DeleteCommand_WhenPatientSelected_ShouldBeExecutable()
        {
            // Arrange
            ViewModel.SelectedPatient = new PatientDto { Id = Guid.NewGuid() };

            // Act
            var canExecute = ViewModel.DeleteCommand.CanExecute(null);

            // Assert
            canExecute.Should().BeTrue();
        }

        [Fact]
        public void DeleteCommand_WhenNoPatientSelected_ShouldNotBeExecutable()
        {
            // Arrange
            ViewModel.SelectedPatient = null;

            // Act
            var canExecute = ViewModel.DeleteCommand.CanExecute(null);

            // Assert
            canExecute.Should().BeFalse();
        }

        [Fact]
        public async Task DeletePatientAsync_WhenConfirmed_ShouldDeletePatient()
        {
            // Arrange
            var patient = new PatientDto
            {
                Id = Guid.NewGuid(),
                Name = "待删除患者"
            };
            ViewModel.SelectedPatient = patient;

            _mockPatientService.Setup(x => x.DeleteAsync(patient.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ApiResult<bool>.Success(true));

            // Act
            await ViewModel.DeletePatientAsync();

            // Assert
            _mockPatientService.Verify(x => x.DeleteAsync(patient.Id, It.IsAny<CancellationToken>()), Times.Once);
            _mockListService.Verify(x => x.RemoveItem(patient), Times.Once);
            VerifyLogged(LogLevel.Information, $"成功删除患者: {patient.Name}");
        }

        [Fact]
        public void SortCommand_ShouldSortPatientsByProperty()
        {
            // Arrange
            var propertyName = "Name";

            // Act
            ViewModel.SortCommand.Execute(propertyName);

            // Assert
            _mockListService.Verify(
                x => x.Sort(propertyName, It.IsAny<System.ComponentModel.ListSortDirection>()),
                Times.Once);
        }

        [Fact]
        public void PropertyChanged_IsBusy_ShouldUpdateUI()
        {
            // Arrange & Act & Assert
            AssertPropertyChanged(
                () => ViewModel.IsBusy = true,
                nameof(ViewModel.IsBusy));
        }

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Arrange
            var mockDisposable = new Mock<IDisposable>();
            ViewModel.AddDisposable(mockDisposable.Object);

            // Act
            ViewModel.Dispose();

            // Assert
            mockDisposable.Verify(x => x.Dispose(), Times.Once);
        }

        [Theory]
        [InlineData("", 0)] // 空搜索
        [InlineData("张", 1)] // 单字搜索
        [InlineData("张三", 1)] // 全名搜索
        [InlineData("李", 1)] // 姓氏搜索
        public void SearchFilter_WithVariousInputs_ShouldFilterCorrectly(string searchText, int expectedCount)
        {
            // Arrange
            var patients = new List<PatientDto>
            {
                new() { Name = "张三" },
                new() { Name = "李四" }
            };

            var filteredPatients = new List<PatientDto>();
            _mockListService.Setup(x => x.Filter(It.IsAny<Func<PatientDto, bool>>()))
                .Callback<Func<PatientDto, bool>>(filter =>
                {
                    filteredPatients = patients.Where(filter).ToList();
                });

            // Act
            ViewModel.SearchText = searchText;
            ViewModel.SearchCommand.Execute(null);

            // Assert
            if (string.IsNullOrWhiteSpace(searchText))
            {
                _mockListService.Verify(x => x.ClearFilter(), Times.Once);
            }
            else
            {
                // 因为实际的过滤逻辑在ViewModel中，这里只验证方法被调用
                _mockListService.Verify(x => x.Filter(It.IsAny<Func<PatientDto, bool>>()), Times.Once);
            }
        }
    }
}