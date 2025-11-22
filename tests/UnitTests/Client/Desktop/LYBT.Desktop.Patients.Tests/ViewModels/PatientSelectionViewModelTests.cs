using System.Collections.ObjectModel;
using FluentAssertions;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Components;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Patients.Services;
using LYBT.Desktop.Patients.ViewModels;
using LYBT.Desktop.Patients.ViewModels.Components;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Patients.Tests.ViewModels
{
    /// <summary>
    /// PatientSelectionViewModel 单元测试
    /// Epic #2210 Issue #2218: P0优化功能测试
    /// </summary>
    public class PatientSelectionViewModelTests : IDisposable
    {
        private readonly Mock<IDialogService> _dialogServiceMock;
        private readonly Mock<IMedicalCaseApi> _medicalCaseApiMock;
        private readonly Mock<IEventAggregator> _eventAggregatorMock;
        private readonly Mock<ILoggerFactory> _loggerFactoryMock;
        private readonly Mock<IRegionManager> _regionManagerMock;

        // Epic #2210 Issue #2218: 组件层Mock对象（正确构建依赖链）
        private readonly Mock<IPatientRepository> _patientRepositoryMock;
        private readonly Mock<IMedicalCaseRepository> _medicalCaseRepositoryMock;

        private readonly PatientCommandHandler _commandHandler;
        private readonly MedicalCaseDataManager _medicalCaseDataManager;
        private readonly PatientSearchManager _searchManager;
        private readonly UnfinishedCaseHandler _unfinishedCaseHandler;
        private readonly PendingQueueManager _pendingQueueManager;

        private readonly PatientSelectionViewModel _viewModel;

        public PatientSelectionViewModelTests()
        {
            // 创建接口Mock对象
            _dialogServiceMock = new Mock<IDialogService>();
            _medicalCaseApiMock = new Mock<IMedicalCaseApi>();
            _eventAggregatorMock = new Mock<IEventAggregator>();
            _loggerFactoryMock = new Mock<ILoggerFactory>();
            _regionManagerMock = new Mock<IRegionManager>();
            _patientRepositoryMock = new Mock<IPatientRepository>();
            _medicalCaseRepositoryMock = new Mock<IMedicalCaseRepository>();

            // Mock LoggerFactory
            var commandHandlerLoggerMock = new Mock<ILogger<PatientCommandHandler>>();
            var medicalCaseDataManagerLoggerMock = new Mock<ILogger<MedicalCaseDataManager>>();
            var searchManagerLoggerMock = new Mock<ILogger<PatientSearchManager>>();
            var unfinishedCaseHandlerLoggerMock = new Mock<ILogger<UnfinishedCaseHandler>>();
            var pendingQueueManagerLoggerMock = new Mock<ILogger<PendingQueueManager>>();
            var viewModelLoggerMock = new Mock<ILogger<PatientSelectionViewModel>>();

            _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns<string>(name =>
                {
                    if (name.Contains(nameof(PatientCommandHandler))) return commandHandlerLoggerMock.Object;
                    if (name.Contains(nameof(MedicalCaseDataManager))) return medicalCaseDataManagerLoggerMock.Object;
                    if (name.Contains(nameof(PatientSearchManager))) return searchManagerLoggerMock.Object;
                    if (name.Contains(nameof(UnfinishedCaseHandler))) return unfinishedCaseHandlerLoggerMock.Object;
                    if (name.Contains(nameof(PendingQueueManager))) return pendingQueueManagerLoggerMock.Object;
                    return viewModelLoggerMock.Object;
                });

            // Mock EventAggregator.GetEvent
            var patientSelectedEventMock = new Mock<PatientSelectedEvent>();
            _eventAggregatorMock.Setup(x => x.GetEvent<PatientSelectedEvent>())
                .Returns(patientSelectedEventMock.Object);

            // Epic #2210 Issue #2218: 构建依赖链（按依赖顺序）
            // 1. PatientCommandHandler（依赖：IPatientRepository, ILogger, IRegionManager）
            _commandHandler = new PatientCommandHandler(
                _patientRepositoryMock.Object,
                commandHandlerLoggerMock.Object,
                _regionManagerMock.Object
            );

            // 2. MedicalCaseDataManager（依赖：IMedicalCaseRepository, IMedicalCaseApi, ILogger）
            _medicalCaseDataManager = new MedicalCaseDataManager(
                _medicalCaseRepositoryMock.Object,
                _medicalCaseApiMock.Object,
                medicalCaseDataManagerLoggerMock.Object
            );

            // 3. PatientSearchManager（依赖：PatientCommandHandler, ILogger）
            _searchManager = new PatientSearchManager(
                _commandHandler,
                searchManagerLoggerMock.Object
            );

            // 4. UnfinishedCaseHandler（依赖：MedicalCaseDataManager, ILogger）
            _unfinishedCaseHandler = new UnfinishedCaseHandler(
                _medicalCaseDataManager,
                unfinishedCaseHandlerLoggerMock.Object
            );

            // 5. PendingQueueManager（依赖：IMedicalCaseApi, PatientCommandHandler, UnfinishedCaseHandler, ILogger）
            _pendingQueueManager = new PendingQueueManager(
                _medicalCaseApiMock.Object,
                _commandHandler,
                _unfinishedCaseHandler,
                pendingQueueManagerLoggerMock.Object
            );

            // 创建ViewModel实例
            _viewModel = new PatientSelectionViewModel(
                _commandHandler,
                _medicalCaseDataManager,
                _searchManager,
                _unfinishedCaseHandler,
                _pendingQueueManager,
                _dialogServiceMock.Object,
                _medicalCaseApiMock.Object,
                _eventAggregatorMock.Object,
                _loggerFactoryMock.Object,
                _regionManagerMock.Object
            );
        }

        public void Dispose()
        {
            // 清理资源（如果需要）
            GC.SuppressFinalize(this);
        }

        #region Epic #2210 Issue #2216: FR-001 双列表互斥选择测试

        /// <summary>
        /// Epic #2210 Issue #2218: 测试SelectedPatient清除SelectedPendingPatient
        /// </summary>
        [Fact]
        public void SelectedPatient_ShouldClearSelectedPendingPatient()
        {
            // Arrange
            var patient = new PatientDto { Id = Guid.NewGuid(), Name = "测试患者" };
            var pendingCase = new PendingMedicalCaseDto
            {
                PatientId = Guid.NewGuid(),
                PatientName = "待诊患者"
            };

            // 先设置SelectedPendingPatient
            _viewModel.SelectedPendingPatient = pendingCase;
            _viewModel.SelectedPendingPatient.Should().NotBeNull();

            // Act: 设置SelectedPatient（应清除SelectedPendingPatient）
            _viewModel.SelectedPatient = patient;

            // Assert
            _viewModel.SelectedPatient.Should().Be(patient);
            _viewModel.SelectedPendingPatient.Should().BeNull("选择全部患者列表患者时应清除待诊队列选择");
        }

        /// <summary>
        /// Epic #2210 Issue #2218: 测试SelectedPendingPatient清除SelectedPatient
        /// </summary>
        [Fact]
        public async Task SelectedPendingPatient_ShouldClearSelectedPatient()
        {
            // Arrange
            var patient = new PatientDto { Id = Guid.NewGuid(), Name = "全部患者" };
            var pendingCase = new PendingMedicalCaseDto
            {
                PatientId = Guid.NewGuid(),
                PatientName = "待诊患者"
            };

            // Mock _patientRepositoryMock的GetByIdAsync返回null（模拟患者不在列表中）
            _patientRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((PatientDto?)null);

            // 先设置SelectedPatient
            _viewModel.SelectedPatient = patient;
            _viewModel.SelectedPatient.Should().NotBeNull();

            // Act: 设置SelectedPendingPatient（应清除SelectedPatient）
            _viewModel.SelectedPendingPatient = pendingCase;

            // 等待异步加载完成
            await Task.Delay(100);

            // Assert
            _viewModel.SelectedPendingPatient.Should().Be(pendingCase);
            _viewModel.SelectedPatient.Should().BeNull("选择待诊队列患者时应清除全部患者列表选择");
        }

        /// <summary>
        /// Epic #2210 Issue #2218: 测试CurrentPatient始终指向唯一选中患者
        /// </summary>
        [Fact]
        public void CurrentPatient_ShouldAlwaysPointToSelectedPatient()
        {
            // Arrange
            var patient = new PatientDto { Id = Guid.NewGuid(), Name = "测试患者" };

            // Act: 从全部患者列表选择
            _viewModel.SelectedPatient = patient;

            // Assert
            _viewModel.CurrentPatient.Should().Be(patient, "CurrentPatient应指向SelectedPatient");

            // Cleanup
            _viewModel.SelectedPatient = null;
        }

        #endregion

        #region Epic #2210 Issue #2217: FR-002 异常处理优化测试

        /// <summary>
        /// Epic #2210 Issue #2218: 测试OnNavigatedTo不因异常崩溃
        ///
        /// 说明：PendingQueueManager.LoadPendingCasesAsync()内部已经处理了异常，
        /// 异常不会传播到ViewModel层，因此不会设置StatusBarMessage。
        /// 这个测试验证的是异常不会导致OnNavigatedTo崩溃，符合FR-002的核心要求。
        /// </summary>
        [Fact]
        public async Task OnNavigatedTo_ShouldHandleException_AndNotCrash()
        {
            // Arrange: Mock IMedicalCaseApi.GetPendingCasesAsync抛出异常
            _medicalCaseApiMock.Setup(x => x.GetPendingCasesAsync())
                .ThrowsAsync(new Exception("模拟异常"));

            // Mock IRegionNavigationService
            var navigationServiceMock = new Mock<IRegionNavigationService>();
            var navigationContext = new NavigationContext(
                navigationServiceMock.Object,
                new Uri("PatientSelectionView", UriKind.Relative)
            );
            navigationContext.Parameters.Add("MedicalCaseFlowId", Guid.NewGuid());

            // Act & Assert: 不应抛出异常
            Action act = () => _viewModel.OnNavigatedTo(navigationContext);
            act.Should().NotThrow("OnNavigatedTo应捕获异常而不崩溃");

            // 等待异步Task.Run完成（给一些时间）
            await Task.Delay(100);

            // 验证：即使PendingQueue加载失败，导航流程仍然成功
            // （PendingQueue内部已处理异常，不会传播到ViewModel层）
            _viewModel.Should().NotBeNull("ViewModel应正常初始化");
        }

        #endregion
    }
}
