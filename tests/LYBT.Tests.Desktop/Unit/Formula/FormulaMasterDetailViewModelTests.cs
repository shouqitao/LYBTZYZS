using System;
using LYBT.Tests.Desktop.Infrastructure;
using System.Collections.ObjectModel;
using FluentAssertions;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Contracts.Services.CrossModule;
using LYBT.Desktop.Catalog.Mappers;
using LYBT.Desktop.Catalog.Models;
using LYBT.Desktop.Catalog.ViewModels;
using LYBT.Desktop.Catalog.ViewModels.Handlers;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop.Unit;

public class FormulaMasterDetailViewModelTests : DesktopTestBase, IDisposable
{
    public void Dispose() => _formulaEditor.Dispose();
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
    private readonly ILoggerFactory _loggerFactory;
    private readonly FormulaEditorViewModel _formulaEditor;

    public FormulaMasterDetailViewModelTests()
    {
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        // T3-1: 使用基类共享装配（原重复装配已消除），保留 Formula 定制行为
        _masterDetailServices = CreateMasterDetailServicesMock<FormulaListDto, FormulaDetailModel>();
        _listViewServices = _masterDetailServices.List;
        _detailEditor = _masterDetailServices.DetailEditor;
        _dialogManager = _masterDetailServices.Dialog;
        _navigationCoordinator = _masterDetailServices.Navigation;
        _loadingState = _masterDetailServices.Loading;
        _pagination = _masterDetailServices.Pagination;
        _search = _masterDetailServices.Search;
        _selection = _masterDetailServices.Selection;
        _errorHandler = _masterDetailServices.ErrorHandler;

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

        _formulaService = Substitute.For<IFormulaService>();
        _statusHandler = Substitute.For<IFormulaStatusHandler>();
        _herbSearchProvider = Substitute.For<IHerbSearchProvider>();
        _cacheManager = Substitute.For<IDesktopCacheManager>();
        _mapper = new FormulaDetailModelMapper();
        _formulaEditor = new FormulaEditorViewModel(_mapper);

        _pagination.CurrentPage.Returns(1);
        _pagination.PageSize.Returns(20);
        _search.SearchText.Returns(string.Empty);
        _dialogManager.ShowConfirmAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _herbSearchProvider.GetAllHerbsAsync().Returns(Task.FromResult<IReadOnlyList<HerbListDto>>(Array.Empty<HerbListDto>()));

        // 验方校验（US-FORM-007/008）默认桩：待校验列表空页 + 详情不存在（各用例自行覆盖）
        _formulaService.GetPendingValidationAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new LYBT.Desktop.Contracts.Results.CommandResult<PagedResult<FormulaDetailDto>>(
                true,
                new PagedResult<FormulaDetailDto> { Items = new List<FormulaDetailDto>(), TotalCount = 0 },
                null));
        _formulaService.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new LYBT.Desktop.Contracts.Results.CommandResult<PagedResult<FormulaListDto>>(
                true,
                new PagedResult<FormulaListDto> { Items = new List<FormulaListDto>(), TotalCount = 0 },
                null));
    }

    private FormulaMasterDetailViewModel CreateSut()
        => new(
            _viewModelServices,
            _masterDetailServices,
            _formulaService,
            _statusHandler,
            _herbSearchProvider,
            _cacheManager,
            Substitute.For<IFileDialogService>(),
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

        _formulaService.GetPagedAsync(1, 20, string.Empty, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new LYBT.Desktop.Contracts.Results.CommandResult<PagedResult<FormulaListDto>>(true, paged, null)));

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

        _formulaService.CreateAsync(
                Arg.Is<FormulaInputDto>(x => x.Name == "新验方"),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LYBT.Desktop.Contracts.Results.CommandResult<FormulaDetailDto>(true, new FormulaDetailDto { Id = savedId, Name = "新验方" }, null)));

        _formulaService.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LYBT.Desktop.Contracts.Results.CommandResult<PagedResult<FormulaListDto>>(true, new PagedResult<FormulaListDto> { Items = new List<FormulaListDto>(), TotalCount = 0 }, null)));

        await sut.SaveCommand.ExecuteAsync(null);

        await _formulaService.Received(1).CreateAsync(
            Arg.Is<FormulaInputDto>(x =>
                x.Name == "新验方" &&
                x.Effect == "益气健脾" &&
                x.Usage == "每日一剂" &&
                x.Property == "甘平" &&
                x.Category == "自拟方" &&
                x.Remark == "测试创建" &&
                !x.IsShared &&
                x.Herbs.Count == 1 && x.Herbs[0].HerbName == "党参"),
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

        _formulaService.UpdateAsync(
                Arg.Is<FormulaInputDto>(x => x.Id == existingId),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LYBT.Desktop.Contracts.Results.CommandResult<FormulaDetailDto>(true, new FormulaDetailDto { Id = existingId, Name = "旧验方" }, null)));

        _formulaService.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LYBT.Desktop.Contracts.Results.CommandResult<PagedResult<FormulaListDto>>(true, new PagedResult<FormulaListDto> { Items = new List<FormulaListDto>(), TotalCount = 0 }, null)));

        await sut.SaveCommand.ExecuteAsync(null);

        await _formulaService.Received(1).UpdateAsync(
            Arg.Is<FormulaInputDto>(x =>
                x.Id == existingId &&
                x.Name == "旧验方" &&
                x.Effect == "更新功效" &&
                x.Usage == "更新用法" &&
                x.Property == "温" &&
                x.Category == "临床方" &&
                x.Herbs.Count == 1 && x.Herbs[0].HerbName == "黄芪"),
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

        _formulaService.DeleteAsync(item.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LYBT.Desktop.Contracts.Results.CommandResult<bool>(true, true, null)));
        _formulaService.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LYBT.Desktop.Contracts.Results.CommandResult<PagedResult<FormulaListDto>>(true, new PagedResult<FormulaListDto> { Items = new List<FormulaListDto>(), TotalCount = 0 }, null)));
        _dialogManager.ShowConfirmAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(true));

        await sut.DeleteCommand.ExecuteAsync(null);

        await _formulaService.Received(1).DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        // 契约：IDialogManager.ShowConfirmAsync(message, title) —— 修复实参颠倒后按正确顺序断言
        await _dialogManager.Received(1).ShowConfirmAsync("确定要删除选中的记录吗？", "确认删除");
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
            .Returns(Task.FromResult(new LYBT.Desktop.Contracts.Results.CommandResult<PagedResult<FormulaListDto>>(true, new PagedResult<FormulaListDto> { Items = new List<FormulaListDto>(), TotalCount = 0 }, null)));

        await sut.SearchCommand.ExecuteAsync(null);

        await _formulaService.Received(1).GetPagedAsync(1, 20, "补气", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchByCategoryCommand_FiltersByCategory()
    {
        var sut = CreateSut();
        _formulaService.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), "分类:经典方", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LYBT.Desktop.Contracts.Results.CommandResult<PagedResult<FormulaListDto>>(true, new PagedResult<FormulaListDto> { Items = new List<FormulaListDto>(), TotalCount = 0 }, null)));

        await sut.SearchByCategoryCommand.ExecuteAsync("经典方");

        sut.SearchText.Should().Be("分类:经典方");
        await _formulaService.Received(1).GetPagedAsync(1, 20, "分类:经典方", Arg.Any<CancellationToken>());
    }

    // ==================== 验方校验（US-FORM-007/008——Desktop 校验 UI） ====================

    /// <summary>等待异步回调（ServiceEventBridge 的选择→详情为 fire-and-forget）完成</summary>
    private static async Task PumpUntilAsync(Func<bool> condition, string failMessage, int timeoutMs = 3000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            if (sw.ElapsedMilliseconds > timeoutMs) break;
            await Task.Delay(10);
        }
        condition().Should().BeTrue(failMessage);
    }

    /// <summary>在校验模式下模拟选中某验方（触发基类 选择→LoadDetailAsync）</summary>
    private static void RaiseSelection(
        IMasterDetailServices<FormulaListDto, FormulaDetailModel> services,
        FormulaListDto item)
    {
        services.Selection.SelectionChanged += Raise.EventWith(
            new SelectionChangedEventArgs<FormulaListDto>(item, null, new[] { item }));
    }

    [Fact]
    public async Task ToggleValidationMode_LoadsPendingValidationList()
    {
        var sut = CreateSut();
        var detailId = Guid.NewGuid();
        var pending = new PagedResult<FormulaDetailDto>
        {
            Items = new List<FormulaDetailDto>
            {
                new() { Id = detailId, Name = "导入验方甲", ValidationStatus = FormulaValidationStatus.Draft, HerbCount = 2 }
            },
            TotalCount = 1
        };
        _formulaService.GetPendingValidationAsync(1, 20, Arg.Any<CancellationToken>())
            .Returns(new LYBT.Desktop.Contracts.Results.CommandResult<PagedResult<FormulaDetailDto>>(true, pending, null));

        await sut.ToggleValidationModeCommand.ExecuteAsync(null);

        sut.IsValidationMode.Should().BeTrue();
        sut.IsNormalMode.Should().BeFalse();
        sut.Items.Should().ContainSingle(i => i.Id == detailId && i.Name == "导入验方甲" && i.ValidationStatus == FormulaValidationStatus.Draft);
        sut.TotalCount.Should().Be(1);
        await _formulaService.Received(1).GetPendingValidationAsync(1, 20, Arg.Any<CancellationToken>());

        // 再次切换回普通模式 → 走全量分页查询
        await sut.ToggleValidationModeCommand.ExecuteAsync(null);
        sut.IsValidationMode.Should().BeFalse();
        await _formulaService.Received(1).GetPagedAsync(1, 20, string.Empty, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SelectingPendingFormulaInValidationMode_LoadsValidationRows()
    {
        var sut = CreateSut();
        await sut.ToggleValidationModeCommand.ExecuteAsync(null);

        var formulaId = Guid.NewGuid();
        var item = new FormulaListDto { Id = formulaId, Name = "导入验方乙" };
        _selection.SelectedItem.Returns(item);
        _selection.HasSelection.Returns(true);

        var detail = new FormulaDetailDto
        {
            Id = formulaId,
            Name = "导入验方乙",
            ValidationStatus = FormulaValidationStatus.Draft,
            Herbs = new List<FormulaHerbItemDto>
            {
                new() { Id = Guid.NewGuid(), IsValidated = true, HerbName = "人参", Dosage = 10, Unit = "g" },
                new() { Id = Guid.NewGuid(), IsValidated = false, HerbName = string.Empty, OriginalHerbName = "参须", Dosage = 5, Unit = "g" }
            }
        };
        _formulaService.GetByIdAsync(formulaId, Arg.Any<CancellationToken>())
            .Returns(new LYBT.Desktop.Contracts.Results.CommandResult<FormulaDetailDto>(true, detail, null));

        RaiseSelection(_masterDetailServices, item);
        await PumpUntilAsync(() => sut.IsValidationDetail, "校验详情面板未加载");

        sut.ValidationFormulaName.Should().Be("导入验方乙");
        sut.ValidationRows.Should().HaveCount(2);
        sut.ValidationRows[0].IsValidated.Should().BeTrue();
        sut.ValidationRows[1].IsValidated.Should().BeFalse();
        sut.ValidationRows[1].DisplayName.Should().Be("参须");
        sut.ValidationPendingCount.Should().Be(1);
        sut.DetailTitle.Should().Be("待校验 · 导入验方乙");
    }

    [Fact]
    public async Task ValidateHerb_BindsSelectedHerbToSystemHerb()
    {
        var sut = CreateSut();
        await sut.ToggleValidationModeCommand.ExecuteAsync(null);

        var formulaId = Guid.NewGuid();
        var herbItemId = Guid.NewGuid();
        var systemHerbId = Guid.NewGuid();
        var item = new FormulaListDto { Id = formulaId, Name = "导入验方丙" };
        _selection.SelectedItem.Returns(item);
        _selection.HasSelection.Returns(true);

        var draft = new FormulaDetailDto
        {
            Id = formulaId,
            Name = "导入验方丙",
            ValidationStatus = FormulaValidationStatus.Draft,
            Herbs = new List<FormulaHerbItemDto>
            {
                new() { Id = herbItemId, IsValidated = false, OriginalHerbName = "参须", Dosage = 5, Unit = "g" }
            }
        };
        var validated = new FormulaDetailDto
        {
            Id = formulaId,
            Name = "导入验方丙",
            ValidationStatus = FormulaValidationStatus.Validated,
            Herbs = new List<FormulaHerbItemDto>
            {
                new() { Id = herbItemId, IsValidated = true, HerbName = "太子参", Dosage = 5, Unit = "g" }
            }
        };
        _formulaService.GetByIdAsync(formulaId, Arg.Any<CancellationToken>())
            .Returns(
                new LYBT.Desktop.Contracts.Results.CommandResult<FormulaDetailDto>(true, draft, null),
                new LYBT.Desktop.Contracts.Results.CommandResult<FormulaDetailDto>(true, validated, null));
        _formulaService.ValidateHerbAsync(formulaId, herbItemId, systemHerbId, Arg.Any<CancellationToken>())
            .Returns(new LYBT.Desktop.Contracts.Results.CommandResult<bool>(true, true, null));

        RaiseSelection(_masterDetailServices, item);
        await PumpUntilAsync(() => sut.ValidationRows.Count == 1, "校验详情面板未加载");

        sut.ValidationRows.Single().SelectedHerbId = systemHerbId;
        await sut.ValidateHerbCommand.ExecuteAsync(sut.ValidationRows.Single());

        await _formulaService.Received(1).ValidateHerbAsync(formulaId, herbItemId, systemHerbId, Arg.Any<CancellationToken>());
        _cacheManager.Received(1).InvalidateFormulaCaches();
        await PumpUntilAsync(() => sut.ValidationPendingCount == 0, "绑定后未重新加载校验详情");
        sut.ValidationRows.Single().IsValidated.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateHerb_WithoutSelectedHerb_ShowsErrorAndSkipsCall()
    {
        var sut = CreateSut();
        await sut.ToggleValidationModeCommand.ExecuteAsync(null);

        var formulaId = Guid.NewGuid();
        var item = new FormulaListDto { Id = formulaId, Name = "导入验方丁" };
        _selection.SelectedItem.Returns(item);
        _selection.HasSelection.Returns(true);

        var draft = new FormulaDetailDto
        {
            Id = formulaId,
            ValidationStatus = FormulaValidationStatus.Draft,
            Herbs = new List<FormulaHerbItemDto>
            {
                new() { Id = Guid.NewGuid(), IsValidated = false, OriginalHerbName = "参须", Dosage = 5, Unit = "g" }
            }
        };
        _formulaService.GetByIdAsync(formulaId, Arg.Any<CancellationToken>())
            .Returns(new LYBT.Desktop.Contracts.Results.CommandResult<FormulaDetailDto>(true, draft, null));

        RaiseSelection(_masterDetailServices, item);
        await PumpUntilAsync(() => sut.ValidationRows.Count == 1, "校验详情面板未加载");

        await sut.ValidateHerbCommand.ExecuteAsync(sut.ValidationRows.Single());

        await _dialogManager.Received(1).ShowErrorAsync(
            Arg.Is<string>(m => m.Contains("请先从列表选择要绑定的系统药材")), Arg.Any<string>());
        await _formulaService.DidNotReceive().ValidateHerbAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateHerb_OnApiFailure_ShowsServerErrorMessage()
    {
        var sut = CreateSut();
        await sut.ToggleValidationModeCommand.ExecuteAsync(null);

        var formulaId = Guid.NewGuid();
        var herbItemId = Guid.NewGuid();
        var systemHerbId = Guid.NewGuid();
        var item = new FormulaListDto { Id = formulaId, Name = "导入验方戊" };
        _selection.SelectedItem.Returns(item);
        _selection.HasSelection.Returns(true);

        var draft = new FormulaDetailDto
        {
            Id = formulaId,
            ValidationStatus = FormulaValidationStatus.Draft,
            Herbs = new List<FormulaHerbItemDto>
            {
                new() { Id = herbItemId, IsValidated = false, OriginalHerbName = "参须", Dosage = 5, Unit = "g" }
            }
        };
        _formulaService.GetByIdAsync(formulaId, Arg.Any<CancellationToken>())
            .Returns(new LYBT.Desktop.Contracts.Results.CommandResult<FormulaDetailDto>(true, draft, null));
        _formulaService.ValidateHerbAsync(formulaId, herbItemId, systemHerbId, Arg.Any<CancellationToken>())
            .Returns(new LYBT.Desktop.Contracts.Results.CommandResult<bool>(false, false, "系统药材不存在或已停用"));

        RaiseSelection(_masterDetailServices, item);
        await PumpUntilAsync(() => sut.ValidationRows.Count == 1, "校验详情面板未加载");
        sut.ValidationRows.Single().SelectedHerbId = systemHerbId;

        await sut.ValidateHerbCommand.ExecuteAsync(sut.ValidationRows.Single());

        await _dialogManager.Received(1).ShowErrorAsync(
            Arg.Is<string>(m => m.Contains("系统药材不存在或已停用")), Arg.Any<string>());
    }
}
