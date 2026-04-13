using System.Collections.ObjectModel;
using FluentAssertions;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Contracts.Services.CrossModule;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Formula.Mappers;
using LYBT.Desktop.Formula.Models;
using LYBT.Desktop.Formula.ViewModels;
using LYBT.Desktop.Formula.ViewModels.Handlers;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.Infrastructure;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.Formula;

[Collection("UserJourney")]
public class FormulaMasterDetailViewModelTests : UserJourneyTestBase
{
    private readonly IViewModelServices _viewModelServices;
    private readonly IMasterDetailServices<FormulaListDto, FormulaDetailModel> _masterDetailServices;
    private readonly IFormulaService _formulaService;
    private readonly IFormulaStatusHandler _statusHandler;
    private readonly IHerbSearchProvider _herbSearchProvider;
    private readonly IDesktopCacheManager _cacheManager;
    private readonly FormulaDetailModelMapper _mapper;

    private readonly IListViewServices<FormulaListDto> _listViewServices;
    private readonly IDetailEditorService<FormulaDetailModel> _detailEditor;
    private readonly IDialogManager _dialogManager;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly ILoadingStateManager _loadingState;
    private readonly IPaginationService _pagination;
    private readonly ISearchService _search;
    private readonly ISelectionService<FormulaListDto> _selection;
    private readonly IErrorHandler _errorHandler;
    private readonly IAsyncExecutor _asyncExecutor;
    private readonly ILoggerFactory _loggerFactory;
    private readonly FormulaEditorViewModel _formulaEditor;

    public FormulaMasterDetailViewModelTests(UserJourneyFixture fixture) : base(fixture)
    {
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        _listViewServices = Substitute.For<IListViewServices<FormulaListDto>>();
        _detailEditor = Substitute.For<IDetailEditorService<FormulaDetailModel>>();
        _dialogManager = Substitute.For<IDialogManager>();
        _navigationCoordinator = Substitute.For<INavigationCoordinator>();
        _loadingState = Substitute.For<ILoadingStateManager>();
        _pagination = Substitute.For<IPaginationService>();
        _search = Substitute.For<ISearchService>();
        _selection = Substitute.For<ISelectionService<FormulaListDto>>();
        _errorHandler = Substitute.For<IErrorHandler>();
        _asyncExecutor = Substitute.For<IAsyncExecutor>();

        _listViewServices.Loading.Returns(_loadingState);
        _listViewServices.Pagination.Returns(_pagination);
        _listViewServices.Search.Returns(_search);
        _listViewServices.Selection.Returns(_selection);
        _listViewServices.ErrorHandler.Returns(_errorHandler);
        _listViewServices.AsyncExecutor.Returns(_asyncExecutor);

        _loadingState.ExecuteWithLoadingAsync(Arg.Any<Func<Task>>(), Arg.Any<string?>(), Arg.Any<bool>())
            .Returns(ci => ci.Arg<Func<Task>>()());

        _detailEditor.When(x => x.CreateNew(Arg.Any<Func<FormulaDetailModel>>()))
            .Do(ci =>
            {
                var factory = ci.Arg<Func<FormulaDetailModel>>();
                _detailEditor.CurrentDetail = factory();
                _detailEditor.IsEditMode = true;
            });

        _detailEditor.When(x => x.EnterEditMode())
            .Do(_ => _detailEditor.IsEditMode = true);

        _search.ExecuteSearchAsync(Arg.Any<Func<string, Task>>())
            .Returns(ci => ci.Arg<Func<string, Task>>()(_search.SearchText));

        _viewModelServices = Substitute.For<IViewModelServices>();
        _viewModelServices.LoggerFactory.Returns(_loggerFactory);
        _viewModelServices.EventAggregator.Returns(Substitute.For<Prism.Events.IEventAggregator>());
        _viewModelServices.RegionManager.Returns(Substitute.For<Prism.Regions.IRegionManager>());
        _viewModelServices.SessionManager.Returns(Substitute.For<ISessionManager>());
        _viewModelServices.UserNotificationService.Returns(Substitute.For<IUserNotificationService>());
        _viewModelServices.CommonDialogService.Returns(Substitute.For<ICommonDialogService>());
        _viewModelServices.RoleRegistry.Returns(Substitute.For<LYBT.Desktop.Contracts.Roles.IRoleRegistry>());

        _masterDetailServices = Substitute.For<IMasterDetailServices<FormulaListDto, FormulaDetailModel>>();
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

        _formulaService = Substitute.For<IFormulaService>();
        _statusHandler = Substitute.For<IFormulaStatusHandler>();
        _herbSearchProvider = Substitute.For<IHerbSearchProvider>();
        _cacheManager = Substitute.For<IDesktopCacheManager>();
        _mapper = new FormulaDetailModelMapper();
        _formulaEditor = new FormulaEditorViewModel();

        _pagination.CurrentPage.Returns(1);
        _pagination.PageSize.Returns(20);
        _search.SearchText.Returns(string.Empty);
        _dialogManager.ShowConfirmAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _herbSearchProvider.GetAllHerbsAsync().Returns(Task.FromResult<IReadOnlyList<HerbListDto>>(Array.Empty<HerbListDto>()));
    }

    private FormulaMasterDetailViewModel CreateSut()
        => new(
            _viewModelServices,
            _masterDetailServices,
            _formulaService,
            _statusHandler,
            _herbSearchProvider,
            _cacheManager,
            _mapper,
            _formulaEditor);

    [Fact]
    public async Task InitializeAsync_LoadsFormulaList()
    {
        var sut = CreateSut();
        var paged = new PagedResult<FormulaListDto>
        {
            Items = new List<FormulaListDto>
            {
                new() { Id = Guid.NewGuid(), Name = "补中益气汤", Category = "经典方" },
                new() { Id = Guid.NewGuid(), Name = "四君子汤", Category = "经典方" }
            },
            TotalCount = 2
        };

        _formulaService.GetPagedAsync(1, 20, string.Empty, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new LYBT.Desktop.Contracts.CommandHandlers.CommandResult<PagedResult<FormulaListDto>>(true, paged, null)));

        await sut.InitializeAsync();

        sut.Items.Should().HaveCount(2);
        sut.TotalCount.Should().Be(2);
        await _formulaService.Received(1).GetPagedAsync(1, 20, string.Empty, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveCommand_CreatesNewFormulaAndInvalidatesCache()
    {
        var sut = CreateSut();
        var savedId = Guid.NewGuid();
        sut.FormulaEditor.InitializeForNewCase();
        sut.FormulaEditor.Formula.Name = "新验方";
        sut.FormulaEditor.Formula.Effect = "益气健脾";
        sut.FormulaEditor.Formula.Usage = "每日一剂";
        sut.FormulaEditor.Formula.Property = "甘平";
        sut.FormulaEditor.Formula.Category = "自拟方";
        sut.FormulaEditor.Formula.Remark = "测试创建";

        _detailEditor.CurrentDetail = new FormulaDetailModel();
        _detailEditor.IsEditMode = true;
        sut.FormulaEditor.EditHerbItems.Clear();
        sut.FormulaEditor.EditHerbItems.Add(new FormulaHerbItemViewModel
        {
            HerbId = Guid.NewGuid(),
            HerbName = "党参",
            Dosage = 12,
            Unit = "g",
            Remark = "先煎",
            DecocteMethod = DecocteMethod.Default
        });

        _formulaService.SaveFormulaAsync(
                Arg.Any<FormulaDetailDto>(),
                "新验方",
                "益气健脾",
                "每日一剂",
                "甘平",
                "自拟方",
                "测试创建",
                false,
                Arg.Any<List<FormulaHerbItemInputDto>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LYBT.Desktop.Contracts.CommandHandlers.CommandResult<FormulaDetailDto>(true, new FormulaDetailDto { Id = savedId, Name = "新验方" }, null)));

        _formulaService.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LYBT.Desktop.Contracts.CommandHandlers.CommandResult<PagedResult<FormulaListDto>>(true, new PagedResult<FormulaListDto> { Items = new List<FormulaListDto>(), TotalCount = 0 }, null)));

        await sut.SaveCommand.ExecuteAsync(null);

        await _formulaService.Received(1).SaveFormulaAsync(
            Arg.Is<FormulaDetailDto>(x => x.Id == Guid.Empty),
            "新验方",
            "益气健脾",
            "每日一剂",
            "甘平",
            "自拟方",
            "测试创建",
            false,
            Arg.Is<List<FormulaHerbItemInputDto>>(x => x.Count == 1 && x[0].HerbName == "党参"),
            Arg.Any<CancellationToken>());
        _cacheManager.Received(1).InvalidateFormulaCaches();
    }

    [Fact]
    public async Task SaveCommand_UpdatesExistingFormula()
    {
        var sut = CreateSut();
        var existingId = Guid.NewGuid();
        sut.FormulaEditor.InitializeForNewCase();
        sut.FormulaEditor.Formula.Id = existingId;
        sut.FormulaEditor.Formula.Name = "旧验方";
        sut.FormulaEditor.Formula.Effect = "更新功效";
        sut.FormulaEditor.Formula.Usage = "更新用法";
        sut.FormulaEditor.Formula.Property = "温";
        sut.FormulaEditor.Formula.Category = "临床方";

        _detailEditor.CurrentDetail = new FormulaDetailModel();
        _detailEditor.IsEditMode = true;
        sut.FormulaEditor.EditHerbItems.Clear();
        sut.FormulaEditor.EditHerbItems.Add(new FormulaHerbItemViewModel
        {
            HerbId = Guid.NewGuid(),
            HerbName = "黄芪",
            Dosage = 15,
            Unit = "g",
            DecocteMethod = DecocteMethod.Default
        });

        _formulaService.SaveFormulaAsync(
                Arg.Any<FormulaDetailDto>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<List<FormulaHerbItemInputDto>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LYBT.Desktop.Contracts.CommandHandlers.CommandResult<FormulaDetailDto>(true, new FormulaDetailDto { Id = existingId, Name = "旧验方" }, null)));

        _formulaService.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LYBT.Desktop.Contracts.CommandHandlers.CommandResult<PagedResult<FormulaListDto>>(true, new PagedResult<FormulaListDto> { Items = new List<FormulaListDto>(), TotalCount = 0 }, null)));

        await sut.SaveCommand.ExecuteAsync(null);

        await _formulaService.Received(1).SaveFormulaAsync(
            Arg.Is<FormulaDetailDto>(x => x.Id == existingId),
            "旧验方",
            "更新功效",
            "更新用法",
            "温",
            "临床方",
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<List<FormulaHerbItemInputDto>>(),
            Arg.Any<CancellationToken>());
        _cacheManager.Received(1).InvalidateFormulaCaches();
    }

    [Fact]
    public void EditCommand_EntersEditModeForSelectedFormula()
    {
        var sut = CreateSut();
        sut.SelectedItem = new FormulaListDto { Id = Guid.NewGuid(), Name = "待编辑验方" };

        sut.EditCommand.Execute(null);

        _detailEditor.Received(1).EnterEditMode();
    }

    [Fact]
    public async Task DeleteCommand_DeletesSelectedFormula()
    {
        var sut = CreateSut();
        var item = new FormulaListDto { Id = Guid.NewGuid(), Name = "待删除验方" };
        sut.SelectedItem = item;
        _selection.HasSelection.Returns(true);
        _selection.SelectedItem.Returns(item);

        _formulaService.DeleteFormulaAsync(item.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LYBT.Desktop.Contracts.CommandHandlers.CommandResult<bool>(true, true, null)));
        _formulaService.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LYBT.Desktop.Contracts.CommandHandlers.CommandResult<PagedResult<FormulaListDto>>(true, new PagedResult<FormulaListDto> { Items = new List<FormulaListDto>(), TotalCount = 0 }, null)));
        _dialogManager.ShowConfirmAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(true));

        await sut.DeleteCommand.ExecuteAsync(null);

        await _formulaService.Received(1).DeleteFormulaAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _dialogManager.Received(1).ShowConfirmAsync("确认删除", "确定要删除选中的记录吗？");
    }

    [Fact]
    public void AddHerbCommand_AddsEditableHerbRow()
    {
        var sut = CreateSut();
        _detailEditor.IsEditMode = true;

        sut.AddHerbCommand.Execute(null);

        sut.FormulaEditor.EditHerbItems.Should().HaveCount(1);
        sut.FormulaEditor.HerbCount.Should().Be(0);
    }

    [Fact]
    public void DeleteHerbCommand_RemovesEditableHerbRow()
    {
        var sut = CreateSut();
        _detailEditor.IsEditMode = true;
        var herb = new FormulaHerbItemViewModel { HerbId = Guid.NewGuid(), HerbName = "白术", Dosage = 10, Unit = "g" };
        sut.FormulaEditor.EditHerbItems.Clear();
        sut.FormulaEditor.EditHerbItems.Add(herb);

        sut.DeleteHerbCommand.Execute(herb);

        sut.FormulaEditor.EditHerbItems.Should().BeEmpty();
        sut.FormulaEditor.HerbCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchCommand_FiltersByKeyword()
    {
        var sut = CreateSut();
        _search.SearchText.Returns("补气");
        _formulaService.GetPagedAsync(1, 20, "补气", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LYBT.Desktop.Contracts.CommandHandlers.CommandResult<PagedResult<FormulaListDto>>(true, new PagedResult<FormulaListDto> { Items = new List<FormulaListDto>(), TotalCount = 0 }, null)));

        await sut.SearchCommand.ExecuteAsync(null);

        await _formulaService.Received(1).GetPagedAsync(1, 20, "补气", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchByCategoryCommand_FiltersByCategory()
    {
        var sut = CreateSut();
        _formulaService.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), "分类:经典方", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LYBT.Desktop.Contracts.CommandHandlers.CommandResult<PagedResult<FormulaListDto>>(true, new PagedResult<FormulaListDto> { Items = new List<FormulaListDto>(), TotalCount = 0 }, null)));

        await sut.SearchByCategoryCommand.ExecuteAsync("经典方");

        sut.SearchText.Should().Be("分类:经典方");
        await _formulaService.Received(1).GetPagedAsync(1, 20, "分类:经典方", Arg.Any<CancellationToken>());
    }
}
