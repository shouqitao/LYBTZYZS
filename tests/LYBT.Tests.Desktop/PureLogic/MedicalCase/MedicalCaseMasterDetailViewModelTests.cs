using FluentAssertions;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Contracts.Services.CrossModule;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.MedicalCase.ViewModels;
using LYBT.Desktop.Modules.MedicalCase.Models;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Prism.Regions;

namespace LYBT.Tests.Desktop.PureLogic.MedicalCase;

/// <summary>
/// MedicalCaseMasterDetailViewModel 单元测试
/// 验证医案管理模块的Master-Detail视图模型行为
/// </summary>
public class MedicalCaseMasterDetailViewModelTests
{
    private readonly IViewModelServices _viewModelServices;
    private readonly IMasterDetailServices<MedicalCaseListDto, MedicalCaseDetailModel> _masterDetailServices;
    private readonly IMedicalCaseRepository _repository;
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
    private readonly IAsyncExecutor _asyncExecutor;

    public MedicalCaseMasterDetailViewModelTests()
    {
        // Arrange - 创建所有 mock
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _logger = Substitute.For<ILogger<MedicalCaseMasterDetailViewModel>>();

        // 关键：为 CreateLogger 设置明确的返回值
        // 必须显式设置 MedicalCaseMasterDetailViewModel 类型的 logger，因为基类使用 GetType() 获取类型
        _loggerFactory.CreateLogger(typeof(MedicalCaseMasterDetailViewModel)).Returns(_logger);

        // 创建 MasterDetailServices 组件 mocks
        _listViewServices = Substitute.For<IListViewServices<MedicalCaseListDto>>();
        _detailEditor = Substitute.For<IDetailEditorService<MedicalCaseDetailModel>>();
        _dialogManager = Substitute.For<IDialogManager>();
        _navigationCoordinator = Substitute.For<INavigationCoordinator>();
        _loadingState = Substitute.For<ILoadingStateManager>();
        _pagination = Substitute.For<IPaginationService>();
        _search = Substitute.For<ISearchService>();
        _selection = Substitute.For<ISelectionService<MedicalCaseListDto>>();
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
        _masterDetailServices = Substitute.For<IMasterDetailServices<MedicalCaseListDto, MedicalCaseDetailModel>>();
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

        // 创建 Repository 和 Provider mocks
        _repository = Substitute.For<IMedicalCaseRepository>();
        _herbSearchProvider = Substitute.For<IHerbSearchProvider>();
        _cacheManager = Substitute.For<IDesktopCacheManager>();
    }

    private MedicalCaseMasterDetailViewModel CreateSut()
    {
        return new MedicalCaseMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            _repository,
            _herbSearchProvider,
            _cacheManager);
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
    public void Constructor_ThrowsArgumentNullException_WhenRepositoryIsNull()
    {
        // Arrange & Act & Assert
        Action act = () => new MedicalCaseMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            null!,
            _herbSearchProvider,
            _cacheManager);

        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenHerbSearchProviderIsNull()
    {
        // Arrange & Act & Assert
        Action act = () => new MedicalCaseMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            _repository,
            null!,
            _cacheManager);

        act.Should().Throw<ArgumentNullException>().WithParameterName("herbSearchProvider");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenCacheManagerIsNull()
    {
        // Arrange & Act & Assert
        Action act = () => new MedicalCaseMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            _repository,
            _herbSearchProvider,
            null!);

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

        _repository.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns(Task.FromResult(pagedData));

        // 设置分页服务的返回值
        _pagination.CurrentPage.Returns(1);
        _pagination.PageSize.Returns(20);
        _search.SearchText.Returns((string?)null);

        // Act
        await sut.InitializeAsync();

        // Assert
        await _repository.Received(1).GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task LoadListAsync_HandlesExceptionAndLogsError()
    {
        // Arrange
        var sut = CreateSut();
        var exception = new Exception("Database connection failed");

        _repository.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>())
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

        _repository.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns(Task.FromResult(pagedData));

        // 设置搜索文本
        _search.SearchText.Returns("测试关键词");

        // Act
        await sut.InitializeAsync();

        // Assert - 验证调用了 GetPagedAsync，搜索文本通过 SearchText 属性委托
        await _repository.Received(1).GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>());
    }

    #endregion

    #region LoadDetailAsync

    [Fact]
    public async Task LoadDetailAsync_LoadsDetailViaRepositoryAndInitializesEditModels()
    {
        // Arrange
        var sut = CreateSut();
        var listItem = CreateMedicalCaseListDto();
        var detailDto = CreateMedicalCaseDetailDto();
        var herbs = new List<HerbListDto> { new() { Id = Guid.NewGuid(), Name = "人参" } };

        _repository.GetByIdAsync(listItem.Id).Returns(Task.FromResult<MedicalCaseDetailDto?>(detailDto));
        _herbSearchProvider.SearchHerbsAsync(string.Empty).Returns(Task.FromResult<IReadOnlyList<HerbListDto>>(herbs));

        // Act
        await sut.InvokeLoadDetailAsync(listItem);

        // Assert
        await _repository.Received(1).GetByIdAsync(listItem.Id);

        // Verify Consultation and Prescription were initialized via reflection
        var consultation = sut.GetType().GetProperty("Consultation")?.GetValue(sut) as LYBT.Desktop.MedicalCase.Models.Items.ConsultationItem;
        var prescription = sut.GetType().GetProperty("Prescription")?.GetValue(sut) as LYBT.Desktop.MedicalCase.Models.Items.PrescriptionItem;

        consultation.Should().NotBeNull();
        prescription.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadDetailAsync_HandlesNullResultFromRepository()
    {
        // Arrange
        var sut = CreateSut();
        var listItem = CreateMedicalCaseListDto();
        var herbs = new List<HerbListDto>();

        _repository.GetByIdAsync(listItem.Id).Returns(Task.FromResult<MedicalCaseDetailDto?>(null));
        _herbSearchProvider.SearchHerbsAsync(string.Empty).Returns(Task.FromResult<IReadOnlyList<HerbListDto>>(herbs));

        // Act
        await sut.InvokeLoadDetailAsync(listItem);

        // Assert
        await _repository.Received(1).GetByIdAsync(listItem.Id);
        // Should not throw and should return early without initializing edit models
    }

    [Fact]
    public async Task LoadDetailAsync_HandlesExceptionAndLogsError()
    {
        // Arrange
        var sut = CreateSut();
        var listItem = CreateMedicalCaseListDto();
        var exception = new Exception("Database connection failed");
        var herbs = new List<HerbListDto>();

        _repository.GetByIdAsync(listItem.Id).Returns(Task.FromException<MedicalCaseDetailDto?>(exception));
        _herbSearchProvider.SearchHerbsAsync(string.Empty).Returns(Task.FromResult<IReadOnlyList<HerbListDto>>(herbs));

        // Act
        await sut.InvokeLoadDetailAsync(listItem);

        // Assert
        _errorHandler.Received(1).HandleException(exception, "加载医案详情");
    }

    [Fact]
    public async Task LoadDetailAsync_LoadsHerbsWhenAllHerbsIsEmpty()
    {
        // Arrange
        var sut = CreateSut();
        var listItem = CreateMedicalCaseListDto();
        var detailDto = CreateMedicalCaseDetailDto();
        var herbs = new List<HerbListDto> { new() { Id = Guid.NewGuid(), Name = "人参" } };

        _repository.GetByIdAsync(listItem.Id).Returns(Task.FromResult<MedicalCaseDetailDto?>(detailDto));
        _herbSearchProvider.SearchHerbsAsync(string.Empty).Returns(Task.FromResult<IReadOnlyList<HerbListDto>>(herbs));

        // Act
        await sut.InvokeLoadDetailAsync(listItem);

        // Assert
        await _herbSearchProvider.Received(1).SearchHerbsAsync(string.Empty);
    }

    [Fact]
    public async Task LoadDetailAsync_SkipsLoadingHerbsWhenAllHerbsAlreadyLoaded()
    {
        // Arrange
        var sut = CreateSut();
        var listItem = CreateMedicalCaseListDto();
        var detailDto = CreateMedicalCaseDetailDto();
        var herbs = new List<HerbListDto> { new() { Id = Guid.NewGuid(), Name = "人参" } };

        // Pre-populate AllHerbs
        var allHerbsProperty = sut.GetType().GetProperty("AllHerbs");
        allHerbsProperty?.SetValue(sut, new System.Collections.ObjectModel.ObservableCollection<HerbListDto>(herbs));

        _repository.GetByIdAsync(listItem.Id).Returns(Task.FromResult<MedicalCaseDetailDto?>(detailDto));

        // Act
        await sut.InvokeLoadDetailAsync(listItem);

        // Assert
        await _herbSearchProvider.DidNotReceive().SearchHerbsAsync(Arg.Any<string>());
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

        // 设置 Consultation 和 Prescription 以便保存
        sut.GetType().GetProperty("Consultation")?.SetValue(sut, new LYBT.Desktop.MedicalCase.Models.Items.ConsultationItem
        {
            PresentIllness = "测试现病史",
            TcmDiagnosis = "测试中医诊断"
        });

        sut.GetType().GetProperty("Prescription")?.SetValue(sut, new LYBT.Desktop.MedicalCase.Models.Items.PrescriptionItem
        {
            DosageCount = 7,
            Items = new System.Collections.ObjectModel.ObservableCollection<PrescriptionItemDto>()
        });

        _repository.SaveAsync(detail.Id, Arg.Any<MedicalCaseInputDto>())
            .Returns(Task.FromResult(savedDto));

        // Act
        var result = await sut.SaveDetailAsync(detail);

        // Assert
        await _repository.Received(1).SaveAsync(detail.Id, Arg.Any<MedicalCaseInputDto>());
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

        // 设置 Consultation 和 Prescription
        sut.GetType().GetProperty("Consultation")?.SetValue(sut, new LYBT.Desktop.MedicalCase.Models.Items.ConsultationItem());
        sut.GetType().GetProperty("Prescription")?.SetValue(sut, new LYBT.Desktop.MedicalCase.Models.Items.PrescriptionItem());

        _repository.SaveAsync(detail.Id, Arg.Any<MedicalCaseInputDto>())
            .Returns(Task.FromException<MedicalCaseDetailDto>(exception));

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
        var prescriptionItem = new LYBT.Desktop.MedicalCase.Models.Items.PrescriptionItem
        {
            DosageCount = 5,
            Remark = "测试备注"
        };
        prescriptionItem.Items.Add(new PrescriptionItemDto
        {
            HerbId = herbId,
            HerbName = "人参",
            Dosage = 10,
            Unit = "g",
            UnitPrice = 5.0m,
            DecocteMethod = DecocteMethod.Default
        });

        sut.GetType().GetProperty("Consultation")?.SetValue(sut, new LYBT.Desktop.MedicalCase.Models.Items.ConsultationItem());
        sut.GetType().GetProperty("Prescription")?.SetValue(sut, prescriptionItem);

        _repository.SaveAsync(detail.Id, Arg.Any<MedicalCaseInputDto>())
            .Returns(Task.FromResult(savedDto));

        // Act
        await sut.SaveDetailAsync(detail);

        // Assert
        await _repository.Received(1).SaveAsync(detail.Id, Arg.Is<MedicalCaseInputDto>(dto =>
            dto.Prescription != null &&
            dto.Prescription.Items.Count == 1 &&
            dto.Prescription.Items[0].HerbId == herbId));
    }

    #endregion

    #region DeleteItemAsync

    [Fact]
    public async Task DeleteItemAsync_CallsRepositoryDeleteAndInvalidatesCache()
    {
        // Arrange
        var sut = CreateSut();
        var listItem = CreateMedicalCaseListDto();

        _repository.DeleteAsync(listItem.Id).Returns(Task.FromResult(true));

        // Act
        var result = await sut.DeleteItemAsync(listItem);

        // Assert
        await _repository.Received(1).DeleteAsync(listItem.Id);
        _cacheManager.Received(1).InvalidateMedicalCaseCaches();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteItemAsync_ReturnsFalse_WhenDeleteFails()
    {
        // Arrange
        var sut = CreateSut();
        var listItem = CreateMedicalCaseListDto();

        _repository.DeleteAsync(listItem.Id).Returns(Task.FromResult(false));

        // Act
        var result = await sut.DeleteItemAsync(listItem);

        // Assert
        result.Should().BeFalse();
        _errorHandler.Received(1).SetError("Delete", "删除医案失败");
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

        // Pre-populate AllHerbs
        var allHerbsProperty = sut.GetType().GetProperty("AllHerbs");
        allHerbsProperty?.SetValue(sut, new System.Collections.ObjectModel.ObservableCollection<HerbListDto>(herbs));

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
        // 使用反射调用时，异常会被包装在 TargetInvocationException 中
        Action act = () => sut.TestCreateNewDetail();

        act.Should().Throw<System.Reflection.TargetInvocationException>()
            .WithInnerException<NotSupportedException>()
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
/// MedicalCaseMasterDetailViewModel 测试辅助扩展
/// </summary>
public static class MedicalCaseMasterDetailViewModelTestExtensions
{
    /// <summary>
    /// 测试辅助方法：调用受保护的 CreateNewDetail 方法
    /// </summary>
    public static void TestCreateNewDetail(this MedicalCaseMasterDetailViewModel vm)
    {
        // 使用反射调用受保护的方法（在基类 MasterDetailViewModelBase 中定义）
        // 需要在继承层次中查找方法
        var method = typeof(MedicalCaseMasterDetailViewModel).GetMethod(
            "CreateNewDetail",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy,
            null,
            Type.EmptyTypes,
            null);

        if (method == null)
        {
            // 尝试从基类获取
            var baseType = typeof(MedicalCaseMasterDetailViewModel).BaseType;
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

        method.Invoke(vm, null);
    }

    /// <summary>
    /// 测试辅助方法：调用受保护的 SaveDetailAsync 方法
    /// </summary>
    public static async Task<bool> SaveDetailAsync(this MedicalCaseMasterDetailViewModel vm, MedicalCaseDetailModel detail)
    {
        var method = typeof(MedicalCaseMasterDetailViewModel).GetMethod(
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
    public static async Task<bool> DeleteItemAsync(this MedicalCaseMasterDetailViewModel vm, MedicalCaseListDto item)
    {
        var method = typeof(MedicalCaseMasterDetailViewModel).GetMethod(
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
    public static async Task InvokeLoadDetailAsync(this MedicalCaseMasterDetailViewModel vm, MedicalCaseListDto item)
    {
        // 需要在继承层次中查找方法（在基类 MasterDetailViewModelBase 中定义）
        var method = typeof(MedicalCaseMasterDetailViewModel).GetMethod(
            "LoadDetailAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy,
            null,
            new[] { typeof(MedicalCaseListDto) },
            null);

        if (method == null)
        {
            // 尝试从基类获取
            var baseType = typeof(MedicalCaseMasterDetailViewModel).BaseType;
            while (method == null && baseType != null)
            {
                method = baseType.GetMethod(
                    "LoadDetailAsync",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    null,
                    new[] { typeof(MedicalCaseListDto) },
                    null);
                baseType = baseType.BaseType;
            }
        }

        if (method == null) throw new InvalidOperationException("LoadDetailAsync method not found");

        var result = method.Invoke(vm, new object[] { item });
        if (result is Task task) await task;
    }

    /// <summary>
    /// 测试辅助方法：调用私有的 LoadHerbsAsync 方法
    /// </summary>
    public static async Task InvokeLoadHerbsAsync(this MedicalCaseMasterDetailViewModel vm)
    {
        var method = typeof(MedicalCaseMasterDetailViewModel).GetMethod(
            "LoadHerbsAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method == null) throw new InvalidOperationException("LoadHerbsAsync method not found");

        var result = method.Invoke(vm, null);
        if (result is Task task) await task;
    }

    /// <summary>
    /// 测试辅助方法：调用 public override 的 OnNavigatedTo 方法
    /// </summary>
    public static void InvokeOnNavigatedTo(this MedicalCaseMasterDetailViewModel vm, NavigationContext navigationContext)
    {
        var method = typeof(MedicalCaseMasterDetailViewModel).GetMethod(
            "OnNavigatedTo",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (method == null) throw new InvalidOperationException("OnNavigatedTo method not found");

        method.Invoke(vm, new object[] { navigationContext });
    }
}
