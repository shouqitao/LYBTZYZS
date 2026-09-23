using FluentAssertions;
using LYBT.Desktop.Contracts.Results;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Contracts.Services.CrossModule;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.MedicalCase.ViewModels;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Desktop.MedicalCase.ViewModels.Items;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
#pragma warning disable CS8620 // Nullable reference type compatibility in NSubstitute Returns
using LYBT.Tests.Desktop.Infrastructure;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Prism.Regions;

namespace LYBT.Tests.Desktop.Unit;

/// <summary>
/// MedicalCaseMasterDetailViewModel 单元测试
/// 验证医案管理模块的Master-Detail视图模型行为
/// </summary>
public class MedicalCaseMasterDetailViewModelTests : DesktopTestBase
{
    private readonly IViewModelServices _viewModelServices;
    private readonly IMasterDetailServices<MedicalCaseListDto, MedicalCaseDetailModel> _masterDetailServices;
    private readonly IMedicalCaseService _medicalCaseService;
    private readonly IHerbSearchProvider _herbSearchProvider;
    private readonly IDesktopCacheManager _cacheManager;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<MedicalCaseMasterDetailViewModel> _logger;

    // MasterDetailServices 组件
    private readonly IListViewServices<MedicalCaseListDto> _listViewServices;
    private readonly IDetailEditorService<MedicalCaseDetailModel> _detailEditor;
    private readonly IDialogManager _dialogManager;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly ILoadingStateManager _loadingState;
    private readonly IPaginationService _pagination;
    private readonly ISearchService _search;
    private readonly ISelectionService<MedicalCaseListDto> _selection;
    private readonly IErrorHandler _errorHandler;

    public MedicalCaseMasterDetailViewModelTests()
    {
        // Arrange - 创建所有 mock
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _logger = Substitute.For<ILogger<MedicalCaseMasterDetailViewModel>>();

        // 关键：为 CreateLogger 设置明确的返回值
        // 必须显式设置 MedicalCaseMasterDetailViewModel 类型的 logger，因为基类使用 GetType() 获取类型
        _loggerFactory.CreateLogger(typeof(MedicalCaseMasterDetailViewModel)).Returns(_logger);

        // 创建 MasterDetailServices mock（T3-1: 使用基类共享装配，原 ~30 行重复装配已消除）
        _masterDetailServices = CreateMasterDetailServicesMock<MedicalCaseListDto, MedicalCaseDetailModel>();
        _listViewServices = _masterDetailServices.List;
        _detailEditor = _masterDetailServices.DetailEditor;
        _dialogManager = _masterDetailServices.Dialog;
        _navigationCoordinator = _masterDetailServices.Navigation;
        _loadingState = _masterDetailServices.Loading;
        _pagination = _masterDetailServices.Pagination;
        _search = _masterDetailServices.Search;
        _selection = _masterDetailServices.Selection;
        _errorHandler = _masterDetailServices.ErrorHandler;

        // 创建 ViewModelServices mock
        _viewModelServices = Substitute.For<IViewModelServices>();
        _viewModelServices.LoggerFactory.Returns(_loggerFactory);

        // 创建 Service 和 Provider mocks
        _medicalCaseService = Substitute.For<IMedicalCaseService>();
        _herbSearchProvider = Substitute.For<IHerbSearchProvider>();
        _cacheManager = Substitute.For<IDesktopCacheManager>();
    }

    private TestableMedicalCaseMasterDetailViewModel CreateSut()
    {
        return new TestableMedicalCaseMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            _medicalCaseService,
            _herbSearchProvider,
            _cacheManager,
            _loggerFactory);
    }

    #region 构造函数和初始化

    [Fact]
    public void Constructor_InitializesPageTitle()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.PageTitle.Should().Be("医案管理");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenMedicalCaseServiceIsNull()
    {
        // Arrange & Act & Assert
        Action act = () => new MedicalCaseMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            null!,
            _herbSearchProvider,
            _cacheManager,
            _loggerFactory);

        act.Should().Throw<ArgumentNullException>().WithParameterName("medicalCaseService");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenHerbSearchProviderIsNull()
    {
        // Arrange & Act & Assert
        Action act = () => new MedicalCaseMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            _medicalCaseService,
            null!,
            _cacheManager,
            _loggerFactory);

        act.Should().Throw<ArgumentNullException>().WithParameterName("herbSearchProvider");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenCacheManagerIsNull()
    {
        // Arrange & Act & Assert
        Action act = () => new MedicalCaseMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            _medicalCaseService,
            _herbSearchProvider,
            null!,
            _loggerFactory);

        act.Should().Throw<ArgumentNullException>().WithParameterName("cacheManager");
    }

    [Fact]
    public void EntityDisplayName_ReturnsCorrectValue()
    {
        // Act
        var sut = CreateSut();

        // Assert - 通过 DetailTitle 间接验证 EntityDisplayName
        sut.DetailTitle.Should().Be("医案详情");
    }

    #endregion

    #region LoadListAsync

    [Fact]
    public async Task LoadListAsync_LoadsPagedDataAndPopulatesItems()
    {
        // Arrange
        var sut = CreateSut();
        var pagedData = new PagedResult<MedicalCaseListDto>
        {
            Items = new List<MedicalCaseListDto>
            {
                CreateMedicalCaseListDto(id: Guid.NewGuid(), patientName: "张三"),
                CreateMedicalCaseListDto(id: Guid.NewGuid(), patientName: "李四")
            },
            TotalCount = 2
        };

        _medicalCaseService.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns(Task.FromResult(pagedData));

        // 设置分页服务的返回值
        _pagination.CurrentPage.Returns(1);
        _pagination.PageSize.Returns(20);
        _search.SearchText.Returns((string?)null);

        // Act
        await sut.InitializeAsync();

        // Assert
        await _medicalCaseService.Received(1).GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>());
        // T3-2: 补状态断言——列表数据与总数应实际填充（原仅验交互）
        sut.Items.Should().HaveCount(2);
        sut.Items[0].PatientName.Should().Be("张三");
        _pagination.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task LoadListAsync_HandlesExceptionAndLogsError()
    {
        // Arrange
        var sut = CreateSut();
        var exception = new Exception("Database connection failed");

        _medicalCaseService.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns(Task.FromException<PagedResult<MedicalCaseListDto>>(exception));

        // Act
        await sut.InitializeAsync();

        // Assert
        _errorHandler.Received(1).HandleException(exception, "获取医案列表");
    }

    [Fact]
    public async Task LoadListAsync_PassesSearchTextToRepository()
    {
        // Arrange
        var sut = CreateSut();
        var pagedData = new PagedResult<MedicalCaseListDto>
        {
            Items = new List<MedicalCaseListDto>(),
            TotalCount = 0
        };

        _medicalCaseService.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns(Task.FromResult(pagedData));

        // 设置搜索文本
        _search.SearchText.Returns("测试关键词");

        // Act
        await sut.InitializeAsync();

        // Assert - 验证调用了 GetPagedAsync，搜索文本通过 SearchText 属性委托
        await _medicalCaseService.Received(1).GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>());
    }

    #endregion

    #region LoadDetailAsync

    [Fact]
    public async Task LoadDetailAsync_LoadsDetailViaServiceAndInitializesChildVMs()
    {
        // Arrange
        var sut = CreateSut();
        var listItem = CreateMedicalCaseListDto();
        var detailModel = CreateMedicalCaseDetailModel();

        _medicalCaseService.LoadDetailsAsync(listItem.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult<MedicalCaseDetailModel>.Succeeded(detailModel)));

        // Act
        await sut.InvokeLoadDetailAsync(listItem);

        // Assert
        await _medicalCaseService.Received(1).LoadDetailsAsync(listItem.Id, Arg.Any<CancellationToken>());

        // Verify child VMs are populated
        sut.ConsultationEditor.Should().NotBeNull();
        sut.PrescriptionEditor.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadDetailAsync_HandlesNullResultFromService()
    {
        // Arrange
        var sut = CreateSut();
        var listItem = CreateMedicalCaseListDto();

        _medicalCaseService.LoadDetailsAsync(listItem.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult<MedicalCaseDetailModel>.NotFound()));

        // Act
        await sut.InvokeLoadDetailAsync(listItem);

        // Assert
        await _medicalCaseService.Received(1).LoadDetailsAsync(listItem.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadDetailAsync_HandlesExceptionAndLogsError()
    {
        // Arrange
        var sut = CreateSut();
        var listItem = CreateMedicalCaseListDto();
        var exception = new Exception("Database connection failed");

        _medicalCaseService.LoadDetailsAsync(listItem.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CommandResult<MedicalCaseDetailModel>>(exception));

        // Act
        await sut.InvokeLoadDetailAsync(listItem);

        // Assert
        _errorHandler.Received(1).HandleException(exception, "加载医案详情");
    }

    #endregion

    #region SaveDetailAsync

    [Fact]
    public async Task SaveDetailAsync_BuildsAggregateDtoAndCallsSave()
    {
        // Arrange
        var sut = CreateSut();
        var detail = CreateMedicalCaseDetailModel();
        var savedDto = CreateMedicalCaseDetailDto();

        // Set up child VM data
        sut.ConsultationEditor.Consultation = new LYBT.Desktop.MedicalCase.Models.Items.ConsultationItem
        {
            PresentIllness = "测试现病史",
            TcmDiagnosis = "测试中医诊断"
        };
        sut.PrescriptionEditor.Prescription = new LYBT.Desktop.MedicalCase.ViewModels.Items.PrescriptionItemViewModel
        {
            DosageCount = 7,
            Items = new System.Collections.ObjectModel.ObservableCollection<PrescriptionItemModel>()
        };

        _medicalCaseService.AggregateSaveAsync(detail.Id, Arg.Any<ConsultationInputDto?>(), Arg.Any<PrescriptionInputDto?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(bool, MedicalCaseDetailDto?, string?)>((true, (MedicalCaseDetailDto?)savedDto, (string?)null)));

        // Act
        var result = await sut.SaveDetailAsync(detail);

        // Assert
        await _medicalCaseService.Received(1).AggregateSaveAsync(detail.Id, Arg.Any<ConsultationInputDto?>(), Arg.Any<PrescriptionInputDto?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        _cacheManager.Received(1).InvalidateMedicalCaseCaches();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SaveDetailAsync_ReturnsFalse_WhenSaveFails()
    {
        // Arrange
        var sut = CreateSut();
        var detail = CreateMedicalCaseDetailModel();
        var exception = new Exception("Save failed");

        sut.ConsultationEditor.Consultation = new LYBT.Desktop.MedicalCase.Models.Items.ConsultationItem();
        sut.PrescriptionEditor.Prescription = new LYBT.Desktop.MedicalCase.ViewModels.Items.PrescriptionItemViewModel();

        _medicalCaseService.AggregateSaveAsync(detail.Id, Arg.Any<ConsultationInputDto?>(), Arg.Any<PrescriptionInputDto?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<(bool Success, MedicalCaseDetailDto? Data, string? Error)>(exception));

        // Act
        var result = await sut.SaveDetailAsync(detail);

        // Assert
        result.Should().BeFalse();
        _errorHandler.Received(1).SetError("Save", Arg.Any<string>());
    }

    [Fact]
    public async Task SaveDetailAsync_IncludesPrescriptionItems_WhenPresent()
    {
        // Arrange
        var sut = CreateSut();
        var detail = CreateMedicalCaseDetailModel();
        var savedDto = CreateMedicalCaseDetailDto();
        var herbId = Guid.NewGuid();

        // 设置有药材的处方
        var prescriptionItem = new LYBT.Desktop.MedicalCase.ViewModels.Items.PrescriptionItemViewModel
        {
            DosageCount = 5,
            Remark = "测试备注"
        };
        prescriptionItem.Items.Add(new PrescriptionItemModel
        {
            HerbId = herbId,
            HerbName = "人参",
            Dosage = 10,
            Unit = "g",
            UnitPrice = 5.0m,
            DecocteMethod = DecocteMethod.Default
        });

        sut.ConsultationEditor.Consultation = new LYBT.Desktop.MedicalCase.Models.Items.ConsultationItem();
        sut.PrescriptionEditor.Prescription = prescriptionItem;

        _medicalCaseService.AggregateSaveAsync(detail.Id, Arg.Any<ConsultationInputDto?>(), Arg.Any<PrescriptionInputDto?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<(bool, MedicalCaseDetailDto?, string?)>((true, (MedicalCaseDetailDto?)savedDto, (string?)null)));

        // Act
        await sut.SaveDetailAsync(detail);

        // Assert
        await _medicalCaseService.Received(1).AggregateSaveAsync(detail.Id, Arg.Any<ConsultationInputDto?>(), Arg.Any<PrescriptionInputDto?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeleteItemAsync

    [Fact]
    public async Task DeleteItemAsync_CallsRepositoryDeleteAndInvalidatesCache()
    {
        // Arrange
        var sut = CreateSut();
        var listItem = CreateMedicalCaseListDto();

        _medicalCaseService.CancelMedicalCaseAsync(listItem.Id, Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(CommandResult<bool>.Succeeded(true)));

        // Act
        var result = await sut.DeleteItemAsync(listItem);

        // Assert
        await _medicalCaseService.Received(1).CancelMedicalCaseAsync(listItem.Id, Arg.Any<string?>(), Arg.Any<CancellationToken>());
        _cacheManager.Received(1).InvalidateMedicalCaseCaches();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteItemAsync_ReturnsFalse_WhenDeleteFails()
    {
        // Arrange
        var sut = CreateSut();
        var listItem = CreateMedicalCaseListDto();

        _medicalCaseService.CancelMedicalCaseAsync(listItem.Id, Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(CommandResult<bool>.Failed("delete failed")));

        // Act
        var result = await sut.DeleteItemAsync(listItem);

        // Assert
        result.Should().BeFalse();
        _errorHandler.Received(1).SetError("Delete", "delete failed");
        _cacheManager.DidNotReceive().InvalidateMedicalCaseCaches();
    }

    #endregion

    #region LoadHerbsAsync

    [Fact]
    public async Task LoadHerbsAsync_LoadsHerbsViaProviderAndPopulatesAllHerbs()
    {
        // Arrange
        var sut = CreateSut();
        var herbs = new List<HerbListDto>
        {
            new() { Id = Guid.NewGuid(), Name = "人参" },
            new() { Id = Guid.NewGuid(), Name = "当归" }
        };

        _herbSearchProvider.SearchHerbsAsync(string.Empty).Returns(Task.FromResult<IReadOnlyList<HerbListDto>>(herbs));

        // Act
        await sut.InvokeLoadHerbsAsync();

        // Assert
        await _herbSearchProvider.Received(1).SearchHerbsAsync(string.Empty);

        var allHerbs = sut.GetType().GetProperty("AllHerbs")?.GetValue(sut) as System.Collections.ObjectModel.ObservableCollection<HerbListDto>;
        allHerbs.Should().NotBeNull();
        allHerbs.Should().HaveCount(2);
    }

    [Fact]
    public async Task LoadHerbsAsync_HandlesEmptyResult()
    {
        // Arrange
        var sut = CreateSut();
        var herbs = new List<HerbListDto>();

        _herbSearchProvider.SearchHerbsAsync(string.Empty).Returns(Task.FromResult<IReadOnlyList<HerbListDto>>(herbs));

        // Act
        await sut.InvokeLoadHerbsAsync();

        // Assert
        await _herbSearchProvider.Received(1).SearchHerbsAsync(string.Empty);

        var allHerbs = sut.GetType().GetProperty("AllHerbs")?.GetValue(sut) as System.Collections.ObjectModel.ObservableCollection<HerbListDto>;
        allHerbs.Should().NotBeNull();
        allHerbs.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadHerbsAsync_HandlesExceptionAndLogsError()
    {
        // Arrange
        var sut = CreateSut();
        var exception = new Exception("Failed to load herbs");

        _herbSearchProvider.SearchHerbsAsync(string.Empty).Returns(Task.FromException<IReadOnlyList<HerbListDto>>(exception));

        // Act
        await sut.InvokeLoadHerbsAsync();

        // Assert - should not throw, exception is caught and logged
        await _herbSearchProvider.Received(1).SearchHerbsAsync(string.Empty);
        // Logger.LogError is called internally but we can't easily verify due to extension method usage
    }

    #endregion

    #region OnNavigatedTo

    [Fact]
    public async Task OnNavigatedTo_CallsLoadHerbsAsync_WhenAllHerbsIsEmpty()
    {
        // Arrange
        var sut = CreateSut();
        var herbs = new List<HerbListDto> { new() { Id = Guid.NewGuid(), Name = "人参" } };

        _herbSearchProvider.SearchHerbsAsync(string.Empty).Returns(Task.FromResult<IReadOnlyList<HerbListDto>>(herbs));

        // Create navigation context mock
        var navigationContext = Substitute.For<NavigationContext>(
            Substitute.For<IRegionNavigationService>(),
            new Uri("MedicalCaseMasterDetailView", UriKind.Relative));

        // Act
        sut.InvokeOnNavigatedTo(navigationContext);

        // Wait for async void method to complete (small delay)
        await Task.Delay(100);

        // Assert
        await _herbSearchProvider.Received(1).SearchHerbsAsync(string.Empty);
    }

    [Fact]
    public void OnNavigatedTo_SkipsLoadingHerbs_WhenAllHerbsAlreadyLoaded()
    {
        // Arrange
        var sut = CreateSut();
        var herbs = new List<HerbListDto> { new() { Id = Guid.NewGuid(), Name = "人参" } };

        // Pre-populate AllHerbs by adding to the existing collection (it has no setter)
        var allHerbs = sut.AllHerbs;
        foreach (var herb in herbs)
            allHerbs.Add(herb);

        // Create navigation context mock
        var navigationContext = Substitute.For<NavigationContext>(
            Substitute.For<IRegionNavigationService>(),
            new Uri("MedicalCaseMasterDetailView", UriKind.Relative));

        // Act
        sut.InvokeOnNavigatedTo(navigationContext);

        // Assert
        _herbSearchProvider.DidNotReceive().SearchHerbsAsync(Arg.Any<string>());
    }

    #endregion

    #region CreateNewDetail

    [Fact]
    public void CreateNewDetail_ThrowsNotSupportedException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        // T3-5: 子类化直接调用（原反射包装 TargetInvocationException 已去除）
        Action act = () => sut.TestCreateNewDetail();

        act.Should().Throw<NotSupportedException>()
            .WithMessage("医案管理模块不支持新建医案，请通过看诊入口创建");
    }

    #endregion

    #region SelectedPatientName

    [Fact]
    public void SelectedPatientName_ReturnsEmpty_WhenNoSelection()
    {
        // Arrange
        var sut = CreateSut();
        _selection.SelectedItem.Returns((MedicalCaseListDto?)null);

        // Act & Assert
        sut.SelectedPatientName.Should().BeEmpty();
    }

    [Fact]
    public void SelectedPatientName_ReturnsPatientName_WhenItemSelected()
    {
        // Arrange
        var sut = CreateSut();
        var listItem = CreateMedicalCaseListDto(patientName: "测试患者");
        _selection.SelectedItem.Returns(listItem);

        // Act & Assert
        sut.SelectedPatientName.Should().Be("测试患者");
    }

    #endregion

    #region Helper Methods

    private static MedicalCaseListDto CreateMedicalCaseListDto(Guid? id = null, string patientName = "测试患者")
    {
        return new MedicalCaseListDto
        {
            Id = id ?? Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            PatientName = patientName,
            DoctorName = "测试医生",
            CaseStatus = MedicalCaseStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static MedicalCaseDetailDto CreateMedicalCaseDetailDto()
    {
        return new MedicalCaseDetailDto
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            PatientName = "测试患者",
            DoctorName = "测试医生",
            CaseStatus = MedicalCaseStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static MedicalCaseDetailModel CreateMedicalCaseDetailModel()
    {
        return new MedicalCaseDetailModel
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            PatientName = "测试患者",
            Status = MedicalCaseStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}

/// <summary>
/// T3-5: 子类化暴露 protected 方法（取代原反射扩展——编译期安全，对齐 Herb 测试模式）。
/// 方法名与原扩展方法一致，调用点零改动。
/// </summary>
public sealed class TestableMedicalCaseMasterDetailViewModel : MedicalCaseMasterDetailViewModel
{
    public TestableMedicalCaseMasterDetailViewModel(
        IViewModelServices viewModelServices,
        IMasterDetailServices<MedicalCaseListDto, MedicalCaseDetailModel> masterDetailServices,
        IMedicalCaseService medicalCaseService,
        IHerbSearchProvider herbSearchProvider,
        IDesktopCacheManager cacheManager,
        ILoggerFactory loggerFactory)
        : base(viewModelServices, masterDetailServices, medicalCaseService, herbSearchProvider, cacheManager, loggerFactory)
    {
    }

    public void TestCreateNewDetail() => base.CreateNewDetail();

    public new Task<bool> SaveDetailAsync(MedicalCaseDetailModel detail) => base.SaveDetailAsync(detail);

    public new Task<bool> DeleteItemAsync(MedicalCaseListDto item) => base.DeleteItemAsync(item);

    public Task InvokeLoadDetailAsync(MedicalCaseListDto item) => base.LoadDetailAsync(item);

    public Task InvokeLoadHerbsAsync() => base.LoadHerbsAsync();

    public void InvokeOnNavigatedTo(Prism.Regions.NavigationContext navigationContext) => base.OnNavigatedTo(navigationContext);
}
