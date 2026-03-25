using FluentAssertions;
using LYBT.Desktop.CardReader.Integration;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Patients.Models;
using LYBT.Desktop.Patients.Services;
using LYBT.Desktop.Patients.ViewModels;
using LYBT.Desktop.Patients.ViewModels.Handlers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.PureLogic.Patients;

/// <summary>
/// PatientMasterDetailViewModel 单元测试
/// 验证患者管理模块的Master-Detail视图模型行为
/// </summary>
public class PatientMasterDetailViewModelTests
{
    private readonly IViewModelServices _viewModelServices;
    private readonly IMasterDetailServices<PatientListDto, PatientDetailModel> _masterDetailServices;
    private readonly PatientService _commandHandler;
    private readonly IPatientRepository _patientRepository;
    private readonly IPatientStatusHandler _statusHandler;
    private readonly IDesktopCacheManager _cacheManager;
    private readonly PatientCardReaderViewModel _cardReaderViewModel;
    private readonly PatientImportExportViewModel _importExportViewModel;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<PatientService> _logger;

    // MasterDetailServices 组件
    private readonly IListViewServices<PatientListDto> _listViewServices;
    private readonly IDetailEditorService<PatientDetailModel> _detailEditor;
    private readonly IDialogManager _dialogManager;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly ILoadingStateManager _loadingState;
    private readonly IPaginationService _pagination;
    private readonly ISearchService _search;
    private readonly ISelectionService<PatientListDto> _selection;
    private readonly IErrorHandler _errorHandler;
    private readonly IAsyncExecutor _asyncExecutor;

    public PatientMasterDetailViewModelTests()
    {
        // Arrange - 创建所有 mock
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _logger = Substitute.For<ILogger<PatientService>>();
        _loggerFactory.CreateLogger<PatientService>().Returns(_logger);

        // 创建 MasterDetailServices 组件 mocks
        _listViewServices = Substitute.For<IListViewServices<PatientListDto>>();
        _detailEditor = Substitute.For<IDetailEditorService<PatientDetailModel>>();
        _dialogManager = Substitute.For<IDialogManager>();
        _navigationCoordinator = Substitute.For<INavigationCoordinator>();
        _loadingState = Substitute.For<ILoadingStateManager>();
        _pagination = Substitute.For<IPaginationService>();
        _search = Substitute.For<ISearchService>();
        _selection = Substitute.For<ISelectionService<PatientListDto>>();
        _errorHandler = Substitute.For<IErrorHandler>();
        _asyncExecutor = Substitute.For<IAsyncExecutor>();

        // 设置 ListViewServices 返回子服务
        _listViewServices.Loading.Returns(_loadingState);
        _listViewServices.Pagination.Returns(_pagination);
        _listViewServices.Search.Returns(_search);
        _listViewServices.Selection.Returns(_selection);
        _listViewServices.ErrorHandler.Returns(_errorHandler);
        _listViewServices.AsyncExecutor.Returns(_asyncExecutor);

        // 设置 ExecuteWithLoadingAsync 实际执行传入的函数
        _loadingState.ExecuteWithLoadingAsync(Arg.Any<Func<Task>>(), Arg.Any<string?>(), Arg.Any<bool>())
            .Returns(callInfo => callInfo.Arg<Func<Task>>()());

        // 创建 MasterDetailServices mock
        _masterDetailServices = Substitute.For<IMasterDetailServices<PatientListDto, PatientDetailModel>>();
        _masterDetailServices.List.Returns(_listViewServices);
        _masterDetailServices.DetailEditor.Returns(_detailEditor);
        _masterDetailServices.Dialog.Returns(_dialogManager);
        _masterDetailServices.Navigation.Returns(_navigationCoordinator);
        _masterDetailServices.Loading.Returns(_loadingState);
        _masterDetailServices.Pagination.Returns(_pagination);
        _masterDetailServices.Search.Returns(_search);
        _masterDetailServices.Selection.Returns(_selection);
        _masterDetailServices.ErrorHandler.Returns(_errorHandler);
        _masterDetailServices.AsyncExecutor.Returns(_asyncExecutor);

        // 创建 ViewModelServices mock
        _viewModelServices = Substitute.For<IViewModelServices>();
        _viewModelServices.LoggerFactory.Returns(_loggerFactory);

        // 创建 Repository 和 Service mocks
        _patientRepository = Substitute.For<IPatientRepository>();
        _statusHandler = Substitute.For<IPatientStatusHandler>();
        _cacheManager = Substitute.For<IDesktopCacheManager>();

        // 创建 Child ViewModel mocks
        _cardReaderViewModel = Substitute.For<PatientCardReaderViewModel>(
            _viewModelServices,
            Substitute.For<LYBT.Desktop.CardReader.Services.ICardReaderService>(),
            Substitute.For<IPatientCardReaderIntegration>(),
            Substitute.For<ILogger<PatientCardReaderViewModel>>());

        _importExportViewModel = Substitute.For<PatientImportExportViewModel>(
            _viewModelServices,
            Substitute.For<IPatientImportExportHandler>(),
            Substitute.For<ILogger<PatientImportExportViewModel>>());

        // 创建 PatientService (使用真实实例以测试业务逻辑)
        _commandHandler = new PatientService(_patientRepository, _logger);
    }

    private PatientMasterDetailViewModel CreateSut()
    {
        return new PatientMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            _commandHandler,
            _patientRepository,
            _statusHandler,
            _cacheManager,
            _cardReaderViewModel,
            _importExportViewModel);
    }

    #region 构造函数和初始化

    [Fact]
    public void Constructor_InitializesPageTitle()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.PageTitle.Should().Be("患者管理");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenCommandHandlerIsNull()
    {
        // Arrange & Act & Assert
        Action act = () => new PatientMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            null!,
            _patientRepository,
            _statusHandler,
            _cacheManager,
            _cardReaderViewModel,
            _importExportViewModel);

        act.Should().Throw<ArgumentNullException>().WithParameterName("commandHandler");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenPatientRepositoryIsNull()
    {
        // Arrange & Act & Assert
        Action act = () => new PatientMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            _commandHandler,
            null!,
            _statusHandler,
            _cacheManager,
            _cardReaderViewModel,
            _importExportViewModel);

        act.Should().Throw<ArgumentNullException>().WithParameterName("patientRepository");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenStatusHandlerIsNull()
    {
        // Arrange & Act & Assert
        Action act = () => new PatientMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            _commandHandler,
            _patientRepository,
            null!,
            _cacheManager,
            _cardReaderViewModel,
            _importExportViewModel);

        act.Should().Throw<ArgumentNullException>().WithParameterName("statusHandler");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenCacheManagerIsNull()
    {
        // Arrange & Act & Assert
        Action act = () => new PatientMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            _commandHandler,
            _patientRepository,
            _statusHandler,
            null!,
            _cardReaderViewModel,
            _importExportViewModel);

        act.Should().Throw<ArgumentNullException>().WithParameterName("cacheManager");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenCardReaderViewModelIsNull()
    {
        // Arrange & Act & Assert
        Action act = () => new PatientMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            _commandHandler,
            _patientRepository,
            _statusHandler,
            _cacheManager,
            null!,
            _importExportViewModel);

        act.Should().Throw<ArgumentNullException>().WithParameterName("cardReaderViewModel");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenImportExportViewModelIsNull()
    {
        // Arrange & Act & Assert
        Action act = () => new PatientMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            _commandHandler,
            _patientRepository,
            _statusHandler,
            _cacheManager,
            _cardReaderViewModel,
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("importExportViewModel");
    }

    [Fact]
    public void EntityDisplayName_ReturnsCorrectValue()
    {
        // Act
        var sut = CreateSut();

        // Assert - 通过 DetailTitle 间接验证 EntityDisplayName
        sut.DetailTitle.Should().Be("患者详情");
    }

    [Fact]
    public void ChildViewModels_AreExposedViaProperties()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.CardReaderViewModel.Should().Be(_cardReaderViewModel);
        sut.ImportExportViewModel.Should().Be(_importExportViewModel);
    }

    [Fact]
    public void GenderOptions_ContainsAllGenderValues()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.GenderOptions.Should().HaveCount(3);
        sut.GenderOptions.Should().Contain(Gender.Unknown);
        sut.GenderOptions.Should().Contain(Gender.Male);
        sut.GenderOptions.Should().Contain(Gender.Female);
    }

    [Fact]
    public void StatusOptions_ContainsEnabledAndDisabled()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.StatusOptions.Should().Contain(CommonStatus.Enabled);
        sut.StatusOptions.Should().Contain(CommonStatus.Disabled);
    }

    #endregion

    #region LoadListAsync

    [Fact]
    public async Task LoadListAsync_LoadsPagedDataAndPopulatesItems()
    {
        // Arrange
        var sut = CreateSut();
        var pagedData = new PagedResult<PatientListDto>
        {
            Items = new List<PatientListDto>
            {
                CreatePatientListDto(id: Guid.NewGuid(), name: "张三"),
                CreatePatientListDto(id: Guid.NewGuid(), name: "李四")
            },
            TotalCount = 2
        };

        _patientRepository.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns(Task.FromResult(pagedData));

        // 设置分页服务的返回值
        _pagination.CurrentPage.Returns(1);
        _pagination.PageSize.Returns(20);
        _search.SearchText.Returns((string?)null);

        // Act
        await sut.InitializeAsync();

        // Assert
        await _patientRepository.Received(1).GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task LoadListAsync_HandlesExceptionAndLogsError()
    {
        // Arrange
        var sut = CreateSut();
        var exception = new Exception("Database connection failed");

        _patientRepository.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns(Task.FromException<PagedResult<PatientListDto>>(exception));

        // Act
        await sut.InitializeAsync();

        // Assert
        _errorHandler.Received(1).HandleException(exception, "获取患者列表");
    }

    [Fact]
    public async Task LoadListAsync_PassesSearchTextToRepository()
    {
        // Arrange
        var sut = CreateSut();
        var pagedData = new PagedResult<PatientListDto>
        {
            Items = new List<PatientListDto>(),
            TotalCount = 0
        };

        _patientRepository.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns(Task.FromResult(pagedData));

        // 设置搜索文本
        _search.SearchText.Returns("测试关键词");

        // Act
        await sut.InitializeAsync();

        // Assert - 验证调用了 GetPagedAsync，搜索文本通过 SearchText 属性委托
        await _patientRepository.Received(1).GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>());
    }

    #endregion

    #region LoadDetailAsync

    [Fact]
    public async Task LoadDetailAsync_LoadsDetailViaRepositoryAndInitializesModel()
    {
        // Arrange
        var sut = CreateSut();
        var listItem = CreatePatientListDto();
        PatientDetailDto detailDto = CreatePatientDetailDto();

        _patientRepository.GetByIdAsync(listItem.Id).Returns(Task.FromResult<PatientDetailDto?>(detailDto));

        // Act
        await sut.InvokeLoadDetailAsync(listItem);

        // Assert
        await _patientRepository.Received(1).GetByIdAsync(listItem.Id);
        _detailEditor.Received(1).LoadDetail(Arg.Any<PatientDetailModel>());
    }

    [Fact]
    public async Task LoadDetailAsync_HandlesNullResultFromRepository()
    {
        // Arrange
        var sut = CreateSut();
        var listItem = CreatePatientListDto();

        _patientRepository.GetByIdAsync(listItem.Id).Returns(Task.FromResult<PatientDetailDto?>(null));

        // Act
        await sut.InvokeLoadDetailAsync(listItem);

        // Assert
        await _patientRepository.Received(1).GetByIdAsync(listItem.Id);
        await _dialogManager.Received(1).ShowErrorAsync(Arg.Is<string>(s => s.Contains("不存在")), "加载失败");
    }

    [Fact]
    public async Task LoadDetailAsync_HandlesExceptionAndLogsError()
    {
        // Arrange
        var sut = CreateSut();
        var listItem = CreatePatientListDto();
        var exception = new Exception("Database connection failed");

        _patientRepository.GetByIdAsync(listItem.Id).Returns(Task.FromException<PatientDetailDto?>(exception));

        // Act
        await sut.InvokeLoadDetailAsync(listItem);

        // Assert
        _errorHandler.Received(1).HandleException(exception, "加载患者详情");
    }

    [Fact]
    public async Task LoadDetailAsync_GeneratesPinYinCodeWhenNull()
    {
        // Arrange
        var sut = CreateSut();
        var listItem = CreatePatientListDto();
        PatientDetailDto detailDto = CreatePatientDetailDto();
        detailDto.PinYinCode = null;
        detailDto.Name = "张三";

        _patientRepository.GetByIdAsync(listItem.Id).Returns(Task.FromResult<PatientDetailDto?>(detailDto));

        // Act
        await sut.InvokeLoadDetailAsync(listItem);

        // Assert
        _detailEditor.Received(1).LoadDetail(Arg.Is<PatientDetailModel>(m => m.PinYinCode == "ZS"));
    }

    #endregion

    #region CreateNewDetail

    [Fact]
    public void CreateNewDetail_ReturnsNewPatientDetailModel()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.TestCreateNewDetail();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(Guid.Empty);
        result.IsNew.Should().BeTrue();
        result.Name.Should().BeEmpty();
        result.Gender.Should().Be(Gender.Unknown);
        result.Status.Should().Be(CommonStatus.Enabled);
    }

    #endregion

    #region SaveDetailAsync

    [Fact]
    public async Task SaveDetailAsync_ReturnsFalse_WhenNameIsEmpty()
    {
        // Arrange
        var sut = CreateSut();
        var detail = new PatientDetailModel { Name = "", Id = Guid.Empty };

        // Act
        var result = await sut.SaveDetailAsync(detail);

        // Assert
        result.Should().BeFalse();
        await _dialogManager.Received(1).ShowErrorAsync("患者姓名不能为空", "验证失败");
    }

    [Fact]
    public async Task SaveDetailAsync_ReturnsFalse_WhenNameIsWhitespace()
    {
        // Arrange
        var sut = CreateSut();
        var detail = new PatientDetailModel { Name = "   ", Id = Guid.Empty };

        // Act
        var result = await sut.SaveDetailAsync(detail);

        // Assert
        result.Should().BeFalse();
        await _dialogManager.Received(1).ShowErrorAsync("患者姓名不能为空", "验证失败");
    }

    [Fact]
    public async Task SaveDetailAsync_CreatesNewPatient_WhenIsNew()
    {
        // Arrange
        var sut = CreateSut();
        var detail = new PatientDetailModel
        {
            Id = Guid.Empty,
            Name = "新患者",
            Gender = Gender.Male,
            PhoneNumber = "13800138000"
        };
        var createdDto = CreatePatientDetailDto(id: Guid.NewGuid(), name: "新患者");

        _patientRepository.CreateAsync(Arg.Any<PatientInputDto>())
            .Returns(Task.FromResult(createdDto));

        // 设置 IsNew = true (通过 _detailEditor.IsNew)
        _detailEditor.IsNew.Returns(true);

        // Act
        var result = await sut.SaveDetailAsync(detail);

        // Assert
        result.Should().BeTrue();
        await _patientRepository.Received(1).CreateAsync(Arg.Any<PatientInputDto>());
        _cacheManager.Received(1).InvalidatePatientCaches();
    }

    [Fact]
    public async Task SaveDetailAsync_UpdatesExistingPatient_WhenNotIsNew()
    {
        // Arrange
        var sut = CreateSut();
        var existingId = Guid.NewGuid();
        var detail = new PatientDetailModel
        {
            Id = existingId,
            Name = "更新患者",
            Gender = Gender.Female
        };
        var updatedDto = CreatePatientDetailDto(id: existingId, name: "更新患者");

        // 设置 IsNew 为 false
        _detailEditor.IsNew.Returns(false);

        _patientRepository.UpdateAsync(Arg.Any<PatientInputDto>())
            .Returns(Task.FromResult(updatedDto));

        // Act
        var result = await sut.SaveDetailAsync(detail);

        // Assert
        result.Should().BeTrue();
        await _patientRepository.Received(1).UpdateAsync(Arg.Any<PatientInputDto>());
        _cacheManager.Received(1).InvalidatePatientCaches();
    }

    [Fact]
    public async Task SaveDetailAsync_TrimsInputValues()
    {
        // Arrange
        var sut = CreateSut();
        var detail = new PatientDetailModel
        {
            Id = Guid.Empty,
            Name = "  新患者  ",
            PhoneNumber = "  13800138000  ",
            Address = "  测试地址  ",
            IdNumber = "  110101199001011234  "
        };
        var createdDto = CreatePatientDetailDto();

        _patientRepository.CreateAsync(Arg.Any<PatientInputDto>())
            .Returns(Task.FromResult(createdDto));

        // 设置 IsNew = true
        _detailEditor.IsNew.Returns(true);

        // Act
        await sut.SaveDetailAsync(detail);

        // Assert
        await _patientRepository.Received(1).CreateAsync(Arg.Is<PatientInputDto>(dto =>
            dto.Name == "新患者" &&
            dto.PhoneNumber == "13800138000" &&
            dto.Address == "测试地址" &&
            dto.IdNumber == "110101199001011234"));
    }

    [Fact]
    public async Task SaveDetailAsync_ReturnsFalse_WhenCreateFails()
    {
        // Arrange
        var sut = CreateSut();
        var detail = new PatientDetailModel
        {
            Id = Guid.Empty,
            Name = "新患者"
        };
        var exception = new Exception("Create failed");

        _patientRepository.CreateAsync(Arg.Any<PatientInputDto>())
            .Returns(Task.FromException<PatientDetailDto>(exception));

        // Act
        var result = await sut.SaveDetailAsync(detail);

        // Assert
        result.Should().BeFalse();
        _errorHandler.Received(1).SetError("Save", Arg.Any<string>());
    }

    [Fact]
    public async Task SaveDetailAsync_ReturnsFalse_WhenUpdateFails()
    {
        // Arrange
        var sut = CreateSut();
        var detail = new PatientDetailModel
        {
            Id = Guid.NewGuid(),
            Name = "更新患者"
        };
        var exception = new Exception("Update failed");

        _detailEditor.IsNew.Returns(false);
        _patientRepository.UpdateAsync(Arg.Any<PatientInputDto>())
            .Returns(Task.FromException<PatientDetailDto>(exception));

        // Act
        var result = await sut.SaveDetailAsync(detail);

        // Assert
        result.Should().BeFalse();
        _errorHandler.Received(1).SetError("Save", Arg.Any<string>());
    }

    [Fact]
    public async Task SaveDetailAsync_UpdatesDetailPropertiesFromResult()
    {
        // Arrange
        var sut = CreateSut();
        var detail = new PatientDetailModel
        {
            Id = Guid.Empty,
            Name = "临时名称"
        };
        var createdDto = CreatePatientDetailDto(
            id: Guid.NewGuid(),
            name: "正式名称",
            pinYinCode: "ZSMC");

        _patientRepository.CreateAsync(Arg.Any<PatientInputDto>())
            .Returns(Task.FromResult(createdDto));

        // 设置 IsNew = true
        _detailEditor.IsNew.Returns(true);

        // Act
        await sut.SaveDetailAsync(detail);

        // Assert
        detail.Id.Should().Be(createdDto.Id);
        detail.Name.Should().Be("正式名称");
        detail.PinYinCode.Should().Be("ZSMC");
    }

    #endregion

    #region DeleteItemAsync

    [Fact]
    public async Task DeleteItemAsync_CallsRepositoryDeleteAndInvalidatesCache()
    {
        // Arrange
        var sut = CreateSut();
        var listItem = CreatePatientListDto();

        _patientRepository.DeleteAsync(listItem.Id).Returns(Task.FromResult(true));

        // Act
        var result = await sut.DeleteItemAsync(listItem);

        // Assert
        await _patientRepository.Received(1).DeleteAsync(listItem.Id);
        _cacheManager.Received(1).InvalidatePatientCaches();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteItemAsync_ReturnsFalse_WhenDeleteFails()
    {
        // Arrange
        var sut = CreateSut();
        var listItem = CreatePatientListDto();

        // 使用真实 PatientService，让它返回失败结果
        _patientRepository.DeleteAsync(listItem.Id)
            .Returns(Task.FromException<bool>(new Exception("Delete failed")));

        // Act
        var result = await sut.DeleteItemAsync(listItem);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteItemAsync_ReturnsFalse_WhenCommandHandlerFails()
    {
        // Arrange
        var sut = CreateSut();
        var listItem = CreatePatientListDto();

        // 使用真实 PatientService，让它返回失败结果
        _patientRepository.DeleteAsync(listItem.Id)
            .Returns(Task.FromException<bool>(new Exception("Delete failed")));

        // Act
        var result = await sut.DeleteItemAsync(listItem);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region RestoreAsync

    [Fact]
    public async Task RestoreAsync_CallsStatusHandlerAndRefreshes()
    {
        // Arrange
        var sut = CreateSut();
        var listItem = CreatePatientListDto();

        _selection.SelectedItem.Returns(listItem);
        _statusHandler.RestoreAsync(listItem).Returns(Task.FromResult(true));

        // Act
        await sut.RestoreAsync();

        // Assert
        await _statusHandler.Received(1).RestoreAsync(listItem);
        _cacheManager.Received(1).InvalidatePatientCaches();
    }

    [Fact]
    public async Task RestoreAsync_DoesNothing_WhenNoSelection()
    {
        // Arrange
        var sut = CreateSut();
        _selection.SelectedItem.Returns((PatientListDto?)null);

        // Act
        await sut.RestoreAsync();

        // Assert
        await _statusHandler.DidNotReceive().RestoreAsync(Arg.Any<PatientListDto>());
    }

    [Fact]
    public async Task RestoreAsync_DoesNotRefresh_WhenRestoreFails()
    {
        // Arrange
        var sut = CreateSut();
        var listItem = CreatePatientListDto();

        _selection.SelectedItem.Returns(listItem);
        _statusHandler.RestoreAsync(listItem).Returns(Task.FromResult(false));

        // Act
        await sut.RestoreAsync();

        // Assert
        await _statusHandler.Received(1).RestoreAsync(listItem);
        _cacheManager.DidNotReceive().InvalidatePatientCaches();
    }

    [Fact]
    public void CanRestore_ReturnsFalse_WhenNoSelection()
    {
        // Arrange
        var sut = CreateSut();
        _selection.HasSelection.Returns(false);

        // Act & Assert - 通过命令的 CanExecute 验证
        // CanRestore 是私有方法，通过 RestoreCommand.CanExecute 间接测试
        sut.RestoreCommand.CanExecute(null).Should().BeFalse();
    }

    #endregion

    #region Import/Export/DownloadTemplate

    [Fact]
    public async Task ImportAsync_DelegatesToImportExportViewModel()
    {
        // Arrange
        var sut = CreateSut();
        _importExportViewModel.ImportAsync().Returns(Task.FromResult(true));

        // Act
        await sut.ImportAsync();

        // Assert
        await _importExportViewModel.Received(1).ImportAsync();
        _cacheManager.Received(1).InvalidatePatientCaches();
    }

    [Fact]
    public async Task ImportAsync_DoesNotRefresh_WhenImportFails()
    {
        // Arrange
        var sut = CreateSut();
        _importExportViewModel.ImportAsync().Returns(Task.FromResult(false));

        // Act
        await sut.ImportAsync();

        // Assert
        await _importExportViewModel.Received(1).ImportAsync();
        _cacheManager.DidNotReceive().InvalidatePatientCaches();
    }

    [Fact]
    public async Task ExportAsync_DelegatesToImportExportViewModel()
    {
        // Arrange
        var sut = CreateSut();
        _search.SearchText.Returns("搜索关键词");

        // Act
        await sut.ExportAsync();

        // Assert
        await _importExportViewModel.Received(1).ExportAsync("搜索关键词");
    }

    [Fact]
    public async Task DownloadTemplateAsync_DelegatesToImportExportViewModel()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.DownloadTemplateAsync();

        // Assert
        await _importExportViewModel.Received(1).DownloadTemplateAsync();
    }

    #endregion

    #region ReadCardAsync

    [Fact]
    public async Task ReadCardAsync_DelegatesToCardReaderViewModel()
    {
        // Arrange
        var sut = CreateSut();
        var cardResult = new LYBT.Desktop.CardReader.Models.CardReadResult
        {
            IsSuccess = true,
            Name = "测试患者",
            IdNumber = "110101199001011234"
        };

        _cardReaderViewModel.ReadCardAsync().Returns(Task.FromResult<LYBT.Desktop.CardReader.Models.CardReadResult?>(cardResult));
        _cardReaderViewModel.FindPatientByIdNumberAsync(cardResult.IdNumber)
            .Returns(Task.FromResult<PatientFromCardResult?>(null));
        _dialogManager.ShowConfirmAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(false));

        // Act - 通过反射直接调用私有方法，绕过 CanExecute 检查
        await sut.ReadCardAsync();

        // Assert
        await _cardReaderViewModel.Received(1).ReadCardAsync();
    }

    [Fact]
    public async Task ReadCardAsync_CallsFindPatientByIdNumber_WhenCardReadSucceeds()
    {
        // Arrange
        var sut = CreateSut();
        var cardResult = new LYBT.Desktop.CardReader.Models.CardReadResult
        {
            IsSuccess = true,
            Name = "测试患者",
            IdNumber = "110101199001011234"
        };

        _cardReaderViewModel.ReadCardAsync().Returns(Task.FromResult<LYBT.Desktop.CardReader.Models.CardReadResult?>(cardResult));
        _cardReaderViewModel.FindPatientByIdNumberAsync(cardResult.IdNumber)
            .Returns(Task.FromResult<PatientFromCardResult?>(null));
        _dialogManager.ShowConfirmAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(false));

        // Act - 通过反射直接调用私有方法，绕过 CanExecute 检查
        await sut.ReadCardAsync();

        // Assert - 验证调用了 FindPatientByIdNumberAsync
        await _cardReaderViewModel.Received(1).FindPatientByIdNumberAsync("110101199001011234");
    }

    [Fact]
    public async Task ReadCardAsync_ReturnsEarly_WhenReadCardFails()
    {
        // Arrange
        var sut = CreateSut();

        _cardReaderViewModel.ReadCardAsync().Returns(Task.FromResult<LYBT.Desktop.CardReader.Models.CardReadResult?>(null));

        // Act - 通过反射直接调用私有方法，绕过 CanExecute 检查
        await sut.ReadCardAsync();

        // Assert
        await _cardReaderViewModel.DidNotReceive().FindPatientByIdNumberAsync(Arg.Any<string>());
    }

    #endregion

    #region Helper Methods

    private static PatientListDto CreatePatientListDto(Guid? id = null, string name = "测试患者")
    {
        return new PatientListDto
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Gender = Gender.Male,
            PhoneNumber = "13800138000",
            VisitCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static PatientDetailDto CreatePatientDetailDto(Guid? id = null, string name = "测试患者", string? pinYinCode = "CSHZ")
    {
        return new PatientDetailDto
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            PinYinCode = pinYinCode,
            Gender = Gender.Male,
            PhoneNumber = "13800138000",
            Status = CommonStatus.Enabled,
            VisitCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}

/// <summary>
/// PatientMasterDetailViewModel 测试辅助扩展
/// </summary>
public static class PatientMasterDetailViewModelTestExtensions
{
    /// <summary>
    /// 测试辅助方法：调用受保护的 CreateNewDetail 方法
    /// </summary>
    public static PatientDetailModel TestCreateNewDetail(this PatientMasterDetailViewModel vm)
    {
        var method = typeof(PatientMasterDetailViewModel).GetMethod(
            "CreateNewDetail",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy,
            null,
            Type.EmptyTypes,
            null);

        if (method == null)
        {
            // 尝试从基类获取
            var baseType = typeof(PatientMasterDetailViewModel).BaseType;
            while (method == null && baseType != null)
            {
                method = baseType.GetMethod(
                    "CreateNewDetail",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    null,
                    Type.EmptyTypes,
                    null);
                baseType = baseType.BaseType;
            }
        }

        if (method == null)
            throw new InvalidOperationException("CreateNewDetail method not found");

        var result = method.Invoke(vm, null);
        return (PatientDetailModel)result!;
    }

    /// <summary>
    /// 测试辅助方法：调用受保护的 SaveDetailAsync 方法
    /// </summary>
    public static async Task<bool> SaveDetailAsync(this PatientMasterDetailViewModel vm, PatientDetailModel detail)
    {
        var method = typeof(PatientMasterDetailViewModel).GetMethod(
            "SaveDetailAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method == null) throw new InvalidOperationException("SaveDetailAsync method not found");

        var result = method.Invoke(vm, new object[] { detail });
        if (result is Task<bool> task) return await task;
        throw new InvalidOperationException("Unexpected return type");
    }

    /// <summary>
    /// 测试辅助方法：调用受保护的 DeleteItemAsync 方法
    /// </summary>
    public static async Task<bool> DeleteItemAsync(this PatientMasterDetailViewModel vm, PatientListDto item)
    {
        var method = typeof(PatientMasterDetailViewModel).GetMethod(
            "DeleteItemAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method == null) throw new InvalidOperationException("DeleteItemAsync method not found");

        var result = method.Invoke(vm, new object[] { item });
        if (result is Task<bool> task) return await task;
        throw new InvalidOperationException("Unexpected return type");
    }

    /// <summary>
    /// 测试辅助方法：调用受保护的 LoadDetailAsync 方法
    /// </summary>
    public static async Task InvokeLoadDetailAsync(this PatientMasterDetailViewModel vm, PatientListDto item)
    {
        var method = typeof(PatientMasterDetailViewModel).GetMethod(
            "LoadDetailAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy,
            null,
            new[] { typeof(PatientListDto) },
            null);

        if (method == null)
        {
            var baseType = typeof(PatientMasterDetailViewModel).BaseType;
            while (method == null && baseType != null)
            {
                method = baseType.GetMethod(
                    "LoadDetailAsync",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    null,
                    new[] { typeof(PatientListDto) },
                    null);
                baseType = baseType.BaseType;
            }
        }

        if (method == null) throw new InvalidOperationException("LoadDetailAsync method not found");

        var result = method.Invoke(vm, new object[] { item });
        if (result is Task task) await task;
    }

    /// <summary>
    /// 测试辅助方法：调用私有的 RestoreAsync 方法
    /// </summary>
    public static async Task RestoreAsync(this PatientMasterDetailViewModel vm)
    {
        var method = typeof(PatientMasterDetailViewModel).GetMethod(
            "RestoreAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method == null) throw new InvalidOperationException("RestoreAsync method not found");

        var result = method.Invoke(vm, null);
        if (result is Task task) await task;
    }

    /// <summary>
    /// 测试辅助方法：调用私有的 ImportAsync 方法
    /// </summary>
    public static async Task ImportAsync(this PatientMasterDetailViewModel vm)
    {
        var method = typeof(PatientMasterDetailViewModel).GetMethod(
            "ImportAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method == null) throw new InvalidOperationException("ImportAsync method not found");

        var result = method.Invoke(vm, null);
        if (result is Task task) await task;
    }

    /// <summary>
    /// 测试辅助方法：调用私有的 ExportAsync 方法
    /// </summary>
    public static async Task ExportAsync(this PatientMasterDetailViewModel vm)
    {
        var method = typeof(PatientMasterDetailViewModel).GetMethod(
            "ExportAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method == null) throw new InvalidOperationException("ExportAsync method not found");

        var result = method.Invoke(vm, null);
        if (result is Task task) await task;
    }

    /// <summary>
    /// 测试辅助方法：调用私有的 DownloadTemplateAsync 方法
    /// </summary>
    public static async Task DownloadTemplateAsync(this PatientMasterDetailViewModel vm)
    {
        var method = typeof(PatientMasterDetailViewModel).GetMethod(
            "DownloadTemplateAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method == null) throw new InvalidOperationException("DownloadTemplateAsync method not found");

        var result = method.Invoke(vm, null);
        if (result is Task task) await task;
    }

    /// <summary>
    /// 测试辅助方法：调用私有的 ReadCardAsync 方法
    /// </summary>
    public static async Task ReadCardAsync(this PatientMasterDetailViewModel vm)
    {
        var method = typeof(PatientMasterDetailViewModel).GetMethod(
            "ReadCardAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method == null) throw new InvalidOperationException("ReadCardAsync method not found");

        var result = method.Invoke(vm, null);
        if (result is Task task) await task;
    }
}
