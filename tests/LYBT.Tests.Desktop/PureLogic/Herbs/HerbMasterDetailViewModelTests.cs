using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Desktop.Contracts.CommandHandlers;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Herbs.Models;
using LYBT.Desktop.Herbs.ViewModels;
using LYBT.Desktop.Herbs.ViewModels.Handlers;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using LYBT.Tests.Desktop.Infrastructure;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.Herbs;

[Collection("UserJourney")]
public class HerbMasterDetailViewModelTests : UserJourneyTestBase
{
    private readonly IViewModelServices _viewModelServices;
    private readonly IMasterDetailServices<HerbListDto, HerbDetailModel> _masterDetailServices;
    private readonly IHerbService _herbService;
    private readonly IHerbStatusHandler _statusHandler;
    private readonly IHerbImportExportHandler _importExportHandler;
    private readonly IDesktopCacheManager _cacheManager;
    private readonly HerbEditorViewModel _herbEditor;

    private sealed class TestableHerbMasterDetailViewModel : HerbMasterDetailViewModel
    {
        public TestableHerbMasterDetailViewModel(
            IViewModelServices viewModelServices,
            IMasterDetailServices<HerbListDto, HerbDetailModel> masterDetailServices,
            IHerbService herbService,
            IHerbStatusHandler statusHandler,
            IHerbImportExportHandler importExportHandler,
            IDesktopCacheManager cacheManager,
            HerbEditorViewModel herbEditor)
            : base(viewModelServices, masterDetailServices, herbService, statusHandler, importExportHandler, cacheManager, herbEditor)
        {
        }

        public Task<bool> SaveDetailPublicAsync(HerbDetailModel detail) => base.SaveDetailAsync(detail);

        public Task<bool> DeleteItemPublicAsync(HerbListDto item) => base.DeleteItemAsync(item);
    }

    public HerbMasterDetailViewModelTests(UserJourneyFixture fixture) : base(fixture)
    {
        _viewModelServices = CreateViewModelServicesMock();
        _masterDetailServices = CreateMasterDetailServicesMock<HerbListDto, HerbDetailModel>();
        _herbService = Substitute.For<IHerbService>();
        _statusHandler = Substitute.For<IHerbStatusHandler>();
        _importExportHandler = Substitute.For<IHerbImportExportHandler>();
        _cacheManager = Substitute.For<IDesktopCacheManager>();
        _herbEditor = new HerbEditorViewModel();
    }

    private TestableHerbMasterDetailViewModel CreateSut()
    {
        return new TestableHerbMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            _herbService,
            _statusHandler,
            _importExportHandler,
            _cacheManager,
            _herbEditor);
    }

    private static HerbListDto CreateHerbListDto(Guid? id = null, string name = "黄芪")
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            PinYinCode = "HQ",
            Unit = "g",
            Price = 10m,
            Status = CommonStatus.Enabled
        };

    private static HerbDetailDto CreateHerbDetailDto(Guid? id = null, string name = "黄芪")
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            PinYinCode = "HQ",
            Origin = "山西",
            Spec = "统货",
            Unit = "g",
            Price = 10m,
            CostPrice = 6m,
            Effect = "补气升阳",
            Usage = "煎服",
            Remark = "测试药材",
            Status = CommonStatus.Enabled
        };

    private static HerbDetailModel CreateDetailModel(string name = "黄芪", Guid? id = null)
    {
        var detail = HerbDetailModel.CreateNew();
        detail.Id = id ?? Guid.Empty;
        detail.Name = name;
        detail.PinYinCode = "HQ";
        detail.Origin = "山西";
        detail.Spec = "统货";
        detail.Unit = "g";
        detail.Price = 10m;
        detail.CostPrice = 6m;
        detail.Effect = "补气升阳";
        detail.Usage = "煎服";
        detail.Remark = "测试药材";
        detail.Status = CommonStatus.Enabled;
        return detail;
    }

    [Fact]
    public void Constructor_InitializesExpectedState()
    {
        var sut = CreateSut();

        sut.PageTitle.Should().Be("药材管理");
        sut.StatusOptions.Should().Contain(CommonStatus.Enabled);
        sut.StatusOptions.Should().Contain(CommonStatus.Disabled);
    }

    [Fact]
    public async Task LoadListAsync_LoadsHerbListAndPopulatesItems()
    {
        var sut = CreateSut();
        var pagedResult = new PagedResult<HerbListDto>
        {
            Items = new List<HerbListDto>
            {
                CreateHerbListDto(name: "黄芪"),
                CreateHerbListDto(name: "党参")
            },
            TotalCount = 2
        };

        _herbService.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(Task.FromResult(new CommandResult<PagedResult<HerbListDto>>(true, pagedResult, null)));

        await sut.InitializeAsync();

        await _herbService.Received(1).GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<System.Threading.CancellationToken>());
        sut.Items.Should().HaveCount(2);
        sut.Items.Select(x => x.Name).Should().Contain(new[] { "黄芪", "党参" });
    }

    [Fact]
    public async Task SaveDetailAsync_CreatesHerbAndInvalidatesCache()
    {
        var sut = CreateSut();
        var detail = CreateDetailModel();
        var created = CreateHerbDetailDto(detail.Id == Guid.Empty ? Guid.NewGuid() : detail.Id, detail.Name);

        _herbEditor.InitializeForNewCase();
        _herbEditor.Herb.Name = "黄芪";
        _herbEditor.Herb.Unit = "g";
        _herbEditor.Herb.Price = 10m;

        _herbService.CreateAsync(Arg.Any<HerbInputDto>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(Task.FromResult(new CommandResult<HerbDetailDto>(true, created, null)));
        _masterDetailServices.DetailEditor.IsNew.Returns(true);

        var result = await sut.SaveDetailPublicAsync(detail);

        result.Should().BeTrue();
        await _herbService.Received(1).CreateAsync(
            Arg.Is<HerbInputDto>(dto => dto.Name == "黄芪" && dto.Unit == "g" && dto.Price == 10m),
            Arg.Any<System.Threading.CancellationToken>());
        _cacheManager.Received(1).InvalidateHerbCaches();
    }

    [Fact]
    public async Task SaveDetailAsync_UpdatesExistingHerbAndUsesUpdate()
    {
        var sut = CreateSut();
        var herbId = Guid.NewGuid();
        var detail = CreateDetailModel(id: herbId);

        _herbEditor.InitializeForNewCase();
        _herbEditor.Herb.Id = herbId;
        _herbEditor.Herb.Name = "黄芪（修订）";
        _herbEditor.Herb.Unit = "g";
        _herbEditor.Herb.Price = 10m;

        var updated = CreateHerbDetailDto(herbId, "黄芪（修订）");

        _herbService.UpdateAsync(Arg.Any<HerbInputDto>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(Task.FromResult(new CommandResult<HerbDetailDto>(true, updated, null)));

        var result = await sut.SaveDetailPublicAsync(detail);

        result.Should().BeTrue();
        await _herbService.Received(1).UpdateAsync(
            Arg.Is<HerbInputDto>(dto => dto.Id == herbId && dto.Name == "黄芪（修订）"),
            Arg.Any<System.Threading.CancellationToken>());
        await _herbService.DidNotReceive().CreateAsync(Arg.Any<HerbInputDto>(), Arg.Any<System.Threading.CancellationToken>());
        _cacheManager.Received(1).InvalidateHerbCaches();
    }

    [Fact]
    public async Task DeleteItemAsync_DeletesHerbAndInvalidatesCache()
    {
        var sut = CreateSut();
        var herb = CreateHerbListDto();

        _herbService.DeleteAsync(herb.Id, Arg.Any<System.Threading.CancellationToken>())
            .Returns(Task.FromResult(new CommandResult<bool>(true, true, null)));

        var result = await sut.DeleteItemPublicAsync(herb);

        result.Should().BeTrue();
        await _herbService.Received(1).DeleteAsync(herb.Id, Arg.Any<System.Threading.CancellationToken>());
        _cacheManager.Received(1).InvalidateHerbCaches();
    }

    [Fact]
    public async Task SearchByCategoryCommand_UpdatesSearchTextAndRefreshes()
    {
        var sut = CreateSut();
        var pagedResult = new PagedResult<HerbListDto> { Items = new List<HerbListDto>(), TotalCount = 0 };

        _herbService.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(Task.FromResult(new CommandResult<PagedResult<HerbListDto>>(true, pagedResult, null)));

        await sut.SearchByCategoryCommand.ExecuteAsync("补气药");

        sut.SearchText.Should().Be("分类:补气药");
        await _herbService.Received(1).GetPagedAsync(
            Arg.Any<int>(),
            Arg.Any<int>(),
            "分类:补气药",
            Arg.Any<string?>(),
            Arg.Any<System.Threading.CancellationToken>());
    }

    [Fact]
    public async Task ImportHerbsCommand_CallsImportHandlerAndRefreshes()
    {
        var sut = CreateSut();
        var pagedResult = new PagedResult<HerbListDto> { Items = new List<HerbListDto>(), TotalCount = 0 };

        _importExportHandler.ImportAsync().Returns(Task.FromResult(true));
        _herbService.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(Task.FromResult(new CommandResult<PagedResult<HerbListDto>>(true, pagedResult, null)));

        await sut.ImportHerbsCommand.ExecuteAsync(null);

        await _importExportHandler.Received(1).ImportAsync();
        await _herbService.Received(1).GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<System.Threading.CancellationToken>());
        _cacheManager.Received(1).InvalidateHerbCaches();
    }

    [Fact]
    public async Task ExportHerbsCommand_PassesSearchTextToHandler()
    {
        var sut = CreateSut();
        sut.SearchText = "黄芪";

        await sut.ExportHerbsCommand.ExecuteAsync(null);

        await _importExportHandler.Received(1).ExportAsync("黄芪");
    }
}
